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
    public class BlockedStorageUnitTest
    {
        [TestMethod]
        public void BlockedStorageTestMethod1()
        {
            var blockStorage = new BlockedStorage<int>(10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(5, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");

            Assert.AreEqual(10, blockStorage[0], "");
            Assert.AreEqual(29, blockStorage[19], "");
            Assert.AreEqual(39, blockStorage[29], "");
            Assert.AreEqual(49, blockStorage[39], "");

            blockStorage[0] = blockStorage[0] + 100;
            blockStorage[19] = blockStorage[19] + 100;
            blockStorage[29] = blockStorage[29] + 100;
            blockStorage[39] = blockStorage[39] + 200;

            Assert.AreEqual(110, blockStorage[0], "");
            Assert.AreEqual(129, blockStorage[19], "");
            Assert.AreEqual(139, blockStorage[29], "");
            Assert.AreEqual(249, blockStorage[39], "");
        }

        [TestMethod]
        public void BlockedStorageTestMethod2()
        {
            var blockStorage = new BlockedStorage<int>(4);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(11, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");
        }

        [TestMethod]
        public void BlockedStorageTestMethod3()
        {
            var blockStorage = new BlockedStorage<int>(10);
            blockStorage.AddOrUpdate(Enumerable.Range(10, 40));
            Assert.AreEqual(5, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40)), "");

            blockStorage.AddOrUpdate(new[] { 5, 6, 7, 8, 9 });
            Assert.AreEqual(5, blockStorage.BlockCount, "");
            Assert.AreEqual(true, blockStorage.Items().SequenceEqual(Enumerable.Range(10, 40).Concat(new[] { 5, 6, 7, 8, 9 })), "");
        }
    }
}
