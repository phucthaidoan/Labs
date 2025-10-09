using CmdSeeding.Slices.UserSeeding.Models;

namespace CmdSeeding.Slices.UserSeeding.Services;

public interface IUserSeeder
{
    Task<SeedingResult> SeedAsync(string jsonFilePath);
    Task<SeedingResult> UpdateAsync(string jsonFilePath);
}

public record SeedingResult(int AddedCount, int UpdatedCount, int SkippedCount, bool Success, string? ErrorMessage = null);
