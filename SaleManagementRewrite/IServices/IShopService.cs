using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum CreateShopResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    UserHasShop,
    BecomeASeller,
}

public enum UpdateShopResult
{
    Success,
    DatabaseError,
    DuplicateValue,
    TokenInvalid,
    UserNotFound,
    ShopNotFound,
    AddressNotFound,
}
public interface IShopService
{
    Task<CreateShopResult> CreateShop(CreateShopRequest request);
    Task<UpdateShopResult> UpdateShop(UpdateShopRequest request);
}