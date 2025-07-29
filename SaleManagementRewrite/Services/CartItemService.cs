using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class CartItemService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    : ICartItemService
{
    public async Task<AddItemToCartResult> AddItemToCart(AddItemToCartRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return AddItemToCartResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return AddItemToCartResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        
        var item =  await dbContext.Items.Include(i => i.Shop).FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return AddItemToCartResult.ItemNotFound;
        }

        if (shop != null)
        {
            if (item.ShopId == shop.Id)
            {
                return AddItemToCartResult.NotAddItemOwner;
            }
        }
        if (request.Quantity <= 0)
        {
            return AddItemToCartResult.QuantityInvalid;
        }

        if (request.Quantity > item.Stock && item.Stock > 0)
        {
            return AddItemToCartResult.InsufficientStock;
        }

        if (item.Stock <= 0)
        {
            return AddItemToCartResult.OutOfStock;
        }
        var cartItem = await dbContext.CartItems.FirstOrDefaultAsync(ci=>ci.ItemId == item.Id);
        if (cartItem != null)
        {
            cartItem.Quantity += request.Quantity;
            if (cartItem.Quantity > item.Stock)
            {
                return AddItemToCartResult.InsufficientStock;
            }
            dbContext.CartItems.Update(cartItem);
        }
        else
        {
            cartItem = new CartItem()
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                Item = item,
                ShopId = item.ShopId,
                Shop = item.Shop,
                Quantity = request.Quantity,
                User = user,
                UserId = userId,
            };
            dbContext.CartItems.Add(cartItem);
        }

        try
        {
            await dbContext.SaveChangesAsync();
            return AddItemToCartResult.Success;
        }
        catch (DbUpdateException)
        {
            return AddItemToCartResult.DatabaseError;
        }
    }

    public async Task<UpdateQuantityItemInCartResult> UpdateQuantityItem(UpdateQuantityItemInCartRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateQuantityItemInCartResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return UpdateQuantityItemInCartResult.UserNotFound;
        }
        
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return UpdateQuantityItemInCartResult.ItemNotFound;
        }
        var cartItem = await dbContext.CartItems.FirstOrDefaultAsync(ci => ci.ItemId == request.ItemId && ci.UserId == userId); 
        if (cartItem == null) 
        { 
            return UpdateQuantityItemInCartResult.CartItemNotFound; 
        }
        if (request.Quantity <= 0)
        {
            return UpdateQuantityItemInCartResult.QuantityInvalid;
        }

        if (item.Stock <= 0)
        {
            return UpdateQuantityItemInCartResult.OutOfStock;
        }
        
        cartItem.Quantity+= request.Quantity;
        if (cartItem.Quantity > item.Stock)
        {
            return  UpdateQuantityItemInCartResult.InsufficientStock;
        }

        try
        {
            dbContext.CartItems.Update(cartItem);
            await dbContext.SaveChangesAsync();
            return UpdateQuantityItemInCartResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateQuantityItemInCartResult.DatabaseError;
        }
    }

    public async Task<DeleteItemFromCartResult> DeleteItemFromCart(DeleteItemFromCartRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return DeleteItemFromCartResult.TokenInvalid;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return DeleteItemFromCartResult.UserNotFound;
        }
        
        var item =  await dbContext.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
        {
            return DeleteItemFromCartResult.ItemNotFound;
        }
        var cartItem = await dbContext.CartItems.FirstOrDefaultAsync(ci => ci.ItemId == item.Id && ci.UserId == userId);
        if (cartItem == null)
        {
            return DeleteItemFromCartResult.CartItemNotFound;
        }

        try
        {
            dbContext.CartItems.Remove(cartItem);
            await dbContext.SaveChangesAsync();
            return DeleteItemFromCartResult.Success;
        }
        catch (DbUpdateException)
        {
            return DeleteItemFromCartResult.DatabaseError;
        }
    }
}