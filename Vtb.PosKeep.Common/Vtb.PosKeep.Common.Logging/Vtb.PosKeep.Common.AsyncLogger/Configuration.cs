using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vtb.PosKeep.Common.Logging
{
    public class AsyncLoggerConfigurationSection : ConfigurationSection
    {
        public const string SectionName = "asyncLog";

        public AsyncLoggerConfigurationSection() { }

        public const string LP = "dir";
        [ConfigurationProperty(LP, DefaultValue = null, IsRequired = false)]
        public string LogPath
        {
            get { return (string)this[LP]; }
            set { this[LP] = value; }
        }

        public const string LL = "level";
        [ConfigurationProperty(LL, DefaultValue = EventsLoggingLevels.All, IsRequired = false)]
        public EventsLoggingLevels LoggingLevel
        {
            get
            {
                return (EventsLoggingLevels)this[LL];
            }
            set { this[LL] = value; }
        }

        public const string SPM = "spaceMin";
        [ConfigurationProperty(SPM, DefaultValue = 1073741824U, IsRequired = false)]
        public uint LogDiscSpaceLeftMinimum
        {
            get
            {
                return (uint)this[SPM];
            }
            set { this[SPM] = value; }
        }

        public const string SL = "logSize";
        [ConfigurationProperty(SL, DefaultValue = 1073741824U, IsRequired = false)]
        public uint LogSizeLimit
        {
            get
            {
                return (uint)this[SL]; 
            }
            set { this[SL] = value; }
        }
    }
}
