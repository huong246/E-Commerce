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

public class ShopService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User> userManager)
    : IShopService
{
    public async Task<Result<Shop>> CreateShop(CreateShopRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Shop>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<Shop>.Failure("User not found", ErrorType.NotFound);
        }
        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);

        if (!isSeller)
        {
            return Result<Shop>.Failure("User not permitted", ErrorType.Conflict);
        }

        var shopTest = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shopTest != null)
        {
            return Result<Shop>.Failure("Shop exist", ErrorType.Conflict);
        }

        var address = new Address()
            {
                Id = Guid.NewGuid(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Name = request.NameAddress,
                User = user,
                UserId = userId,
                IsDefault = true,
            };

            var shop = new Shop()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                PrepareTime = request.PrepareTime,
                UserId = userId,
                User = user,
                Address = address,
                AddressId = address.Id,
            };
            try
            {
                await dbContext.Addresses.AddAsync(address);
                await dbContext.Shops.AddAsync(shop);
                await dbContext.SaveChangesAsync();
                return Result<Shop>.Success(shop);
            }
            catch (DbUpdateException)
            {
                return Result<Shop>.Failure("Database error", ErrorType.Conflict);
            } 
    }

    public async Task<Shop?> GetShopByIdAsync(Guid id)
    {
        return await dbContext.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }
    public async Task<Result<Shop>> UpdateShop(UpdateShopRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Shop>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<Shop>.Failure("User not found", ErrorType.NotFound);
        }

        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);

        if (!isSeller && !isAdmin)
        {
            return Result<Shop>.Failure("User not permitted", ErrorType.Conflict);
        }
        Shop? shop;
        if (isAdmin)
        {
            shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.Id == request.Id);
        }
        else
        {
            shop = await dbContext.Shops.Include(shop => shop.Address)
                .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId);
        }
        if (shop == null)
        {
            return Result<Shop>.Failure("Shop not found", ErrorType.NotFound);
        }
        Address? address;
        if (request.AddressId != null)
        {
            address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId);
            if (address == null)
            { 
                return Result<Shop>.Failure("Address not found", ErrorType.NotFound);
            }
        }
        else if (request is { Latitude: not null, Longitude: not null, NameAddress: not null })
        {
            var addressDefault = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && shop.Address != null && a.Name == shop.Address.Name);
            if (addressDefault != null)
            {
                addressDefault.IsDefault = false;
            }
            address = new Address()
            {
                Id = Guid.NewGuid(),
                Latitude = (double)request.Latitude,
                Longitude = (double)request.Longitude,
                Name = request.NameAddress,
                User = user,
                UserId = userId,
                IsDefault = true,
            };
            shop.Address = address;
            dbContext.Addresses.Add(address);
        }
        else
        {
            return Result<Shop>.Failure("Do not have address",  ErrorType.Conflict);
        }
        var noChanges = (request.Name == null || request.Name == shop.Name) && 
                        (request.PrepareTime == null || request.PrepareTime == shop.PrepareTime) &&
                        (request.Latitude == null || request.Latitude == address.Latitude) &&
                        (request.Longitude == null || request.Longitude == address.Longitude) &&
                        (request.NameAddress == null || request.NameAddress == address.Name);
        if (noChanges)
        { 
            return Result<Shop>.Failure("Duplicate value", ErrorType.Conflict);
        }
        address.Latitude = request.Latitude ?? address.Latitude;
        address.Longitude = request.Longitude ?? address.Longitude;
        address.Name = request.Name ?? address.Name;
        shop.Name = request.Name ?? shop.Name;
        shop.Address = address;
        shop.PrepareTime = request.PrepareTime ?? shop.PrepareTime;
        try
        {
            dbContext.Addresses.Update(address); 
            dbContext.Update(shop);
            await dbContext.SaveChangesAsync();
            return Result<Shop>.Success(shop);
        }
        catch(DbUpdateException)
        {
            return Result<Shop>.Failure("Database error", ErrorType.Conflict);
        }
    }
}

    