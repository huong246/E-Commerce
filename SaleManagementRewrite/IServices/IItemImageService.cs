using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum UploadItemImageResult
{
    Success,
    DatabaseError,
    TokenInvalid, 
    UserNotFound,
    ShopNotFound,
    ItemNotFound,
    FileInvalid,
}

public enum DeleteItemImageResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    ShopNotFound,
    ItemNotFound,
    ItemImageItemNotFound,
}

public enum SetIsAvatarResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    ShopNotFound,
    ItemNotFound,
    ItemImageItemNotFound,
}
public interface IItemImageService
{
    Task<UploadItemImageResult> UploadItemImage(UploadItemImageRequest request);
    Task<DeleteItemImageResult> DeleteItemImage(DeleteItemImageRequest request);
    Task<SetIsAvatarResult> SetIsAvatar(SetIsAvatarRequest request);
}