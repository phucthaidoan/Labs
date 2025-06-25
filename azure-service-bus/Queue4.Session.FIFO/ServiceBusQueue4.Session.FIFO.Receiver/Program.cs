using Azure;
using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue.session1";

Console.WriteLine("Receiving a message to queue...");

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);

var processorOptions = new ServiceBusSessionProcessorOptions
{
    // By default after the message handler returns, the processor will complete the message
    // If I want more fine-grained control over settlement, I can set this to false.
    AutoCompleteMessages = false,

    // I can also allow for processing multiple sessions
    MaxConcurrentSessions = 5,

    // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
    // Set AutoCompleteMessages to false to [settle messages](https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
    // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
    MaxConcurrentCallsPerSession = 1, // Set to greater than 1 will not achieve the FIFO behavior.

    // Processing can be optionally limited to a subset of session Ids.
    SessionIds = { "mySessionId", "mySessionId2" },
};

// Create a ServiceBusProcessor for the queue.
await using ServiceBusSessionProcessor processor = client.CreateSessionProcessor(queueName, processorOptions);


// Specify handler methods for messages and errors.
processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

// Start processing messages.
await processor.StartProcessingAsync();

Console.Read();

// Close the processor.
await processor.CloseAsync();

async Task MessageHandler(ProcessSessionMessageEventArgs args)
{

    var body = args.Message.Body.ToString();
    Console.WriteLine($"Received message ({body}) - seq={args.Message.SequenceNumber} in session {args.SessionId}.");

    Thread.Sleep(Random.Shared.Next(3000, 5000)); // Simulate some work
    Console.WriteLine($"\t\tProceeded message ({body}) - seq={args.Message.SequenceNumber} in session {args.SessionId}.");

    //// we can evaluate application logic and use that to determine how to settle the message.
    await args.CompleteMessageAsync(args.Message);

    //// we can also set arbitrary session state using this receiver
    //// the state is specific to the session, and not any particular message
    await args.SetSessionStateAsync(new BinaryData("Some state specific to this session when processing a message."));
}

static Task ErrorHandler(ProcessErrorEventArgs args)
{
    // print the exception message
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}