# Azure Service Bus Scheduled Messages Sample

This sample demonstrates how to schedule messages in Azure Service Bus for future delivery.

## Prerequisites

- .NET 9.0 SDK
- Azure Service Bus namespace with a queue named `queue-test`
- Connection string for your Azure Service Bus namespace

## Setting the Connection String

### Windows Terminal (PowerShell)

#### Option 1: Process-Level (Current Session Only)

To set the connection string for the current session:

```powershell
# Remove any existing value in the current process
Remove-Item Env:\SB_CONNECTION_STRING -ErrorAction SilentlyContinue

# Set the new connection string
$env:SB_CONNECTION_STRING = "your-connection-string-here"
```

**Important**: After setting the environment variable, restart your IDE/terminal or rebuild the project so the new value is picked up.

#### Option 2: User-Level (Persistent Across Sessions)

For a persistent setting that survives terminal restarts:

**Set user-level environment variable:**
```powershell
[System.Environment]::SetEnvironmentVariable("SB_CONNECTION_STRING", "your-connection-string-here", "User")
```

**Read user-level environment variable:**
```powershell
[System.Environment]::GetEnvironmentVariable("SB_CONNECTION_STRING", "User")
```

**Remove user-level environment variable:**
```powershell
[System.Environment]::SetEnvironmentVariable("SB_CONNECTION_STRING", $null, "User")
```

**Note**: After setting or removing a user-level environment variable, close all terminal/IDE windows and open a new Windows Terminal session for the change to take effect.

### Verify the Environment Variable

Check if it's set correctly (process-level):
```powershell
$env:SB_CONNECTION_STRING
```

Check if it's set correctly (user-level):
```powershell
[System.Environment]::GetEnvironmentVariable("SB_CONNECTION_STRING", "User")
```

## Running the Sample

### 1. Run the Sender

The sender schedules 3 messages with delays of 10, 30, and 60 seconds:

```powershell
dotnet run --project ServiceBusQueue5.Scheduled.Sender
```

### 2. Run the Receiver

The receiver waits for scheduled messages and displays when they arrive:

```powershell
dotnet run --project ServiceBusQueue5.Scheduled.Receiver
```

## How It Works

- **Sender**: Uses `ServiceBusMessage.ScheduledEnqueueTime` to schedule messages for future delivery
- **Receiver**: Receives messages as they become available at their scheduled times
- The receiver displays the scheduled time vs actual receive time to demonstrate the scheduling feature

## Troubleshooting

If `Environment.GetEnvironmentVariable("SB_CONNECTION_STRING")` returns an old value:

1. **Check for user-level environment variable**: There might be a user-level environment variable overriding your process-level one
   ```powershell
   [System.Environment]::GetEnvironmentVariable("SB_CONNECTION_STRING", "User")
   ```

2. **Remove user-level variable if needed**:
   ```powershell
   [System.Environment]::SetEnvironmentVariable("SB_CONNECTION_STRING", $null, "User")
   ```

3. **Set process-level variable**:
   ```powershell
   Remove-Item Env:\SB_CONNECTION_STRING -ErrorAction SilentlyContinue
   $env:SB_CONNECTION_STRING = "your-new-connection-string-here"
   ```

4. **Restart your IDE/terminal** to ensure the new value is picked up

The code checks process-level environment variables first, which respects the `$env:` variable set in PowerShell.

