using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string subscriptionName = "subscription1";
string topicName = "topic1";

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the sender that we will use to send to our topic
ServiceBusSender sender = client.CreateSender(topicName);

// create a message that we can send. UTF-8 encoding is used when providing a string.
ServiceBusMessage message = new("Hello world!");

// send the message
await sender.SendMessageAsync(message);

// create a receiver for our subscription that we can use to receive the message
ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

// the received message is a different type as it contains some service set properties
ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

// get the message body as a string
string body = receivedMessage.Body.ToString();
Console.WriteLine(body);