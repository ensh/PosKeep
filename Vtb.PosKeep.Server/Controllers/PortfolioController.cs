using System;
using System.Collections.Generic;
using System.IO;

using Vtb.PosKeep.Entity;
using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;
using Vtb.PosKeep.Entity.Business.Model;
using Vtb.PosKeep.Entity.Storage;

namespace Vtb.PosKeep.Server.Controllers
{
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using ValueTokensEntry = KeyValuePair<PositionKey, IEnumerable<HD<ValueToken, HR>>>;
    using ValueEntry = KeyValuePair<PositionKey, IEnumerable<HD<decimal, HR>>>;
    using QuotesEntry = KeyValuePair<PositionKey, IEnumerable<HD<ConvertPosition, CPR>>>;

    public class PortfolioController : Controller
    {
        private readonly IServiceProvider services;
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan cacheExpiration;

        public PortfolioController(IMemoryCache cache, IServiceProvider aservices)
        {
            memoryCache = cache;
            services = aservices;

            cacheExpiration = new TimeSpan(0, 0, services.GetService<IOptions<ConfigOptions>>().Value.CacheExpiration);
        }

        [HttpGet]
        public async Task<JsonResult> CurrentPosition(int client_id, int place_id, long moment, int currency)
        {
            if (moment == 0)
                moment = ((Timestamp)DateTime.Now.Date).AsUtc();

            TryParseTimestamp(moment, moment, out var t1, out var t2);

            var config = services.GetService<IOptions<ConfigOptions>>().Value;

            t1 = t1.Down(config.RecalcPositionPeriod);
            t2 = t2.Upper(config.RecalcPositionPeriod) + 1;

            var portfolioState = await GetPortfolio(client_id, place_id, t1.AsUtc(), t2.AsUtc(), currency) as Portfolio;

            var result = portfolioState.Quotes.SelectMany(quotes =>
            {
                return quotes.Value.PositionCurve(quotes.Key.Account.Place, quotes.Key.Instrument);//.Skip(1);
            });

            return new JsonResult(result.Aggregate());
        }

