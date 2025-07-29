using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class ItemService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    : IItemService
{
    public async Task<CreateItemResult> CreateItemAsync(CreateItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CreateItemResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return CreateItemResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return CreateItemResult.ShopNotFound;
        }
        if (request.Stock <= 0 || request.Price < 0)
        {
            return CreateItemResult.InvalidValue;
        }

        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Stock = request.Stock,
            Price = request.Price,
            Description = request.Description,
            Color = request.Color,
            Size = request.Size,
            Shop = shop,
            ShopId = shop.Id,
        };
        try
        {
            await dbContext.Items.AddAsync(item);
            await dbContext.SaveChangesAsync();
            return CreateItemResult.Success;
        }
        catch (DbUpdateException)
        {
            return CreateItemResult.DatabaseError;
        }
    }

    public async Task<UpdateItemResult> UpdateItemAsync(UpdateItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateItemResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return UpdateItemResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return UpdateItemResult.ShopNotFound;
        }
        var item = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id ==request.ItemId && i.ShopId == shop.Id);
        if (item == null)
        {
            return UpdateItemResult.ItemNotFound;
        }
        bool noChange = ((request.Name == item.Name) || (request.Name == null))
                        && ((request.Stock == item.Stock) || (request.Stock == null))
                        && ((request.Price == item.Price) || (request.Price == null))
                        && ((request.Description == item.Description) || (request.Description == null))
                        && ((request.Size == item.Size) || (request.Size == null))
                        && ((request.Color == item.Color) || (request.Color == null));
        if (noChange)
        {
            return UpdateItemResult.DuplicateValue;
        }

        if (request.Price < 0 || request.Stock < 0)
        {
            return UpdateItemResult.InvalidValue;
        }
        item.Name = request.Name??item.Name;
        item.Stock = request.Stock ?? item.Stock;
        item.Price = request.Price ?? item.Price;
        item.Description = request.Description ?? item.Description;
        item.Color = request.Color ?? item.Color;
        item.Size = request.Size ?? item.Size;
        try
        {
            dbContext.Items.Update(item);
            await dbContext.SaveChangesAsync();
            return UpdateItemResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateItemResult.DatabaseError;
        }
    }

    public async Task<DeleteItemResult> DeleteItemAsync(DeleteItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return DeleteItemResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return DeleteItemResult.UserNotFound;
        }

        if (user.UserRole == UserRole.Customer)
        {
            return DeleteItemResult.UserNotPermitted;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return DeleteItemResult.ShopNotFound;
        }
        var item = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id ==request.ItemId && i.ShopId == shop.Id);
        if (item == null)
        {
            return DeleteItemResult.ItemNotFound;
        }
        try
        {
            dbContext.Items.Remove(item);
            await dbContext.SaveChangesAsync();
            return DeleteItemResult.Success;
        }
        catch (DbUpdateException)
        {
            return DeleteItemResult.DatabaseError;
        }
    }
}