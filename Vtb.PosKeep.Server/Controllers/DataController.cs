namespace Vtb.PosKeep.Server.Controllers
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Business.Model;
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;
    using Vtb.PosKeep.Entity.Storage;

    public class DataController : Controller
    {
        private IServiceProvider services;
        public DataController(IServiceProvider services)
        {
            this.services = services;
        }

        /// <summary>
        /// Информация по клиенту
        /// </summary>
        [HttpGet]
        public ActionResult NewAccount(string code, string name)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                return BadRequest();

            var a = new Account(code, name);

            return Ok(new { id = (int)a.ID });
        }

        /// <summary>
        /// Информация по счету
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> NewAccounts([FromBody] AccountValue[] clients)
        {
            if (clients is AccountValue[] && clients.Length > 0)
            {
                await Task.Run(() =>
                {
                    foreach (var client in clients
                        .Where(c => !string.IsNullOrEmpty(c.code) && !string.IsNullOrEmpty(c.name))
                        .Select(c => new Account(c.code, c.name))) ;
                });
                return Ok();
            }

            return BadRequest();
        }

        public struct AccountValue
        {
            public string code { get; set; }
            public string name { get; set; }
        }

        /// <summary>
        /// Информация по инструменту
        /// </summary>
        [HttpGet]
        public ActionResult NewInstrument(int instrument_id, string short_name, string name)
        {
            if (instrument_id <= 0 || string.IsNullOrEmpty(short_name) || string.IsNullOrEmpty(name))
                return BadRequest();

            new Instrument(instrument_id, short_name, name);

            return Ok();
        }

        /// <summary>
        /// Информация по инструменту
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> NewInstruments([FromBody] InstrumentValue[] instruments)
        {
            if (instruments is InstrumentValue[] && instruments.Length > 0)
            {
                await Task.Run(() =>
                {
                    foreach (var instrument in instruments
                        .Where(i => i.ID > 0 && !string.IsNullOrEmpty(i.short_name) && !string.IsNullOrEmpty(i.name))
                        .Select(i => new Instrument(i.ID, i.short_name, i.name))) ;
                });

                return Ok();
            }

            return BadRequest();
        }

        public struct InstrumentValue
        {
            public int ID { get; set; }
            public string short_name { get; set; }
            public string name { get; set; }
        }

        /// <summary>
        /// Информация по валюте
        /// </summary>
        [HttpGet]
        public ActionResult NewCurrency(string dcode, string code, string name)
        {
            if (string.IsNullOrEmpty(dcode) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                return BadRequest();

            var currency = new Currency(dcode, code, name);

            return Ok();
        }

        /// <summary>
        /// Информация по инструменту
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> NewCurrencies([FromBody] CurrencyValue[] currencies)
        {
            if (currencies is CurrencyValue[] && currencies.Length > 0)
            {
                await Task.Run(() =>
                {
                    foreach (var currency in currencies
                        .Where(i => !string.IsNullOrEmpty(i.dcode) && !string.IsNullOrEmpty(i.code) && !string.IsNullOrEmpty(i.name))
                        .Select(c => new Currency(c.dcode, c.code, c.name))) ;
                });

                return Ok();
            }

            return BadRequest();
        }

        public struct CurrencyValue
        {
            public string dcode { get; set; }
            public string code { get; set; }
            public string name { get; set; }
        }

        /// <summary>
        /// Информация по котировкам
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> NewQuotes(int instrument_id, int currency_id, [FromBody] QuoteValue[] quotes)
        {
            var qstorage = services.GetService<QuoteStorage>();
            if (instrument_id > 0 && currency_id > 0 && quotes is QuoteValue[] && quotes.Length > 0)
            {
                await Task.Run(() =>
                {
                    foreach (var quote in quotes)
                    {
                        var qkey = new QuoteKey(currency_id.ToString(), instrument_id);
                        var qvalue = new HD<Quote, QR>(quote.dateTime, quote.value);

                        qstorage.Add(qkey, qvalue);
                    }
                });

                return Ok();
            }

            return BadRequest();
        }

        /// <summary>
        /// Информация по сделкам
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> NewDeals(string client_id, int instrument_id, int place, [FromBody] DealValue[] deals, [FromQuery] bool recalc = false)
        {
            var dstorage = services.GetService<DealStorage>();
            var pstorage = services.GetService<PositionStorage>();
            var config = services.GetService<IOptions<ConfigOptions>>().Value;

            if (!string.IsNullOrEmpty(client_id) && instrument_id > 0 && deals is DealValue[] && deals.Length > 0)
            {
                new Account(client_id); // регистрируем фейковый счет если надо

                foreach (var currencyDeals in deals.GroupBy(d => d.cry))
                {
                    var dealKey = new DealKey(new TradeAccountKey((AccountKey)client_id, place),
                        new TradeInstrumentKey(currencyDeals.Key, (InstrumentCode)instrument_id));

                    await Task.Run(() =>
                    {
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        dstorage.AddRange(dealKey,
                            deals.Where(d => !string.IsNullOrEmpty(d.tp))
                            .Select(deal =>
                            {
                                return new HD<Deal, DR>(DateTime.ParseExact(deal.dt, "yyyy-MM-dd HH:mm:ss.fff", provider),
                                    Deal.Create(deal.cd, Server.DataLoader.DealLoader.AsDealType(deal.tp),
                                                decimal.Parse(deal.vol), decimal.Parse(deal.qty)));
                            }));
                    });
                }
                /*
                if (recalc)
                {
                    await Task.Run(() =>
                        ((PositionRecalcStorage)pstorage).Recalc(client_id, dstorage, true, positions => positions.Aggregate(config.RecalcPositionPeriod)));
                }
                */
                return Ok();
            }

            return BadRequest();
        }


        /// <summary>
        /// Пересчет сделок по клиенту
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Recalc(int client_id)
        {
            var config = services.GetService<IOptions<ConfigOptions>>().Value;
            var dstorage = services.GetService<DealStorage>();
            var pstorage = services.GetService<PositionStorage>();
            
            await Task.Run(() =>
                ((PositionRecalcStorage)pstorage).Recalc(client_id, dstorage, true, positions => positions.Aggregate(config.RecalcPositionPeriod)));
                
            return Ok();
        }

        /// <summary>
        /// Пересчет сделок по всем клиентам
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> RecalcAll()
        {
            var config = services.GetService<IOptions<ConfigOptions>>().Value;
            var dstorage = services.GetService<DealStorage>();
            var pstorage = services.GetService<PositionStorage>();
            
            await Task.WhenAll(Account.Accounts().Select(a =>
                Task.Run(()=> ((PositionRecalcStorage)pstorage).Recalc(a.ID, dstorage, true, positions => positions.Aggregate(config.RecalcPositionPeriod)))));
                
            return Ok();
        }

        public struct QuoteValue
        {
            public DateTime dateTime;
            public decimal value;
        }

        private static volatile bool DemoClientAccountLoaded = false;
        private static volatile bool DemoInstrumentsLoaded = false;
        private static volatile bool DemoQuotesLoaded = false;
        private static volatile bool DemoRatesLoaded = false;
        private static volatile bool DemoDealsLoaded = false;
        
        /// <summary>
        /// Загрузка демо-данных
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> DemoLoader([FromQuery] bool load = false)
        {
            var configOptions = services.GetService<IOptions<ConfigOptions>>().Value;
            configOptions.DefaultCurrencyDCode = 840;// баксы
            
            if (load && !DemoClientAccountLoaded)
            {
                DemoClientAccountLoaded = true;
                Account.Init(configOptions.AccountCount);
                InfoController.LoadClients(configOptions);
            }

            if (load && !DemoInstrumentsLoaded)
            {
                DemoInstrumentsLoaded = true;
                Currency.Init(configOptions.CurrencyCount);
                Instrument.Init(configOptions.InstrumentCount);
                InfoController.LoadInstruments(configOptions);
            }

            if (load && !DemoQuotesLoaded)
            {
                DemoQuotesLoaded = true;
                InfoController.LoadQuotes(configOptions, services.GetService<QuoteStorage>());
            }

            if (load && !DemoRatesLoaded)
            {
                DemoRatesLoaded = true;
                var rates = services.GetService<RateStorage>();
                rates.BaseCurrency = "840";
            }

            if (load && !DemoDealsLoaded)
            {
                DemoDealsLoaded = true;
                var positions = services.GetService<PositionStorage>();
                var deals = services.GetService<DealStorage>();
                var config = services.GetService<IOptions<ConfigOptions>>().Value;
                PortfolioController.LoadDeals(deals, positions, config);
            }
            
            return new JsonResult(
                new
                {
                    state = new []
                    {
                        new {name = "Справочник клиентов", loaded = DemoClientAccountLoaded? "загружен" : "не загружен"},
                        new {name = "Справочник инструментов", loaded = DemoInstrumentsLoaded? "загружен" : "не загружен"},
                        new {name = "Справочник котировок", loaded = DemoQuotesLoaded? "загружен" : "не загружен"},
                        new {name = "Справочник курсов валют", loaded = DemoRatesLoaded? "загружен" : "не загружен"},
                        new {name = "Реестр сделок", loaded = DemoDealsLoaded? "загружен" : "не загружен"},
                    },
                    
                    statistic = new []
                    {
                        new {name = "Справочник валют", value = Currency.LastNumber },
                        new {name = "Справочник клиентов", value = Account.LastNumber },
                        new {name = "Справочник инструментов", value = Instrument.LastNumber },
                        new {name = "Справочник котировок", value = Quote.LastNumber },
                        new {name = "Справочник курсов валют", value = Rate.LastNumber },
                        new {name = "Реестр сделок", value = Deal.LastNumber },
                        new {name = "Реестр позиций", value = Position.LastNumber },
                    }
                }); 
        }
        
        private static volatile bool CurrencyLoaded = false;
        private static volatile bool InstrumentsLoaded = false;
        private static volatile bool QuotesLoaded = false;
        private static volatile bool RatesLoaded = false;
        private static volatile bool DealsLoaded = false;
        
        /// <summary>
        /// Загрузка данных из файлов
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> DataLoader([FromQuery] bool load = false)
        {
            var configOptions = services.GetService<IOptions<ConfigOptions>>().Value;

            configOptions.DefaultCurrencyDCode = 810;// рубли
            
            if (load && !CurrencyLoaded)
            {
                CurrencyLoaded = true;
                Currency.Init(configOptions.CurrencyCount);
                Server.DataLoader.CurrencyLoader.Load(configOptions);
            }

            if (load && !InstrumentsLoaded)
            {
                InstrumentsLoaded = true;
                Instrument.Init(configOptions.InstrumentCount);
                TradeInstrument.Init(configOptions.TradeInsCount);
                Server.DataLoader.InstrumentLoader.Load(configOptions);
            }

            if (load && !QuotesLoaded)
            {
                QuotesLoaded = true;
                Quote.Init(configOptions.QuoteCount);

                var quotes = services.GetService<QuoteStorage>();
                var config = services.GetService<IOptions<ConfigOptions>>().Value;
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Server.DataLoader.QuoteLoader.Load(config, quotes, ref InfoController.quote_years);
                    }
                    catch (Exception e)
                    {
                        Server.DataLoader.QuoteLoader.WriteDebug(DateTime.Now.ToShortTimeString() + "|" + e.Message + Environment.NewLine + e.StackTrace);
                    }
                });
            }

            if (load && !RatesLoaded)
            {
                RatesLoaded = true;
                Rate.Init(configOptions.RateCount);

                var rates = services.GetService<RateStorage>();
                var config = services.GetService<IOptions<ConfigOptions>>().Value;
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Server.DataLoader.RateLoader.Load(config, rates, ref InfoController.rate_years);
                    }
                    catch (Exception e)
                    {
                        Server.DataLoader.RateLoader.WriteDebug(DateTime.Now.ToShortTimeString() + "|" + e.Message + Environment.NewLine + e.StackTrace);
                    }
                });
            }

            if (load && !DealsLoaded)
            {
                DealsLoaded = true;
                Account.Init(configOptions.AccountCount);
                TradeAccount.Init(configOptions.TradeAccCount);
                var positions = services.GetService<PositionStorage>();
                var deals = services.GetService<DealStorage>();
                var config = services.GetService<IOptions<ConfigOptions>>().Value;
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Server.DataLoader.DealLoader.Load(config, deals, positions);
                    }
                    catch (Exception e)
                    {
                        Server.DataLoader.DealLoader.WriteDebug(DateTime.Now.ToShortTimeString() + "|" + e.Message + Environment.NewLine + e.StackTrace);
                    }
                });
            }
            
            return new JsonResult(
                new
                {
                    state = new []
                    {
                        new {name = "Справочник валют", loaded = CurrencyLoaded? "загружен" : "не загружен"},
                        new {name = "Справочник инструментов", loaded = InstrumentsLoaded? "загружен" : "не загружен"},
                        new {name = "Справочник котировок", loaded = QuotesLoaded? Server.DataLoader.QuoteLoader.Loading? "загрузка" : "загружен" : "не загружен"},
                        new {name = "Справочник курсов валют", loaded = RatesLoaded? Server.DataLoader.RateLoader.Loading? "загрузка" : "загружен" : "не загружен"},
                        new {name = "Реестр сделок", loaded = DealsLoaded? Server.DataLoader.DealLoader.Loading? "загрузка" : "загружен" : "не загружен"},
                    },
                    
                    statistic = new []
                    {
                        new {name = "Справочник валют", value = Currency.LastNumber },
                        new {name = "Справочник клиентов", value = Account.LastNumber },
                        new {name = "Справочник инструментов", value = Instrument.LastNumber },
                        new {name = "Справочник котировок", value = Quote.LastNumber },
                        new {name = "Справочник курсов валют", value = Rate.LastNumber },
                        new {name = "Строк котировок импортировано", value = Server.DataLoader.QuoteLoader.LinesNumber },
                        new {name = "Реестр сделок", value = Deal.LastNumber },
                        new {name = "Реестр позиций", value = Position.LastNumber },
                        new {name = "Строк сделок импортировано", value = Server.DataLoader.DealLoader.LinesNumber },
                        //new {name = "Строк пересчитано", value = Server.DataLoader.Deal.DealsCount },
                    }
                }); 
        }
        
        public struct DealValue
        {
            public string cd { get; set; }
            public string dt { get; set; }
            public string tp { get; set; }
            public string qty { get; set; }
            public string vol { get; set; }
            public string cry { get; set; }
        }
    }
}
