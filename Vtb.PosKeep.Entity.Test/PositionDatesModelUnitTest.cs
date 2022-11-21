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

    [TestClass]
    public class PositionDatesModelUnitTest
    {
        const int ClientID = 1;
        static readonly int Instrument1ID;
        static readonly int Instrument2ID;
        const int RubCurrencyId = 1;
        const int UsdCurrencyId = 2;
        const string RubCurrencyDCode = "810";
        const string UsdCurrencyDCode = "840";

        static PositionDatesModelUnitTest()
        {
            Account.Init(10);
            Currency.Init(10);
            Instrument.Init(10);
            Position.Init(100, 10);
            TradeAccount.Init(10);
            TradeInstrument.Init(10);

            new Account("AAA", "Иванов - акции");

            Instrument1ID = new Instrument(1, "VTBR", "ПАО Банк ВТБ");
            Instrument2ID = new Instrument(2, "GAZP", "ПАО Газпром");

            new Currency(RubCurrencyDCode, "руб", "Российский рубль");
            new Currency(UsdCurrencyDCode, "долл", "Доллар США");
        }

        [TestMethod]
        public void PositionDatesModelTestMethod1()
        {
            const int period = 24 * 3600;
            var moment = new DateTime(2000, 01, 17).AddHours(10);

            //Assert.AreEqual(true, Position.Empty == 10);

            var positions = new[]
            {
                // RubCurrencyDCode
                new HP(moment.AddDays(25), Position.Create(10000.ToOutcome(), 100.ToBuy())),
                new HP(moment.AddDays(45), Position.Create(20000.ToOutcome(), 200.ToBuy())),
                new HP(moment.AddDays(65), Position.Create(30000.ToOutcome(), 300.ToBuy())),
            };

            PositionDatesModel.Context context = new PositionDatesModel.Context(
                new DateTime(2000, 01, 10), 
                new DateTime(2000, 04, 10), period, ps => ps.AggregatePeriod(period));

            var pp = PositionDatesModel.GetPositionDates(positions, context).ToArray();

            Assert.AreEqual(true, Position.LastNumber == 3); 

            Assert.AreEqual(true, (Timestamp)moment.AddDays(26).Date == pp[0].Timestamp && pp[0].Data == Position.Empty + 1);
            Assert.AreEqual(true, (Timestamp)moment.AddDays(45).Date == pp[19].Timestamp && pp[19].Data == Position.Empty + 1);

            Assert.AreEqual(true, (Timestamp)moment.AddDays(46).Date == pp[20].Timestamp && pp[20].Data == Position.Empty + 2);
            Assert.AreEqual(true, (Timestamp)moment.AddDays(65).Date == pp[39].Timestamp && pp[39].Data == Position.Empty + 2);

            Assert.AreEqual(true, (Timestamp)moment.AddDays(66).Date == pp[40].Timestamp && pp[40].Data == Position.Empty + 3);
            Assert.AreEqual(true, (Timestamp)new DateTime(2000, 04, 9) == pp.Last().Timestamp && pp.Last().Data == Position.Empty + 3);
        }    
    }
}
