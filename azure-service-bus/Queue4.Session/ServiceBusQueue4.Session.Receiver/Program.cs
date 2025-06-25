using Azure;
using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue.session1";

Console.WriteLine("Receiving a message to queue...");

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);

// create a session receiver that we can use to receive the message. Since we don't specify a
// particular session, we will get the next available session from the service.
ServiceBusSessionReceiver receiver = await client.AcceptNextSessionAsync(queueName);

// the received message is a different type as it contains some service set properties
ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();
Console.WriteLine($"Received message ({receivedMessage.Body.ToString()}) - seq={receivedMessage.SequenceNumber} in session {receivedMessage.SessionId}.");

Thread.Sleep(Random.Shared.Next(3000, 5000)); // Simulate some work
Console.WriteLine($"\t\tProceeded message ({receivedMessage.Body.ToString()}) - seq={receivedMessage.SequenceNumber} in session {receivedMessage.SessionId}.");
// we can also set arbitrary session state using this receiver
// the state is specific to the session, and not any particular message
await receiver.SetSessionStateAsync(new BinaryData("some state"));

// the state can be retrieved for the session as well
BinaryData state = await receiver.GetSessionStateAsync();
