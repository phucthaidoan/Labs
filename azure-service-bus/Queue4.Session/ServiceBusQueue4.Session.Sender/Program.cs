using Azure.Messaging.ServiceBus;
using System.Text;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue.session1";

Console.WriteLine("Sending a message to queue...");

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the sender
ServiceBusSender sender = client.CreateSender(queueName);

for (var i = 1; i <= 200; i++)
{

    // create a session message that we can send
    ServiceBusMessage message = new(Encoding.UTF8.GetBytes($"M{i}"))
    {
        SessionId = "mySessionId"
    };

    // send the message
    await sender.SendMessageAsync(message);
}