using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vtb.PosKeep.Common.Logging
{
    public class AsyncLogger : ILogger, IDisposable
    {
        /// <summary>
        /// Оставшееся место на диске в данный момент
        /// </summary>
        private long m_currentFreeDiscSpaceLeft;

        /// <summary>
        /// Настройка с минимальным допустимым остатком места на диске
        /// </summary>
        private readonly long m_minimunFreeDiscSpaceLeft;

        private DateTime _lastLowDiscSpaceAlert = DateTime.MaxValue;
        private DateTime _lastOverHeadQueueAlert = DateTime.MinValue;

        /// <summary>
        /// уровень логирования (All,Nothing,ErrorsOnly)
        /// </summary>
        private EventsLoggingLevels m_loggingLevel = EventsLoggingLevels.All;

        public EventsLoggingLevels LoggingLevel
        {
            get { return m_loggingLevel; }
            set { m_loggingLevel = value; }
        }

        public string FolderPath
        {
            get { return m_folderPath; }
            set
            {
                m_folderPath = value;
            }
        }

        public uint SizeLimit
        {
            get { return m_sizeLimit; }
            set
            {
                m_sizeLimit = value;
            }
        }

        public void CutAllStreams()
        {
            foreach (var fileName in _files.Keys)
            {
                if (_files.TryRemove(fileName, out var file))
                    file.Dispose();
            }
        }

        private ConcurrentDictionary<string, AsyncLogFile> _files =
            new ConcurrentDictionary<string, AsyncLogFile>();

        private bool InternalAdd(string eventText, Exception innerException, string fileName)
        {
            return InternalAdd(eventText, innerException, fileName, DateTime.Now);
        }

        private bool InternalAdd(string eventText, Exception innerException, string fileName, DateTime moment)
        {
            // отметаем запись в файлы если надо
            if (LoggingLevel == EventsLoggingLevels.Nothing ||
                (LoggingLevel == EventsLoggingLevels.ErrorsOnly && !(eventText != "critical")))
            {
                return false;
            }

            var file = _files.GetOrAdd(fileName, fn => new AsyncLogFile(
                AsyncLogFile.GetCurrentFileName(fn, m_folderPath)));

            var sb = new StringBuilder(Environment.NewLine, 50);
            
            sb.Append(string.Concat(
                moment.Year.ToString(), ".", moment.Month.ToString("00"), ".", moment.Day.ToString("00"), " ",
                moment.Hour.ToString("00"), ":", moment.Minute.ToString("00"), ":", moment.Second.ToString("00"), ".",
                moment.Millisecond.ToString("000"), "\t"));

            sb.Append(eventText);

            while (innerException != null)
            {
                sb.Append(" ");
                sb.Append(innerException);
                innerException = innerException.InnerException;
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            file.Write(bytes, (offset) => CheckOffset(offset, bytes, fileName));

            return true;
        }

        public bool CheckOffset(long offset, byte[] bytes, string fileName)
        {
            if (offset < m_sizeLimit)
                return true;

            AsyncLogFile file = _files.AddOrUpdate(fileName,
                (fn) => new AsyncLogFile(
                    AsyncLogFile.GetCurrentFileName(fn, m_folderPath)),
                (fn, fl) =>
                {
                    if (fl.Offset >= offset)
                    {
                        fl.Dispose();
                        fl = new AsyncLogFile(AsyncLogFile.GetCurrentFileName(fn, m_folderPath));
                    }
                    return fl;
                });

            file.Write(bytes);

            return false;
        }

        public bool AddInfo(string eventText, string fileName = "info")
        {
            return InternalAdd(eventText, null, fileName);
        }

        public bool AddError(string eventText, Exception innerException, string fileName = "error")
        {
            return InternalAdd(eventText, innerException, fileName);
        }

        public void Dispose()
        {
            CutAllStreams();
        }

        public AsyncLogger(AsyncLoggerConfigurationSection config)
        {
            m_folderPath = (string.IsNullOrEmpty(config.LogPath) || string.IsNullOrWhiteSpace(config.LogPath)) ?
                          Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Logs\" : config.LogPath;

            if (!Directory.Exists(m_folderPath))
            {
                Directory.CreateDirectory(m_folderPath);
            }

            m_loggingLevel = config.LoggingLevel;
            // если в конфиге указано сколько надо оставить на диске места то приводим,
            // а если нет то остается значение по умолчанию
            m_currentFreeDiscSpaceLeft = m_minimunFreeDiscSpaceLeft = config.LogDiscSpaceLeftMinimum;
            //var prms = new FreeSpaceCheckerThreadParams("DiscFreeSpaceLeftChecker_Thread")
            //{
            //	DriveLetter = _folderPath[0]
            //};
            //(new ThreadBase(CheckForDiscSpace, prms)).Start();
            //Task.Factory.StartNew(() => CheckForDiscSpace(_folderPath[0]));
            m_sizeLimit = config.LogSizeLimit;
        }

        private string m_folderPath;
        private uint m_sizeLimit;
    }
}
