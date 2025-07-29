using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;

namespace SaleManagement.Services;

public class VoucherUpdateStatusService(IServiceProvider serviceProvider, ILogger<VoucherUpdateStatusService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Voucher Status Updater Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeactivateExpiredVouchers();
                await ActivateAvailableVouchers();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating voucher statuses.");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        logger.LogInformation("Voucher Status Updater Service is stopping.");
    }
    private async Task ActivateAvailableVouchers()
    {
        logger.LogInformation("Running job to activate available vouchers.");

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        var now = DateTime.UtcNow;
        var vouchersToActivate = await dbContext.Vouchers
            .Where(v => !v.IsActive && v.StartDate <= now && v.EndDate >= now && v.Quantity > 0)
            .ToListAsync();

        if (vouchersToActivate.Count != 0)
        {
            foreach (var voucher in vouchersToActivate)
            {
                voucher.IsActive = true;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Successfully activated {Count} vouchers.", vouchersToActivate.Count);
        }
        else
        {
            logger.LogInformation("No vouchers needed activation at this time.");
        }
    }

    private async Task DeactivateExpiredVouchers()
    {
        logger.LogInformation("Running job to deactivate expired or out-of-stock vouchers.");

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        var now = DateTime.UtcNow;

           
        var vouchersToUpdate = await dbContext.Vouchers
            .Where(v => v.IsActive && (v.EndDate < now || v.Quantity <= 0))
            .ToListAsync();

        if (vouchersToUpdate.Any())
        {
            foreach (var voucher in vouchersToUpdate)
            {
                voucher.IsActive = false;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation($"Successfully deactivated {vouchersToUpdate.Count} vouchers.");
        }
        else
        {
            logger.LogInformation("No vouchers needed deactivation at this time.");
        }
    }
}