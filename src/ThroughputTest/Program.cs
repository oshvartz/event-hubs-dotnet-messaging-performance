//setup our DI
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThroughputTest;

var serviceCollection = new ServiceCollection();
serviceCollection
        .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "[hh:mm:ss] ";
        }))
    .AddSingleton<CliRunner>()
    .AddSingleton<IPerformanceTaskFactory, PerformanceTaskFactory>();
    
var serviceProvider = serviceCollection.BuildServiceProvider();

// Add this to your C# console app's Main method to give yourself
// a CancellationToken that is canceled when the user hits Ctrl+C.
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Canceling...");
    cts.Cancel();
    e.Cancel = true;
};


var cliRunner = serviceProvider.GetService<CliRunner>();

await Parser.Default.ParseArguments<CliOptions>(args)
                  .MapResult(async
                  o =>
                  {
                      await cliRunner.RunCliAsync(o, cts.Token);
                  },
                 errors => Task.FromResult(0)
      );