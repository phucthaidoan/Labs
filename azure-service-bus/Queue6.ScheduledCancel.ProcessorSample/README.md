# Queue6.ScheduledCancel.ProcessorSample

Sample demonstrating scheduled message cancellation alongside a `ServiceBusProcessor` that handles active messages.

## Projects
- `ServiceBusQueue6.ScheduledCancel.ProcessorSample.Sender`
  - Schedules messages with `ScheduleMessageAsync`.
  - Prints **scheduled sequence numbers** (the values you must use to cancel).
- `ServiceBusQueue6.ScheduledCancel.ProcessorSample.Receiver`
  - Uses `ServiceBusProcessor` to process active messages.
  - Prompts you to enter a **scheduled sequence number** and calls `CancelScheduledMessageAsync`.

## Queue & Connection
- Default queue: `queue-test-processor` (override with first CLI arg in both sender/receiver).
- Connection string: `SB_CONNECTION_STRING` (user-level env var preferred, falls back to process).

## How scheduling and cancellation work
- `ScheduleMessageAsync` stores the message in a **scheduled state** and returns a **scheduled sequence number**.
- At the scheduled time, Service Bus moves the message into the main queue as an **active message** and assigns it a **new** sequence number.
- `CancelScheduledMessageAsync` only works **while the message is still scheduled** (before the scheduled time). After it enqueues, the original scheduled entry is gone → `MessageNotFound`.
- Do **not** use the sequence number you see when peeking active messages in the portal; use the **scheduled sequence number** printed by the sender (or from the portal’s **Scheduled** view before enqueue).

## Run
### Sender (schedule messages)
```powershell
cd azure-service-bus/Queue6.ScheduledCancel.ProcessorSample
dotnet run --project ServiceBusQueue6.ScheduledCancel.ProcessorSample.Sender
```
Note the `scheduledSeq` values printed.

### Receiver (processor + manual cancel)
```powershell
cd azure-service-bus/Queue6.ScheduledCancel.ProcessorSample
dotnet run --project ServiceBusQueue6.ScheduledCancel.ProcessorSample.Receiver
```
- Processor will handle active messages as they arrive.
- To cancel, paste a **scheduled** sequence number (from sender output). If the time already passed or you use an active seq number, you’ll see `MessageNotFound`.


