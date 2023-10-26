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

    }
}
