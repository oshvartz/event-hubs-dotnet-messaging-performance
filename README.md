# Teams Chats Downloader
Inspired from https://github.com/Azure-Samples/service-bus-dotnet-messaging-performance
This repo enables to run benchmarks of Azure Event Hubs

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

## View Metrics
Note:make sure you are running command as administrator
install dotnet-counters:
```
  dotnet tool install --global dotnet-counters
```
execute dotnet-counters:
```
  dotnet-counters monitor -n ThroughputTest EventHub.ThroughputTest
```

# Example Usage
```
 ThroughputTest -C "Endpoint=...." -N theEventhubName -s 3 -c counsumer1 counsumer2 -S \"DefaultEndpointsProtocol=https;AccountName=..." -B TheContainerName -r 5 -w 100
```

