using CommandLine;
using Microsoft.Extensions.Logging;

namespace ThroughputTest
{
    public class CliRunner
    {
        private readonly ILogger<CliRunner> _logger;
       
        public CliRunner(ILogger<CliRunner> logger)
        {
            _logger = logger;
        }

        public async Task RunCliAsync(CliOptions options)
        {
            await Task.Delay(10);
        }
    }

    public class CliOptions
    {
        [Option('C', "connection-string", Required = true, HelpText = "namesapce connection string")]
        public string ConnectionString { get; set; }

    }
}
