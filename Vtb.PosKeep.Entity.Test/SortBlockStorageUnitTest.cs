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
    public class SortBlockStorageUnitTest
    {
        [TestMethod]
        public void SortBlockStorageTestMethod1()
        {
            var blockStorage = new SortBlockStorage<int>(10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod2()
        {
            var blockStorage = new SortBlockStorage<int>(4);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(10, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod3()
        {
            var blockStorage = new SortBlockStorage<int>(10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");

            blockStorage.AddOrUpdate(new[] { 5, 6, 7, 8, 9});
            Assert.AreEqual(5, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(5, 45)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod4()
        {
            var blockStorage = new SortBlockStorage<int>(MergeUtils.DistinctMerge, 10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");

            blockStorage.AddOrUpdate(new[] { 5, 6, 7, 8, 9, 10, 11 });
            Assert.AreEqual(5, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(5, 45)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod5()
        {
            var blockStorage = new SortBlockStorage<int>(MergeUtils.DistinctMerge, 10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 16).Concat(Enumerable.Range(30, 20)));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 16).Concat(Enumerable.Range(30, 20))), "");

            blockStorage.AddOrUpdate(Enumerable.Range(25, 5));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod6()
        {
            var blockStorage = new SortBlockStorage<int>(10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");

            blockStorage.AddOrUpdate(new[] { 50, 51, 52, 53, 54 });
            Assert.AreEqual(5, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 45)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod7()
        {
            var blockStorage = new SortBlockStorage<int>(MergeUtils.DistinctMerge, 10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");

            blockStorage.AddOrUpdate(Enumerable.Range(30, 10));
            Assert.AreEqual(4, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");
        }

        [TestMethod]
        public void SortBlockStorageTestMethod8()
        {
            var testArray = new short[] { 1, 2, 3, 4, 5, 7, 8, 9, 11, 12, 15, 16, 17, 18 };
            var blockStorage = new SortBlockStorage<short>(MergeUtils.DistinctMerge, 10);
            blockStorage.AddOrUpdate(testArray);

            Assert.AreEqual(2, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(testArray), "");

            Assert.AreEqual(true, blockStorage.Items((short)1, (short)6).SequenceEqual(new short[] { 1, 2, 3, 4, 5, } ), "");
            Assert.AreEqual(true, blockStorage.Items((short)0, (short)6).SequenceEqual(new short[] { 1, 2, 3, 4, 5, }), "");

            Assert.AreEqual(true, blockStorage.Items((short)10, (short)14).SequenceEqual(new short[] { 11, 12, }), "");
            Assert.AreEqual(true, blockStorage.Items((short)10, (short)14, -1).SequenceEqual(new short[] { 9, 11, 12, }), "");
        }
    }
}
