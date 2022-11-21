using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Vtb.PosKeep.Entity.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Vtb.PosKeep.Entity;

    [TestClass]
    public class TimestampUnitTest
    {
        [TestMethod]
        public void TimestampTestMethod1()
        {
            int seconds = 12345;

            Assert.AreEqual(seconds, TimestampUtils.BaseTimestamp.AddSeconds(seconds).SecondsFromDateTime(), "TimestampUtils SecondsFromDateTime");
            Assert.AreEqual(TimestampUtils.BaseTimestamp.AddSeconds(seconds), seconds.DateTimeFromSeconds(), "TimestampUtils DateTimeFromSeconds");

            Assert.AreEqual(TimestampUtils.BaseTimestamp.AddSeconds(seconds).AddSeconds(-seconds % 120).AddMinutes(2),
                ((Timestamp)seconds).Upper(120).GetHashCode().DateTimeFromSeconds(), "TimestampUtils Upper");

            Assert.AreEqual(TimestampUtils.BaseTimestamp.AddSeconds(seconds - seconds % 120),
                ((Timestamp)seconds).Down(120).GetHashCode().DateTimeFromSeconds(), "TimestampUtils Down");

            Assert.AreEqual(120, (Timestamp)(seconds + 120) - (Timestamp)seconds, "Timestamp - operator");

            Assert.AreEqual(true, (Timestamp)(seconds + 120) > (Timestamp)seconds, "Timestamp '>' operator");
            Assert.AreEqual(false, (Timestamp)seconds > (Timestamp)(seconds + 12), "Timestamp '>' operator");
            Assert.AreEqual(false, (Timestamp)seconds > (Timestamp)seconds, "Timestamp '>' operator");

            Assert.AreEqual(true, (Timestamp)seconds < (Timestamp)(seconds + 12), "Timestamp '<' operator");
            Assert.AreEqual(false, (Timestamp)(seconds + 120) < (Timestamp)seconds, "Timestamp '<' operator");
            Assert.AreEqual(false, (Timestamp)seconds < (Timestamp)seconds, "Timestamp '<' operator");

            Assert.AreEqual(true, (Timestamp)(seconds + 120) >= (Timestamp)seconds, "Timestamp '>=' operator");
            Assert.AreEqual(false, (Timestamp)seconds >= (Timestamp)(seconds + 12), "Timestamp '>=' operator");
            Assert.AreEqual(true, (Timestamp)seconds >= (Timestamp)seconds, "Timestamp '>=' operator");

            Assert.AreEqual(true, (Timestamp)seconds <= (Timestamp)(seconds + 12), "Timestamp '<=' operator");
            Assert.AreEqual(false, (Timestamp)(seconds + 120) <= (Timestamp)seconds, "Timestamp '<=' operator");
            Assert.AreEqual(true, (Timestamp)seconds <= (Timestamp)seconds, "Timestamp '<=' operator");

            Assert.AreEqual(true, (Timestamp)seconds == (Timestamp)seconds, "Timestamp == operator");
            Assert.AreEqual(false, (Timestamp)(1 + seconds) == (Timestamp)seconds, "Timestamp == operator");
            Assert.AreEqual(false, (Timestamp)seconds == (Timestamp)(1 + seconds), "Timestamp == operator");

            Assert.AreEqual(false, (Timestamp)seconds != (Timestamp)seconds, "Timestamp != operator");
            Assert.AreEqual(true, (Timestamp)(1 + seconds) != (Timestamp)seconds, "Timestamp != operator");
            Assert.AreEqual(true, (Timestamp)seconds != (Timestamp)(1 + seconds), "Timestamp != operator");

            Assert.AreEqual(seconds, ((Timestamp)seconds).GetHashCode(), "Timestamp GetHashCode");

            Assert.AreEqual(true, ((Timestamp)seconds).Equals(seconds), "Timestamp Equals");
            Assert.AreEqual(false, ((Timestamp)seconds).Equals(1 + seconds), "Timestamp Equals");
            Assert.AreEqual(false, ((Timestamp)seconds + 1).Equals(seconds), "Timestamp Equals");
        }
    }
}
