using Azure.Core.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.Tracing;

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
                using var azureEventSourceListener = new AzureEventSourceListener((ea, m) => _logger.Log(ToLogLevel(ea.Level), m), EventLevel.Verbose);

                _logger.LogInformation("input Settings:{@options}", options);
                var perfTasks = _performanceTaskFactory.CreatePerformanceTasks(options);

                var tasks = new List<Task>();

                foreach (var perfTask in perfTasks)
                {
                    tasks.Add(perfTask.ExecuteAsync(cancellationToken));
                }
                
                await Task.WhenAll(tasks);
            }
            catch(TaskCanceledException) { }
        }

        private LogLevel ToLogLevel(EventLevel level) =>  
            level switch
            {
                EventLevel.Verbose => LogLevel.Trace,
                EventLevel.Warning => LogLevel.Warning,
                EventLevel.Informational => LogLevel.Information,
                EventLevel.Error => LogLevel.Error,
                EventLevel.Critical => LogLevel.Critical,
                _ => LogLevel.Trace,

            };
    }
}
