using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum CreateItemResult
{
    Success,
    DatabaseError,
    UserNotFound,
    ShopNotFound,
    InvalidValue,
    TokenInvalid,
}

public enum UpdateItemResult
{
    Success,
    DatabaseError,
    UserNotFound,
    ShopNotFound,
    InvalidValue,
    ItemNotFound,
    TokenInvalid,
    DuplicateValue,
}

public enum DeleteItemResult
{
    Success,
    DatabaseError,
    UserNotFound,
    ShopNotFound,
    ItemNotFound,
    UserNotPermitted, 
    TokenInvalid,
}
public interface IItemService
{
    Task<CreateItemResult> CreateItemAsync(CreateItemRequest request);
    Task<UpdateItemResult> UpdateItemAsync(UpdateItemRequest request);
    Task<DeleteItemResult> DeleteItemAsync(DeleteItemRequest request);
    
}