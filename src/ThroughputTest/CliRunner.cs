using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ThroughputTest
{
    public class CliRunner
    {
        private readonly ILogger<CliRunner> _logger;
        private readonly IPerformanceTaskFactory _performanceTaskFactory;

        public CliRunner(ILogger<CliRunner> logger, IPerformanceTaskFactory performanceTaskFactory)
        {
            _logger = logger;
            _performanceTaskFactory = performanceTaskFactory;
        }

        public async Task RunCliAsync(CliOptions options, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("input Settings:{@options}", options);
                var perfTasks = _performanceTaskFactory.CreatePerformanceTasks(options);

                var tasks = new List<Task>();

                foreach (var perfTask in perfTasks)
                {
                    tasks.Add(perfTask.ExecuteAsync(cancellationToken));
                }
                
                await Task.WhenAll(tasks);

                await Task.Delay(50000, cancellationToken);
            }
            catch(TaskCanceledException) { }
        }
    }
}
