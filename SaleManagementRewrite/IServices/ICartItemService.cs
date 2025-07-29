using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum AddItemToCartResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    ShopNotFound,
    ItemNotFound,
    InsufficientStock,
    OutOfStock,
    QuantityInvalid,
    NotAddItemOwner,
}

public enum UpdateQuantityItemInCartResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    ItemNotFound,
    InsufficientStock,
    OutOfStock,
    QuantityInvalid,
    CartItemNotFound,
}
    
public enum DeleteItemFromCartResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    ItemNotFound,
    CartItemNotFound,
}
public interface ICartItemService
{
    Task<AddItemToCartResult> AddItemToCart(AddItemToCartRequest request);
    Task<UpdateQuantityItemInCartResult> UpdateQuantityItem(UpdateQuantityItemInCartRequest request);
    Task<DeleteItemFromCartResult> DeleteItemFromCart(DeleteItemFromCartRequest request);
    
}