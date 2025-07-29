using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class ShopService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    : IShopService
{
    public async Task<CreateShopResult> CreateShop(CreateShopRequest request)
    {
       var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
       if (!Guid.TryParse(userIdString, out var userId))
       {
           return CreateShopResult.TokenInvalid;
       }
       var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
       if (user == null)
       {
           return CreateShopResult.UserNotFound;
       }

       if (!user.UserRole.HasFlag(UserRole.Seller))
       {
           return CreateShopResult.BecomeASeller;
       }
       var shopTest = await dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
       if (shopTest != null)
       {
           return CreateShopResult.UserHasShop;
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
           return CreateShopResult.Success;
       }
       catch (DbUpdateException)
       {
           return CreateShopResult.DatabaseError;
       }
    }

    public async Task<UpdateShopResult> UpdateShop(UpdateShopRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateShopResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return UpdateShopResult.UserNotFound;
        }
        var shop = await dbContext.Shops.Include(shop => shop.Address).FirstOrDefaultAsync(s=>s.Id == request.Id && s.UserId == userId);
        if (shop == null)
        {
            return UpdateShopResult.ShopNotFound;
        }
        var address = await dbContext.Addresses.FirstOrDefaultAsync(a=>a.Id == request.AddressId && a.UserId == userId );
        if (address == null)
        {
            return UpdateShopResult.AddressNotFound;
        }
        bool noChanges = (request.Name == null || request.Name == shop.Name) &&
                         (request.PrepareTime == null || request.PrepareTime == shop.PrepareTime) &&
                         (request.Latitude == null || request.Latitude == address.Latitude) &&
                         (request.Longitude == null || request.Longitude == address.Longitude) &&
                         (request.NameAddress == null || request.NameAddress == address.Name);
        if (noChanges)
        {
            return UpdateShopResult.DuplicateValue;
        }
        address.Latitude = request.Latitude ?? address.Latitude;
        address.Longitude = request.Longitude ?? address.Longitude;
        address.Name = request.Name ?? address.Name;
        shop.Name = request.Name ?? shop.Name;
        shop.Address =address;
        shop.PrepareTime = request.PrepareTime ?? shop.PrepareTime;
        try
        { 
            dbContext.Addresses.Update(address);
            dbContext.Update(shop);
            await dbContext.SaveChangesAsync();
            return UpdateShopResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateShopResult.DatabaseError;
        }
    }
}