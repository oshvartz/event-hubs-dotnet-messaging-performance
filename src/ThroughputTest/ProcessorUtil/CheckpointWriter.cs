using Azure.Messaging.EventHubs.Processor;
using Microsoft.Azure.Amqp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThroughputTest.ProcessorUtil
{
    public class CheckpointWriter
    {
        private static readonly Counter<int> _checkpointWriteCounter;
        private static readonly Histogram<long> _checkpointWriteDurationMs;

        // Checkpoint writing fields
        private readonly TimeSpan _checkpointStopwatchWaitingInterval;
        private readonly int _messagesCountCheckpointThreshold;
        private readonly Func<string, long, long?, CancellationToken, Task> _updateCheckPointTask;
        private readonly ConcurrentDictionary<string, PartitionCheckpointCounters> _partitionsCheckpointCounters;

        static CheckpointWriter()
        {
            _checkpointWriteCounter = AppMeterProvider.AppMeter.CreateCounter<int>("checkpoint-write-count",
           "WriteCount", "Total number of checkpoint write");

            _checkpointWriteDurationMs = AppMeterProvider.AppMeter.CreateHistogram<long>("checkpoint-write-duration", "ms", "checkpoint write duration in millisecond");
        }

        public CheckpointWriter(TimeSpan checkpointStopwatchWaitingInterval, int messagesCountCheckpointThreshold,Func<string,long,long?,CancellationToken,Task> updateCheckPointTask)
        {
            _checkpointStopwatchWaitingInterval = checkpointStopwatchWaitingInterval;
            _messagesCountCheckpointThreshold = messagesCountCheckpointThreshold;
            _updateCheckPointTask = updateCheckPointTask;
            // PartitionId -> PartitionCheckpointCounters
            _partitionsCheckpointCounters = new ConcurrentDictionary<string, PartitionCheckpointCounters>();
        }

        public async Task WriteCheckpointIfNeededAsync(string partitionId, long offset, long? sequenceNumber, CancellationToken cancellationToken)
        {
            var partitionCheckpointCounters = _partitionsCheckpointCounters.GetOrAdd(partitionId, new PartitionCheckpointCounters());
            long processedMessagesSinceLastCheckpoint = partitionCheckpointCounters.ProcessedMessagesSinceLastCheckpoint + 1;
            TimeSpan elapsedTimeSinceLastCheckpoint = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - partitionCheckpointCounters.LastCheckpointTimeTicks);

            bool updatedCheckpointCounters;
            if (elapsedTimeSinceLastCheckpoint >= _checkpointStopwatchWaitingInterval || processedMessagesSinceLastCheckpoint >= _messagesCountCheckpointThreshold)
            {
                var sw = Stopwatch.StartNew();
                await _updateCheckPointTask(partitionId, offset, sequenceNumber, cancellationToken);
                _checkpointWriteDurationMs.Record(sw.ElapsedMilliseconds);
                _checkpointWriteCounter.Add(1);
                
                updatedCheckpointCounters = _partitionsCheckpointCounters.TryUpdate(partitionId, new PartitionCheckpointCounters(), partitionCheckpointCounters);
            }
            else
            {
                updatedCheckpointCounters = _partitionsCheckpointCounters.TryUpdate(
                    partitionId,
                    new PartitionCheckpointCounters()
                    {
                        LastCheckpointTimeTicks = partitionCheckpointCounters.LastCheckpointTimeTicks,
                        ProcessedMessagesSinceLastCheckpoint = processedMessagesSinceLastCheckpoint
                    },
                    partitionCheckpointCounters);
            }

        }

        private record PartitionCheckpointCounters
        {
            public long LastCheckpointTimeTicks { get; set; } = DateTime.UtcNow.Ticks;

            public long ProcessedMessagesSinceLastCheckpoint { get; set; }
        }
    }

}
