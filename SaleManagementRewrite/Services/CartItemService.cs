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

public class CartItemService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User> userManager)
    : ICartItemService
{
    public async Task<Result<CartItem>> AddItemToCart(AddItemToCartRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CartItem>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CartItem>.Failure("User not found", ErrorType.NotFound);
        }
        if (!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<CartItem>.Failure("User not permitted", ErrorType.Conflict);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        
        var item =  await dbContext.Items.Include(i => i.Shop).FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return Result<CartItem>.Failure("Item not found", ErrorType.NotFound);
        }

        if (shop != null)
        {
            if (item.ShopId == shop.Id)
            {
                return Result<CartItem>.Failure("Cannot add your item to the cart", ErrorType.Conflict);
            }
        }
        if (request.Quantity <= 0 || request.Quantity > item.Stock && item.Stock > 0)
        {
            return Result<CartItem>.Failure("QuantityRequest invalid", ErrorType.Conflict);
        }

        if (item.Stock <= 0)
        {
            return Result<CartItem>.Failure("Out of stock", ErrorType.Conflict);
        }
        var cartItem = await dbContext.CartItems.FirstOrDefaultAsync(ci=>ci.ItemId == item.Id && ci.UserId == userId);
        if (cartItem != null)
        {
            cartItem.Quantity += request.Quantity;
            if (cartItem.Quantity > item.Stock)
            {
                return Result<CartItem>.Failure("Insufficient stock", ErrorType.Conflict);
            }
            dbContext.CartItems.Update(cartItem);
        }
        else
        {
            cartItem = new CartItem()
            {
                ItemId = item.Id,
                ShopId = item.ShopId,
                Quantity = request.Quantity,
                UserId = userId,
            };
            dbContext.CartItems.Add(cartItem);
        }

        try
        {
            await dbContext.SaveChangesAsync();
            return Result<CartItem>.Success(cartItem);
        }
        catch (DbUpdateException)
        {
            return Result<CartItem>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<CartItem>> UpdateQuantityItem(UpdateQuantityItemInCartRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CartItem>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CartItem>.Failure("User not found", ErrorType.NotFound);
        }
        if (!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<CartItem>.Failure("User not permitted", ErrorType.Conflict);
        }
        
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return Result<CartItem>.Failure("Item not found", ErrorType.NotFound);
        }
        var cartItem = await dbContext.CartItems.Include(ci => ci.Item)
            .FirstOrDefaultAsync(ci => ci.ItemId == request.ItemId && ci.UserId == user.Id);
        if (cartItem == null) 
        { 
            return Result<CartItem>.Failure("CartItem not found", ErrorType.NotFound);
        }
        if (request.Quantity <= 0)
        {
            return Result<CartItem>.Failure("Quantity invalid", ErrorType.Conflict);
        }
        else
        {
            if (cartItem.Item != null && request.Quantity > cartItem.Item.Stock)
            {
                return Result<CartItem>.Failure("Insufficient stock",  ErrorType.Conflict);
            }
        }
        if (item.Stock <= 0)
        {
            return Result<CartItem>.Failure("Out of stock", ErrorType.Conflict);
        }
        cartItem.Quantity= request.Quantity;
        if (cartItem.Quantity > item.Stock)
        {
            return Result<CartItem>.Failure("Insufficient stock", ErrorType.Conflict);
        }

        try
        {
            dbContext.CartItems.Update(cartItem);
            await dbContext.SaveChangesAsync();
            return Result<CartItem>.Success(cartItem);
        }
        catch (DbUpdateException)
        {
            return Result<CartItem>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> DeleteItemFromCart(DeleteItemFromCartRequest request)
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
        var isCustomer = await userManager.IsInRoleAsync(user, UserRoles.Customer);
        var isAdmin = await userManager.IsInRoleAsync(user, UserRoles.Admin);

        if (!isCustomer && !isAdmin)
        {
            return Result<bool>.Failure("User not permitted", ErrorType.Conflict);
        }
        
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return Result<bool>.Failure("Item not found",  ErrorType.NotFound);
        }
        var cartItem = await dbContext.CartItems.FirstOrDefaultAsync(ci => ci.ItemId == item.Id && ci.UserId == userId);
        if (cartItem == null)
        {
            return Result<bool>.Failure("CartItem not found",  ErrorType.NotFound);
        }

        try
        {
            dbContext.CartItems.Remove(cartItem);
            await dbContext.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("Database error",  ErrorType.Conflict);
        }
    }

    public async Task<Result<IEnumerable<CartItem>>> LoadCart()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<IEnumerable<CartItem>>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<IEnumerable<CartItem>>.Failure("User not found", ErrorType.NotFound);
        }

        var cartItems = await dbContext.CartItems.Include(ci => ci.Item).Include(ci=>ci.Shop).Where(ci => ci.UserId == user.Id).ToListAsync();
        if (!cartItems.Any())
        {
            //fix sau
            return Result<IEnumerable<CartItem>>.Failure("Not found cartItems", ErrorType.NotFound);
        }
        return Result<IEnumerable<CartItem>>.Success(cartItems);
    }

    public async Task<Result<bool>> ClearCart()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return  Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        var cartItems = await dbContext.CartItems.Include(ci => ci.Item).Include(ci=>ci.Shop).Where(ci => ci.UserId == user.Id).ToListAsync();
        if (!cartItems.Any())
        {
            return  Result<bool>.Failure("Not found cartItems", ErrorType.NotFound);
        }

        dbContext.Remove(cartItems);
        await dbContext.SaveChangesAsync();
        return Result<bool>.Success(true);
    }
}