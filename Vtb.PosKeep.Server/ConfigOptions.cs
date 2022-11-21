
namespace Vtb.PosKeep.Server
{
    using System;
    using System.Xml.Linq;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;
    using Vtb.PosKeep.Entity.Storage;

    public static class ConfigUtils
    {
        public static int ToInt(this string text, int defValue)
        {
            if (!string.IsNullOrEmpty(text) && int.TryParse(text, out var value))
                return value;

            return defValue;
        }

        public static IBlockStorageFactory<HD<Deal, DR>> DealStorageFactory(this ConfigOptions options)
        {
            return new DealStorageFactoryImpl() { options = options };
        }

        public static IBlockStorageFactory<HD<Position, PR>> PositionStorageFactory(this ConfigOptions options)
        {
            return new PositionStorageFactoryImpl() { options = options };
        }

        public static IBlockStorageFactory<HD<PortfolioState, PSR>> PortfolioStateStorageFactory(this ConfigOptions options)
        {
            return new PortfolioStateStorageFactoryImpl() { options = options };
        }

        public static IBlockStorageFactory<HD<Quote, QR>> QuoteStorageFactory(this ConfigOptions options)
        {
            return new QuoteStorageFactoryImpl() { options = options };
        }

        public static IBlockStorageFactory<HD<Rate, RR>> RateStorageFactory(this ConfigOptions options)
        {
            return new RateStorageFactoryImpl() { options = options };
        }

        private class DealStorageFactoryImpl : IBlockStorageFactory<HD<Deal, DR>>
        {
            public ConfigOptions options;
            public SortBlockStorage<HD<Deal, DR>> Create()
            {
                return new SortBlockStorage<HD<Deal, DR>>(MergeUtils.Merge, options.DealBlockSize, options.DealBlockCount);
            }
            
            public SortBlockStorage<HD<Deal, DR>> Create(int blockSize, int blockCount)
            {
                return new SortBlockStorage<HD<Deal, DR>>(MergeUtils.Merge, 
                    Math.Max(blockSize, options.DealBlockSize), 
                    Math.Min(blockCount, options.DealBlockCount));
            }
        }

        private class PositionStorageFactoryImpl : IBlockStorageFactory<HD<Position, PR>>
        {
            public ConfigOptions options;
            public SortBlockStorage<HD<Position, PR>> Create()
            {
                return new SortBlockStorage<HD<Position, PR>>(MergeUtils.DistinctMerge, options.PositionBlockSize, options.PositionBlockCount);
            }
            public SortBlockStorage<HD<Position, PR>> Create(int blockSize, int blockCount)
            {
                return new SortBlockStorage<HD<Position, PR>>(MergeUtils.DistinctMerge, 
                    Math.Max(blockSize, options.PositionBlockSize), 
                    Math.Min(blockCount, options.PositionBlockCount));
            }
        }

        private class PortfolioStateStorageFactoryImpl : IBlockStorageFactory<HD<PortfolioState, PSR>>
        {
            public ConfigOptions options;
            public SortBlockStorage<HD<PortfolioState, PSR>> Create()
            {
                return new SortBlockStorage<HD<PortfolioState, PSR>>(MergeUtils.DistinctMerge, options.PositionBlockSize, options.PositionBlockCount);
            }
            public SortBlockStorage<HD<PortfolioState, PSR>> Create(int blockSize, int blockCount)
            {
                return new SortBlockStorage<HD<PortfolioState, PSR>>(MergeUtils.DistinctMerge, 
                    Math.Max(blockSize, options.PositionBlockSize), 
                    Math.Min(blockCount, options.PositionBlockCount));
            }
        }

        private class QuoteStorageFactoryImpl : IBlockStorageFactory<HD<Quote, QR>>
        {
            public ConfigOptions options;
            public SortBlockStorage<HD<Quote, QR>> Create()
            {
                return new SortBlockStorage<HD<Quote, QR>>(MergeUtils.DistinctMerge, options.QuoteBlockSize, options.QuoteBlockCount);
            }
            public SortBlockStorage<HD<Quote, QR>> Create(int blockSize, int blockCount)
            {
                return new SortBlockStorage<HD<Quote, QR>>(MergeUtils.DistinctMerge, 
                    Math.Max(blockSize, options.QuoteBlockSize), 
                    Math.Min(blockCount, options.QuoteBlockCount));
            }
        }

        private class RateStorageFactoryImpl : IBlockStorageFactory<HD<Rate, RR>>
        {
            public ConfigOptions options;
            public SortBlockStorage<HD<Rate, RR>> Create()
            {
                return new SortBlockStorage<HD<Rate, RR>>(MergeUtils.DistinctMerge, options.RateBlockSize, options.RateBlockCount);
            }
            public SortBlockStorage<HD<Rate, RR>> Create(int blockSize, int blockCount)
            {
                return new SortBlockStorage<HD<Rate, RR>>(MergeUtils.DistinctMerge,
                    Math.Max(blockSize, options.RateBlockSize),
                    Math.Min(blockCount, options.RateBlockCount));
            }
        }
    }

    public class ConfigOptions
    {
        public int AccountCount { get; set; }
        public int AccruedintCount { get; set; }
        public int CurrencyCount { get; set; }
        public int DealCount { get; set; }
        public int InstrumentCount { get; set; }
        public int PositionCount { get; set; }
        public int QuoteCount { get; set; }
        public int RateCount { get; set; }
        public int TradeAccCount { get; set; }
        public int TradeInsCount { get; set; }


        public int DealBlockSize { get; set; }
        public int DealBlockCount { get; set; }

        public int PositionBlockSize { get; set; }
        public int PositionBlockCount { get; set; }

        public int QuoteBlockSize { get; set; }
        public int QuoteBlockCount { get; set; }

        public int RateBlockSize { get; set; }
        public int RateBlockCount { get; set; } 

        public int AccruedintBlockSize { get; set; }
        public int AccruedintBlockCount { get; set; }

        public int CacheExpiration { get; set; }

        public int DefaultCurrencyDCode { get; set; }

        public int DefaultPointsCount { get; set; }
        public int DefaultInstrumentsPageSize { get; set; }
        public int DefaultAccountsPageSize { get; set; }
        public int RecalcPositionPeriod { get; set; }

        public Func<XElement> DemoClients { get; set; }
        public Func<XElement> DemoCurrencies { get; set; }
        public Func<XElement> DemoDeals { get; set; }
        public Func<XElement> DemoInstruments { get; set; }
        public Func<XElement> DemoQuotes { get; set; }
        public Func<XElement> DemoRates { get; set; }

        public string CurrencyFileName { get; set; }
        public string DealsFileName { get; set; }
        public string InstrumentsFileName { get; set; }
        public string QuotesFileName { get; set; }
        public string RatesFileName { get; set; }

        //public int ClientCount { get; set; }
        //public int InstrumentCount { get; set; }
    }
}