        [HttpGet]
        public async Task<IActionResult> DealsCsv(int client_id, long from, long to)
        {
            TryParseTimestamp(from, to, out var t1, out var t2);

            var getDealsTask = Task.Factory.StartNew(() => GetDeals(client_id, t1, t2).ToArray());
            await getDealsTask;

            using (var memoryStream = new MemoryStream(10000))
            {
                using (var writer = new StreamWriter(memoryStream))
                {
                    var s = default(string);
                    var ckey = default(CurrencyKey);
                    var code = default(string);
                    writer.Write(s = string.Join(Environment.NewLine,
                        getDealsTask.Result.Select(deal =>
                        {
                            if (ckey != deal.Key.Instrument.Currency)
                            {
                                ckey = deal.Key.Instrument.Currency;
                                code = ((Currency)deal.Key.Instrument.Currency).DCode;
                            }

                            return string.Concat(deal.Value.Timestamp.ToString(), "|",
                                ((ValueTokenType)deal.Value.Data.Type).ToString(), "|",
                                ((Instrument)deal.Key.Instrument.Instrument).Code, "|",
                                deal.Value.Data.Volume.ForCost.ToString(), "|",
                                deal.Value.Data.Price.ToString(), "|",
                                deal.Value.Data.Quantity.ForCost.ToString(), "|",
                                deal.Value.Data.Comission.ToString(), "|",
                                code);
                        })));
                }

                return File(memoryStream.ToArray(), "text/csv", "deals.csv");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PositionsCsv(int client_id, long from, long to)
        {
            TryParseTimestamp(from, to, out var t1, out var t2);

            var positions = services.GetService<PositionStorage>();
            var getPositionsTask = Task.Factory.StartNew(() =>
            {
                var portfolioState = GetPortfolio(client_id, t1.AsUtc(), t2.AsUtc()).Result as Portfolio;

                return portfolioState.Quotes.SelectMany(quotes => quotes.Value.PositionCurve(quotes.Key.Account.Place, quotes.Key.Instrument)).OrderBy(p => p.date).ToArray();
            });
            await getPositionsTask;

            using (var memoryStream = new MemoryStream(10000))
            {
                using (var writer = new StreamWriter(memoryStream))
                {
                    var s = default(string);
                    writer.Write(s = string.Join(Environment.NewLine,
                        getPositionsTask.Result.Select(position =>
                            string.Concat(position.date.ToString(), "|", // закешировать потом!!!!
                                position.place.ToString(), "|",
                                position.short_name, "|",
                                position.bquantity.ToString(), "|", position.bquote.ToString(), "|", position.bvolume.ToString(), "|",
                                position.quantity.ToString(), "|", position.price.ToString(), "|", position.cost.ToString(), "|", position.comission.ToString(), "|",
                                position.qbuy.ToString(), "|", position.pbuy.ToString(), "|", position.vbuy.ToString(), "|",
                                position.qsell.ToString(), "|", position.psell.ToString(), "|", position.vsell.ToString(), "|",
                                position.reprice.ToString(), "|", position.allprofit.ToString(), "|", position.percent.ToString(), "|",
                                position.currency, "|")
                    )));
                }

                return File(memoryStream.ToArray(), "text/csv", "positions.csv");
            }
        }

        private IEnumerable<KeyValuePair<DealKey, HD<Deal, DR>>> GetDeals(int client_id, Timestamp from, Timestamp to)
        {
            var deals = services.GetService<DealStorage>();

            foreach (var dealKey in deals.DealKeys(client_id))
            {
                foreach (var deal in deals.Items(dealKey, from, to))
                {
                    yield return new KeyValuePair<DealKey, HD<Deal, DR>>(dealKey, deal);
                }
            }
        }

        [HttpGet]
        public async Task<JsonResult> PortfolioPositions(int client_id, long from, long to, int currency, int point_count = 1000)
        {
            var portfolioState = await GetPortfolio(client_id, from, to, currency, point_count) as Portfolio;

            return new JsonResult(portfolioState.Positions.SelectMany(p =>
                p.Value.Select(pp =>
                {
                    var position = (Position)pp.Data;
                    return new
                    {
                        instrument_id = (int)p.Key.Instrument.Instrument,
                        currency = (int)p.Key.Instrument.Currency,
                        quantity = position.Quantity.ToString(),
                        profit = position.Profit.ToString(),
                        cost = position.Cost.ToString()
                    };
                })));
        }

        [HttpGet]
        public async Task<JsonResult> ProfitCurve(int client_id, long from, long to, int point_count, int currency)
        {
            var portfolioState = await GetPortfolio(client_id, from, to, currency, point_count) as Portfolio;

            var profitAggregate = portfolioState.Dating.Select(dt => new ValueTokensEntry(dt.Key, dt.Value.ProfitAggregate()))
                .Append(new ValueTokensEntry(default(PositionKey), portfolioState.FreeDating.ProfitAggregate()));

            var dc = (Currency)currency.ToString();
            var result = new
            {
                curve = profitAggregate.Select(pa => {
                    PositionInfo(pa.Key, dc, out var instrument, out var curr);

                    return new
                    {
                        instrument_id = (int)instrument.IdCode,
                        short_name = instrument.Code,
                        name = instrument.Name,
                        currency_id = (int)pa.Key.Instrument.Currency,
                        currency = curr.DCode,
                        profit = pa.Value.Select(pf => new { t = pf.Timestamp.AsUtc(), v = pf.Data.ForProfit })
                    };
                })
            };

            return new JsonResult(result) { ContentType = "application/json" };
        }

        [HttpGet]
        public async Task<JsonResult> IncomeInstrumentCurve(int client_id, int instrument_id, long from, long to, int point_count, int currency)
        {
            var portfolioState = await GetPortfolio(client_id, from, to, currency, point_count) as Portfolio;

            var positionDating = portfolioState.Dating.FirstOrDefault(pd => pd.Key.Instrument.Instrument == instrument_id);
            var profitAggregate = new ValueTokensEntry(positionDating.Key, positionDating.Value.ProfitAggregate());

            var result = new
            {
                curve = new
                {
                    instrument_id = (int)profitAggregate.Key.Instrument.Instrument,
                    currency = (int)profitAggregate.Key.Instrument.Currency,
                    profit = profitAggregate.Value.Select(pf => new { t = pf.Timestamp.AsUtc(), v = pf.Data.Value })
                }
            };

            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<JsonResult> ValueCurve(int client_id, long from, long to, int point_count, int currency)
        {
            var portfolioState = await GetPortfolio(client_id, from, to, currency, point_count) as Portfolio;

            var quotesAggregate = portfolioState.Quotes.Select(dt => new ValueEntry(dt.Key, dt.Value.ValueCurve()))
                .Append(new ValueEntry(default(PositionKey), portfolioState.FreeQuotes.ValueCurve()));

            var dc = (Currency)currency.ToString();
            var result = new
            {
                curve = quotesAggregate.Select(pa => {
                    PositionInfo(pa.Key, dc, out var instrument, out var curr);

                    return new
                    {
                        instrument_id = (int)instrument.IdCode,
                        short_name = instrument.Code,
                        name = instrument.Name,
                        currency_id = (int)curr.ID,
                        currency = curr.DCode,
                        value = pa.Value.Select(pf => new { t = pf.Timestamp.AsUtc(), v = pf.Data })
                    };
                })
            };

            return new JsonResult(result) { ContentType = "application/json" };
        }

        [HttpGet]
        public async Task<JsonResult> ValueInstrumentCurve(int client_id, int instrument_id, long from, long to, int point_count, int currency)
        {
            var portfolioState = await GetPortfolio(client_id, from, to, currency, point_count) as Portfolio;

            var positionQuotes = portfolioState.Quotes.FirstOrDefault(pd => pd.Key.Instrument.Instrument == instrument_id);
            var quotesAggregate = new ValueEntry(positionQuotes.Key, positionQuotes.Value.ValueCurve());

            var result = new
            {
                curve = new
                {
                    instrument_id = (int)quotesAggregate.Key.Instrument.Instrument,
                    currency_id = (int)quotesAggregate.Key.Instrument.Currency,
                    value = quotesAggregate.Value.Select(pf => new { t = pf.Timestamp.AsUtc(), v = pf.Data })
                }
            };

            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<JsonResult> ResultCurve(int client_id, long from, long to, int point_count, int currency)
        {
            var portfolioState = await GetPortfolio(client_id, from, to, currency, point_count) as Portfolio;

            var costAggregate = portfolioState.Dating.Select(dt => new ValueTokensEntry(dt.Key, dt.Value.CostAggregate()))
                .Append(new ValueTokensEntry(default(PositionKey), portfolioState.FreeDating.CostAggregate()))
                .ToArray();

            var quotesAggregate = portfolioState.Quotes.Select(dt => new ValueTokensEntry(dt.Key, dt.Value.ValueAggregate()))
                .Append(new ValueTokensEntry(default(PositionKey), portfolioState.FreeQuotes.ValueAggregate()))
                .ToArray();

            var profitAggregate = portfolioState.Dating.Select(dt => new ValueTokensEntry(dt.Key, dt.Value.ProfitAggregate()))
                .Append(new ValueTokensEntry(default(PositionKey), portfolioState.FreeDating.ProfitAggregate()))
                .ToArray();

            var dc = (Currency)currency.ToString();
            var result = new
            {
                curve = costAggregate.Select(pa => {
                    PositionInfo(pa.Key, dc, out var instrument, out var curr);

                    var quotes = (quotesAggregate.FirstOrDefault(qa => qa.Key.Instrument.GetHashCode() == pa.Key.Instrument.GetHashCode()).Value)
                            ?? Enumerable.Empty<HD<ValueToken, HR>>();
                    var profits = (profitAggregate.FirstOrDefault(pfa => pfa.Key.Instrument.GetHashCode() == pa.Key.Instrument.GetHashCode()).Value)
                            ?? Enumerable.Empty<HD<ValueToken, HR>>();

                    return new
                    {
                        instrument_id = (int)instrument.IdCode,
                        short_name = instrument.Code,
                        name = instrument.Name,
                        currency_id = (int)curr.ID,
                        currency = curr.Code,
                        cost = pa.Value.Select(ct => new { t = ct.Timestamp.AsUtc(), v = ct.Data.ForCost }),
                        value = quotes.Select(qa => new { t = qa.Timestamp.AsUtc(), v = qa.Data.ForCost }),
                        profit = profits.Select(pf => new { t = pf.Timestamp.AsUtc(), v = pf.Data.ForProfit })
                    };
                })
            };

            return new JsonResult(result) { ContentType = "application/json" };
        }

        [HttpGet]
        public async Task<JsonResult> SetContext(int point_count, int currency_id, int period)
        {
            var internalKey = InternalContextKey.Next();
            var cacheKey = new CacheKey(typeof(PortfolioController), internalKey);


            var cacheOption = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.MaxValue };
            memoryCache.Set(cacheKey, Tuple.Create(point_count, currency_id, period), cacheOption);

            return new JsonResult(new { internalKey }) ;
        }

        private IEnumerable<KeyValuePair<PositionKey, HD<Position, PR>>> GetPositions(int client_id, Timestamp from, Timestamp to)
        {
            var positions = services.GetService<PositionStorage>();
            foreach (var positionKey in positions.PositionKeys(client_id))
            {
                foreach (var position in positions.Items(positionKey, from, to))
                {
                    yield return new KeyValuePair<PositionKey, HD<Position, PR>>(positionKey, position);
                }
            }
        }

        public static void LoadDeals(DealStorage dealStorage, PositionStorage positionStorage, ConfigOptions configOptions)
        {
            Func<string, decimal> convert = c => decimal.Parse(c);
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
                convert = c => decimal.Parse(c.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));

            foreach (var clientDeals in configOptions.DemoDeals().Nodes()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .Select(c =>
                    {
                        var i = (XElement)c;
                        return new
                        {
                            dealKey = new DealKey(
                            new TradeAccountKey((AccountKey)i.Attribute("ClientId").Value.ToInt(0), 0),
                            new TradeInstrumentKey(i.Attribute("CurrencyId").Value, 
                                (InstrumentCode)i.Attribute("InstrumentId").Value.ToInt(0))),
                            deals = i.Nodes().Select(d =>
                            {
                                var ii = (XElement)d;

                                var dt = DateTime.Parse(ii.Attribute("Date").Value, CultureInfo.InvariantCulture.DateTimeFormat);
                                var tp = Enum.Parse<ValueTokenType>(ii.Attribute("Type").Value);

                                return new HD<Deal, DR>(dt, Deal.Create(ii.Attribute("Code").Value, (ushort)tp, convert(ii.Attribute("Volume").Value),
                                    convert(ii.Attribute("Quantity").Value)/*, ii.Attribute("CurrencyId").Value*/));
                            })
                        };
                    }))
            {
                dealStorage.AddRange(clientDeals.dealKey, clientDeals.deals);
            }

            // пересчет позиций, после загрузки сделок
            foreach (var c in Account.Accounts())
            {
                ((PositionRecalcStorage)positionStorage).Recalc(c.ID, dealStorage, true, positions => positions.Aggregate(configOptions.RecalcPositionPeriod));
            }
        }

        private void TryParseTimestamp(long from, long to, out Timestamp t1, out Timestamp t2)
        {
            t1 = (from > 0) ? (Timestamp)from.DateTimeFromMilliseconds() : Timestamp.MinValue;
            t2 = (to > 0) ? (Timestamp)to.DateTimeFromMilliseconds() : Timestamp.MaxValue;
        }

        private static readonly Instrument defaultInstrument = new Instrument(0, "Итого", "Итого");
        private void PositionInfo(PositionKey positionKey, Currency defaultCurrency, out Instrument instrument, out Currency currency)
        {
            instrument = (positionKey.Instrument.Instrument != InstrumentKey.Empty) ?
                        positionKey.Instrument.Instrument : defaultInstrument;

            currency = (positionKey.Instrument.Currency != 0) ?
                positionKey.Instrument.Currency : defaultCurrency;
        }

        private async Task<object> GetPortfolio(int client_id, long from, long to, int currency_id = 0, int point_count = 0)
        {
            return await GetPortfolio(client_id, 0, from, to, currency_id, point_count);
        }

        private async Task<object> GetPortfolio(int client_id, int place_id, long from, long to, int currency_id = 0, int point_count = 0)
        {
            TryParseTimestamp(from, to, out var t1, out var t2);
            
            var internalKey = new InternalKey(client_id, place_id, t1, t2, point_count, currency_id);
            var cacheKey = new CacheKey(typeof(PortfolioController), internalKey);
                
            if (!memoryCache.TryGetValue(cacheKey, out var cachePortfolio))
            {
                var portfolioTask = Task.Factory.StartNew(() => 
                {
                    var positions = services.GetService<PositionStorage>();
                    var rates = services.GetService<RateStorage>();

                    if (t1 == Timestamp.MinValue)
                        t1 = positions.ClientFirstTime(client_id).Date.AddDays(-1);

                    if (t2 == Timestamp.MaxValue)
                        //t2 = DateTime.Now.Date.AddDays(1);
                        t2 = positions.ClientLastTime(client_id).Date.AddDays(2);

                    var period = 24 * 3600;
                    if (point_count == 0)
                    {
                        period = services.GetService<IOptions<ConfigOptions>>().Value.RecalcPositionPeriod;
                    }
                    else
                    {
                        period = (t2.GetHashCode() - t1.GetHashCode()) / point_count;
                    }

                    var context = new PortfolioModel.Context(client_id, place_id, (currency_id > 0) ? currency_id.ToString() : CurrencyKey.Empty, t1, t2, period);
                    return (object) PortfolioModel.GetPortfolioState(positions, rates, context);
                });

                await portfolioTask;

                var cacheOption = new MemoryCacheEntryOptions { SlidingExpiration = cacheExpiration };
                memoryCache.Set(cacheKey, cachePortfolio = portfolioTask.Result, cacheOption);
            }

            return cachePortfolio;
        }

        private struct InternalKey : IEquatable<InternalKey>
        {
            public readonly int account_id;
            public readonly int place_id;
            public readonly int from;
            public readonly int to;
            public readonly int point_count;
            public readonly int currency;

            public InternalKey(int account_id, int place_id, int from, int to, int point_count, int currency)
            {
                this.account_id = account_id;
                this.place_id = place_id;
                this.from = from;
                this.to = to;
                this.point_count = point_count;
                this.currency = currency.ToString().ToCurrencyKey();
            }

            public override bool Equals(object obj)
            {
                return Equals((InternalKey)obj);
            }

            public bool Equals(InternalKey other)
            {
                return account_id == other.account_id && place_id == other.place_id && from == other.from && 
                    to == other.to && point_count == other.point_count && currency == other.currency;
            }

            public override int GetHashCode()
            {
                return account_id ^ (place_id << 24) ^ from ^ to ^ point_count ^ (currency<<16);
            }
        }


        private struct InternalContextKey : IEquatable<InternalContextKey>
        {
            public readonly int id;
            public InternalContextKey(int id) { this.id = id; }

            public bool Equals(InternalContextKey other)
            {
                return id == other.id;
            }

            public override bool Equals(object obj)
            {
                return Equals((InternalContextKey) obj);
            }

            public override int GetHashCode()
            {
                return id;
            }

            public static InternalContextKey Next()
            {
                return new InternalContextKey(EntityPool<CtP>.Next());
            }

        }
    }
}

