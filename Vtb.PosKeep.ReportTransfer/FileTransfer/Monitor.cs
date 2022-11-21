using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vtb.PosKeep.ReportTransfer.FileProcessing
{
    public class Monitor
    {
        public const string ARCH = "arch";
        public const string SOURCE = "source";

        public Monitor(MonitorConfigElement config)
        {
            m_config = config;
        }

        public IEnumerable<string> Run()
        {
            var fileNames = GetArchFileNames(GetFileNames())
                .Select(f => new KeyValuePair<string, string>(Path.GetFileName(f), f))
                .ToArray();

            // обработка архивных файлов
            foreach (var config in m_config.Processors)
            {
                var processor = new Processor((ProcessorConfigElement)config);
                foreach (var result in processor.Run(fileNames))
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Сравнение файлов в каталогах
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetFileNames()
        {
            var archDirectory = m_config.DirCollection[ARCH].Value;
            var sourceDirectory = m_config.DirCollection[SOURCE].Value;

            foreach (var pattern in m_config.PatternCollection.Cast<StringElement>())
            {                
                var fileNames = new HashSet<string>(Directory.EnumerateFiles(archDirectory, pattern.Value).Select(f => Path.GetFileName(f)));

                foreach (var fileName in Directory.EnumerateFiles(sourceDirectory, pattern.Value).Select(f => Path.GetFileName(f)))
                {
                    if (!fileNames.Contains(fileName))
                        yield return fileName;
                }
            }
        }

        /// <summary>
        /// Копирование новых файлов в архив
        /// </summary>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        private IEnumerable<string> GetArchFileNames(IEnumerable<string> fileNames)
        {
            var archDirectory = m_config.DirCollection[ARCH].Value;
            var sourceDirectory = m_config.DirCollection[SOURCE].Value;

            foreach (var fileName in fileNames)
            {
                var copyFileName = Path.Combine(archDirectory, fileName);
                File.Copy(Path.Combine(sourceDirectory, fileName), copyFileName);

                yield return copyFileName;
            }
        }

        private MonitorConfigElement m_config;
    }
}
