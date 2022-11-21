using Microsoft.Extensions.Configuration;
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
        /// Настройка с минимальным допустимым остатком места на диске
        /// </summary>
        public uint FreeDiscSpace { get; protected set; }

        /// <summary>
        /// уровень логирования (All,Nothing,ErrorsOnly)
        /// </summary>
        public EventsLoggingLevels LoggingLevel { get; set; }

        public string FolderPath { get; set; }

        public uint SizeLimit { get; set; }

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
                AsyncLogFile.GetCurrentFileName(fn, FolderPath)));

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
            if (offset < SizeLimit)
                return true;

            AsyncLogFile file = _files.AddOrUpdate(fileName,
                (fn) => new AsyncLogFile(
                    AsyncLogFile.GetCurrentFileName(fn, FolderPath)),
                (fn, fl) =>
                {
                    if (fl.Offset >= offset)
                    {
                        fl.Dispose();
                        fl = new AsyncLogFile(AsyncLogFile.GetCurrentFileName(fn, FolderPath));
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

        public const string LogPath = "AsyncLogger:path";
        public const string Level = "AsyncLogger:level";
        public const string DiskSpace = "AsyncLogger:diskspace";
        public const string LogSize = "AsyncLogger:size";

        public AsyncLogger(IConfiguration config)
        {
            FolderPath = (string.IsNullOrEmpty(config[LogPath]) || string.IsNullOrWhiteSpace(config[LogPath])) ?
                          Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Logs\" : config[LogPath];

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            LoggingLevel = Enum.TryParse<EventsLoggingLevels>(config[Level]??"All", out var level) ? level : EventsLoggingLevels.All;
            FreeDiscSpace = uint.TryParse(config[DiskSpace]?? "", out var value) ? value : uint.MaxValue;
            SizeLimit = uint.TryParse(config[LogSize]?? "", out value) ? value : 10_000_000;
        }
    }
}
