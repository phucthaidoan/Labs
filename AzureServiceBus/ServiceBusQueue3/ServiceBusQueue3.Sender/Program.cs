using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue1";

Console.WriteLine("Sending a message to queue...");

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the sender
ServiceBusSender sender = client.CreateSender(queueName);

// create a message that we can send. UTF-8 encoding is used when providing a string.
var messageBody = "Hello world! OK3";
ServiceBusMessage message = new(messageBody);

Console.WriteLine($"Sending message: {messageBody}");

// send the message
await sender.SendMessageAsync(message);

// Calling DisposeAsync on client types is required to ensure that network
// resources and other unmanaged objects are properly cleaned up.
await sender.DisposeAsync();
await client.DisposeAsync();