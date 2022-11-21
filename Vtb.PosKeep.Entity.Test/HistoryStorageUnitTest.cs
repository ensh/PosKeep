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
    using Vtb.PosKeep.Entity.Storage;

    using HD = HD<int, HistoryStorageUnitTest.TestReference>;

    [TestClass]
    public class HistoryStorageUnitTest
    {
        public abstract class TestReference : HR { }

        static readonly IBlockStorageFactory<HD> factory = new HistoryStorageFactory();

        class HistoryStorageFactory : IBlockStorageFactory<HD>
        {
            public SortBlockStorage<HD> Create()
            {
                int blockSize = 100, blockCount = 10;
                return new SortBlockStorage<HD>(MergeUtils.DistinctMerge, blockSize, blockCount);
            }

            public SortBlockStorage<HD<int, TestReference>> Create(int blockSize, int blockCount)
            {
                return Create();
            }
        }

        [TestMethod]
        public void HistoryStorageTestMethod1()
        {
            var history = new HistoryStorage<int, int, TestReference>(factory);

            history.AddRange(1, new[]
            {
                new HD(new DateTime(2019,09,06), 6),
                new HD(new DateTime(2019,09,07), 7),
                new HD(new DateTime(2019,09,08), 8),
                new HD(new DateTime(2019,09,09), 9),
                new HD(new DateTime(2019,09,10), 10),
            });

            history.Add(1, new HD(new DateTime(2019, 09, 11), 11));

            history.AddRange(1, new[]
            {
                new HD(new DateTime(2019,09,12), 12),
                new HD(new DateTime(2019,09,13), 13),
            });

            history.AddRange(21, new[]
            {
                new HD(new DateTime(2019,09,06), 26),
                new HD(new DateTime(2019,09,07), 27),
                new HD(new DateTime(2019,09,08), 28),
                new HD(new DateTime(2019,09,09), 29),
                new HD(new DateTime(2019,09,10), 30),
            });

            history.Add(31, new HD(new DateTime(2019, 09, 11), 11));

            Assert.AreEqual(true, history.Items(1).Select(i => i.Data).SequenceEqual(Enumerable.Range(6, 8)), "");
            Assert.AreEqual(true, history.Items(1).Select(i => i.Timestamp)
                .SequenceEqual(Enumerable.Range(6, 8).Select(i => (Timestamp)new DateTime(2019, 09, i))), "");

            Assert.AreEqual(true, history.Items(21).Select(i => i.Data).SequenceEqual(Enumerable.Range(26, 5)), "");
            Assert.AreEqual(true, history.Items(21).Select(i => i.Timestamp)
                .SequenceEqual(Enumerable.Range(6, 5).Select(i => (Timestamp)new DateTime(2019, 09, i))), "");

            Assert.AreEqual(true, history.Items(31).Select(i => i.Data).SequenceEqual(Enumerable.Range(11, 1)), "");
            Assert.AreEqual(true, history.Items(31).Select(i => i.Timestamp)
                .SequenceEqual(Enumerable.Range(11, 1).Select(i => (Timestamp)new DateTime(2019, 09, i))), "");
        }

        [TestMethod]
        public void HistoryStorageTestMethod2()
        {
            var history = new HistoryStorage<int, int, TestReference>(factory);

            history.AddRange(1, new[]
            {
                new HD(new DateTime(2019,09,06), 6),
                new HD(new DateTime(2019,09,07), 7),
                new HD(new DateTime(2019,09,08), 8),
                new HD(new DateTime(2019,09,09), 9),
                new HD(new DateTime(2019,09,10), 10),
            });

            history.Add(1, new HD(new DateTime(2019, 09, 11), 11));

            history.AddRange(1, new[]
            {
                new HD(new DateTime(2019,09,12), 12),
                new HD(new DateTime(2019,09,13), 13),
            });

            Assert.AreEqual(true, history.Items(1, new DateTime(2019, 09, 01), DateTime.MaxValue)
                .Select(i => i.Data).SequenceEqual(Enumerable.Range(6,8)), "");
            Assert.AreEqual(true, history.Items(1, new DateTime(2019, 09, 01), DateTime.MaxValue)
                .Select(i => i.Timestamp)
                .SequenceEqual(Enumerable.Range(6,8).Select(i => (Timestamp)new DateTime(2019, 09, i))), "");

            Assert.AreEqual(true, history.Items(1, new DateTime(2019, 09, 08), DateTime.MaxValue)
                .Select(i => i.Data).SequenceEqual(Enumerable.Range(8, 6)), "");
            Assert.AreEqual(true, history.Items(1, new DateTime(2019, 09, 08), DateTime.MaxValue)
                .Select(i => i.Timestamp)
                .SequenceEqual(Enumerable.Range(8, 6).Select(i => (Timestamp)new DateTime(2019, 09, i))), "");

            Assert.AreEqual(true, history.Items(1, new DateTime(2019, 09, 08), new DateTime(2019, 09, 10))
                .Select(i => i.Data).SequenceEqual(Enumerable.Range(8, 2)), "");
            Assert.AreEqual(true, history.Items(1, new DateTime(2019, 09, 08), new DateTime(2019, 09, 10))
                .Select(i => i.Timestamp)
                .SequenceEqual(Enumerable.Range(8, 2).Select(i => (Timestamp)new DateTime(2019, 09, i))), "");
        }
    }
}
