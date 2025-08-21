using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

 
public interface IItemImageService
{
    Task<Result<ItemImage>> UploadItemImage(UploadItemImageRequest request);
    Task<Result<bool>> DeleteItemImage(DeleteItemImageRequest request);
    Task<Result<ItemImage>> SetIsAvatar(SetIsAvatarRequest request);
}