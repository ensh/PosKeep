    using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Vtb.PosKeep.Entity;
using Vtb.PosKeep.Entity.Business.Model;
using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;
using Vtb.PosKeep.Entity.Storage;

namespace Vtb.PosKeep.Server
{
    public static class DataLoader
    {
        public static class CurrencyLoader
        {
            const int DCodeColumn = 0;
            const int CodeColumn = 1;
            const int NameColumn = 2;
            const int MaxCount = 3;

            public static void Load(ConfigOptions configOptions)
            {
                using (var reader = new StreamReader(configOptions.CurrencyFileName))
                {
                    foreach (var line in lines(reader))
                    {
                        if (!string.IsNullOrEmpty(line[DCodeColumn]) && !string.IsNullOrEmpty(line[CodeColumn]) &&
                            !string.IsNullOrEmpty(line[NameColumn]))
                        {
                            new Currency(line[DCodeColumn], line[CodeColumn], line[NameColumn]);
                        }
                    }
                }
            }
            
            private static IEnumerable<string[]> lines(TextReader textReader)
            {
                string line;
                while (!string.IsNullOrEmpty(line = textReader.ReadLine()))
                {
                    var result = line.Split("|");
                    if (result.Length == MaxCount)
                        yield return result;
                }
            }
        }

        public static class DealLoader
        {
            const int CodeColumn = 0;
            const int DateColumn = 1;
            const int AccountColumn = 2;
            const int PlaceColumn = 3;
            const int InstrumentColumn = 4;
            const int BuySellColumn = 5;
            const int VolumeColumn = 6;
            const int QuantityColumn = 7;
            const int CurrencyColumn = 8;
            const int ComissionColumn = 9;
            const int MaxColumn = 10;

