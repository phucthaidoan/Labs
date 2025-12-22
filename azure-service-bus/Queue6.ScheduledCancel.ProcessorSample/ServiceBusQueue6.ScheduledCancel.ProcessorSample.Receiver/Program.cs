using Azure.Messaging.ServiceBus;

// Connection string: prefer user-level env var, fall back to process
string connectionString =
    Environment.GetEnvironmentVariable("SB_CONNECTION_STRING", EnvironmentVariableTarget.User)
    ?? Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));

string queueName = "queue-test";
if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    queueName = args[0].Trim();
}

Console.WriteLine("Queue6.ScheduledCancel.ProcessorSample - Receiver (ServiceBusProcessor)");
Console.WriteLine($"Queue: {queueName}");
Console.WriteLine();

await using ServiceBusClient client = new(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);

// Processor to show normal message handling
ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 2
});

// Processor basics:
// - ProcessMessageAsync: your handler; settle messages yourself when AutoCompleteMessages=false.
// - ProcessErrorAsync: logs receive/lock/connection errors.
// - Lock renewal is automatic while the handler runs; keep handlers short or renew manually for long work.
// - The processor only sees messages once they are active (after scheduled time); scheduled messages are not enumerable.

processor.ProcessMessageAsync += async args =>
{
    var msg = args.Message;
    Console.WriteLine($"[PROCESSOR] Received active message: id={msg.MessageId}, seq={msg.SequenceNumber}, body={msg.Body}");
    await args.CompleteMessageAsync(msg);
};

processor.ProcessErrorAsync += args =>
{
    Console.WriteLine($"[PROCESSOR] Error: {args.Exception.Message}");
    return Task.CompletedTask;
};

await processor.StartProcessingAsync();

Console.WriteLine("Processor started. It will process active messages as they arrive.");
Console.WriteLine("To cancel a scheduled message, enter the *scheduled* sequence number (from sender output).");
Console.WriteLine("Note: Cancel works only while the message is still scheduled; once enqueued, the scheduled entry is gone.");
Console.WriteLine("Type 'q' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("Sequence number to cancel (or 'q' to quit): ");
    var input = Console.ReadLine();

    if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Exiting receiver...");
        break;
    }

    if (!long.TryParse(input, out long sequenceNumber))
    {
        Console.WriteLine("  Invalid sequence number. Please enter a numeric value.");
        Console.WriteLine();
        continue;
    }

    try
    {
        await sender.CancelScheduledMessageAsync(sequenceNumber);
        Console.WriteLine($"  Successfully requested cancellation for scheduled message seq={sequenceNumber}.");
    }
    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
    {
        Console.WriteLine("  Not found as scheduled. Likely already enqueued or wrong (active) sequence number.");
        Console.WriteLine($"  ServiceBus reason: {ex.Reason}, Message: {ex.Message}");
    }
    catch (ServiceBusException ex)
    {
        Console.WriteLine($"  Failed to cancel scheduled message. Reason: {ex.Reason}, Message: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Unexpected error: {ex.Message}");
    }

    Console.WriteLine();
}

await processor.StopProcessingAsync();
await processor.DisposeAsync();
