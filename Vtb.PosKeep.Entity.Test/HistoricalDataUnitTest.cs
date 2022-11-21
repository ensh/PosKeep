using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    [TestClass]
    public class HistoricalDataUnitTest
    {
        static HistoricalDataUnitTest()
        {
        }

        [TestMethod]
        public void AggregateHistoryTestMethod1()
        {
            var signal_template = new[]
            {
                1,1,1,1, -1,-1,-1,-1, 1,1,1,1, -1,-1,-1,-1, 1,1,1,1, -1,-1,-1,-1, 1,1,1,1,
            };

            var start_time = new DateTime(2017, 1, 17, 10, 0, 0);
            var signal1 = Enumerable.Range(0, signal_template.Length)
                .Select(i => new HD<int, HR>(start_time.AddMinutes(i), 50 * signal_template[i]));

            var signal2 = Enumerable.Range(0, signal_template.Length)
                .Select(i => new HD<int, HR>(start_time.AddMinutes(i), -50 * signal_template[i]));

            var beginTime = default(Timestamp);
            var sum = default(int);

            var result = signal1.Concat(signal2).OrderBy(s => s.Timestamp).AggregateHistory(
                item => item.Timestamp != beginTime,
                item => { beginTime = item.Timestamp; sum = item; },
                item => sum += item,
                () => new HD<int, HR>(beginTime, sum)).ToArray();

            Assert.AreEqual(true, result.SequenceEqual(Enumerable.Range(0, signal_template.Length)
                .Select(i => new HD<int, HR>(start_time.AddMinutes(i), 0))
                .Prepend(new HD<int, HR>(start_time, 50))));

            beginTime = default(Timestamp);
            sum = default(int);

            result = (new[] { signal1, signal2 }).AggregateHistory(
                item => item.Timestamp != beginTime,
                item => { beginTime = item.Timestamp; sum = item.Data.Value; },
                item => sum += item.Data.Value,
                () => new HD<int, HR>(beginTime, sum)).ToArray();

            Assert.AreEqual(true, result.SequenceEqual(Enumerable.Range(0, signal_template.Length)
                .Select(i => new HD<int, HR>(start_time.AddMinutes(i), 0))
                .Prepend(new HD<int, HR>(start_time, 50))));

            result = (new[] { signal1, signal2 }).AggregateHistory(
                item => { beginTime = item.Timestamp; sum = item.Data.Value; },
                item => sum += item.Data.Value,
                t => new HD<int, HR>(t, sum), false).ToArray();

            Assert.AreEqual(true, result.SequenceEqual(Enumerable.Range(0, signal_template.Length)
                .Select(i => new HD<int, HR>(start_time.AddMinutes(i), 0))
                .Prepend(new HD<int, HR>(start_time, 50))));

            result = (new[] { signal1, signal2 }).AggregateHistory(
                item => { beginTime = item.Timestamp; sum = item.Data.Value; },
                item => sum += item.Data.Value,
                t => new HD<int, HR>(t, sum)).ToArray();

            Assert.AreEqual(true, result.SequenceEqual(Enumerable.Range(0, signal_template.Length)
                .Select(i => new HD<int, HR>(start_time.AddMinutes(i), 0))));
        }
    }
}
