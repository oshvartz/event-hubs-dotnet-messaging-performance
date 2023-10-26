using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputTest
{
    public class PerformanceTaskFactory : IPerformanceTaskFactory
    {
        private readonly ILogger<SenderTask> _senderTaskLogger;

        public PerformanceTaskFactory(ILogger<SenderTask> senderTaskLogger)
        {
            _senderTaskLogger = senderTaskLogger;
        }

        public IEnumerable<PerformanceTask> CreatePerformanceTasks(CliOptions options)
        {
            if (options.SenderCount > 0)
            {
                yield return new SenderTask(options, _senderTaskLogger);
            }
        }
    }
}
