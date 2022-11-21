namespace Vtb.PosKeep.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;
    using Vtb.PosKeep.Entity.Storage;

    public class InfoController : Controller
    {
        private IServiceProvider services;
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan cacheExpiration;
        private readonly int instrumentsPageSize;
        private readonly int accountsPageSize;

        public InfoController(IMemoryCache cache, IServiceProvider services)
        {
            this.services = services;
            memoryCache = cache;

            var configOptions = services.GetService<IOptions<ConfigOptions>>().Value;
            cacheExpiration = new TimeSpan(0, 0, configOptions.CacheExpiration);
            instrumentsPageSize = configOptions.DefaultInstrumentsPageSize;
            accountsPageSize = configOptions.DefaultAccountsPageSize;
        }

        /// <summary>
        /// Список счетов
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Accounts(int id)
        {
            var cacheKey = new CacheKey(id, 0);
            if (!memoryCache.TryGetValue(cacheKey, out var cachedAccountPage))
            {
                var count = Entity.Data.Account.Count();
                var pageCount = count / accountsPageSize;

                id = Math.Max(0, Math.Min(id -1, pageCount));

                cachedAccountPage = Pair.Create(
                        pageCount, 
                        Entity.Data.Account.Accounts()
                            .Skip(id * accountsPageSize)
                            .Take(accountsPageSize));

                var cacheOption = new MemoryCacheEntryOptions { SlidingExpiration = cacheExpiration };
                memoryCache.Set(cacheKey, cachedAccountPage, cacheOption);
            }

            var result = (Pair<int, IEnumerable<Account>>)cachedAccountPage;
            return new JsonResult(new 
                {
                    pages = result.Item1,
                    accounts = result.Item2.Select(a => new { id = (int)a.ID, name = a.Name })
                });            
        }

        /// <summary>
        /// Поиск счета по коду
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> ACode(string id)
        {
            var accountId = Entity.Data.Account.AccountIdByCode((id));
            if (accountId > 0)
            {
                var account = (Account)accountId;
                return new JsonResult(
                    new
                    {
                        id = accountId,
                        name = account.Name,
                    });
            }

            return new JsonResult(new 
            {
                id = 0
            });            
        }
        
        /// <summary>
        /// Поиск инструмента по коду
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> ICode(string id)
        {
            var quotes = services.GetService<QuoteStorage>();
            foreach (var instrument in Entity.Data.Instrument.Instruments().Where(i => i.Code == id))
            {
                if (quotes.Items(new QuoteKey("810".ToCurrencyKey(), instrument.ID)).Any())
                {
                    return new JsonResult(
                        new
                        {
                            id = (int)instrument.IdCode,
                            name = instrument.Name,
                            year = quotes.Items(new QuoteKey("810".ToCurrencyKey(), instrument.ID)).First().Timestamp.Date.Year,
                            currency = 810,
                        });
                }
            }

            return new JsonResult(new 
            {
                id = 0
            });            
        }
        public static void LoadClients(ConfigOptions configOptions)
        {
            foreach (var client in configOptions.DemoClients().Nodes()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .Select(c =>
                    {
                        var client = (XElement)c;
                        return new Account(
                            client.Attribute("Id").Value,
                             client.Attribute("Name")?.Value);
                    })) ;
        }
        
        /// <summary>
        /// Информация по счету
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Account(int id)
        {
            var account = (Account)id;
            var positions = services.GetService<PositionStorage>();
            var last = (Timestamp)Math.Min(positions.ClientLastTime(account.ID), (Timestamp)DateTime.Now.Date);
            return new JsonResult(new { id, code = account.Code, name = account.Name, last = last.AsUtc() });
        }

        /// <summary>
        /// Список инструментов
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Instruments(int id)
        {
            var cacheKey = new CacheKey(0, id);
            if (!memoryCache.TryGetValue(cacheKey, out var cachedInstrumentPage))
            {
                var count = Entity.Data.Instrument.LastNumber;
                var pageCount = count / instrumentsPageSize;

                id = Math.Max(0, Math.Min(id -1, pageCount));

                cachedInstrumentPage = Pair.Create(
                        pageCount, 
                        Entity.Data.Instrument.Instruments()
                            .Skip(id * instrumentsPageSize)
                            .Take(instrumentsPageSize));

                var cacheOption = new MemoryCacheEntryOptions { SlidingExpiration = cacheExpiration };
                memoryCache.Set(cacheKey, cachedInstrumentPage, cacheOption);
            }

            var result = (Pair<int, IEnumerable<Instrument>>)cachedInstrumentPage;
            return new JsonResult(new 
                {
                    pages = result.Item1,
                    instruments = result.Item2.Select(i => new { id = (int)i.ID, name = i.Name, code = i.Code })
                });
        }

        /// <summary>
        /// Информация по инструменту
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Instrument(int id)
        {
            var instrument = (Instrument)id;

            return new JsonResult(new { id, icode = (int)instrument.IdCode, code = instrument.Code, name = instrument.Name });
        }

        public static void LoadInstruments(ConfigOptions configOptions)
        {
            foreach (var c in configOptions.DemoCurrencies().Nodes()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(cc =>
                {
                    var currency = (XElement)cc;
                    return new Currency(
                        currency.Attribute("Id").Value,
                        currency.Attribute("ShortName")?.Value,
                        currency.Attribute("Name")?.Value);
                })) ;

            foreach (var i in configOptions.DemoInstruments().Nodes()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(ii =>
                    {
                        var instrument = (XElement)ii;
                        return new Instrument(
                            instrument.Attribute("Id").Value.ToInt(0),
                            instrument.Attribute("ShortName")?.Value,
                            instrument.Attribute("Name")?.Value);
                    })) ;            
        }

        public volatile static int[] quote_years = new int [0];

        /// <summary>
        /// Котировки
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Quotes(int id, int currency = 840, int year = 2000)
        {
            if (quote_years.Length > 0)
                year = Math.Max(year, quote_years[0]);

            var quotes = services.GetService<QuoteStorage>();

            var currencyKey = currency.ToString().ToCurrencyKey();
            if (!quotes.Currencies(id).Contains(currencyKey))
                currencyKey = quotes.Currencies(id).FirstOrDefault();
            
            var result = quotes.Items(new QuoteKey(currencyKey, id), new DateTime(year, 1, 1), new DateTime(year, 12, 31));

            return new JsonResult(
                new
                {
                    years = quotes.Years(new QuoteKey(currencyKey, id)),
                    quotes = result.Select(q =>
                    new
                    {
                        t = q.Timestamp.AsUtc(),
                        v = q.Data.Value,
                    })
                });
        }

        public static void LoadQuotes(ConfigOptions configOptions, QuoteStorage quotes)
        {
            int i = 0;
            LoadQuotes(configOptions, quotes, ref quote_years, ref i);
        }

        private static void LoadQuotes(ConfigOptions configOptions, QuoteStorage quotes, ref int[] years, ref int year)
        {
            var yearsHash = new SortedSet<int>();
            Func<string, decimal> convert = c => decimal.Parse(c);
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
                convert = c => decimal.Parse(c.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));

            foreach (var instrumentQuotes in configOptions.DemoQuotes().Nodes()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .OfType<XElement>())
            {
                var id = instrumentQuotes.Attribute("ID").Value.ToInt(-1);
                var quoteValues = instrumentQuotes.Nodes()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .OfType<XElement>()
                    .Select(q => {
                        var dt = DateTime.Parse(q.Attribute("Date").Value, CultureInfo.InvariantCulture.DateTimeFormat);
                        var close = convert(q.Attribute("Close").Value);
                        yearsHash.Add(dt.Year);

                        return new HD<Quote, QR>(dt, close);
                    });

                quotes.AddRange(new QuoteKey(840.ToString(), id), quoteValues);
            }

            years = yearsHash.ToArray();
            year = yearsHash.Min;
        }

        public volatile static int[] rate_years = new int[0];

        /// <summary>
        /// Котировки
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> Rates(int currency, int year = 2000)
        {
            if (rate_years.Length > 0)
                year = Math.Max(year, rate_years[0]);

            var rates = services.GetService<RateStorage>();

            var currencyKey = currency.ToString().ToCurrencyKey();
            var result = rates.Items(currencyKey, new DateTime(year, 1, 1), new DateTime(year, 12, 31));

            return new JsonResult(
                new
                {
                    years = rates.Years(currencyKey),
                    rates = result.Select(q =>
                    new
                    {
                        t = q.Timestamp.AsUtc(),
                        v = q.Data.Value,
                    })
                });
        }

        public static void LoadRates(ConfigOptions configOptions, RateStorage rates)
        {
            int i = 0;
            LoadRates(configOptions, rates, ref rate_years, ref i);
        }

        private static void LoadRates(ConfigOptions configOptions, RateStorage rates, ref int[] years, ref int year)
        {
            var yearsHash = new SortedSet<int>();
            Func<string, decimal> convert = c => decimal.Parse(c);
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
                convert = c => decimal.Parse(c.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));

            foreach (var currencyRates in configOptions.DemoRates().Nodes()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .OfType<XElement>())
            {
                var id = currencyRates.Attribute("ID").Value.ToCurrencyKey();
                var rateValues = currencyRates.Nodes()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .OfType<XElement>()
                    .Select(q => {
                        var dt = DateTime.Parse(q.Attribute("Date").Value, CultureInfo.InvariantCulture.DateTimeFormat);
                        var rate = convert(q.Attribute("Rate").Value);
                        yearsHash.Add(dt.Year);

                        return new HD<Rate, RR>(dt, rate);
                    });

                rates.AddRange(id, rateValues);
            }

            years = yearsHash.ToArray();
            year = yearsHash.Min;
        }

        private struct CacheKey : IEquatable<CacheKey>
        {
            public readonly int account_page;
            public readonly int instrument_page;
            public CacheKey(int account_page, int instrument_page)
            {
                this.account_page = account_page;
                this.instrument_page = instrument_page;
            }

            public override bool Equals(object obj)
            {
                return Equals((CacheKey)obj);
            }

            public bool Equals(CacheKey other)
            {
                return account_page == other.account_page && instrument_page == other.instrument_page;
            }

            public override int GetHashCode()
            {
                return account_page | (instrument_page<<16);
            }
        }
    }
}

