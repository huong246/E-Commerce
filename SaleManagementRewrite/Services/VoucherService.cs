
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class VoucherService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User> userManager)
    : IVoucherService
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public async Task<Result<Voucher>> CreateVoucher(CreateVoucherRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Voucher>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<Voucher>.Failure("User not found", ErrorType.NotFound);
        }

        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);

        if (!isSeller && !isAdmin)
        {
            return Result<Voucher>.Failure("User not permitted", ErrorType.Conflict);
        }

        if (request.Quantity < 0)
        {
            return Result<Voucher>.Failure("QuantityRequest invalid", ErrorType.Conflict);
        }

        if (request.ShopId.HasValue)
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.Id == request.ShopId && s.UserId == userId);
            if (shop == null)
            {
                return Result<Voucher>.Failure("Shop not found", ErrorType.NotFound);
            }
        }

        if (request.ItemId.HasValue)
        {
            var item = await dbContext.Items.FirstOrDefaultAsync(i =>
                i.Id == request.ItemId && i.ShopId == request.ShopId);
            if (item == null)
            {
                return Result<Voucher>.Failure("Item not found", ErrorType.NotFound);
            }
        }

        if (isSeller && request.ShopId == null)
        {
            return Result<Voucher>.Failure("ShopId not null",  ErrorType.Conflict);
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var newVoucher = new Voucher
            {
                Id = Guid.NewGuid(),
                Quantity = request.Quantity,
                VoucherMethod = request.Method,
                VoucherTarget = request.Target,
                EndDate = request.EndDate,
                StartDate = request.StartDate,
                Value = request.Value,
                Maxvalue = request.MaxDiscountAmount,
                MinSpend = request.MinSpend,
                ShopId = request.ShopId,
                ItemId = request.ItemId,
            };
            var ktr = false;
            while (ktr == false)
            {
                var newCode = GenerateCode(request.LengthCode);
                var codeExist = await dbContext.Vouchers.AnyAsync(v => v.Code == newCode);
                if (codeExist) continue;
                newVoucher.Code = newCode;
                ktr = true;
            }

            if (newVoucher.EndDate <= DateTime.Now || newVoucher.Quantity <= 0)
            {
                newVoucher.IsActive = false;
            }
            else
            {
                newVoucher.IsActive = request.IsActive;
            }

            dbContext.Vouchers.Add(newVoucher);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Voucher>.Success(newVoucher);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Voucher>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Voucher>.Failure("Database error", ErrorType.Conflict);
        }
    }

    private static string GenerateCode(int length)
    {
        var random = new Random(); 
        var stringBuilder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            stringBuilder.Append(Chars[random.Next(Chars.Length)]);
        }
        return stringBuilder.ToString();
    }

    public async Task<Voucher?> GetVoucherByIdAsync(Guid id)
    {
        return await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == id);
    }
    public async Task<Result<bool>> DeleteVoucher(DeleteVoucherRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        { 
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);

        if (!isSeller && !isAdmin)
        {
            return Result<bool>.Failure("User not permitted", ErrorType.Conflict);
        }

        Voucher? voucher = null;
        if(await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return Result<bool>.Failure("Shop not found", ErrorType.NotFound);
            }
            voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == request.VoucherId && v.ShopId == shop.Id);
        }
        else if(await userManager.IsInRoleAsync(user, UserRoles.Admin)) 
        {
            voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == request.VoucherId);
        }

        if (voucher == null)
        {
            return  Result<bool>.Failure("Voucher not found", ErrorType.NotFound);
        }

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            voucher.IsActive = false;
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Voucher>> UpdateVoucher(UpdateVoucherRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        { 
            return Result<Voucher>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<Voucher>.Failure("User not found", ErrorType.NotFound);
        }

        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);

        if (!isSeller && !isAdmin)
        {
            return Result<Voucher>.Failure("User not permitted", ErrorType.Conflict);
        }
        Voucher? voucher = null;
        if(await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return Result<Voucher>.Failure("Shop not found", ErrorType.NotFound);
            }
            voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == request.VoucherId && v.ShopId == shop.Id);
        }
        else if(await userManager.IsInRoleAsync(user, UserRoles.Admin)) 
        {
            voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == request.VoucherId);
        }

        if (voucher == null)
        {
            return Result<Voucher>.Failure("Voucher not found",  ErrorType.NotFound);
        }

        var noChanges = (request.Quantity == voucher.Quantity || request.Quantity == null)
                        && (request.IsActive == voucher.IsActive || request.IsActive == null)
                        && (request.StartDate == voucher.StartDate || request.StartDate == null)
                        && (request.EndDate == voucher.EndDate || request.EndDate == null)
                        && (request.Method == voucher.VoucherMethod || request.Method == null)
                        && (request.Target == voucher.VoucherTarget || request.Target == null)
                        && (request.MaxDiscountAmount == voucher.Maxvalue || request.MaxDiscountAmount == null)
                        && (request.MinSpend == voucher.MinSpend || request.MinSpend == null)
                        && (request.IsActive == voucher.IsActive || request.IsActive == null);
        if (noChanges)
        {
            return Result<Voucher>.Failure("Duplicate value",  ErrorType.Conflict);
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            voucher.Quantity = request.Quantity ?? voucher.Quantity;
            voucher.ItemId = request.ItemId ?? voucher.ItemId;
            voucher.VoucherMethod = request.Method ?? voucher.VoucherMethod;
            voucher.VoucherTarget = request.Target?? voucher.VoucherTarget;
            voucher.StartDate = request.StartDate ?? voucher.StartDate;
            voucher.EndDate = request.EndDate ?? voucher.EndDate;
            voucher.MinSpend = request.MinSpend ?? voucher.MinSpend;
            voucher.Maxvalue = request.MaxDiscountAmount ?? voucher.Maxvalue;
            voucher.Value = request.Value ?? voucher.Value;
            voucher.Version =  Guid.NewGuid();
            if (voucher.EndDate <= DateTime.Now || voucher.Quantity <= 0)
            {
                voucher.IsActive = false;
            }
            else
            {
                voucher.IsActive = request.IsActive ?? voucher.IsActive;
            }

            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<Voucher>.Success(voucher);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Voucher>.Failure("Concurrency conflict",  ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<Voucher>.Failure("Database error",  ErrorType.Conflict);
        }
    }
}