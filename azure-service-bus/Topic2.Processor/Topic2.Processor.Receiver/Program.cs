﻿using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string subscriptionName = "subscription1";
string topicName = "topic1";

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);

// create the options to use for configuring the processor
ServiceBusProcessorOptions options = new()
{
    // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
    // Set AutoCompleteMessages to false to [settle messages](https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
    // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
    AutoCompleteMessages = false,

    // I can also allow for multi-threading
    MaxConcurrentCalls = 1
};

// create a processor that we can use to process the messages
await using ServiceBusProcessor processor =  client.CreateProcessor(topicName, subscriptionName, options);

// configure the message and error handler to use
processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

Console.WriteLine($"Starting to process messages from {topicName}.{subscriptionName}...");

// start processing
await processor.StartProcessingAsync();

// since the processing happens in the background, we add a Console.ReadKey to allow the processing to continue until a key is pressed.
Console.ReadKey();

async Task MessageHandler(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received from {topicName}.{subscriptionName}: {body}");

    // we can evaluate application logic and use that to determine how to settle the message.
    await args.CompleteMessageAsync(args.Message);
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    // the error source tells me at what point in the processing an error occurred
    Console.WriteLine(args.ErrorSource);
    // the fully qualified namespace is available
    Console.WriteLine(args.FullyQualifiedNamespace);
    // as well as the entity path
    Console.WriteLine(args.EntityPath);
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}