using Azure.Messaging.ServiceBus;
using System.Text;

// Read connection string from environment (user-level or process-level).
// The same connection/queue are used by the Receiver project to cancel scheduled messages.
string connectionString =
    Environment.GetEnvironmentVariable("SB_CONNECTION_STRING", EnvironmentVariableTarget.User)
    ?? Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));

const string queueName = "queue-test";

Console.WriteLine("Azure Service Bus - Queue6.ScheduledCancel Sender");
Console.WriteLine($"Queue: {queueName}");
Console.WriteLine();

await using ServiceBusClient client = new(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);

// Schedule several messages with staggered delays.
// NOTE: ScheduleMessageAsync returns a *scheduled* sequence number, which is different
// from the sequence number of the active message that will later appear in the queue.
// The scheduled sequence number is what you must pass to CancelScheduledMessageAsync.
var delays = new[] { 30, 60, 90 }; // seconds
var now = DateTimeOffset.UtcNow;

Console.WriteLine("Scheduling messages...");

for (int i = 0; i < delays.Length; i++)
{
    int delaySeconds = delays[i];
    DateTimeOffset scheduledTime = now.AddSeconds(delaySeconds);

    string messageId = Guid.NewGuid().ToString();
    ServiceBusMessage message = new(Encoding.UTF8.GetBytes($"Q6 Scheduled message {i + 1} - delay {delaySeconds}s"))
    {
        MessageId = messageId
    };

    long sequenceNumber = await sender.ScheduleMessageAsync(message, scheduledTime);

    Console.WriteLine(
        $"  Message {i + 1}: id={messageId}, seq={sequenceNumber}, scheduledFor={scheduledTime:HH:mm:ss} UTC (in {delaySeconds}s)");
}

Console.WriteLine();
Console.WriteLine("All messages have been scheduled. Use the Receiver project to list and cancel scheduled messages.");