            private static volatile int dealsCount = 0;
            public static volatile bool Loading = false;
            private static volatile int Lines;
            public static int LinesNumber
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Interlocked.CompareExchange(ref Lines, 0, 0); }
            }
            public static int DealsCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Interlocked.CompareExchange(ref dealsCount, 0, 0); }
            }
            
            public static void Load(ConfigOptions configOptions, DealStorage dealStorage, PositionStorage positionStorage)
            {
                Task appendDeals(Context context, bool recalc)
                {
                    return Task.Factory.StartNew(() =>
                    {
                        var account = new Account(context.account_id); // регистрируем фейковый счет если надо
                        var trade_account = new TradeAccountKey(account.ID, context.place_id.ToInt(0));
                        var dealKey = new DealKey(trade_account, 
                            new TradeInstrumentKey(context.currency_code, (InstrumentCode)int.Parse(context.instrument_id)));

                        dealStorage.AddRange(dealKey, context.lines);

                        var moneyKey = new DealKey(trade_account, new TradeInstrumentKey(context.currency_code, Instrument.Money.ID));
                        dealStorage.AddRange(moneyKey, context.lines);

                        if (recalc)
                        {
                            if (context.tasks != null && context.tasks.Count > 0)
                                Task.WaitAll(context.tasks.ToArray());
                            
                            ((PositionRecalcStorage) positionStorage).Recalc(account.ID, dealStorage, true, positions =>
                            {
                                return positions.Aggregate(configOptions.RecalcPositionPeriod);
                            });
                        }
                    });
                }

                Loading = true;
                using (var reader = new StreamReader(configOptions.DealsFileName))
                {
                    WriteDebug("Start at " + DateTime.Now.ToString() + Environment.NewLine);
                    Context context = new Context { account_id = "", place_id = "", instrument_id = "", currency_code = "", lines = new LinkedList<HD<Deal, DR>>() };
                    foreach (var line in lines(reader))
                    {
                        var new_account = line[AccountColumn] != context.account_id;
                        if (new_account)
                            new Account(line[AccountColumn]); // регистрируем фейковый счет если надо
                        
                        if (line[PlaceColumn] == context.place_id && line[InstrumentColumn] == context.instrument_id && line[CurrencyColumn] == context.currency_code && !new_account)
                        {
                            context.lines.AddLast(Create(line));
                        }
                        else
                        {
                            if (context.lines.Count > 0)
                            {
                                //context.totalLines += context.lines.Count;
                                if (new_account)
                                {
                                    appendDeals(context, true);
                                    //context.totalLines = 0;
                                    context.tasks = null;
                                }
                                else
                                {
                                    (context.tasks ?? (context.tasks = new LinkedList<Task>())).AddLast(appendDeals(context, false));
                                }

                                context.lines = new LinkedList<HD<Deal, DR>>();
                            }

                            context.account_id = line[AccountColumn];
                            context.place_id = line[PlaceColumn];
                            context.instrument_id = line[InstrumentColumn];
                            context.currency_code = line[CurrencyColumn];
                            context.lines.AddLast(Create(line));
                        }
                    }
                    
                    if (context.lines.Count > 0)
                    { 
                        //context.totalLines += context.lines.Count;
                        appendDeals(context, true);
                    }
                    
                    WriteDebug("Finished at " + DateTime.Now.ToString() + Environment.NewLine);

                    Loading = false;
                }
            }

            public static DealType AsDealType(string t)
            {
                switch (t)
                {
                    case "B": return DealType.Buy;
                    case "S": return DealType.Sell;
                    case "RB": return DealType.RepoBuy;
                    case "RS": return DealType.RepoSell;
                    case "SB": return DealType.SwapBuy;
                    case "SS": return DealType.SwapSell;
                    case "I": return DealType.Income;
                    case "O": return DealType.Outcome;
                }

                return 0;
            }

            static readonly CultureInfo provider = CultureInfo.InvariantCulture;
            private static HD<Deal, DR> Create(string[] line)
            {
                return new HD<Deal, DR>(
                    //DateTime.ParseExact(line[DateColumn], "yyyy-MM-dd HH:mm:ss.fff", provider),
                    int.Parse(line[DateColumn]),
                    Deal.Create(line[CodeColumn], AsDealType(line[BuySellColumn]), decimal.Parse(line[VolumeColumn]), decimal.Parse(line[QuantityColumn]), 
                        /*line[CurrencyColumn],*/ decimal.Parse(line[ComissionColumn])));
            }

            private static IEnumerable<string[]> lines(TextReader textReader)
            {
                string line;
                while (!string.IsNullOrEmpty(line = textReader.ReadLine()))
                {
                    Interlocked.Increment(ref Lines);
                    var result = line.Split("|");
                    if (result.Length == MaxColumn)
                        yield return result;
                }
            }

            private static string DebugFileName;

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void WriteDebug(string text)
            {
                if (DebugFileName == null)
                {
                    DebugFileName = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) +
                                    "\\dloader.log";
                    using (var stream = new StreamWriter(DebugFileName, true))
                    {
                    }
                }

                using (var writer = new StreamWriter(DebugFileName, true))
                {
                    writer.Write(text);
                }
            }

            private struct Context
            {
                public string account_id;
                public string place_id;
                public string instrument_id;
                public string currency_code;
                public LinkedList<HD<Deal, DR>> lines;
                public LinkedList<Task> tasks;
                //public int totalLines;
            }
        }

        public static class InstrumentLoader
        {
            const int IdColumn = 0;
            const int CodeColumn = 1;
            const int NameColumn = 2;
            const int MaxColumn = 3;

            public static void Load(ConfigOptions configOptions)
            {
                using (var reader = new StreamReader(configOptions.InstrumentsFileName))
                {
                    foreach (var line in lines(reader))
                    {
                        if (!string.IsNullOrEmpty(line[IdColumn]) && !string.IsNullOrEmpty(line[CodeColumn]) &&
                            !string.IsNullOrEmpty(line[NameColumn]))
                        {
                            new Instrument(int.Parse(line[IdColumn]), line[CodeColumn], line[NameColumn]);
                        }
                    }
                }
            }

            private static IEnumerable<string[]> lines(TextReader textReader)
            {
                string line;
                while (!string.IsNullOrEmpty(line = textReader.ReadLine()))
                {
                    var result = line.Split("|");
                    if (result.Length == MaxColumn)
                    {
                        yield return result;
                    }
                }
            }
        }

        public static class QuoteLoader
        {
            const int DateColumn = 0;
            const int InstrumentColumn = 1;
            const int PriceColumn = 2;
            const int CurrencyColumn = 3;
            const int MaxColumn = 4;
            
            public static volatile bool Loading = false;
            private static volatile int Lines;
            
            public static int LinesNumber
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Interlocked.CompareExchange(ref Lines, 0, 0); }
            }
            
            public static void Load(ConfigOptions configOptions, QuoteStorage quoteStorage, ref int [] years)
            {
                Task appendQuotes(Context context)
                {
                    CurrencyKey currencyKey = context.currency.ToCurrencyKey();
                    return Task.Factory.StartNew(() =>
                    {
                        var quoteKey = new QuoteKey(currencyKey, (InstrumentCode)int.Parse(context.instrument_id));

                        quoteStorage.AddRange(quoteKey, context.lines);
                    });
                }
                
                var yearsSet = new HashSet<int>();  
                using (var reader = new StreamReader(configOptions.QuotesFileName))
                {
                    Context context = new Context { instrument_id = "", currency = "", lines = new LinkedList<HD<Quote, QR>>()};
                    foreach (var line in lines(reader))
                    {
                        var new_instrument = line[InstrumentColumn] != context.instrument_id || line[CurrencyColumn] != context.currency;
                        
                        if (!new_instrument)
                        {
                            var q = Create(line);
                            yearsSet.Add(q.Timestamp.Date.Year);
                            context.lines.AddLast(q);
                        }
                        else
                        {
                            if (context.lines.Count > 0)
                            {
                                if (new_instrument)
                                {
                                    appendQuotes(context);
                                    context.tasks = null;
                                }
                                else
                                {
                                    (context.tasks ?? (context.tasks = new LinkedList<Task>())).AddLast(appendQuotes(context));
                                }

                                context.lines = new LinkedList<HD<Quote, QR>>();
                            }

                            context.instrument_id = line[InstrumentColumn];
                            context.currency = line[CurrencyColumn];
                            var q = Create(line);
                            yearsSet.Add(q.Timestamp.Date.Year);
                            context.lines.AddLast(q);
                        }
                    }
                }

                years = yearsSet.OrderBy(i => i).ToArray();
            }

            static readonly CultureInfo provider = CultureInfo.InvariantCulture;
            private static HD<Quote, QR> Create(string[] line)
            {
                return new HD<Quote, QR>(
                    //DateTime.ParseExact(line[DateColumn], "yyyy-MM-dd HH:mm:ss.fff", provider),
                    int.Parse(line[DateColumn])*100,
                    Quote.Create(decimal.Parse(line[PriceColumn]))
                );
            }
            
            private static IEnumerable<string[]> lines(TextReader textReader)
            {
                string line;
                while (!string.IsNullOrEmpty(line = textReader.ReadLine()))
                {
                    Interlocked.Increment(ref Lines);
                    var result = line.Split("|");
                    if (result.Length == MaxColumn)
                    {
                        yield return result;
                    }
                }
            }
            
            private static string DebugFileName;

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void WriteDebug(string text)
            {
                if (DebugFileName == null)
                {
                    DebugFileName = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) +
                                    "\\qloader.log";
                    using (var stream = new StreamWriter(DebugFileName, true))
                    {
                    }
                }

                using (var writer = new StreamWriter(DebugFileName, true))
                {
                    writer.Write(text);
                }
            }
            
            private struct Context
            {
                public string instrument_id;
                public string currency;
                public LinkedList<HD<Quote, QR>> lines;
                public LinkedList<Task> tasks;
                //public int totalLines;
            }
        }

        public static class RateLoader
        {
            const int DateColumn = 0;
            const int CurrencyColumn = 1;
            const int PriceColumn = 2;
            const int MaxColumn = 3;

            public static volatile bool Loading = false;
            private static volatile int Lines;

            public static int LinesNumber
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Interlocked.CompareExchange(ref Lines, 0, 0); }
            }

            public static void Load(ConfigOptions configOptions, RateStorage rateStorage, ref int[] years)
            {
                Task appendRates(Context context)
                {
                    CurrencyKey currencyKey = context.currency.ToCurrencyKey();
                    return Task.Factory.StartNew(() =>
                    {
                        rateStorage.AddRange(currencyKey, context.lines);
                    });
                }

                var yearsSet = new HashSet<int>();
                using (var reader = new StreamReader(configOptions.RatesFileName))
                {
                    rateStorage.BaseCurrency = configOptions.DefaultCurrencyDCode.ToString();

                    Context context = new Context { currency = "", lines = new LinkedList<HD<Rate, RR>>() };
                    foreach (var line in lines(reader))
                    {
                        var new_currency = line[CurrencyColumn] != context.currency;

                        if (!new_currency)
                        {
                            var q = Create(line);
                            yearsSet.Add(q.Timestamp.Date.Year);
                            context.lines.AddLast(q);
                        }
                        else
                        {
                            if (context.lines.Count > 0)
                            {
                                if (new_currency)
                                {
                                    appendRates(context);
                                    context.tasks = null;
                                }
                                else
                                {
                                    (context.tasks ?? (context.tasks = new LinkedList<Task>())).AddLast(appendRates(context));
                                }

                                context.lines = new LinkedList<HD<Rate, RR>>();
                            }

                            context.currency = line[CurrencyColumn];
                            var q = Create(line);
                            yearsSet.Add(q.Timestamp.Date.Year);
                            context.lines.AddLast(q);
                        }
                    }
                }

                years = yearsSet.OrderBy(i => i).ToArray();
            }

            static readonly CultureInfo provider = CultureInfo.InvariantCulture;
            private static HD<Rate, RR> Create(string[] line)
            {
                return new HD<Rate, RR>(
                    //DateTime.ParseExact(line[DateColumn], "yyyy-MM-dd HH:mm:ss.fff", provider),
                    int.Parse(line[DateColumn]) * 100,
                    Rate.Create(decimal.Parse(line[PriceColumn]))
                );
            }

            private static IEnumerable<string[]> lines(TextReader textReader)
            {
                string line;
                while (!string.IsNullOrEmpty(line = textReader.ReadLine()))
                {
                    Interlocked.Increment(ref Lines);
                    var result = line.Split("|");
                    if (result.Length == MaxColumn)
                    {
                        yield return result;
                    }
                }
            }

            private static string DebugFileName;

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void WriteDebug(string text)
            {
                if (DebugFileName == null)
                {
                    DebugFileName = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) +
                                    "\\rloader.log";
                    using (var stream = new StreamWriter(DebugFileName, true))
                    {
                    }
                }

                using (var writer = new StreamWriter(DebugFileName, true))
                {
                    writer.Write(text);
                }
            }

            private struct Context
            {
                public string currency;
                public LinkedList<HD<Rate, RR>> lines;
                public LinkedList<Task> tasks;
                //public int totalLines;
            }
        }
    }
}