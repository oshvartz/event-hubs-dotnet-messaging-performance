using CommandLine;

namespace ThroughputTest
{
    public record CliOptions
    {
        [Option('C', "connection-string", Required = true, HelpText = "event hubs namesapce connection string")]
        public string ConnectionString { get; set; }

        [Option('N', "eventhub-name", Required = true, HelpText = "EventHub Name")]
        public string EventHubName { get; set; }

        [Option('b', "message-size-bytes", Required = false, HelpText = "Bytes per message (default 1024)")]
        public int MessageSizeInBytes { get; set; } = 1024;

        [Option('t', "send-batch-count", Required = false, HelpText = "Number of messages per batch (default 1, no batching)")]
        public int SendBatchCount { get; set; } = 1;

        [Option('s', "sender-count", Required = false, HelpText = "Number of concurrent senders (default 0)")]
        public int SenderCount { get; set; } = 0;

        [Option('c', "consumer-groups", Required = false, HelpText = "conusmer groups")]
        public IEnumerable<string> ConsumerGroups { get; set; }

        [Option('r', "recieve-max-batch-size", Required = false, HelpText = "Max number of messages to read or process (default 1)")]
        public int RecieveMaxBatchSize { get; set; } = 1;

        [Option('p', "recieve-prefetch-count", Required = false, HelpText = "Recieve Prefetch Count (default 300)")]
        public int PrefetchCount { get; set; } = 300;

        [Option('w', "processing-duration-ms", Required = false, HelpText = "Processing time duration in millisecond (default is 0)")]
        public int ProcessingTimeDurationMs { get; set; } = 0;

        [Option('S', "storage-connection-string", Required = false, HelpText = "checkpoint storage connection string")]
        public string StorageConnectionString { get; set; }

        [Option('B', "blob-container-name", Required = false, HelpText = "checkpoint blob container name")]
        public string BlobContainerName { get; set; }

    }
}
