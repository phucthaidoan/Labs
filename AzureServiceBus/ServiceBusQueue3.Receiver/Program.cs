using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

Console.WriteLine("======================================================");
Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
Console.WriteLine("======================================================");

string queueName = "queue1";

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);

var processorOptions = new ServiceBusProcessorOptions
{
    MaxConcurrentCalls = 1,
    AutoCompleteMessages = false
};

// Create a ServiceBusProcessor for the queue.
await using ServiceBusProcessor processor = client.CreateProcessor(
    queueName,
    processorOptions);

// Specify handler methods for messages and errors.
processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

// Start processing messages.
await processor.StartProcessingAsync();

Console.Read();

// Close the processor.
await processor.CloseAsync();

static async Task MessageHandler(ProcessMessageEventArgs args)
{
    // extract the message
    string body = args.Message.Body.ToString();

    // print the message
    Console.WriteLine($"Received: {body}");

    // complete the message so that message is deleted from the queue.
    await args.CompleteMessageAsync(args.Message);
}

static Task ErrorHandler(ProcessErrorEventArgs args)
{
    // print the exception message
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}