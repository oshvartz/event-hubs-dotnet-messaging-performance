using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputTest
{
    public abstract class PerformanceTask
    {
        protected readonly CliOptions cliOptions;

        public PerformanceTask(CliOptions cliOptions)
        {
            this.cliOptions = cliOptions;
        }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
