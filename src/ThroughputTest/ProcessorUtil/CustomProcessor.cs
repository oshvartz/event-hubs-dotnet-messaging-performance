﻿using Azure.Messaging.EventHubs.Primitives;
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
using Microsoft.Extensions.Azure;

namespace ThroughputTest.ProcessorUtil
{
    public class CustomProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
    {
        private readonly ILogger<CustomProcessor> _logger;
        private readonly int _processingTimeDurationMs;
        private readonly Counter<int> _successConsumerProcessCounter;
        private readonly Counter<int> _maxBatchProcessCounter;
        private readonly Counter<int> _failedConsumerProcessCounter;
        private readonly Histogram<long> _processConsumerDurationMs;
        private readonly Histogram<double> _totalTimeInQueueDurationMs;
        private readonly Histogram<int> _RecievedBatchSize;
        private readonly CheckpointWriter _checkpointWriter;
        private readonly int _eventBatchMaximumCount;

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
            _successConsumerProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{eventHubName}-{consumerGroup}-success-process-count",
           "MessageCount", "Total number of succesful process messages");
            _maxBatchProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{eventHubName}-{consumerGroup}-max-batch-process-count",
                "MessageCount", "Total number of processor that got max messages in batch");
            _failedConsumerProcessCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{eventHubName}-{consumerGroup}-fail-process-count",
           "MessageCount", "Total number of failed process messages");
            _processConsumerDurationMs = AppMeterProvider.AppMeter.CreateHistogram<long>($"{eventHubName}-{consumerGroup}-process-duration", "ms", "process events duration in millisecond");
            _totalTimeInQueueDurationMs = AppMeterProvider.AppMeter.CreateHistogram<double>($"{eventHubName}-{consumerGroup}-timeinqueue", "ms", "time in queue in millisecond");
            _RecievedBatchSize = AppMeterProvider.AppMeter.CreateHistogram<int>($"{eventHubName}-{consumerGroup}-recive-batchsize", "MessageCount", "Recieved batch size");
            //TODO: add to setting
            _checkpointWriter = new CheckpointWriter(TimeSpan.FromMinutes(2), 700, this.UpdateCheckpointAsync);
            _eventBatchMaximumCount = eventBatchMaximumCount;
        }

        protected async override Task OnProcessingEventBatchAsync(
            IEnumerable<EventData> events,
            EventProcessorPartition partition,
            CancellationToken cancellationToken)
        {
            EventData lastEvent = null;

            try
            {
                if(events?.Any() == false) 
                {
                    _logger.LogInformation("Empty Batch");
                    return;
                }
                var processingSw = Stopwatch.StartNew();
                var processTasks = new List<Task>();
                foreach (var currentEvent in events)
                {
                    _totalTimeInQueueDurationMs.Record((DateTimeOffset.Now - currentEvent.EnqueuedTime).TotalMilliseconds);
                    processTasks.Add(ProcessEventAsync(events, cancellationToken));
                    lastEvent = currentEvent;
                }

                await Task.WhenAll(processTasks);

                _processConsumerDurationMs.Record(processingSw.ElapsedMilliseconds);
                _successConsumerProcessCounter.Add(events.Count());
                _RecievedBatchSize.Record(events.Count());
                if(_eventBatchMaximumCount == events.Count())
                {
                    _maxBatchProcessCounter.Add(1);
                }

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
