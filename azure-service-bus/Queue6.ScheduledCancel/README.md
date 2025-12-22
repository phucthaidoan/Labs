# Queue6.ScheduledCancel – Scheduled Messages & Cancellation

This sample shows how to **schedule** Azure Service Bus messages and **cancel** them using their scheduled sequence numbers.

## Key Concepts

- **Scheduled vs active messages**
  - When you call `ScheduleMessageAsync`, the message is stored in a **scheduled state** (internal sub-queue), not yet in the main queue.
  - At the scheduled time, Service Bus moves it into the **main queue** as a normal message and assigns it a **new sequence number**.

- **Scheduled sequence number**
  - `ScheduleMessageAsync` returns a **scheduled sequence number**:
    ```csharp
    long sequenceNumber = await sender.ScheduleMessageAsync(message, scheduledTime);
    ```
  - This value identifies the scheduled entry and is the **only value** you can pass to `CancelScheduledMessageAsync`.

- **Cancellation timing**
  - `CancelScheduledMessageAsync` only works **while the message is still scheduled**, i.e., **before** its scheduled enqueue time passes.
  - After the scheduled time:
    - The message becomes an active queue message.
    - The original scheduled entry is gone and its sequence number is no longer valid.
    - Cancellation using the old scheduled sequence number results in `MessageNotFound`.

- **Azure Portal vs scheduled sequence number**
  - The sequence number you see when you **peek active messages** in the Portal is the **active message’s** sequence number.
  - This is **different** from the scheduled sequence number returned by `ScheduleMessageAsync`.
  - To cancel correctly, use the **number printed by the sender** (or from the queue’s **Scheduled** view, before it enqueues), not the sequence from active messages.

## Projects

- `ServiceBusQueue6.ScheduledCancel.Sender`
  - Schedules several messages in the future using `ScheduleMessageAsync`.
  - Prints:
    - `MessageId`
    - scheduled sequence number (`seq`) returned by `ScheduleMessageAsync`
    - scheduled enqueue time.

- `ServiceBusQueue6.ScheduledCancel.Receiver`
  - Prompts for a **sequence number** (from sender output or Portal’s **Scheduled** view).
  - Calls `CancelScheduledMessageAsync(sequenceNumber)` to cancel the scheduled message.
  - Will return `MessageNotFound` if:
    - The message has already been enqueued (scheduled time passed), or
    - The sequence number is from an active (already enqueued) message instead of the scheduled entry.

## Recommended Flow

1. Run the **Sender** to schedule messages and note the `seq=` values it prints.
2. Before the scheduled times elapse, run the **Receiver**.
3. Paste one of the printed sequence numbers into the Receiver when prompted.
4. Observe that:
   - The call to `CancelScheduledMessageAsync` succeeds.
   - That message will not appear as an active message when its scheduled time arrives.


