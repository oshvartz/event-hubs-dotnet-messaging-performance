using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputTest
{
    public class SenderTask : PerformanceTask
    {
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<SenderTask> _logger;
        private readonly Counter<int> _successSendCounter;
        private readonly Counter<int> _failedSendCounter;
        private readonly Histogram<long> _sendDurationMs;

        public SenderTask(CliOptions options, ILogger<SenderTask> logger) : base(options)
        {
            _eventHubProducerClient = new EventHubProducerClient(options.ConnectionString, options.EventHubName);
            _logger = logger;
            _successSendCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{options.EventHubName}-success-send-count",
        "SendCount", "Total number of succesful send messages");
            _failedSendCounter = AppMeterProvider.AppMeter.CreateCounter<int>($"{options.EventHubName}-fail-send-count",
           "SendCount", "Total number of failed send messages");
            _sendDurationMs = AppMeterProvider.AppMeter.CreateHistogram<long>($"{options.EventHubName}-send-duration", "ms", "send events duration in millisecond");
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var payload = new byte[_options.MessageSizeInBytes];
            var tasks = new List<Task>(_options.SenderCount);
            for (var i = 0; i < _options.SenderCount; i++)
            {
                tasks.Add(StartSendingAsync(payload, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

        private async Task StartSendingAsync(Byte[] payload, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var eventsData = CreateEventsData(payload);
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    await _eventHubProducerClient.SendAsync(eventsData, cancellationToken);
                    _sendDurationMs.Record(stopwatch.ElapsedMilliseconds);
                    _successSendCounter.Add(eventsData.Count());
                }
                catch (Exception ex)
                {
                    _failedSendCounter.Add(eventsData.Count());
                    _logger.LogError(ex, "fail to send events");
                }
            }
        }

        private IEnumerable<EventData> CreateEventsData(Byte[] payload)
        {
            var events = new List<EventData>();
            for (int i = 0; i < _options.SendBatchCount; i++)
            {
                events.Add(new EventData(new BinaryData(payload)));
            }
            return events;
        }
    }
}
