using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace DurableFunctionSolution1;

public class Function1
{
    private readonly ILogger _logger;

    public Function1(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
    }

    [Function("Function1")]
    [QueueOutput("output-queue", Connection = "AzureWebJobsStorage")]
    public MySyncItem[] Run(
        [TimerTrigger("*/15 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {ExecutionTime}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextSchedule}", myTimer.ScheduleStatus.Next);
        }

        // Use a string array to return more than one message.
        MySyncItem[] messages =
            {
                new MySyncItem
                {
                    Id = 1,
                    SyncName = "Sync1",
                    TriggerDateUtc = DateTime.UtcNow
                },
                new MySyncItem
                {
                    Id = 2,
                    SyncName = "Sync2",
                    TriggerDateUtc = DateTime.UtcNow.AddMinutes(5)
                }
            };

        _logger.LogInformation("{Msg1},{Msg2}", messages[0], messages[1]);

        // Queue Output messages
        return messages;
    }

    // create a new azure durable function to handle the output queue
    [Function("SyncQueueProcessor")]
    public async Task SyncQueueProcessor(
        [QueueTrigger("output-queue", Connection = "AzureWebJobsStorage")] MySyncItem mySyncItem,
        [DurableClient] DurableTaskClient starter,
        FunctionContext context)
    {
        // write for each async the following code



        var instances = starter.GetAllInstancesAsync(new OrchestrationQuery
        {
            InstanceIdPrefix = "Workflow-"
        });

        await foreach (var instance in instances)
        {
            Console.WriteLine($" ->> InstanceId={instance.InstanceId} ** Name={instance.Name} ** RuntimeStatus={instance.RuntimeStatus}");
        }

        var logger = context.GetLogger(nameof(SyncQueueProcessor));
        logger.LogInformation("C# Durable function triggered for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
        // Start a new orchestration instance
        string instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(
            orchestratorName: nameof(MainWorkflow),
            input: mySyncItem,
            options: new StartOrchestrationOptions
            {
                InstanceId = $"Workflow-{mySyncItem.Id}-{mySyncItem.SyncName}-{DateTime.UtcNow:yyyyMMdd}"
            });
        logger.LogInformation("Started orchestration with ID = '{InstanceId}'.", instanceId);
    }

    // create a new orchestrator trigger function to handle the "MainWorkflow"
    [Function("MainWorkflow")]
    public async Task MainWorkflow(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var mySyncItem = context.GetInput<MySyncItem>();
        var logger = context.CreateReplaySafeLogger(nameof(MainWorkflow));
        logger.LogInformation("C# Orchestrator function started for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
        // Here you can add your orchestration logic
        // For example, calling an activity function or another orchestrator
        // Simulate some work

        logger.LogInformation("Sample business logic - Processing SyncItem: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);

        // activity trigger 1
        await context.CallActivityAsync("WorkflowItem1", mySyncItem);

        // activity trigger 2
        await context.CallActivityAsync("WorkflowItem2", mySyncItem);

        logger.LogInformation("C# Orchestrator function completed for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
    }

    // create a new activity function to handle the "WorkflowItem1"
    [Function("WorkflowItem1")]
    public async Task WorkflowItem1(
        [ActivityTrigger] MySyncItem mySyncItem,
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(WorkflowItem1));
        logger.LogInformation("C# Activity WorkflowItem2 started for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
        if (mySyncItem.Id == 2)
        {
            throw new Exception("Unexpected failed.");
        }

        // Simulate some work
        await Task.Delay(1000); // Simulating work with a delay
        logger.LogInformation("C# Activity WorkflowItem1 completed for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
    }

    [Function("WorkflowItem2")]
    public async Task WorkflowItem2(
        [ActivityTrigger] MySyncItem mySyncItem,
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(WorkflowItem2));
        logger.LogInformation("C# Activity WorkflowItem2 started for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
        // Simulate some work
        await Task.Delay(1000); // Simulating work with a delay
        logger.LogInformation("C# Activity WorkflowItem2 completed for: {Id}, {SyncName}, {TriggerDateUtc}",
            mySyncItem.Id, mySyncItem.SyncName, mySyncItem.TriggerDateUtc);
    }
}

public sealed record MySyncItem
{
    public int Id { get; set; }
    public string SyncName { get; set; }
    public DateTime TriggerDateUtc { get; set; }
}