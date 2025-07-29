using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Services;

public class CompleteOrderService(IServiceProvider serviceProvider, ILogger<CompleteOrderService> logger)
    : BackgroundService
{
    private const int ReturnPeriodInDays = 7;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Complete Order Service is starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while completing orders.");
            }
            
            logger.LogInformation("Complete Order Service is waiting for the next run.");
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }

        logger.LogInformation("Complete Order Service is stopping.");
    }
    private async Task DoWork(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            
        var shopsToComplete = await dbContext.OrderShops
            .Include(os => os.Order)
            .ThenInclude(o => o.OrderShops)
            .Where(os => os.Status == OrderShopStatus.Delivered &&
                         os.DeliveredDate != null &&
                         os.DeliveredDate.Value.AddDays(ReturnPeriodInDays) < DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        foreach (var shopOrder in shopsToComplete)
        {
            shopOrder.Status = OrderShopStatus.Completed;

            logger.LogInformation("OrderShop {ShopOrderId} has been marked as completed.", shopOrder.Id);
            var parentOrder = shopOrder.Order;
            Debug.Assert(parentOrder != null, nameof(parentOrder) + " != null");
            if (parentOrder.OrderShops.All(s => s.Status == OrderShopStatus.Completed))
            {
                parentOrder.Status = OrderStatus.Completed;
                logger.LogInformation("Entire Order {ParentOrderId} has been marked as completed.", parentOrder.Id);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}