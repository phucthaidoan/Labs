using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue1";

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the sender
ServiceBusSender sender = client.CreateSender(queueName);

// create a message that we can send. UTF-8 encoding is used when providing a string.
ServiceBusMessage message = new($"Hello world! Schedule");
message.ScheduledEnqueueTime = DateTimeOffset.UtcNow.AddSeconds(10); // Schedule the message to be sent in 10 seconds
message.TimeToLive = TimeSpan.FromSeconds(5); // Expire at certain time

// send the message
await sender.SendMessageAsync(message);