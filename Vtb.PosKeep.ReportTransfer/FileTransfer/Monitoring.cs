using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vtb.PosKeep.ReportTransfer.FileProcessing
{
    public class Monitoring : IDisposable
    {

        public Monitoring(MonitoringConfigurationSection config)
        {
            m_config = config;
            m_timeout = Math.Max(1, Math.Min(m_config.Interval, 86400))*1000;
            m_terminateEvent = new ManualResetEvent(false);
        }

        public IEnumerable<string> Run()
        {
            do
            {
                foreach (var config in m_config.Monitoring)
                {
                    var monitor = new Monitor((MonitorConfigElement)config);
                    using (var resultCursor = monitor.Run().GetEnumerator())
                    {
                        if (resultCursor.MoveNext())
                        {
                            yield return "start processing";

                            do yield return resultCursor.Current; while (resultCursor.MoveNext());

                            yield return "finish processing";
                        }
                    }
                }
            }
            while (!m_terminateEvent.WaitOne(m_timeout));
        }

        public void Dispose()
        {
            m_terminateEvent.Set();
        }

        private int m_timeout;
        private ManualResetEvent m_terminateEvent;
        private MonitoringConfigurationSection m_config;
    }
}
