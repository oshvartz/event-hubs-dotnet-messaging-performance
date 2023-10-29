using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThroughputTest.ProcessorUtil;

namespace ThroughputTest
{
    public class PerformanceTaskFactory : IPerformanceTaskFactory
    {
        private readonly ILogger<SenderTask> _senderTaskLogger;
        private readonly ILogger<CustomProcessor> _processorLogger;

        public PerformanceTaskFactory(ILogger<SenderTask> senderTaskLogger, ILogger<CustomProcessor> processorLogger)
        {
            _senderTaskLogger = senderTaskLogger;
            _processorLogger = processorLogger;
        }

        public IEnumerable<PerformanceTask> CreatePerformanceTasks(CliOptions options)
        {
           if (options.SenderCount > 0)
            {
                yield return new SenderTask(options, _senderTaskLogger);
            }
           if(options.ConsumerGroups?.Any() == true)
            {
                yield return new ProcessorTask(options, _processorLogger);
            }
        }
    }
}
