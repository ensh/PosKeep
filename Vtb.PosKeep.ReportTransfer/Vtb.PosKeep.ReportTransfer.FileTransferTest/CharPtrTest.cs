using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Vtb.PosKeep.ReportTransfer.FileProcessing;

namespace Vtb.PosKeep.ReportTransfer.FileTransferTest
{
    [TestClass]
    public class CharPtrTest
    {
        [TestMethod]
        public void charPtrTestMethod()
        {
            Assert.IsTrue("".CheckByPattern("*"), "_*");
            Assert.IsTrue("aaaaa".CheckByPattern("*"), "aaaaa_*");
            Assert.IsTrue("aaaaa".CheckByPattern("aaaaa"), "aaaaa_aaaaa");
            Assert.IsTrue("aaaaa.aaa".CheckByPattern("aaaaa.aaa"), "aaaaa.aaa_aaaaa.aaa");
            Assert.IsTrue("aaaaa".CheckByPattern("?????"), "aaaaa_?????");
            Assert.IsTrue("aaaaa".CheckByPattern("aaaa?"), "aaaaa_aaaa?");
            Assert.IsTrue("aaaaa".CheckByPattern("?aaa?"), "aaaaa_?aaa?");
            Assert.IsTrue("aaaaa".CheckByPattern("?a?a?"), "aaaaa_?a?a?");
            Assert.IsTrue("aaaaa".CheckByPattern("?*?"), "aaaaa_?*?");
            Assert.IsFalse("aaaaa".CheckByPattern("bbbbb"), "aaaaa_bbbbb");
        }

        /*
        [TestMethod]
        public void ClassificatorTestMethod()
        {
            var fileNames = new[] { "FO12345678", "FO12345678_1", "FO12345678_2", "F12345678" };
            using (var typeEnumerator = fileNames.Classificator().GetEnumerator())
            {
                Assert.IsTrue(typeEnumerator.MoveNext(), "start");

                Assert.IsTrue(typeEnumerator.Current.Key == FileProcessing.FileProcessing.FileType.FO, "fo");
                var i = 0;
                foreach (var fileName in typeEnumerator.Current)
                {
                    Assert.IsTrue(fileName == fileNames[i++], "fo_fileName" + i.ToString());
                }

                Assert.IsTrue(typeEnumerator.MoveNext(), "next");

                Assert.IsTrue(typeEnumerator.Current.Key == FileProcessing.FileProcessing.FileType.Unknown, "unknown");
                Assert.IsTrue(typeEnumerator.Current.First() == fileNames[i++], "unknown_fileName" + i.ToString());
            }
        }

        [TestMethod]
        public void FoProcessorTestMethod()
        {
            var fileNames = new[] { "FO20200202", "FO12345678", "FO12345678_1", "FO12345678_2", "F12345678" };
            using (var typeEnumerator = fileNames.Classificator().GetEnumerator())
            {
                Assert.IsTrue(typeEnumerator.MoveNext(), "start");
                Assert.IsTrue(typeEnumerator.Current.Key == FileProcessing.FileProcessing.FileType.FO, "fo");

                using (var resultEnumerator = typeEnumerator.Current.FoProcessor(new FoParams(), f => new[] { f }).GetEnumerator())
                {
                    Assert.IsTrue(resultEnumerator.MoveNext(), "result_next");
                    Assert.IsTrue(resultEnumerator.Current == fileNames[0], "result_0");

                    Assert.IsTrue(resultEnumerator.MoveNext(), "result_next");
                    Assert.IsTrue(resultEnumerator.Current == fileNames[3], "result_3");
                }

                Assert.IsTrue(typeEnumerator.MoveNext(), "next");
                Assert.IsTrue(typeEnumerator.Current.Key == FileProcessing.FileProcessing.FileType.Unknown, "unknown");

                using (var resultEnumerator = typeEnumerator.Current.GetEnumerator())
                {
                    Assert.IsTrue(resultEnumerator.MoveNext(), "result_next");
                    Assert.IsTrue(resultEnumerator.Current == fileNames[4], "result_4");
                }
            }
        }
        */
    }
}
