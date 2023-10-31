# Event Hubs Throughput Test
Inspired from https://github.com/Azure-Samples/service-bus-dotnet-messaging-performance
This repo enables to run benchmarks of Azure Event Hubs - simulating load on event hubs to measture performance.

## Command Line arguments
```
  -C, --connection-string            Required. event hubs namesapce connection string

  -N, --eventhub-name                Required. EventHub Name

  -b, --message-size-bytes           Bytes per message (default 1024)

  -t, --send-batch-count             Number of messages per batch (default 1, no batching)

  -s, --sender-count                 Number of concurrent senders (default 0)

  -c, --consumer-groups              conusmer groups

  -r, --recieve-max-batch-size       Max number of messages to read or process (default 1)

  -w, --processing-duration-ms       Processing time duration in millisecond (default is 0)

  -S, --storage-connection-string    checkpoint storage connection string

  -B, --blob-container-name          checkpoint blob container name

```

## Metrics
Using System.Diagnostics.Metrics - we can use multiple tools to visualize\monitor the benchmark execution.
all metrics have meter name: EventHub.ThroughputTest
One of the common tools that can be used is dotnet-counters https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters

### How to install
Note:make sure you are running command as administrator

```
  dotnet tool install --global dotnet-counters
```

### Example dotnet-counters usage

```
  dotnet-counters monitor -n ThroughputTest EventHub.ThroughputTest
```

# Example ThroughputTest Usage
```
 ThroughputTest -C "Endpoint=...." -N theEventhubName -s 3 -c counsumer1 counsumer2 -S \"DefaultEndpointsProtocol=https;AccountName=..." -B TheContainerName -r 5 -w 100
```

# Roadmap features

- [X] Integration of System.Diagnostics Metrics
- [ ] Open Telemtry integration - allow to get metrics using otel protocol
- [ ] Add paramters to control consumers configuration such as PrefetchCount and PrefetchSizeInBytes
- [ ] Add paramters to control CheckpointWriter configuration: WaitingInterval and messagesCountCheckpointThreshold