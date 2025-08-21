using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class TransactionService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, IConfiguration configuration, UserManager<User> userManager)
    : ITransactionService
{
    public async Task<Result<Transaction>> DepositIntoBalanceAsync(DepositIntoBalanceRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Transaction>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<Transaction>.Failure("User not found", ErrorType.NotFound);
        }

        if (request.Amount <= 0)
        {
            return Result<Transaction>.Failure("RequestAmount invalid", ErrorType.Conflict);
        }

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            user.Balance += request.Amount;
            var updateUserResult = await userManager.UpdateAsync(user);
            if (!updateUserResult.Succeeded)
            {
                await dbTransaction.RollbackAsync();
                return Result<Transaction>.Failure("Failed to update user balance.", ErrorType.Conflict);
            }

            var transaction = new Transaction()
            {
                Amount = request.Amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                FromUserId = null,
                ToUserId = userId,
                CreateAt = DateTime.UtcNow,
                Notes = "User deposited money into balance",
                OrderId = null,
            };
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Transaction>.Success(transaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Transaction>> CreatePaymentAsync(CreatePaymentRequest request)
    {
        var buyer = await userManager.FindByIdAsync(request.BuyerId.ToString());
        var order = await dbContext.Orders.Include(o=>o.OrderShops).FirstOrDefaultAsync(o=>o.Id == request.OrderId);
        var platformWallet = await GetPlatformWallet();
        if (buyer == null)
        {
            return Result<Transaction>.Failure("User not found", ErrorType.NotFound);
        }

        if (order == null)
        {
            return Result<Transaction>.Failure("Order not found", ErrorType.NotFound);
        }

        if (order.UserId != request.BuyerId)
        {
            return Result<Transaction>.Failure("UserRole not permitted", ErrorType.Conflict);
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return Result<Transaction>.Failure("OrderStatus invalid", ErrorType.Conflict);
        }

        if (order.TotalAmount != request.Amount)
        {
            return Result<Transaction>.Failure("Amount mis match", ErrorType.Conflict);
        }

        if (request.Amount <0)
        {
            return Result<Transaction>.Failure("Amount invalid", ErrorType.Conflict);
        }
        if (buyer.Balance < request.Amount)
        {
            return Result<Transaction>.Failure("RequestAmount invalid", ErrorType.Conflict);
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
            var buyerUpdateResult = await userManager.UpdateAsync(buyer);
            var platformUpdateResult = await userManager.UpdateAsync(platformWallet);
            if (!buyerUpdateResult.Succeeded || !platformUpdateResult.Succeeded)
            {
                await dbTransaction.RollbackAsync();
                return Result<Transaction>.Failure("Failed to update balances.", ErrorType.Conflict);
            }
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Transaction>.Success(transaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Transaction>> CreatePayOutAsync(CreatePayOutRequest request)
    {
        var seller = await userManager.FindByIdAsync(request.SellerId.ToString());
        if (seller == null)
        {
            return Result<Transaction>.Failure("User not found", ErrorType.NotFound);
        }
        var shop =  await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == request.SellerId);
        if (shop == null)
        {
            return Result<Transaction>.Failure("Shop not found", ErrorType.NotFound);
        }
        var platformWallet = await GetPlatformWallet();

        var orderShop = await dbContext.OrderShops.Include(os=>os.Order).FirstOrDefaultAsync(o => o.Id == request.OrderShopId);
        if (orderShop == null)
        {
            return Result<Transaction>.Failure("OrderShop not found", ErrorType.NotFound);
        }

        if (orderShop.ShopId != shop.Id)
        {
            return Result<Transaction>.Failure("Order not belong to shop", ErrorType.Conflict);
        }

        if (request.Amount < 0)
        {
            return Result<Transaction>.Failure("RequestAmount invalid", ErrorType.Conflict);
        }

        if (request.Amount != orderShop.TotalShopAmount)
        {
            return Result<Transaction>.Failure("Amount mis match", ErrorType.Conflict);
        }

        if (orderShop.Status != OrderShopStatus.Completed)
        {
            return Result<Transaction>.Failure("OrderShopStatus invalid", ErrorType.Conflict);
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
            orderShop.Status = OrderShopStatus.Completed;
            var sellerUpdateResult = await userManager.UpdateAsync(seller);
            var platformUpdateResult = await userManager.UpdateAsync(platformWallet);
            if (!sellerUpdateResult.Succeeded || !platformUpdateResult.Succeeded)
            {
                await dbTransaction.RollbackAsync();
                return Result<Transaction>.Failure("Failed to update balances.", ErrorType.Conflict);
            }
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Transaction>.Success(transaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Transaction>> CreateRefundAsync(CreateRefundRequest request)
    {
        var buyer = await userManager.FindByIdAsync(request.BuyerId.ToString());
        if (buyer == null)
        {
            return Result<Transaction>.Failure("User not found", ErrorType.NotFound);
        }
        var returnOrder = await dbContext.ReturnOrders.Include(ro => ro.Order).FirstOrDefaultAsync(o => o.Id == request.ReturnOrderId);
        if (returnOrder == null)
        {
            return Result<Transaction>.Failure("ReturnOrder not found", ErrorType.NotFound);
        }
        var platformWallet = await GetPlatformWallet();

        if (request.Amount < 0)
        {
            return Result<Transaction>.Failure("RequestAmount invalid", ErrorType.Conflict);
        }

        if (request.Amount != returnOrder.Amount)
        {
            return Result<Transaction>.Failure("Amount mis match", ErrorType.Conflict);
        }

        if (returnOrder.Status != ReturnStatus.Approved)
        {
            return Result<Transaction>.Failure("ReturnStatus invalid", ErrorType.Conflict);
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
                Status = TransactionStatus.Completed, 
                Type = TransactionType.Refund,
            };
            platformWallet.Balance -= request.Amount;
            buyer.Balance += request.Amount;
            returnOrder.Status = ReturnStatus.Completed;
            var buyerUpdateResult = await userManager.UpdateAsync(buyer);
            var platformUpdateResult = await userManager.UpdateAsync(platformWallet);

            if (!buyerUpdateResult.Succeeded || !platformUpdateResult.Succeeded)
            {
                await dbTransaction.RollbackAsync();
                return Result<Transaction>.Failure("Failed to update balances.", ErrorType.Conflict);
            }
            await dbContext.Transactions.AddAsync(transaction);
            dbContext.ReturnOrders.Update(returnOrder);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Transaction>.Success(transaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Transaction>> CreateRefundWhenCancelAsync(CreateRefundWhenCancelRequest request)
    {
        var cancelRequest = await dbContext.CancelRequests.FirstOrDefaultAsync(c=>c.Id == request.CancelRequestId);
        if (cancelRequest == null)
        {
            return Result<Transaction>.Failure("CancelRequest not found",  ErrorType.NotFound);
        }
        var buyer = await userManager.FindByIdAsync(request.BuyerId.ToString());
        if (buyer == null)
        {
            return Result<Transaction>.Failure("User not found",  ErrorType.NotFound);
        }
        if (cancelRequest.Status != RequestStatus.Approved)
        {
            return Result<Transaction>.Failure("CancelRequestStatus not approved", ErrorType.Conflict);
        }

        if (cancelRequest.UserId != buyer.Id)
        {
            return Result<Transaction>.Failure("Buyer invalid", ErrorType.Conflict);
        }
        if (request.Amount < 0)
        {
            return Result<Transaction>.Failure("RequestAmount invalid", ErrorType.Conflict);
        }

        if (request.Amount != cancelRequest.Amount)
        {
            return Result<Transaction>.Failure("Amount mis match", ErrorType.Conflict);
        }
        var platformWallet = await GetPlatformWallet();
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction()
            {
                Amount = request.Amount,
                CreateAt = DateTime.UtcNow,
                FromUserId = platformWallet.Id,
                ToUserId = buyer.Id,
                Notes = "Refund for cancelOrderShop",
                OrderId = cancelRequest.OrderId,
                OrderShopId = cancelRequest.OrderShopId,
                Status = TransactionStatus.Completed,
                Type = TransactionType.Refund,
                CancelRequestId = cancelRequest.Id,
            };
            platformWallet.Balance -= request.Amount;
            buyer.Balance += request.Amount;
            var buyerUpdateResult = await userManager.UpdateAsync(buyer);
            var platformUpdateResult = await userManager.UpdateAsync(platformWallet);
            if (!buyerUpdateResult.Succeeded || !platformUpdateResult.Succeeded)
            {
                await dbTransaction.RollbackAsync();
                return Result<Transaction>.Failure("Failed to update balances.", ErrorType.Conflict);
            }
            await dbContext.Transactions.AddAsync(transaction);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Transaction>.Success(transaction);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Transaction>> CreateRefundWhenSellerCancelAsync(
        CreateRefundWhenSellerCancelRequest request)
    {
        var orderShop = await dbContext.OrderShops.Include(os=>os.Order).ThenInclude(o=>o.User).FirstOrDefaultAsync(o => o.Id == request.OrderShopId);
        if (orderShop == null)
        {
            return Result<Transaction>.Failure("OrderShop not found",  ErrorType.NotFound);
        }
        var order = await dbContext.Orders.Include(o=>o.User).FirstOrDefaultAsync(o=>o.Id == orderShop.OrderId);
        if (order == null)
        {
            return Result<Transaction>.Failure("Order not found",  ErrorType.NotFound);
        }

        var buyer = await userManager.FindByIdAsync(order.UserId.ToString());
        if (buyer == null)
        {
            return Result<Transaction>.Failure("Buyer not found",  ErrorType.NotFound);
        }
        var platformWallet = await GetPlatformWallet();
        if (order.Status != OrderStatus.Paid)
        {
            return Result<Transaction>.Failure("OrderStatus not paid",  ErrorType.Conflict);
        }

        if (request.Amount < 0)
        {
            return Result<Transaction>.Failure("RequestAmount invalid",  ErrorType.Conflict);
        }

        if (request.Amount != orderShop.TotalShopAmount)
        {
            return Result<Transaction>.Failure("TotalShopAmount mismatch", ErrorType.Conflict);
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var transaction = new Transaction()
            {
                Amount = request.Amount,
                CreateAt = DateTime.UtcNow,
                FromUserId = platformWallet.Id,
                ToUserId = buyer.Id,
                Notes = "Refund for cancelOrderShop",
                OrderId = order.Id,
                OrderShopId = orderShop.Id,
                Status = TransactionStatus.Completed,
                Type = TransactionType.Refund,
            };
            await dbContext.Transactions.AddAsync(transaction);
            platformWallet.Balance -= request.Amount;
            buyer.Balance += request.Amount;
            var buyerUpdateResult = await userManager.UpdateAsync(buyer);
            var platformUpdateResult = await userManager.UpdateAsync(platformWallet);
            if (!buyerUpdateResult.Succeeded || !platformUpdateResult.Succeeded)
            {
                await dbTransaction.RollbackAsync();
                return Result<Transaction>.Failure("Failed to update balances.", ErrorType.Conflict);
            }
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Transaction>.Success(transaction);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Transaction>.Failure("Database error", ErrorType.Conflict);
        }
    }
    public async Task<Result<IEnumerable<Transaction?>>> GetTransactionAsync(GetTransactionForOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<IEnumerable<Transaction?>>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<IEnumerable<Transaction?>>.Failure("User not found", ErrorType.NotFound);
        }
        
        var query = dbContext.Transactions.Where(t => t.OrderId == request.OrderId);
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(UserRoles.Admin))
        {
        }
        else if (roles.Contains(UserRoles.Customer))
        {
            query = query.Where(t => t.Order != null && t.Order.UserId == user.Id);
        }
        else if (roles.Contains(UserRoles.Seller))
        {
            query = query.Where(t => t.OrderShop != null && t.OrderShop.Shop != null && t.OrderShop.Shop.UserId == user.Id);
        }
        else
        {
            return Result<IEnumerable<Transaction?>>.Failure("User role not permitted to view transactions.", ErrorType.Forbidden);
        }
        
        var transactions = await query
            .Include(t => t.Order)
            .Include(t => t.OrderShop)
            .ToListAsync();
        return Result<IEnumerable<Transaction?>>.Success(transactions);
    }

    public async Task<string> CreatePaymentVnPayUrlAsync(CreatePaymentVnPayRequest request)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order == null || order.Status != OrderStatus.PendingPayment)
        {
            throw new Exception("Order invalid");
        }
        var vnPaySettings = configuration.GetSection("VnPaySettings");
        var vnpUrl = vnPaySettings["BaseUrl"];
        var vnpTmnCode = vnPaySettings["TmnCode"];
        var vnpHashSecret = vnPaySettings["HashSecret"];
        var vnpReturnUrl = vnPaySettings["ReturnUrl"];
        var vnPayLibrary = new VnPayLibrary();

        vnPayLibrary.AddRequestData("vnp_Version", "2.1.0");
        vnPayLibrary.AddRequestData("vnp_Command", "pay");
        if (vnpTmnCode != null) vnPayLibrary.AddRequestData("vnp_TmnCode", vnpTmnCode);
        vnPayLibrary.AddRequestData("vnp_Amount", ((long)order.TotalAmount * 100).ToString());
        vnPayLibrary.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnPayLibrary.AddRequestData("vnp_CurrCode", "VND");
        vnPayLibrary.AddRequestData("vnp_IpAddr", request.IpAddress);
        vnPayLibrary.AddRequestData("vnp_Locale", "vn");
        vnPayLibrary.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.Id}");
        vnPayLibrary.AddRequestData("vnp_OrderType", "other");
        if (vnpReturnUrl != null) vnPayLibrary.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
        vnPayLibrary.AddRequestData("vnp_TxnRef", order.Id.ToString());
        return vnPayLibrary.CreateRequestUrl(vnpUrl, vnpHashSecret);
    }

    public async Task<VnpayIpnResponse> ProcessIpnAsync(IQueryCollection vnPayData)
    {
        var vnPay = new VnPayLibrary();
        var vnpHashSecret = configuration["VnPaySettings:HashSecret"];

        foreach (var (key, value) in vnPayData)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnPay.AddResponseData(key, value.ToString());
            }
        }
        var vnpSecureHash = vnPay.GetResponseData("vnp_SecureHash");
        var isValidSignature = vnpHashSecret != null && vnPay.ValidateSignature(vnpSecureHash, vnpHashSecret);

        if (!isValidSignature)
        {
            return new VnpayIpnResponse("97", "Invalid Signature");
        }
        var vnpResponseCode = vnPay.GetResponseData("vnp_ResponseCode");
        var vnpTxnRef = vnPay.GetResponseData("vnp_TxnRef");

        if (vnpResponseCode == "00") 
        {
            var orderId = Guid.Parse(vnpTxnRef);
            var order = await dbContext.Orders.FindAsync(orderId);

            if (order == null)
            {
                return new VnpayIpnResponse("01", "Order not found");
            }
            
            if (order.Status == OrderStatus.PendingPayment)
            {
                await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
                try
                {
                    order.Status = OrderStatus.Paid;
                    await dbContext.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return new VnpayIpnResponse("00", "Confirm Success");
                }
                catch (DbUpdateException)
                {
                    await dbTransaction.RollbackAsync();
                    return new VnpayIpnResponse("99", "An error occurred during processing");
                }
                
            }
            else
            {
                return new VnpayIpnResponse("02", "Order already confirmed");
            }
        }
        else
        {
            return new VnpayIpnResponse("99", "Transaction failed");
        }
    }

    private async Task<User> GetPlatformWallet()
    {
        var platformWalletId = configuration["PlatformWalletId"];
        if (string.IsNullOrEmpty(platformWalletId))
        {
            throw new InvalidOperationException("PlatformWalletId is not configured.");
        }
        var platformWallet = await userManager.FindByIdAsync(platformWalletId);
        return platformWallet ?? throw new InvalidOperationException("Platform wallet user not found.");
    }
}