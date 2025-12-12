namespace CmdSeeding.Shared.Commands;

using System.CommandLine;

public interface ICommandProvider
{
	IEnumerable<Command> GetCommands(IServiceProvider services);
}
