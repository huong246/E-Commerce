using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class TransactionService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, IConfiguration configuration)
    : ITransactionService
{
    public async Task<DepositIntoBalanceResult> DepositIntoBalanceAsync(DepositIntoBalanceRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return DepositIntoBalanceResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return DepositIntoBalanceResult.UserNotFound;
        }

        if (request.Amount <= 0)
        {
            return DepositIntoBalanceResult.AmountInvalid;
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                FromUserId = null,
                ToUserId = userId,
                CreateAt = DateTime.UtcNow,
                Notes = "User deposited money into balance",
                OrderId = null,
            };
            user.Balance += request.Amount;
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return DepositIntoBalanceResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return DepositIntoBalanceResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return DepositIntoBalanceResult.DatabaseError;
        }
    }

    public async Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var buyer = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == request.BuyerId);
        var order = await dbContext.Orders.Include(o=>o.OrderShops).FirstOrDefaultAsync(o=>o.Id == request.OrderId);
        var platformWalletId = Guid.Parse(configuration["PlatformWalletId"] ?? string.Empty);
        var platformWallet = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == platformWalletId);
        if (buyer == null)
        {
            return CreatePaymentResult.UserNotFound;
        }

        if (order == null)
        {
            return CreatePaymentResult.OrderNotFound;
        }

        if (platformWallet == null)
        {
            return CreatePaymentResult.PlatformWalletNotFound;
        }

        if (order.UserId != request.BuyerId)
        {
            return CreatePaymentResult.NotPermitted;
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return CreatePaymentResult.InvalidState;
        }

        if (order.TotalAmount != request.Amount)
        {
            return CreatePaymentResult.AmountMismatch;
        }

        if (request.Amount <0)
        {
            return CreatePaymentResult.AmountInvalid;
        }
        if (buyer.Balance < request.Amount)
        {
            return CreatePaymentResult.BalanceNotEnough;
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                FromUserId = buyer.Id,
                ToUserId = platformWallet.Id,
                CreateAt = DateTime.UtcNow,
                Notes = "Buyer payment order",
                OrderId = order.Id,
                Order = order,
                Type = TransactionType.Payment,
                Status = TransactionStatus.Completed,
            };
            buyer.Balance -= request.Amount;
            platformWallet.Balance += request.Amount;
            order.Status = OrderStatus.Paid;
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return CreatePaymentResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CreatePaymentResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CreatePaymentResult.DatabaseError;
        }
    }

    public async Task<CreatePayOutResult> CreatePayOutAsync(CreatePayOutRequest request)
    {
        var seller = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == request.SellerId);
        if (seller == null)
        {
            return  CreatePayOutResult.UserNotFound;
        }
        var shop =  await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == request.SellerId);
        if (shop == null)
        {
            return CreatePayOutResult.ShopNotFound;
        }
        var platformWalletId = Guid.Parse(configuration["PlatformWalletId"] ?? string.Empty);
        var platformWallet = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == platformWalletId);
        if (platformWallet == null)
        {
            return CreatePayOutResult.PlatformWalletNotFound;
        }

        var orderShop = await dbContext.OrderShops.Include(os=>os.Order).FirstOrDefaultAsync(o => o.Id == request.OrderShopId);
        if (orderShop == null)
        {
            return CreatePayOutResult.OrderNotFound;
        }

        if (orderShop.ShopId != shop.Id)
        {
            return CreatePayOutResult.OrderNotOfShop;
        }

        if (request.Amount < 0)
        {
            return CreatePayOutResult.AmountInvalid;
        }

        if (request.Amount != orderShop.TotalShopAmount)
        {
            return CreatePayOutResult.AmountMismatch;
        }

        if (orderShop.Status != OrderShopStatus.Completed)
        {
            return CreatePayOutResult.InvalidState;
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                CreateAt = DateTime.UtcNow,
                FromUserId = platformWallet.Id,
                ToUserId = seller.Id,
                Notes = "Payment for order",
                Order = orderShop.Order,
                OrderId = orderShop.OrderId,
                OrderShop = orderShop,
                OrderShopId = orderShop.Id,
                Status = TransactionStatus.Completed,
                Type = TransactionType.PayoutToSeller,
            };
            platformWallet.Balance -= request.Amount;
            seller.Balance += request.Amount;
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return CreatePayOutResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CreatePayOutResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CreatePayOutResult.DatabaseError;
        }
    }

    public async Task<CreateRefundResult> CreateRefundAsync(CreateRefundRequest request)
    {
        var buyer = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.BuyerId);
        if (buyer == null)
        {
            return CreateRefundResult.UserNotFound;
        }
        var returnOrder = await dbContext.ReturnOrders.Include(ro => ro.Order).FirstOrDefaultAsync(o => o.Id == request.ReturnOrderId);
        if (returnOrder == null)
        {
            return CreateRefundResult.ReturnOrderNotFound;
        }
        var platformWalletId = Guid.Parse(configuration["PlatformWalletId"] ?? string.Empty);
        var platformWallet = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == platformWalletId);
        if (platformWallet == null)
        {
            return CreateRefundResult.PlatformWalletNotFound;
        }

        if (request.Amount < 0)
        {
            return CreateRefundResult.AmountInvalid;
        }

        if (request.Amount != returnOrder.Amount)
        {
            return CreateRefundResult.AmountMismatch;
        }

        if (returnOrder.Status != ReturnStatus.Approved)
        {
            return CreateRefundResult.InvalidState;
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                CreateAt = DateTime.UtcNow,
                FromUserId = platformWallet.Id,
                ToUserId = buyer.Id,
                Notes = "Refund for returnOrder",
                Order = returnOrder.Order,
                OrderId = returnOrder.OrderId,
                ReturnOrder = returnOrder,
                ReturnOrderId = returnOrder.Id,
            };
            platformWallet.Balance -= request.Amount;
            buyer.Balance += request.Amount;
            returnOrder.Status = ReturnStatus.Completed;
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return CreateRefundResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CreateRefundResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CreateRefundResult.DatabaseError;
        }
    }

    public async Task<IEnumerable<Transaction?>> GetTransactionAsync(GetTransactionForOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return [];
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return [];
        }
        
        var query = dbContext.Transactions
            .Where(t => t.OrderId == request.OrderId);
        
        switch (user.UserRole)
        {
            case UserRole.Customer:
                query = query.Where(t => t.Order != null && t.Order.UserId == userId);
                break;

            case UserRole.Seller:
                query = query.Where(t => t.OrderShop != null && t.OrderShop.Shop != null && t.OrderShop.Shop.UserId == userId);
                break;

            case UserRole.Admin:
                break;
            
            default:
                return [];
        }
        
        return await query
            .Include(t => t.Order)
            .Include(t => t.OrderShop)
            .ToListAsync();
    }
}