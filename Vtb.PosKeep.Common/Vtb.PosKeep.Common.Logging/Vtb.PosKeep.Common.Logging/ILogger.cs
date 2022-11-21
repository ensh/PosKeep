using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vtb.PosKeep.Common.Logging
{
    public enum EventsLoggingLevels
    {
        All,
        Nothing,
        ErrorsOnly
    }

    public interface ILogger
    {
            /// <summary>
            /// Каталог сохранения логов.
            /// </summary>
            string FolderPath { get; set; }

            /// <summary>
            /// уровень логирования (All,Nothing,ErrorsOnly)
            /// </summary>
            EventsLoggingLevels LoggingLevel { get; set; }

            /// <summary>
            /// Максимальный размер файла лога
            /// </summary>
            uint SizeLimit { get; set; }

            /// <summary>
            /// Закрыть все открытые файлы и начать новые части.
            /// </summary>
            void CutAllStreams();

            bool AddInfo(string eventText, string fileName = "info");
            bool AddError(string eventText, Exception innerException, string fileName = "error");
    }
}
