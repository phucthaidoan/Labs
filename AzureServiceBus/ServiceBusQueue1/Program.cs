using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue1";

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the sender
ServiceBusSender sender = client.CreateSender(queueName);

// create a message that we can send. UTF-8 encoding is used when providing a string.
ServiceBusMessage message = new("Hello world! OK1");

// send the message
await sender.SendMessageAsync(message);

// Create a ServiceBusProcessor for the queue.
await using ServiceBusProcessor processor = client.CreateProcessor(
    queueName,
    new ServiceBusProcessorOptions
    {

    });

// Specify handler methods for messages and errors.
processor.ProcessMessageAsync += MessageHandler;

async Task MessageHandler(ProcessMessageEventArgs args)
{
    ServiceBusReceivedMessage receivedMessage = args.Message;

    // get the message body as a string
    string body = receivedMessage.Body.ToString();
    Console.WriteLine(body); Console.WriteLine($"Receive message: {body}");

    await args.CompleteMessageAsync(args.Message);
}

processor.ProcessErrorAsync += ErrorHandler;

async Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine($"Error {args.FullyQualifiedNamespace}");
}