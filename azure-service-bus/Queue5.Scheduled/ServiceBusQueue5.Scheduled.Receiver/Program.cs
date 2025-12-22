using Azure.Messaging.ServiceBus;

string _connectionString = Environment.GetEnvironmentVariable("SB_CONNECTION_STRING", EnvironmentVariableTarget.User);

ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

string queueName = "queue-test";

Console.WriteLine("Receiving scheduled messages from queue...");
Console.WriteLine("Waiting for scheduled messages to become available...\n");

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
await using ServiceBusClient client = new(_connectionString);
// create the receiver
ServiceBusReceiver receiver = client.CreateReceiver(queueName);

var receivedCount = 0;
var startTime = DateTimeOffset.UtcNow;

// Receive messages as they become available at their scheduled times
while (receivedCount < 3)
{
    try
    {
        // Receive message with a timeout
        ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(90));
        
        if (receivedMessage != null)
        {
            var receiveTime = DateTimeOffset.UtcNow;
            var scheduledTime = receivedMessage.ScheduledEnqueueTime;
            var delay = receiveTime - scheduledTime;
            
            Console.WriteLine($"Received message: '{receivedMessage.Body}'");
            Console.WriteLine($"  MessageId:     {receivedMessage.MessageId}");
            Console.WriteLine($"  Sequence #:    {receivedMessage.SequenceNumber}");
            Console.WriteLine($"  Scheduled for: {scheduledTime:HH:mm:ss.fff} UTC");
            Console.WriteLine($"  Received at:   {receiveTime:HH:mm:ss.fff} UTC");
            Console.WriteLine($"  Delay:         {delay.TotalMilliseconds:F0} ms");
            Console.WriteLine();
            
            // Complete the message
            await receiver.CompleteMessageAsync(receivedMessage);
            receivedCount++;
        }
    }
    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
    {
        // No message available yet, continue waiting
        Console.WriteLine($"Waiting for scheduled messages... ({DateTimeOffset.UtcNow:HH:mm:ss})");
        await Task.Delay(1000);
    }
}

var totalTime = DateTimeOffset.UtcNow - startTime;
Console.WriteLine($"All scheduled messages received. Total time: {totalTime.TotalSeconds:F1} seconds");

