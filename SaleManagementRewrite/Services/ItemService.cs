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

public class ItemService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User>  userManager)
    : IItemService
{
    public async Task<Result<CreateItemResponse>> CreateItemAsync(CreateItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CreateItemResponse>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CreateItemResponse>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            return Result<CreateItemResponse>.Failure("User not permitted", ErrorType.Conflict);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return Result<CreateItemResponse>.Failure("shop not found", ErrorType.NotFound);
        }
        if (request.Stock <= 0 || request.Price < 0)
        {
            return Result<CreateItemResponse>.Failure("Quantity invalid", ErrorType.Conflict);
        }
        var categoryExists = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
        if (categoryExists == null)
        {
            return Result<CreateItemResponse>.Failure("Category not found", ErrorType.NotFound);
        }

        var item = new Item()
        {
            Name = request.Name,
            Stock = request.Stock,
            Price = request.Price,
            Description = request.Description,
            Color = request.Color,
            Size = request.Size,
            Shop = shop,
            ShopId = shop.Id,
            Category = categoryExists,
            CategoryId = request.CategoryId,
        };
        try
        {
            await dbContext.Items.AddAsync(item);
            await dbContext.SaveChangesAsync();
            var response = new CreateItemResponse(item);
            return Result<CreateItemResponse>.Success(response);
        }
        catch (DbUpdateException)
        {
            return Result<CreateItemResponse>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<Item>> UpdateItemAsync(UpdateItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<Item>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<Item>.Failure("User not found", ErrorType.NotFound);
        }

        var isSeller = await userManager.IsInRoleAsync(user, UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);
        if (!isSeller && !isAdmin)
        {
            return Result<Item>.Failure("User not permitted", ErrorType.Conflict);
        }
        Item? item;
        if (isAdmin)
        {
            item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        }
        else
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return Result<Item>.Failure("Shop not found", ErrorType.NotFound);
            }
            item = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id ==request.ItemId && i.ShopId == shop.Id);
        }

        if (item == null)
        {
            return Result<Item>.Failure("Item not found", ErrorType.NotFound);
        }
        var noChange = ((request.Name == item.Name) || (request.Name == null))
                       && ((request.Stock == item.Stock) || (request.Stock == null))
                       && ((request.Price == item.Price) || (request.Price == null))
                       && ((request.Description == item.Description) || (request.Description == null))
                       && ((request.Size == item.Size) || (request.Size == null))
                       && ((request.Color == item.Color) || (request.Color == null))
                       && ((request.CategoryId == item.CategoryId) || (request.CategoryId == null));
        if (noChange)
        {
            return Result<Item>.Failure("Duplicate value", ErrorType.Conflict);
        }

        if (request.Price is < 0 || request.Stock is < 0)
        {
            return Result<Item>.Failure("Request invalid", ErrorType.Conflict);
        }
        if (request.CategoryId.HasValue && request.CategoryId.Value != item.CategoryId)
        {
            var categoryExists = await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
            {
                return Result<Item>.Failure("New category not found", ErrorType.NotFound);
            }
            item.CategoryId = (Guid)request.CategoryId;
        }
        item.Name = request.Name ?? item.Name;
        item.Stock = request.Stock ?? item.Stock;
        item.Price = request.Price ?? item.Price;
        item.Description = request.Description ?? item.Description;
        item.Color = request.Color ?? item.Color;
        item.Size = request.Size ?? item.Size;
        item.Version = Guid.NewGuid();

        try
        {
            dbContext.Items.Update(item);
            await dbContext.SaveChangesAsync();
            return Result<Item>.Success(item);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<Item>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            return Result<Item>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> DeleteItemAsync(DeleteItemRequest request)
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

        var isSeller = await userManager.IsInRoleAsync(user,  UserRoles.Seller);
        var isAdmin = await userManager.IsInRoleAsync(user,  UserRoles.Admin);

        if (!isSeller && !isAdmin)
        {
            return Result<bool>.Failure("User not permitted", ErrorType.Conflict);
        }
        Item? item;
        if (isAdmin)
        {
            item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        }
        else
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return Result<bool>.Failure("Shop not found", ErrorType.NotFound);
            }
            item = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id ==request.ItemId && i.ShopId == shop.Id);
        }
        if (item == null)
        {
            return Result<bool>.Failure("Item not found", ErrorType.NotFound);
        }
        try
        {
            dbContext.Items.Remove(item);
            await dbContext.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }
}