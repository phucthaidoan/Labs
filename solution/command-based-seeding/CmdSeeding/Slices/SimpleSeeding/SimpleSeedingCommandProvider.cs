using System.CommandLine;
using CmdSeeding.Shared.Commands;
using CmdSeeding.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CmdSeeding.Slices.SimpleSeeding;

public class SimpleSeedingCommandProvider : ICommandProvider
{
	public IEnumerable<Command> GetCommands(IServiceProvider services)
	{
		var cmd = new Command("seed", "Run a simple seeding by name and file, returns Users count");
		var nameArg = new Argument<string>("name", "Seeding name");
		var fileOpt = new Option<string>("--file", description: "Path to JSON file");
		cmd.AddArgument(nameArg);
		cmd.AddOption(fileOpt);

		cmd.SetHandler(async (string name, string? file) =>
		{
			var logger = Log.ForContext("Operation", "SimpleSeed");
			logger.Information("Simple seeding started for {Name} with file {File}", name, file);
			using var scope = services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var count = await db.Users.CountAsync();
			logger.Information("Users count: {Count}", count);
		}, nameArg, fileOpt);

		return new[] { cmd };
	}
}

