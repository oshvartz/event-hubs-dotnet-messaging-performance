using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputTest
{
    public abstract class PerformanceTask
    {
        protected readonly CliOptions _options;

        public PerformanceTask(CliOptions options)
        {
            _options = options;
        }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
