using CmdSeeding.Slices.UserSeeding.Commands;
using CmdSeeding.Slices.UserSeeding.Services;
using CmdSeeding.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;

// Configuration setup
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Serilog setup
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Command-based Seeding Application");

    // Host builder with Serilog
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            // Database
            services.AddDatabase(configuration.GetConnectionString("DefaultConnection")!);
            
            // Logging
            services.AddSingleton(Log.Logger);
            
            // User Seeding Slice
            services.AddScoped<IUserSeeder, UserSeeder>();
            services.AddScoped<SeedUserCommand>();
            services.AddScoped<UpdateUserCommand>();
        })
        .Build();

    // Command line setup
    var rootCommand = new RootCommand("Command-based seeding application for EF Core 9");

    var seedCommand = new Command("seed", "Seed data from JSON files");
    var entityArgument = new Argument<string>("entity", "Entity type to seed (e.g., users)");
    var fileOption = new Option<string>("--file", "Path to JSON file");
    
    seedCommand.AddArgument(entityArgument);
    seedCommand.AddOption(fileOption);

    var updateCommand = new Command("update-users", "Update user data");
    var updateFileOption = new Option<string>("--file", "Path to JSON file");
    updateCommand.AddOption(updateFileOption);

    // Command handlers
    seedCommand.SetHandler(async (string entity, string? file) =>
    {
        using var scope = host.Services.CreateScope();
        var logger = Log.ForContext("Operation", "Seed");
        
        try
        {
            logger.Information("Starting seed operation for entity: {Entity}", entity);
            
            var result = entity.ToLower() switch
            {
                "users" => await scope.ServiceProvider.GetRequiredService<SeedUserCommand>().ExecuteAsync(file),
                _ => throw new ArgumentException($"Unknown entity type: {entity}")
            };
            
            logger.Information("Seed operation completed for entity: {Entity} with result: {Result}", entity, result);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during seed operation for entity: {Entity}", entity);
        }
    }, entityArgument, fileOption);

    updateCommand.SetHandler(async (string? file) =>
    {
        using var scope = host.Services.CreateScope();
        var logger = Log.ForContext("Operation", "Update");
        
        try
        {
            logger.Information("Starting update operation for users");
            
            var result = await scope.ServiceProvider.GetRequiredService<UpdateUserCommand>().ExecuteAsync(file);
            
            logger.Information("Update operation completed for users with result: {Result}", result);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during update operation for users");
        }
    }, updateFileOption);

    rootCommand.AddCommand(seedCommand);
    rootCommand.AddCommand(updateCommand);

    // Execute the command
    var exitCode = await rootCommand.InvokeAsync(args);
    
    Log.Information("Application completed with exit code: {ExitCode}", exitCode);
    return exitCode;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
