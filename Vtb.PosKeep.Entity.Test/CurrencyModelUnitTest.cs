using Microsoft.VisualStudio.TestTools.UnitTesting;

using Vtb.PosKeep.Entity.Data;
using Vtb.PosKeep.Entity.Key;

namespace Vtb.PosKeep.Entity.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Business.Model;
    using Vtb.PosKeep.Entity.Storage;

    using HP = HD<Position, PR>;
    using HQ = HD<Quote, QR>;
    using HR = HD<Rate, RR>;

    [TestClass]
    public class CurrencyModelUnitTest
    {
        const int ClientID = 1;
        static readonly int VTBR;
        static readonly int GAZP;
        static readonly int USDTOM;
        const int RubCurrencyId = 1;
        const int UsdCurrencyId = 2;
        const string RubCurrencyDCode = "810";
        const string UsdCurrencyDCode = "840";
        const string EurCurrencyDCode = "978";

        static CurrencyModelUnitTest()
        {
            Account.Init(10);
            Currency.Init(10);
            Instrument.Init(10);
            Position.Init(100, 10);
            Quote.Init(1000);
            Rate.Init(1000);
            TradeAccount.Init(10);
            TradeInstrument.Init(10);

            new Account("AAA", "Иванов - акции");

            VTBR = new Instrument(1, "VTBR", "ПАО Банк ВТБ");
            GAZP = new Instrument(2, "GAZP", "ПАО Газпром");
            USDTOM = new Instrument(3, "USD TOM", "USD tommorow");

            new Currency(RubCurrencyDCode, "руб", "Российский рубль");
            new Currency(UsdCurrencyDCode, "долл", "Доллар США");
            new Currency(EurCurrencyDCode, "евро", "Евро");

            quoteFactory = new QuoteStorageFactory();
            rateFactory = new RateStorageFactory();
        }

        class QuoteStorageFactory : IBlockStorageFactory<HD<Quote, QR>>
        {
            public SortBlockStorage<HD<Quote, QR>> Create()
            {
                int blockSize = 100, blockCount = 10;
                return new SortBlockStorage<HD<Quote, QR>>(MergeUtils.DistinctMerge, blockSize, blockCount);
            }

            public SortBlockStorage<HQ> Create(int blockSize, int blockCount)
            {
                throw new NotImplementedException();
            }
        }

        class RateStorageFactory : IBlockStorageFactory<HD<Rate, RR>>
        {
            public SortBlockStorage<HD<Rate, RR>> Create()
            {
                int blockSize = 100, blockCount = 10;
                return new SortBlockStorage<HD<Rate, RR>>(MergeUtils.DistinctMerge, blockSize, blockCount);
            }

            public SortBlockStorage<HR> Create(int blockSize, int blockCount)
            {
                throw new NotImplementedException();
            }
        }

        static readonly IBlockStorageFactory<HD<Quote, QR>> quoteFactory;
        static readonly IBlockStorageFactory<HD<Rate, RR>> rateFactory;

        [TestMethod]
        public void CurrencyModelTestMethod1()
        {
            const int period = 24 * 3600;
            var moment = new DateTime(2000, 01, 17).AddHours(10);

            var rub_positions = new[]
            {
                // RubCurrencyDCode
                new HP(moment.AddDays(25), Position.Create(10000.ToOutcome(), 100.ToBuy())),
                new HP(moment.AddDays(45), Position.Create(20000.ToOutcome(), 200.ToBuy())),
                new HP(moment.AddDays(65), Position.Create(30000.ToOutcome(), 300.ToBuy())),
            };

            var usd_positions = new[]
            {
                // UsdCurrencyDCode
                new HP(moment.AddDays(25), Position.Create(10000.ToOutcome(), 100.ToBuy())),
                new HP(moment.AddDays(45), Position.Create(20000.ToOutcome(), 200.ToBuy())),
                new HP(moment.AddDays(65), Position.Create(30000.ToOutcome(), 300.ToBuy())),
            };

            var context = new CurrencyModel.Context(
                new PositionKey(new TradeAccountKey(ClientID), new TradeInstrumentKey(RubCurrencyDCode, VTBR)), RubCurrencyDCode,
                new DateTime(2000, 01, 10), new DateTime(2000, 04, 10), period, ps => ps.AggregatePeriod(period));

            var rateStorage = new RateStorage(rateFactory, RubCurrencyDCode);

            rateStorage.AddRange(UsdCurrencyDCode,
                Enumerable.Range(1, 74).Select(i => new HR(moment.Date.AddDays(i + 10), Rate.Create(10m + i))));

            rateStorage.AddRange(EurCurrencyDCode,
                Enumerable.Range(1, 74).Select(i => new HR(moment.Date.AddDays(i + 10), Rate.Create(10m + i))));

            var pp = CurrencyModel.ConvertedPositions(rub_positions.OfToken(), rateStorage, context).ToArray();

            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && pp[0].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[1].Timestamp && ((Position)pp[1].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[1].Timestamp && ((Position)pp[1].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[1].Timestamp && pp[1].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[2].Timestamp && ((Position)pp[2].Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[2].Timestamp && ((Position)pp[2].Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[2].Timestamp && pp[2].Data == 1m, "");

            context = new CurrencyModel.Context(
                new PositionKey(new TradeAccountKey(ClientID), new TradeInstrumentKey(UsdCurrencyDCode, GAZP)), UsdCurrencyDCode,
                new DateTime(2000, 01, 10), new DateTime(2000, 04, 10), period, ps => ps.AggregatePeriod(period));

            pp = CurrencyModel.ConvertedPositions(usd_positions.OfToken(), rateStorage, context).ToArray();

            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && pp[0].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[1].Timestamp && ((Position)pp[1].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[1].Timestamp && ((Position)pp[1].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[1].Timestamp && pp[1].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[2].Timestamp && ((Position)pp[2].Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[2].Timestamp && ((Position)pp[2].Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[2].Timestamp && pp[2].Data == 1m, "");

            context = new CurrencyModel.Context(
                new PositionKey(new TradeAccountKey(ClientID), new TradeInstrumentKey(UsdCurrencyDCode, GAZP)), RubCurrencyDCode,
                new DateTime(2000, 01, 10), new DateTime(2000, 04, 10), period, ps => ps.AggregatePeriod(period));

            pp = CurrencyModel.ConvertedPositions(usd_positions.OfToken(), rateStorage, context).ToArray();

            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && pp[0].Data == 26m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && ((Position)pp[19].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && ((Position)pp[19].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && pp[19].Data == 45m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && ((Position)pp[20].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && ((Position)pp[20].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && pp[20].Data == 46m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && ((Position)pp[39].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && ((Position)pp[39].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && pp[39].Data == 65m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && ((Position)pp[40].Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && ((Position)pp[40].Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && pp[40].Data == 66m, "");

            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && ((Position)pp.Last().Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && ((Position)pp.Last().Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && pp.Last().Data == 83m, "");

            context = new CurrencyModel.Context(
                new PositionKey(new TradeAccountKey((AccountKey)ClientID, 0), new TradeInstrumentKey(UsdCurrencyDCode, GAZP)), EurCurrencyDCode,
                new DateTime(2000, 01, 10), new DateTime(2000, 04, 10), period, ps => ps.AggregatePeriod(period));

            pp = CurrencyModel.ConvertedPositions(usd_positions.OfToken(), rateStorage, context).ToArray();

            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && pp[0].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && ((Position)pp[19].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && ((Position)pp[19].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && pp[19].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && ((Position)pp[20].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && ((Position)pp[20].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && pp[20].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && ((Position)pp[39].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && ((Position)pp[39].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && pp[39].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && ((Position)pp[40].Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && ((Position)pp[40].Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && pp[40].Data == 1m, "");

            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && ((Position)pp.Last().Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && ((Position)pp.Last().Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && pp.Last().Data == 1m, "");
        }
    }
}
