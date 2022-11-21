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
    using System.Threading.Tasks;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Storage;

    [TestClass]
    public class DealStorageUnitTest
    {
        const int ClientID = 1;
        static readonly int Instrument1ID;
        static readonly int Instrument2ID;
        const int RubCurrencyId = 1;
        const string RubCurrencyDCode = "810";

        static DealStorageUnitTest()
        {
            Account.Init(10);
            Currency.Init(10);
            Instrument.Init(10);
            Deal.Init(1000);
            TradeInstrument.Init(10);
            TradeAccount.Init(10);

            new Account("AAA", "Иванов - акции");

            Instrument1ID = new Instrument(1, "VTBR", "ПАО Банк ВТБ");
            Instrument2ID = new Instrument(2, "GAZP", "ПАО Газпром");

            new Currency(RubCurrencyDCode, "руб", "Российский рубль");

            factory = new DealStorageFactory();
        }

        class DealStorageFactory : IBlockStorageFactory<HD<Deal, DR>>
        {
            public SortBlockStorage<HD<Deal, DR>> Create()
            {
                int blockSize = 100, blockCount = 10;
                return new SortBlockStorage<HD<Deal, DR>>(MergeUtils.DistinctMerge, blockSize, blockCount);
            }

            public SortBlockStorage<HD<Deal, DR>> Create(int blockSize, int blockCount)
            {
                return Create();
            }
        }

        static readonly IBlockStorageFactory<HD<Deal, DR>> factory;

        private Deal CreateBuy(string code, decimal vol, decimal qty, int cur)
        {
            return Deal.Create(code, (ushort)ValueTokenType.Buy, vol, qty, cur);
        }

        [TestMethod]
        public void DealStorageTestMethod1()
        {
            var storage = new DealStorage(factory);

            Timestamp moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantity = 100m;

            var ip1 = new TradeInstrumentKey(RubCurrencyDCode, Instrument1ID);
            var ip2 = new TradeInstrumentKey(RubCurrencyDCode, Instrument2ID);

            HD<Deal, DR> dealGetter (int index) =>
                new HD<Deal, DR>(moment + index * 20, CreateBuy(index.ToString(), price * (1 + index % 2), quantity, RubCurrencyId));

            foreach (var deal in GetDeals(100, dealGetter))
                storage.Add(new DealKey(new TradeAccountKey((AccountKey)ClientID, 0), ip1), deal);

            storage.AddRange(new DealKey(new TradeAccountKey((AccountKey)ClientID, 0), ip2), GetDeals(100, dealGetter));

            Assert.AreEqual(true, storage.Instruments((AccountKey)ClientID).SequenceEqual(new [] { ip1, ip2 }), "");

            Assert.AreEqual(true, GetDeals(100, dealGetter)
                .SequenceEqual(storage.Items(new DealKey(new TradeAccountKey((AccountKey)ClientID, 0), storage.Instruments(ClientID).First()))
                , new DealComparer()), "");

            Assert.AreEqual(true, GetDeals(100, dealGetter)
                .SequenceEqual(storage.Items(new DealKey(new TradeAccountKey((AccountKey)ClientID, 0), storage.Instruments(ClientID).Skip(1).First()))
                , new DealComparer()), "");
        }

        [TestMethod]
        public void DealStorageTestMethod2()
        {
            var storage = new DealStorage(factory);

            Timestamp moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantity = 100m;
            var ip1 = new TradeInstrumentKey(RubCurrencyDCode, Instrument1ID);

            using (var startEvent = new ManualResetEvent(false))
            {
                // одновременная запись сделок 5 потоками
                var tasks = Enumerable.Range(0, 5).Select(i => Task.Factory.StartNew(() =>
                {
                    var start = moment + 20 * 20 * i;
                    HD<Deal, DR> dealGetter (int index) =>
                        new HD<Deal, DR>(start + index * 20, CreateBuy(index.ToString(), price * (1 + index % 2), quantity, RubCurrencyId));

                    startEvent.WaitOne();
                    foreach (var deal in GetDeals(i*20, 20, dealGetter))
                        storage.Add(new DealKey(new TradeAccountKey((AccountKey)ClientID, 0), ip1), deal);
                }));

                startEvent.Set();
                Task.WaitAll(tasks.ToArray());
            }

            {
                HD<Deal, DR> dealGetter (int index) =>
                    new HD<Deal, DR>(moment + index * 20, CreateBuy(index.ToString(), price * (1 + index % 2), quantity, RubCurrencyId));
                var deals = storage.Items(new DealKey(new TradeAccountKey((AccountKey)ClientID, 0), storage.Instruments(ClientID).First()))
                    .OrderBy(deal => deal.Timestamp).ToList();

                Assert.AreEqual(true, GetDeals(100, dealGetter)
                    .SequenceEqual(deals, new DealComparer()), "");
            }
        }

        private IEnumerable<HD<Deal, DR>> GetDeals(int count, Func<int, HD<Deal, DR>> dealGetter)
        {
            return GetDeals(0, count, dealGetter);
        }

        private IEnumerable<HD<Deal, DR>> GetDeals(int start, int count, Func<int, HD<Deal, DR>> dealGetter)
        {
            return Enumerable.Range(start, count).Select(i => dealGetter(i));
        } 

        private class DealComparer : IEqualityComparer<HD<Deal, DR>>
        {
            public bool Equals(HD<Deal, DR> x, HD<Deal, DR> y)
            {
                return x.Timestamp == y.Timestamp && x.Data.Equals(y.Data)
                    && x.Data.Price == y.Data.Price && x.Data.Comission == y.Data.Comission 
                    && x.Data.Quantity == y.Data.Quantity && x.Data.Volume == y.Data.Volume;
            }

            public int GetHashCode(HD<Deal, DR> deal)
            {
                return deal.Timestamp;
            }
        }
    }
}
