using Splittat.API.Services;

namespace Splittat.API.Jobs;

/// <summary>
/// Background job for cleaning up old OCR result files (older than 30 days)
/// Scheduled to run daily via Hangfire
/// </summary>
public class CleanupOcrFilesJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupOcrFilesJob> _logger;

    public CleanupOcrFilesJob(IServiceProvider serviceProvider, ILogger<CleanupOcrFilesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Deletes OCR JSON files older than the specified number of days
    /// </summary>
    public async Task CleanupOldOcrFilesAsync(int olderThanDays = 30)
    {
        using var scope = _serviceProvider.CreateScope();
        var fileStorageService = scope.ServiceProvider.GetRequiredService<FileStorageService>();

        _logger.LogInformation("Hangfire cleanup job started: Deleting OCR files older than {Days} days", olderThanDays);

        try
        {
            var deletedCount = await fileStorageService.CleanupOldOcrFilesAsync(olderThanDays);

            _logger.LogInformation("Hangfire cleanup job completed: Deleted {Count} old OCR files", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire cleanup job failed: Error cleaning up old OCR files");
            throw; // Re-throw to let Hangfire retry
        }
    }
}
