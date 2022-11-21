using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vtb.PosKeep.ReportTransfer.FileProcessing
{
    public class Processor
    {
        public const string DESTINATION = "destination";
        public const string DAY_FOLDER = "dayFolder";
        public const string OVERWRITE = "overwrite";

        public Processor(ProcessorConfigElement config)
        {
            m_config = config;
        }

        public IEnumerable<string> Run(IEnumerable<KeyValuePair<string, string>> fileNames)
        {
            var destinationDirectory = m_config.DirCollection[DESTINATION]?.Value;
            if (string.IsNullOrEmpty(destinationDirectory))
            {
                // Не задан целевой каталог
                throw new ArgumentException("destinationDirectory value is empty");
            }

            if (!Directory.Exists(destinationDirectory))
            {
                // Отсутствут целевой каталог
                throw new DirectoryNotFoundException(string.Concat("destinationDirectory=[", destinationDirectory,"]"));
            }

            if (m_config.FlagsCollection[DAY_FOLDER]?.Value ?? false)
            {
                destinationDirectory = CreateDayFolder(destinationDirectory);
            }

            var patterns = m_config.PatternCollection.Cast<StringElement>().Select(p => p.Value).ToArray();

            if (patterns.Length == 0)
            {
                // Не заданы маски файлов
                throw new ArgumentException("patterns mast defined");
            }

            var overwrite = m_config.FlagsCollection[OVERWRITE]?.Value ?? true;
            var baseFileName = m_config.FileName ?? "";
            var baseFileNamePattern = baseFileName + "*.zip";

            foreach (var fileName in fileNames.Where(f => f.Key.CheckByPattern(baseFileNamePattern)).GetLastFileName(baseFileName))
            {
                yield return fileName;
                foreach (var result in fileName.Process(destinationDirectory, patterns, overwrite))
                {
                    yield return result;
                }
            }
        }

        private string CreateDayFolder(string destinationDirectory)
        {
            var dt = DateTime.Now;
            var dayFolderName = dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00");
            destinationDirectory = Path.Combine(destinationDirectory, dayFolderName);

            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            return destinationDirectory;
        }

        private ProcessorConfigElement m_config;
    }

    static class ProcessorUtils
    {
        public static IEnumerable<string> GetLastFileName(this IEnumerable<KeyValuePair<string, string>> fileNames, string baseFileName)
        {
            var baseFileNameLength = baseFileName.Length;

            if (baseFileNameLength > 0)
            {
                // группируем по шаблону FOYYYYMMDD
                foreach (var fs in fileNames.GroupBy(fileName => fileName.Key.Substring(0, baseFileNameLength)))
                {
                    // большее имя файла FOYYYYMMDD или FOYYYYMMDD_Z
                    yield return fs.Max(f => f.Value);
                }
            }
            else
            {
                foreach (var fileName in fileNames)
                    yield return fileName.Value;
            }
        }

        public static IEnumerable<string> Process(this string fileName, string destinationDirectory, string[] patterns, bool overwrite)
        {
            using (var zip = ZipFile.OpenRead(fileName))
            {
                foreach (var entry in zip.Entries.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.CheckByPattern(patterns)))
                {
                    var outputFileName = Path.Combine(destinationDirectory, entry.Name);

                    if (overwrite || !File.Exists(outputFileName))
                    {
                        using (var file = new FileStream(outputFileName, FileMode.Create))
                        {
                            entry.Open().CopyToAsync(file);

                            yield return outputFileName;
                        }
                    }
                }
            }
        }
    }
}
