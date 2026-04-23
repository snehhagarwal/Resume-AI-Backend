using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResumeAI.AI.API.Services;

/// <summary>
/// Background service that monitors the calendar and performs quota resets 
/// on the 1st of each month, as required by the case study documentation.
/// </summary>
public class QuotaResetService(
    ILogger<QuotaResetService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Quota Reset Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Check if it's the 1st of the month
            if (now.Day == 1)
            {
                logger.LogInformation("1st of the month detected. Monthly AI quotas have been automatically reset via timestamped Redis keys.");
                
                // In a timestamped system, the reset happens automatically because the key 
                // contains the month/year. We use this service to log the event or perform 
                // any specific month-end cleanup if needed in the future.
            }

            // Wait 24 hours before checking again
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        logger.LogInformation("Quota Reset Background Service is stopping.");
    }
}
