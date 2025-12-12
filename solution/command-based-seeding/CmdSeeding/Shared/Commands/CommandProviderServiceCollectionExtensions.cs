using System.Reflection;
using CmdSeeding.Shared.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace CmdSeeding.Shared.Commands;

public static class CommandProviderServiceCollectionExtensions
{
	public static IServiceCollection AddCommandProviders(this IServiceCollection services)
	{
		var interfaceType = typeof(ICommandProvider);
		var types = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => SafeGetTypes(a))
			.Where(t => !t.IsAbstract && !t.IsInterface && interfaceType.IsAssignableFrom(t));

		foreach (var type in types)
		{
			services.AddSingleton(interfaceType, type);
		}

		return services;
	}

	private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
	{
		try { return assembly.GetTypes(); }
		catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
	}
}
