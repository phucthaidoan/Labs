# Azure Durable Functions - Sync Processing Workflow

## Overview

This solution demonstrates a comprehensive Azure Durable Functions implementation that orchestrates a multi-step sync processing workflow. The architecture combines timer-triggered functions, queue processing, and durable orchestration patterns to create a robust, scalable system for handling scheduled synchronization tasks.

## Architecture Components

### 1. Timer Trigger Function (`Function1`)
- **Trigger**: Runs every 15 seconds using cron expression `*/15 * * * * *`
- **Purpose**: Generates sync items and queues them for processing
- **Output**: Produces `MySyncItem` objects to the `output-queue`

### 2. Queue Processor Function (`SyncQueueProcessor`)
- **Trigger**: Azure Storage Queue trigger on `output-queue`
- **Purpose**: Processes queued sync items and initiates durable orchestrations
- **Features**:
  - Lists existing orchestration instances with prefix "Workflow-"
  - Creates unique instance IDs using pattern: `Workflow-{Id}-{SyncName}-{Date}`
  - Starts new orchestration instances for each sync item

### 3. Main Orchestrator (`MainWorkflow`)
- **Trigger**: Orchestration trigger
- **Purpose**: Coordinates the execution of multiple activity functions
- **Workflow**:
  1. Receives `MySyncItem` input from queue processor
  2. Logs orchestration start with sync details
  3. Executes `WorkflowItem1` activity
  4. Executes `WorkflowItem2` activity
  5. Logs orchestration completion

### 4. Activity Functions

#### WorkflowItem1
- **Purpose**: First step in the sync processing workflow
- **Error Handling**: Throws exception for sync items with `Id == 2` (simulates failure scenario)
- **Processing**: Simulates work with 1-second delay

#### WorkflowItem2
- **Purpose**: Second step in the sync processing workflow
- **Processing**: Simulates work with 1-second delay

## Data Model

### MySyncItem
```csharp
public sealed record MySyncItem
{
    public int Id { get; set; }
    public string SyncName { get; set; }
    public DateTime TriggerDateUtc { get; set; }
}
```

## Workflow Execution Flow

1. **Timer Trigger** (every 15 seconds)
   - Generates two sync items with different IDs and trigger times
   - Queues items to `output-queue`

2. **Queue Processing**
   - `SyncQueueProcessor` picks up queued items
   - Lists existing orchestration instances
   - Starts new orchestration with unique instance ID

3. **Orchestration Execution**
   - `MainWorkflow` orchestrator coordinates the process
   - Calls `WorkflowItem1` (may fail for ID=2)
   - Calls `WorkflowItem2` (always succeeds)
   - Logs completion status

## Key Features

### Error Handling
- `WorkflowItem1` includes intentional failure simulation for sync items with `Id == 2`
- Demonstrates how durable functions handle failures and retries

### Instance Management
- Unique instance IDs prevent duplicate orchestrations
- Instance listing capability for monitoring and debugging

### Scalability
- Queue-based architecture allows for horizontal scaling
- Timer trigger ensures consistent workload generation
- Durable orchestration provides reliability and state management

## Configuration Requirements

### Azure Storage Account
- Connection string in `AzureWebJobsStorage` setting
- Used for queue storage and durable function state

### Application Settings
```json
{
  "AzureWebJobsStorage": "your-storage-connection-string",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
}
```

## Monitoring and Observability

### Logging
- Comprehensive logging at each step of the workflow
- Instance tracking and status monitoring
- Error logging for failed activities

### Instance Tracking
- Orchestration instances can be queried and monitored
- Instance IDs follow predictable naming pattern
- Runtime status tracking for debugging

## Use Cases

This solution is ideal for:
- **Scheduled Data Synchronization**: Regular sync operations with multiple steps
- **ETL Processes**: Multi-stage data processing workflows
- **Business Process Automation**: Complex workflows with error handling
- **Batch Processing**: Queue-based processing with orchestration

## Deployment

1. Ensure Azure Storage Account is configured
2. Update connection strings in application settings
3. Deploy to Azure Functions with .NET 8 isolated runtime
4. Monitor function execution through Azure Portal

## Development Notes

- Built with .NET 8 and Azure Functions v4
- Uses isolated worker process model
- Implements durable task patterns for reliable execution
- Includes comprehensive error handling and logging