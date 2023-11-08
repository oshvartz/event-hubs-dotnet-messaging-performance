using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThroughputTest.ProcessorUtil;

namespace ThroughputTest
{
    public class ProcessorTask : PerformanceTask
    {
        private IEnumerable<CustomProcessor> _customProcessors;

        public ProcessorTask(CliOptions options, ILogger<CustomProcessor> logger) : base(options)
        {
            var storageClient = new BlobContainerClient(options.StorageConnectionString, options.BlobContainerName);
            _customProcessors = options.ConsumerGroups.Select(consumerGroup =>
            new CustomProcessor(logger, storageClient,
                                options.RecieveMaxBatchSize,
                                consumerGroup,
                                options.ConnectionString,
                                options.EventHubName,
                                options.ProcessingTimeDurationMs,
                                //we want to masure latecy so we are reading from the last event
                                new Azure.Messaging.EventHubs.Primitives.EventProcessorOptions { DefaultStartingPosition = EventPosition.Earliest, MaximumWaitTime = TimeSpan.FromMinutes(100), PrefetchCount = options.PrefetchCount })).ToList();
        }

        public async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var processor in _customProcessors)
                {
                    tasks.Add(processor.StartProcessingAsync(cancellationToken));
                }

                await Task.WhenAll(tasks);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // This is expected if the cancellation token is
                // signaled.
            }
            finally
            {
                // Stopping may take up to the length of time defined
                // as the TryTimeout configured for the processor;
                // By default, this is 60 seconds.
                var tasks = new List<Task>();
                foreach (var processor in _customProcessors)
                {
                    tasks.Add(processor.StopProcessingAsync());
                }
                await Task.WhenAll(tasks);
            }
        }

    }
}
