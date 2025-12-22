using Azure.Messaging.ServiceBus;
using System.Text;

// Connection string: prefer user-level env var, fall back to process
string connectionString =
    Environment.GetEnvironmentVariable("SB_CONNECTION_STRING", EnvironmentVariableTarget.User)
    ?? Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));

// Queue name for this processor sample
string queueName = "queue-test";

if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    queueName = args[0].Trim();
}

Console.WriteLine("Queue6.ScheduledCancel.ProcessorSample - Sender");
Console.WriteLine($"Queue: {queueName}");
Console.WriteLine();

await using ServiceBusClient client = new(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);

// Schedule several messages with staggered delays
var delays = new[] { 60, 120, 180 }; // seconds
var now = DateTimeOffset.UtcNow;

Console.WriteLine("Scheduling messages (captures scheduled sequence numbers) ...");

for (int i = 0; i < delays.Length; i++)
{
    int delaySeconds = delays[i];
    DateTimeOffset scheduledTime = now.AddSeconds(delaySeconds);

    string messageId = Guid.NewGuid().ToString();
    ServiceBusMessage message = new(Encoding.UTF8.GetBytes($"Processor sample message {i + 1} - delay {delaySeconds}s"))
    {
        MessageId = messageId
    };

    long scheduledSequence = await sender.ScheduleMessageAsync(message, scheduledTime);

    // IMPORTANT: This is the scheduled sequence number, not the active message sequence
    // that will appear when it enqueues. Use THIS value with CancelScheduledMessageAsync.
    Console.WriteLine(
        $"  Message {i + 1}: id={messageId}, scheduledSeq={scheduledSequence}, scheduledFor={scheduledTime:HH:mm:ss} UTC (in {delaySeconds}s)");
}

Console.WriteLine();
Console.WriteLine("Done. Use the Receiver (processor) to consume messages or cancel by scheduled sequence number before they enqueue.");
