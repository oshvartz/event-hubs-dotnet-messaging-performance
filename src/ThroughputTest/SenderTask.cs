using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputTest
{
    public class SenderTask : PerformanceTask
    {
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly ILogger<SenderTask> _logger;

        public SenderTask(CliOptions options, ILogger<SenderTask> logger) : base(options)
        {
            _eventHubProducerClient = new EventHubProducerClient(options.ConnectionString, options.EventHubName);
            _logger = logger;
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var payload = new byte[_options.MessageSizeInBytes];
            var tasks = new List<Task>(_options.SenderCount);
            for (var i = 0; i < _options.SenderCount; i++) 
            {
                tasks.Add(StartSendingAsync(payload,cancellationToken));
            }

            await Task.WhenAll(tasks);
        }

        private async Task StartSendingAsync(Byte[] payload, CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var eventsData = CreateEventsData(payload);
                try
                {
                    await _eventHubProducerClient.SendAsync(eventsData, cancellationToken);
                }
                catch(Exception ex) 
                {
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
