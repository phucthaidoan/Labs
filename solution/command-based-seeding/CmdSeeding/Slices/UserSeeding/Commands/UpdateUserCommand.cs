using CmdSeeding.Slices.UserSeeding.Services;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CmdSeeding.Slices.UserSeeding.Commands;

public class UpdateUserCommand
{
    private readonly IUserSeeder _userSeeder;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public UpdateUserCommand(IUserSeeder userSeeder, IConfiguration configuration, ILogger logger)
    {
        _userSeeder = userSeeder;
        _configuration = configuration;
        _logger = logger.ForContext<UpdateUserCommand>();
    }

    public async Task<int> ExecuteAsync(string? filePath = null)
    {
        var jsonPath = filePath ?? Path.Combine(
            _configuration["Seeding:DataPath"] ?? "Data", 
            "users.json");

        _logger.Information("Executing update user command with file: {FilePath}", jsonPath);

        var result = await _userSeeder.UpdateAsync(jsonPath);

        if (result.Success)
        {
            _logger.Information("Update user command completed successfully - Updated: {UpdatedCount}, Added: {AddedCount}", 
                result.UpdatedCount, result.AddedCount);
            return 0;
        }
        else
        {
            _logger.Error("Update user command failed: {ErrorMessage}", result.ErrorMessage);
            return 1;
        }
    }
}
