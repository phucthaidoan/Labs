using Azure.Messaging.ServiceBus;
using System.Text;

// Check process-level environment variable first (respects $env: in PowerShell),
// then fall back to user-level if not found
string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING", EnvironmentVariableTarget.User);

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue-test";

Console.WriteLine("Scheduling messages to queue...");

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the sender
ServiceBusSender sender = client.CreateSender(queueName);

// Schedule messages with different delays
var delays = new[] { 10, 30, 60 }; // 10 seconds, 30 seconds, 1 minute
var now = DateTimeOffset.UtcNow;

for (var i = 0; i < delays.Length; i++)
{
    var delaySeconds = delays[i];
    var scheduledTime = now.AddSeconds(delaySeconds);

    // create a scheduled message
    var messageId = Guid.NewGuid().ToString();
    ServiceBusMessage message = new(Encoding.UTF8.GetBytes($"Scheduled message {i + 1} - delay {delaySeconds}s"))
    {
        MessageId = messageId,
        ScheduledEnqueueTime = scheduledTime
    };

    // schedule the message and capture the sequence number returned by the service
    long sequenceNumber = await sender.ScheduleMessageAsync(message, scheduledTime);
    
    Console.WriteLine($"Scheduled message {i + 1}: id={messageId}, seq={sequenceNumber}, for {scheduledTime:HH:mm:ss} UTC (in {delaySeconds} seconds)");
}

Console.WriteLine("\nAll messages have been scheduled. They will be available for processing at their scheduled times.");

