using CmdSeeding.Slices.UserSeeding.Services;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CmdSeeding.Slices.UserSeeding.Commands;

public class SeedUserCommand
{
    private readonly IUserSeeder _userSeeder;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public SeedUserCommand(IUserSeeder userSeeder, IConfiguration configuration, ILogger logger)
    {
        _userSeeder = userSeeder;
        _configuration = configuration;
        _logger = logger.ForContext<SeedUserCommand>();
    }

    public async Task<int> ExecuteAsync(string? filePath = null)
    {
        var jsonPath = filePath ?? Path.Combine(
            _configuration["Seeding:DataPath"] ?? "Data", 
            "users.json");

        _logger.Information("Executing seed user command with file: {FilePath}", jsonPath);

        var result = await _userSeeder.SeedAsync(jsonPath);

        if (result.Success)
        {
            _logger.Information("Seed user command completed successfully - Added: {AddedCount}, Skipped: {SkippedCount}", 
                result.AddedCount, result.SkippedCount);
            return 0;
        }
        else
        {
            _logger.Error("Seed user command failed: {ErrorMessage}", result.ErrorMessage);
            return 1;
        }
    }
}
