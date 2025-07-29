
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class VoucherService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    : IVoucherService
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public async Task<CreateVoucherResult> CreateVoucher(CreateVoucherRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CreateVoucherResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return CreateVoucherResult.UserNotFound;
        }

        if (user.UserRole != UserRole.Admin && user.UserRole != UserRole.Seller)
        {
            return CreateVoucherResult.NotPermitted;
        }

        if (request.Quantity < 0)
        {
            return CreateVoucherResult.QuantityInvalid;
        }

        if (request.ShopId.HasValue)
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.Id == request.ShopId && s.UserId == userId);
            if (shop == null)
            {
                return  CreateVoucherResult.ShopNotFound;
            }
        }

        if (request.ItemId.HasValue)
        {
            var item = await dbContext.Items.FirstOrDefaultAsync(i =>
                i.Id == request.ItemId && i.ShopId == request.ShopId);
            if (item == null)
            {
                return CreateVoucherResult.ItemNotFound;
            }
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
                bool codeExist = await dbContext.Vouchers.AnyAsync(v => v.Code == newCode);
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
            return CreateVoucherResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CreateVoucherResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CreateVoucherResult.DatabaseError;
        }
    }

    private string GenerateCode(int length)
    {
        var random = new Random(); 
        var stringBuilder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            stringBuilder.Append(Chars[random.Next(Chars.Length)]);
        }
        return stringBuilder.ToString();
    }
    public async Task<DeleteVoucherResult> DeleteVoucher(DeleteVoucherRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        { 
            return DeleteVoucherResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return DeleteVoucherResult.UserNotFound;
        }
        if (user.UserRole != UserRole.Admin && user.UserRole != UserRole.Seller)
        {
            return DeleteVoucherResult.NotPermitted;
        }

        Voucher? voucher;
        switch (user.UserRole)
        {
            case UserRole.Seller:
            {
                var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
                if (shop == null)
                {
                    return DeleteVoucherResult.ShopNotFound;
                }
                voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == request.VoucherId && v.ShopId == shop.Id);
                if (voucher == null)
                {
                    return  DeleteVoucherResult.VoucherNotFound;
                }

                break;
            }
            case UserRole.Admin:
            {
                voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == request.VoucherId);
                if (voucher == null)
                {
                    return DeleteVoucherResult.VoucherNotFound;
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            voucher.IsActive = false;
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return DeleteVoucherResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return DeleteVoucherResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return DeleteVoucherResult.DatabaseError;
        }
    }

    public async Task<UpdateVoucherResult> UpdateVoucher(UpdateVoucherRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        { 
            return UpdateVoucherResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return UpdateVoucherResult.UserNotFound;
        }

        if (user.UserRole != UserRole.Admin && user.UserRole != UserRole.Seller)
        {
            return UpdateVoucherResult.NotPermitted;
        }
        Voucher? voucher;
        switch (user.UserRole)
        {
            case UserRole.Seller:
            {
                var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
                if (shop == null)
                {
                    return UpdateVoucherResult.ShopNotFound;
                }
                voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == request.VoucherId && v.ShopId == shop.Id);
                break;
            }
            case UserRole.Admin:
            {
                voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == request.VoucherId);
                break;
            }
            default:
                return UpdateVoucherResult.NotPermitted;
        }
        if (voucher == null)
        {
            return UpdateVoucherResult.VoucherNotFound;
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
            return UpdateVoucherResult.DuplicateValue;
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            dbContext.Entry(voucher).Property("RowVersion").OriginalValue = request.RowVersion;
            voucher.Quantity = request.Quantity ?? voucher.Quantity;
            voucher.ItemId = request.ItemId ?? voucher.ItemId;
            voucher.VoucherMethod = request.Method ?? voucher.VoucherMethod;
            voucher.VoucherTarget = request.Target?? voucher.VoucherTarget;
            voucher.StartDate = request.StartDate ?? voucher.StartDate;
            voucher.EndDate = request.EndDate ?? voucher.EndDate;
            voucher.MinSpend = request.MinSpend ?? voucher.MinSpend;
            voucher.Maxvalue = request.MaxDiscountAmount ?? voucher.Maxvalue;
            voucher.Value = request.Value ?? voucher.Value;
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
            return UpdateVoucherResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return UpdateVoucherResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return UpdateVoucherResult.DatabaseError;
        }
    }
}