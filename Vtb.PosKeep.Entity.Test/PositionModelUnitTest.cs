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

    //using RS = RecalcState<HD<Deal, DR>>;
    //using HD<Deal, DR> = HD<Deal, DR>;
    using HP = HD<Position.Recalc, RPR>;

    [TestClass]
    public class PositionModelUnitTest
    {
        const int ClientID = 1;
        static readonly int Instrument1ID = 111;
        static readonly int Instrument2ID = 222;
        static readonly int RubCurrencyId = 1;
        static readonly int UsdCurrencyId = 2;
        const string RubCurrencyDCode = "810";
        const string UsdCurrencyDCode = "840";

        static PositionModelUnitTest()
        {
            Account.Init(10);
            Currency.Init(10);
            Instrument.Init(10);
            Deal.Init(1000);
            Position.Init(100, 10);
            TradeAccount.Init(10);
            TradeInstrument.Init(10);

            new Account("AAA", "Иванов - акции");

            Instrument1ID = new Instrument(1, "VTBR", "ПАО Банк ВТБ");
            Instrument2ID = new Instrument(2, "GAZP", "ПАО Газпром");

            RubCurrencyId = new Currency(RubCurrencyDCode, "руб", "Российский рубль");
            UsdCurrencyId = new Currency(UsdCurrencyDCode, "долл", "Доллар США");
        }

        [TestMethod]
        public void PositionModelTestMethod1()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantity = 100m;

            try
            {
                var deal = Deal.Create("", (ushort)ValueTokenType.Buy, quantity * price, quantity, RubCurrencyId);
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), deal),
                    new HD<Deal, DR>(moment.AddMinutes(7), deal),
                    new HD<Deal, DR>(moment.AddMinutes(9), deal),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToBuy(), 100.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToBuy(), 200.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToBuy(), 300.ToBuy(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod2()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantityBuy = 100m;
            decimal quantitySell = 50m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Sell(price, quantitySell, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(13), Sell(price+10, quantitySell, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToBuy(),   100.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToBuy(),   200.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToBuy(),   300.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(11), P(25000.ToBuy(),  250.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(13), P(20000.ToBuy(), 500.ToSell(), 200.ToBuy(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod3()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantityBuy = 100m;
            decimal quantitySell = 250m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Sell(price, quantitySell, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToBuy(), 100.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToBuy(), 200.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToBuy(), 300.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(11), P(5000.ToBuy(),  50.ToBuy(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod4()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantityBuy = 100m;
            decimal quantitySell = 250m;
            decimal profit = 10m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Sell(price+profit, quantitySell, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToBuy(), 100.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToBuy(), 200.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToBuy(), 300.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(11), P(5000.ToBuy(), (250 * profit).ToSell(),  50.ToBuy(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod5()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantityBuy = 100m;
            decimal quantitySell = 400m;
            decimal profit = 10m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Sell(price+profit, quantitySell, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToBuy(), 100.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToBuy(), 200.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToBuy(), 300.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(11), P(11000.ToSell(), (300 * profit).ToSell(), 100.ToSell(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod6()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantityBuy = 100m;
            decimal quantitySell = 100m;
            decimal profit = 10m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Buy(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Sell(price+profit, 4*quantitySell, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(13), Sell(price+profit, quantitySell, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(15), Buy(price, 2*quantityBuy, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToBuy(), 100.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToBuy(), 200.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToBuy(), 300.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddMinutes(11), P(11000.ToSell(), (300 * profit).ToSell(),  100.ToSell(), RubCurrencyId)),
                    new HP(moment.AddMinutes(13), P(22000.ToSell(), (300 * profit).ToSell(),  200.ToSell(), RubCurrencyId)),
                    new HP(moment.AddMinutes(15), P(0.ToBuy(), (500 * profit).ToSell(),  0.ToSell(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod7()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantity = 100m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(13), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(13), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(15), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(15), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(21), Buy(price, quantity, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(22), Buy(price, quantity, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(6),  P(new { Cost = 10000.ToBuy(),  Quantity = 100.ToBuy(), QuantityBuy = 100m, QuantitySell = 0m, VolumeBuy = 1000m, VolumeSell = 0m })),
                    new HP(moment.AddMinutes(8),  P(new { Cost = 20000.ToBuy(),  Quantity = 200.ToBuy(), QuantityBuy = 100m, QuantitySell = 0m, VolumeBuy = 1000m, VolumeSell = 0m })),
                    new HP(moment.AddMinutes(10), P(new { Cost = 30000.ToBuy(),  Quantity = 300.ToBuy(), QuantityBuy = 100m, QuantitySell = 0m, VolumeBuy = 1000m, VolumeSell = 0m })),
                    new HP(moment.AddMinutes(12), P(new { Cost = 40000.ToBuy(),  Quantity = 400.ToBuy(), QuantityBuy = 100m, QuantitySell = 0m, VolumeBuy = 1000m, VolumeSell = 0m })),
                    new HP(moment.AddMinutes(14), P(new { Cost = 60000.ToBuy(),  Quantity = 600.ToBuy(), QuantityBuy = 200m, QuantitySell = 0m, VolumeBuy = 2000m, VolumeSell = 0m })),
                    new HP(moment.AddMinutes(16), P(new { Cost = 80000.ToBuy(),  Quantity = 800.ToBuy(), QuantityBuy = 200m, QuantitySell = 0m, VolumeBuy = 2000m, VolumeSell = 0m })),
                    new HP(moment.AddMinutes(22), P(new { Cost = 100000.ToBuy(),  Quantity = 1000.ToBuy(), QuantityBuy = 200m, QuantitySell = 0m, VolumeBuy = 2000m, VolumeSell = 0m })),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.Positions(RubCurrencyId).Aggregate(120), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        /*
        [TestMethod]
        public void PositionModelTestMethod8()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantity = 100m;

            var deals = new[]
            {
                new HD<Deal, DR>(moment.AddMinutes(5), CreateBuy(quantity*price, quantity, RubCurrencyId)),
                new HD<Deal, DR>(moment.AddMinutes(7), CreateBuy(quantity*price, quantity, RubCurrencyId)),
                new HD<Deal, DR>(moment.AddMinutes(9), CreateBuy(quantity*price, quantity, RubCurrencyId)),

                new HD<Deal, DR>(moment.AddMinutes(11), CreateMoneyIncome(10*price, 1, RubCurrencyId)),
                new HD<Deal, DR>(moment.AddMinutes(13), CreateMoneyIncome(10*price, 1, RubCurrencyId)),

                new HD<Deal, DR>(moment.AddMinutes(15), CreateMoneyOutcome(10*price, 1, RubCurrencyId)),
                new HD<Deal, DR>(moment.AddMinutes(17), CreateMoneyOutcome(10*price, 1, RubCurrencyId)),
            };

            var positions = new[]
            {
                new HP(moment.AddMinutes(5), new RecalcPosition(10000.ToOutcome(),                  100.ToBuy(), RubCurrencyId)),
                new HP(moment.AddMinutes(7), new RecalcPosition(20000.ToOutcome(),                  200.ToBuy(), RubCurrencyId)),
                new HP(moment.AddMinutes(9), new RecalcPosition(30000.ToOutcome(),                  300.ToBuy(), RubCurrencyId)),
                new HP(moment.AddMinutes(11), new RecalcPosition(30000.ToOutcome(), 1000.ToIncome().ToMoney(), 300.ToBuy(), RubCurrencyId)),
                new HP(moment.AddMinutes(13), Position.Create(30000.ToOutcome(), 2000.ToIncome().ToMoney(), 300.ToBuy(), RubCurrencyId)),
                new HP(moment.AddMinutes(15), Position.Create(30000.ToOutcome(), 1000.ToIncome().ToMoney(), 300.ToBuy(), RubCurrencyId)),
                new HP(moment.AddMinutes(17), Position.Create(30000.ToOutcome(), 0.ToIncome().ToMoney(),    300.ToBuy(), RubCurrencyId)),

            };

            Assert.AreEqual(true, positions.SequenceEqual(deals.Positions(RubCurrencyId), new PositionComparer()), "");
        }
        */

        [TestMethod]
        public void PositionModelTestMethod9()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);
            decimal price = 100m;
            decimal quantityBuy = 100m;
            decimal quantitySell = 250m;

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddMinutes(5), Income(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(7), Income(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(9), Income(price, quantityBuy, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddMinutes(11), Sell(price, quantitySell, RubCurrencyId)),
                };

                var positions = new[]
                {
                    new HP(moment.AddMinutes(5), P(10000.ToIncome(), 100.ToIncome(), RubCurrencyId)),
                    new HP(moment.AddMinutes(7), P(20000.ToIncome(), 200.ToIncome(), RubCurrencyId)),
                    new HP(moment.AddMinutes(9), P(30000.ToIncome(), 300.ToIncome(), RubCurrencyId)),
                    new HP(moment.AddMinutes(11), P(5000.ToIncome(),  50.ToIncome(), RubCurrencyId)),
                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod10()
        {
            var moment = new DateTime(2004, 12, 07, 11, 25, 00);

            try
            {
                var deals = new[]
                {
                    new HD<Deal, DR>(moment.AddSeconds(2), Buy(7.373m, 3300, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddSeconds(3), Sell(7.373m, 3300, RubCurrencyId)),

                    new HD<Deal, DR>(moment.AddSeconds(4), Buy(7.373m, 50000, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddSeconds(5), Sell(7.373m, 50000, RubCurrencyId)),

                    new HD<Deal, DR>(moment.AddSeconds(6), Buy(7.372m, 6000, RubCurrencyId)),
                    new HD<Deal, DR>(moment.AddSeconds(7), Sell(7.372m, 6000, RubCurrencyId)),

                };

                var positions = new[]
                {
                    new HP(moment.AddSeconds(2), P((24330.9m).ToBuy(),   3300.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddSeconds(3), P(ValueToken.Null,   ValueToken.Null, RubCurrencyId)),

                    new HP(moment.AddSeconds(4), P((368650m).ToBuy(),   50000.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddSeconds(5), P(ValueToken.Null,   ValueToken.Null, RubCurrencyId)),

                    new HP(moment.AddSeconds(6), P((44232m).ToBuy(), 6000.ToBuy(), RubCurrencyId)),
                    new HP(moment.AddSeconds(7), P(ValueToken.Null,   ValueToken.Null, RubCurrencyId)),

                };

                Assert.AreEqual(true, positions.SequenceEqual(deals.TestPositions(RubCurrencyId), new PositionComparer()), "");
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        [TestMethod]
        public void PositionModelTestMethod11()
        {
            var moment = new DateTime(2000, 01, 17).AddHours(10);


            try
            {
                var positions = new[]
                {
                    (Position)Position.Create(new {Cost = 100.ToOutcome(), Quantity = 200.ToBuy() } ),
                    (Position)Position.Create(new {Cost = 100.ToOutcome(), Quantity = 300.ToBuy() }),
                    (Position)Position.Create(new {Cost = 1200.ToOutcome(), Quantity = 200.ToBuy() }),
                };

                //Assert.AreEqual(true, positions.SequenceEqual(deals.Positions(), new PositionComparer()), "");

                Assert.AreEqual(positions[0].Cost, 100.ToOutcome());
                Assert.AreEqual(positions[0].Quantity, 200.ToBuy());

                Assert.AreEqual(positions[1].Cost, 100.ToOutcome());
                Assert.AreEqual(positions[1].Quantity, 300.ToBuy());

                Assert.AreEqual(positions[2].Cost, 1200.ToOutcome());
                Assert.AreEqual(positions[2].Quantity, 200.ToBuy());
            }
            finally
            {
                EntityPool<ReP>.Reset();
                EntityPool<PP>.Reset();
            }
        }

        public static Position.Recalc P(object position)
        {
            return Position.Recalc.Create(position);
        }
        public static Position.Recalc P(ValueToken cost, ValueToken quantity, CurrencyKey currency)
        {
            return Position.Recalc.Create(cost, quantity);
        }

        public static Position.Recalc P(ValueToken cost, ValueToken profit, ValueToken quantity, CurrencyKey currency)
        {
            return Position.Recalc.Create(cost, profit, quantity);
        }

        private Deal Buy(decimal price, decimal quantity, CurrencyKey currency)
        {
            return Deal.Create("", (ushort)ValueTokenType.Buy, price * quantity, quantity);
        }

        public static Deal Sell(decimal price, decimal quantity, CurrencyKey currency)
        {
            return Deal.Create("", DealType.Sell, price * quantity, quantity);
        }

        public static Deal Income(decimal price, decimal quantity, CurrencyKey currency)
        {
            return Deal.Create("", DealType.Income, price * quantity, quantity);
        }

        public static Deal Outcome(decimal price, decimal quantity, CurrencyKey currency)
        {
            return Deal.Create("", DealType.Outcome, price * quantity, quantity);
        }

        private class PositionComparer : IEqualityComparer<HP>
        {
            public bool Equals(HP x, HP y)
            {
                return x.Timestamp == y.Timestamp 
                    && x.Data.Quantity == y.Data.Quantity 
                    && x.Data.Cost == y.Data.Cost 
                    && x.Data.Profit == y.Data.Profit;
            }

            public int GetHashCode(HP position)
            {
                return position.Timestamp;
            }
        }
    }
}
