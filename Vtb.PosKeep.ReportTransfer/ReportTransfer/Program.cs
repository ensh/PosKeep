using System;
using System.Configuration;
using System.IO;

using Vtb.PosKeep.Common.Logging;
using Vtb.PosKeep.ReportTransfer.FileProcessing;

namespace Vtb.PosKeep.ReportTransfer
{
    class Program
    {
        static void Main(string[] args)
        {

            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            using (var logger = new AsyncLogger((AsyncLoggerConfigurationSection)configFile.GetSection(AsyncLoggerConfigurationSection.SectionName)))
            {
                using (var monitoring = new Monitoring((MonitoringConfigurationSection)configFile.GetSection(MonitoringConfigurationSection.SectionName)))
                {
                    logger.AddInfo("start");

                    try
                    {
                        foreach (var result in monitoring.Run())
                        {
                            logger.AddInfo(result);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.AddError("critical", e);
                    }

                    logger.AddInfo("finish");
                }
            }
        }
    }
}
