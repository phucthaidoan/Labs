using CmdSeeding.Shared.Infrastructure;
using CmdSeeding.Shared.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.CommandLine;

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

			// Command providers (auto-discovery)
			services.AddCommandProviders();
		})
		.Build();

	// Root command
	var rootCommand = new RootCommand("Command-based seeding application for EF Core 9");

	// Load commands from providers using root provider (avoid capturing disposed scopes)
	var providers = host.Services.GetServices<ICommandProvider>();
	foreach (var provider in providers)
	{
		foreach (var cmd in provider.GetCommands(host.Services))
		{
			rootCommand.AddCommand(cmd);
		}
	}

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
