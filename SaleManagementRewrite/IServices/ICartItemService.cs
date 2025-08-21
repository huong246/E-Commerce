using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;
 
public interface ICartItemService
{
    Task<Result<CartItem>> AddItemToCart(AddItemToCartRequest request);
    Task<Result<CartItem>> UpdateQuantityItem(UpdateQuantityItemInCartRequest request);
    Task<Result<bool>> DeleteItemFromCart(DeleteItemFromCartRequest request);
    
}