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
    using Vtb.PosKeep.Entity.Storage;

    using HistoricalPosition = HD<Position, PR>; 

    [TestClass]
    public class PositionStorageUnitTest
    {
        const int Account1ID = 1;
        const int Account2ID = 2;
        static readonly int Instrument1ID;
        static readonly int Instrument2ID;
        static readonly int Instrument3ID;
        static readonly int Instrument4ID;
        const int RubCurrencyId = 1;
        const int UsdCurrencyId = 2;
        const string RubCurrencyDCode = "810";
        const string UsdCurrencyDCode = "840";

        static PositionStorageUnitTest()
        {
            Account.Init(10);
            Currency.Init(10);
            Instrument.Init(10);
            Position.Init(100, 10);
            TradeAccount.Init(10);
            TradeInstrument.Init(10);

            new Account("AAA", "Иванов - акции");
            new Account("BBB", "Петров - акции");

            Instrument1ID = new Instrument(1, "VTBR", "ПАО Банк ВТБ");
            Instrument2ID = new Instrument(2, "GAZP", "ПАО Газпром");
            Instrument3ID = new Instrument(3, "SBER", "ПАО Сбербанк");
            Instrument4ID = new Instrument(4, "INOUT", "Ввод-вывод дс");

            new Currency(RubCurrencyDCode, "руб", "Российский рубль");
            new Currency(UsdCurrencyDCode, "долл", "Доллар США");

            factory = new PositionStorageFactory();
        }

        static readonly IBlockStorageFactory<HistoricalPosition> factory = new PositionStorageFactory();

        class PositionStorageFactory : IBlockStorageFactory<HistoricalPosition>
        {
            public SortBlockStorage<HistoricalPosition> Create()
            {
                int blockSize = 100, blockCount = 10;
                return new SortBlockStorage<HistoricalPosition>(MergeUtils.DistinctMerge, blockSize, blockCount);
            }

            public SortBlockStorage<HistoricalPosition> Create(int blockSize, int blockCount)
            {
                return Create();
            }
        }

        [TestMethod]
        public void HistoryStorageTestMethod1()
        {
            var positionStorage = new PositionStorage(factory);

            var shortPositionFactory = PositionFactory.SuccessShortPositionFactory.SetDefaultCurrency(RubCurrencyId);
            var longPositionFactory = PositionFactory.SuccessLongPositionFactory.SetDefaultCurrency(RubCurrencyId);
            var incomeMoneyPositionFactory = PositionFactory.IncomeMoneyPositionFactory.SetDefaultCurrency(RubCurrencyId);
            var outcomeMoneyPositionFactory = PositionFactory.OutcomeMoneyPositionFactory.SetDefaultCurrency(RubCurrencyId);

            var sequence1 = new[]
            {
                new DateTime(2019, 09, 11).AsPositionHistory(shortPositionFactory.Create(1000, 0, 10)),
                new DateTime(2019, 09, 13).AsPositionHistory(shortPositionFactory.Create(2000, 0, 20)),
                new DateTime(2019, 09, 15).AsPositionHistory(longPositionFactory.Create(1000, 200, 10)),
            };

            var sequence2 = new[]
            {
                new DateTime(2019, 09, 11).AsPositionHistory(shortPositionFactory.Create(1000, 0, 10)),
                new DateTime(2019, 09, 13).AsPositionHistory(longPositionFactory.Create(1000, 100, 10)),
            };

            var sequence3 = new[] 
            {
                // UsdCurrencyId
                new DateTime(2019, 09, 11).AsPositionHistory(longPositionFactory.Create(100, 0, 10)),
                new DateTime(2019, 09, 13).AsPositionHistory(longPositionFactory.Create(0, 100, 0)),
            };

            var sequence4 = new[]
            {
                new DateTime(2019, 09, 11).AsPositionHistory(incomeMoneyPositionFactory.Create(1000, 0)),
                new DateTime(2019, 09, 13).AsPositionHistory(incomeMoneyPositionFactory.Create(3000, 0)),
            };

            var sequence5 = new[]
            {
                //UsdCurrencyId
                new DateTime(2019, 09, 11).AsPositionHistory(incomeMoneyPositionFactory.Create(100, 100, 10)),
            };

            var account1 = new TradeAccountKey((AccountKey)Account1ID, 0);
            var account2 = new TradeAccountKey((AccountKey)Account2ID, 0);
            var account3 = new TradeAccountKey((AccountKey)Account2ID, 1);

            positionStorage.AddRange(PositionKey.Create(Account1ID, 0, RubCurrencyId, Instrument1ID), sequence1);
            positionStorage.AddRange(PositionKey.Create(Account1ID, 0, RubCurrencyId, Instrument2ID), sequence2);
            positionStorage.AddRange(PositionKey.Create(Account2ID, 0, UsdCurrencyId, Instrument3ID), sequence3);
            positionStorage.AddRange(PositionKey.Create(Account1ID, 0, RubCurrencyId, Instrument4ID), sequence4);
            positionStorage.AddRange(PositionKey.Create(Account2ID, 0, UsdCurrencyId, Instrument4ID), sequence5);
            positionStorage.AddRange(PositionKey.Create(Account2ID, 1, UsdCurrencyId, Instrument3ID), sequence3);

            Assert.AreEqual(true, positionStorage.Instruments(Account1ID).SequenceEqual(new[]
            {
                TradeInstrumentKeyUtils.ToTradeInstrumentKey(new [] { RubCurrencyId, Instrument1ID }),
                TradeInstrumentKeyUtils.ToTradeInstrumentKey(new [] { RubCurrencyId, Instrument2ID }),
                TradeInstrumentKeyUtils.ToTradeInstrumentKey(new [] { RubCurrencyId, Instrument4ID }),
            }), "");

            Assert.AreEqual(true, positionStorage.Instruments(Account2ID).SequenceEqual(new[]
            {
                TradeInstrumentKeyUtils.ToTradeInstrumentKey(new [] { UsdCurrencyId, Instrument3ID }),
                TradeInstrumentKeyUtils.ToTradeInstrumentKey(new [] { UsdCurrencyId, Instrument4ID }),
                TradeInstrumentKeyUtils.ToTradeInstrumentKey(new [] { UsdCurrencyId, Instrument3ID }),
            }), "");

            Assert.AreEqual(true, positionStorage.Items(
                PositionKey.Create(account1, positionStorage.Instruments(Account1ID).First()))
                .SequenceEqual(sequence1, new PositionComparer()), "");

            Assert.AreEqual(true, positionStorage.Items(
                PositionKey.Create(account1, positionStorage.Instruments(Account1ID).Skip(1).First()))
                .SequenceEqual(sequence2, new PositionComparer()), "");

            Assert.AreEqual(true, positionStorage.Items(
                PositionKey.Create(account1, positionStorage.Instruments(Account1ID).Skip(2).First()))
                .SequenceEqual(sequence4, new PositionComparer()), "");

            Assert.AreEqual(true, positionStorage.Items(
                PositionKey.Create(account2, positionStorage.Instruments(account2).First()))
                .SequenceEqual(sequence3, new PositionComparer()), "");

            Assert.AreEqual(true, positionStorage.Items(
                PositionKey.Create(account2, positionStorage.Instruments(account2).Skip(1).First()))
                .SequenceEqual(sequence5, new PositionComparer()), "");

            Assert.AreEqual(true, positionStorage.Items(Account1ID, Timestamp.MinValue, Timestamp.MaxValue)
                .SelectMany(ps => ps.Value)
                .SequenceEqual(sequence1.Concat(sequence2).Concat(sequence4), new PositionComparer()), "");

            Assert.AreEqual(true, positionStorage.Items(account3, Timestamp.MinValue, Timestamp.MaxValue)
                .SelectMany(ps => ps.Value)
                .SequenceEqual(sequence3, new PositionComparer()), "");
        }

        private class PositionComparer : IEqualityComparer<HistoricalPosition>
        {
            public bool Equals(HistoricalPosition x, HistoricalPosition y)
            {
                return x.Timestamp == y.Timestamp
                    && x.Data.Quantity == y.Data.Quantity
                    && x.Data.Cost == y.Data.Cost
                    && x.Data.Profit == y.Data.Profit;
            }

            public int GetHashCode(HistoricalPosition position)
            {
                return position.Timestamp;
            }
        }
    }
}
