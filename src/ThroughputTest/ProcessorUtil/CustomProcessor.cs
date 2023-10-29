using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace ThroughputTest.ProcessorUtil
{
    public class CustomProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
    {
        private static readonly Counter<int> _successProcessCounter;
        private static readonly Counter<int> _failedProcessCounter;
        private static readonly Histogram<long> _processDurationMs;
        private readonly ILogger<CustomProcessor> _logger;
        private readonly int _processingTimeDurationMs;
        private readonly Counter<int> _successConsumerProcessCounter;
        private readonly Counter<int> _failedConsumerProcessCounter;
        private readonly Histogram<long> _processConsumerDurationMs;
        private readonly CheckpointWriter _checkpointWriter;

        static CustomProcessor()
        {
            _successProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>("success-process-count",
           "MessageCount", "Total number of succesful process messages");
            _failedProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>("fail-process-count",
           "MessageCount", "Total number of failed process messages");
            _processDurationMs = AppMeterProvider.AppMeter.CreateHistogram<long>("process-duration", "ms", "process events duration in millisecond");
        }


        public CustomProcessor(
            ILogger<CustomProcessor> logger,
            BlobContainerClient storageClient,
            int eventBatchMaximumCount,
            string consumerGroup,
            string connectionString,
            string eventHubName,
            int processingTimeDurationMs,
            EventProcessorOptions clientOptions = default)
                : base(
                    new BlobCheckpointStore(storageClient),
                    eventBatchMaximumCount,
                    consumerGroup,
                    connectionString,
                    eventHubName,
                    clientOptions)
        {
            _logger = logger;
            _processingTimeDurationMs = processingTimeDurationMs;
            _successConsumerProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{consumerGroup}-success-process-count",
           "MessageCount", "Total number of succesful process messages");
            _failedConsumerProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{consumerGroup}-fail-process-count",
           "MessageCount", "Total number of failed process messages");
            _processConsumerDurationMs = AppMeterProvider.AppMeter.CreateHistogram<long>($"{consumerGroup}-process-duration", "ms", "process events duration in millisecond");
            //TODO: add to setting
            _checkpointWriter = new CheckpointWriter(TimeSpan.FromMinutes(2), 700, this.UpdateCheckpointAsync);
        }

        protected async override Task OnProcessingEventBatchAsync(
            IEnumerable<EventData> events,
            EventProcessorPartition partition,
            CancellationToken cancellationToken)
        {
            EventData lastEvent = null;

            try
            {
                var processingSw = Stopwatch.StartNew();
                var processTasks = new List<Task>();
                foreach (var currentEvent in events)
                {
                    processTasks.Add(ProcessEventAsync(events, cancellationToken));
                    lastEvent = currentEvent;
                }

                await Task.WhenAll(processTasks);

                _processDurationMs.Record(processingSw.ElapsedMilliseconds);
                _processConsumerDurationMs.Record(processingSw.ElapsedMilliseconds);
                _successProcessCounter.Add(events.Count());
                _successConsumerProcessCounter.Add(events.Count());

                if (lastEvent != null)
                {
                    await _checkpointWriter.WriteCheckpointIfNeededAsync(
                        partition.PartitionId,
                        lastEvent.Offset,
                        lastEvent.SequenceNumber,
                        cancellationToken)
                    .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions in
                // your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to take.
                // Any exceptions from your handlers go uncaught by the processor and
                // will NOT be redirected to the error handler.
                //
                // In this case, the partition processing task will fault and be restarted
                // from the last recorded checkpoint.
                _failedProcessCounter.Add(events.Count());
                _failedConsumerProcessCounter.Add(events.Count());
                _logger.LogError(ex, "Exception while processing events");
            }
        }

        private async Task ProcessEventAsync(IEnumerable<EventData> events,CancellationToken cancellationToken)
        {
            await Task.Delay(_processingTimeDurationMs, cancellationToken);
        }

        protected override Task OnProcessingErrorAsync(
            Exception exception,
            EventProcessorPartition partition,
            string operationDescription,
            CancellationToken cancellationToken)
        {
            try
            {
                if (partition != null)
                {
                    _logger.LogError(
                        $"Exception on partition {partition.PartitionId} while " +
                        $"performing {operationDescription}: {exception}");
                }
                else
                {
                    _logger.LogError(
                        $"Exception while performing {operationDescription}: {exception}");
                }
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions
                // in your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to
                // take.  Any exceptions from your handlers go uncaught by the
                // processor and will NOT be handled in any way.
                //
                // In this case, unhandled exceptions will not impact the processor
                // operation but will go unobserved, hiding potential application problems.

                _logger.LogError(ex,"Exception while processing events");
            }

            return Task.CompletedTask;
        }

        protected override Task OnInitializingPartitionAsync(
            EventProcessorPartition partition,
            CancellationToken cancellationToken)
        {
            try
            {
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions in
                // your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to take.
                // Any exceptions from your handlers go uncaught by the processor and
                // will NOT be redirected to the error handler.
                //
                // In this case, the partition processing task will fault and the
                // partition will be initialized again.

                _logger.LogError(ex, "Exception while initializing a partition");
            }

            return Task.CompletedTask;
        }

        protected override Task OnPartitionProcessingStoppedAsync(
            EventProcessorPartition partition,
            ProcessingStoppedReason reason,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    $"No longer processing partition {partition.PartitionId} " +
                    $"because {reason}");
            }
            catch (Exception ex)
            {
                // It is very important that you always guard against exceptions in
                // your handler code; the processor does not have enough
                // understanding of your code to determine the correct action to take.
                // Any exceptions from your handlers go uncaught by the processor and
                // will NOT be redirected to the error handler.
                //
                // In this case, unhandled exceptions will not impact the processor
                // operation but will go unobserved, hiding potential application problems.

                _logger.LogError(ex, "Exception while stopping processing for a partition");
            }

            return Task.CompletedTask;
        }
    }
}
