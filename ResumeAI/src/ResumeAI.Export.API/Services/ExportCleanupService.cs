using ResumeAI.Export.API.Interfaces;

namespace ResumeAI.Export.API.Services;

public class ExportCleanupService(
    IServiceProvider serviceProvider,
    ILogger<ExportCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Export Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Running daily cleanup of expired export files.");
                
                using (var scope = serviceProvider.CreateScope())
                {
                    var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();
                    await exportService.CleanupExpiredExportsAsync();
                }

                logger.LogInformation("Cleanup completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during export cleanup.");
            }

            // Wait for 24 hours before next execution
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }

        logger.LogInformation("Export Cleanup Service is stopping.");
    }
}
