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

    using HP = HD<Position, PR>;
    using HR = HD<Rate, RR>;

    [TestClass]
    public class RateModelUnitTest
    {
        const int ClientID = 1;
        static readonly int Instrument1ID;
        static readonly int Instrument2ID;
        static readonly int RubCurrencyId = 1;
        static readonly int UsdCurrencyId = 2;
        const string RubCurrencyDCode = "810";
        const string UsdCurrencyDCode = "840";

        static RateModelUnitTest()
        {
            Account.Init(10);
            Currency.Init(10);
            Instrument.Init(10);
            Position.Init(100, 10);
            Rate.Init(1000);
            TradeAccount.Init(10);
            TradeInstrument.Init(10);

            new Account("AAA", "Иванов - акции");

            Instrument1ID = new Instrument(1, "VTBR", "ПАО Банк ВТБ");
            Instrument2ID = new Instrument(2, "GAZP", "ПАО Газпром");

            RubCurrencyId = new Currency(RubCurrencyDCode, "руб", "Россuийский рубль");
            UsdCurrencyId = new Currency(UsdCurrencyDCode, "долл", "Доллар США");
        }

        [TestMethod]
        public void RateModelTestMethod1()
        {
            const int period = 24 * 3600;
            var moment = new DateTime(2000, 01, 17).AddHours(10);

            var positions = new[]
            {
                new HP(moment.AddDays(25), Position.Create(10000.ToOutcome(), 100.ToBuy())),
                new HP(moment.AddDays(45), Position.Create(20000.ToOutcome(), 200.ToBuy())),
                new HP(moment.AddDays(65), Position.Create(30000.ToOutcome(), 300.ToBuy())),
            };

            var rates = Enumerable.Range(0, 74)
                .Select(i => new HR(moment.AddDays(i + 10).Date, Rate.Create(i * 10m)))
                .ToArray();

            RateModel.Context context = new RateModel.Context(new DateTime(2000, 01, 10),
                new DateTime(2000, 04, 10), period, ps => ps.AggregatePeriod(period));
            
            var pp = RateModel.RatedPositions(positions.OfToken(), rates.OfToken(), context).ToArray();
            
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && ((Position)pp[0].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && pp[0].Data == 160m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && ((Position)pp[19].Data).Cost.Value == 10000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && ((Position)pp[19].Data).Quantity.Value == 100, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && pp[19].Data == 160m + 190m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && ((Position)pp[20].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && ((Position)pp[20].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && pp[20].Data == 160m + 200m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && ((Position)pp[39].Data).Cost.Value == 20000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && ((Position)pp[39].Data).Quantity.Value == 200, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && pp[39].Data == 160m + 390m, "");

            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && ((Position)pp[40].Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && ((Position)pp[40].Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && pp[40].Data == 160m + 400m, "");

            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && ((Position)pp.Last().Data).Cost.Value == 30000, "");
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && ((Position)pp.Last().Data).Quantity.Value == 300, "");
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && pp.Last().Data == 160m + 570m, "");
        }
    }
}
