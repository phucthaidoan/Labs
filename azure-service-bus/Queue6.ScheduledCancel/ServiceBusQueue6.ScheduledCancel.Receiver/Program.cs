using Azure.Messaging.ServiceBus;

// Read connection string from environment (user-level or process-level).
// The receiver uses the same queue as the sender and only performs cancellation;
// it does not list scheduled messages, because the SDK cannot enumerate the internal
// scheduled sub-queue. Instead, you must provide the scheduled sequence number
// that was returned by ScheduleMessageAsync (and printed by the sender) or shown
// in the queue's Scheduled view in the Azure Portal *before* the scheduled time.
string connectionString =
    Environment.GetEnvironmentVariable("SB_CONNECTION_STRING", EnvironmentVariableTarget.User)
    ?? Environment.GetEnvironmentVariable("SB_CONNECTION_STRING");

ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));

const string queueName = "queue-test";

await using ServiceBusClient client = new(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);

Console.WriteLine("Azure Service Bus - Queue6.ScheduledCancel Receiver (Manual Cancel)");
Console.WriteLine($"Queue: {queueName}");
Console.WriteLine();
Console.WriteLine("Type the scheduled sequence number to cancel (from sender logs or the queue's Scheduled view).");
Console.WriteLine("Type 'q' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("Sequence number to cancel (or 'q' to quit): ");
    var input = Console.ReadLine();

    if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Exiting receiver.");
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
        // Cancel works only while the message is still scheduled. If the scheduled
        // enqueue time has already passed, or if you use the sequence number of an
        // active/peeked message instead of the scheduled one, Service Bus will return
        // MessageNotFound because there is no scheduled entry with that sequence.
        await sender.CancelScheduledMessageAsync(sequenceNumber);
        Console.WriteLine($"  Successfully requested cancellation for scheduled message seq={sequenceNumber}.");
    }
    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
    {
        Console.WriteLine("  Not found as scheduled. Likely already enqueued or wrong sequence number (active message).");
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
