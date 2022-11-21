using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vtb.PosKeep.ReportTransfer.FileMonitor
{
    public static class Comparer
    {
        public static IEnumerable<string> GetChanges(this string sourcePath, string destinationPath, string patternString)
        {
            var patterns = patternString.Split("|".ToCharArray(), 7, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pattern in patterns)
            {
                var files = new HashSet<string>(Directory.EnumerateFiles(destinationPath, pattern).Select(f => Path.GetFileName(f)));

                foreach (var file in Directory.EnumerateFiles(sourcePath, pattern))
                {
                    if (!files.Contains(Path.GetFileName(file)))
                        yield return file;
                }
            }
        }
    }
}
