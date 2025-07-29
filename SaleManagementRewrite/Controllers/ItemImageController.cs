using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ItemImageController(IItemImageService itemImageService) : ControllerBase
{
    [HttpPost("upload_item_image")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> UploadItemImage([FromForm] UploadItemImageRequest request)
    {
        var result = await itemImageService.UploadItemImage(request);
        return result switch
        {
            UploadItemImageResult.Success => Ok("Item image uploaded successfully"),
            UploadItemImageResult.TokenInvalid => BadRequest("Token invalid"),
            UploadItemImageResult.UserNotFound => NotFound("User not found"),
            UploadItemImageResult.ItemNotFound => NotFound("Item not found"),
            UploadItemImageResult.FileInvalid => BadRequest("File invalid"),
            UploadItemImageResult.ShopNotFound => NotFound("Shop not found"),
            _ => StatusCode(500, "DatabaseError"),
        };
    }

    [HttpDelete("delete_item_image")]
    [Authorize(Roles = $"{nameof(UserRole.Seller) }, {nameof(UserRole.Admin)}" )]
    public async Task<IActionResult> DeleteItemImage([FromForm] DeleteItemImageRequest request)
    {
        var result = await itemImageService.DeleteItemImage(request);
        return result switch
        {
            DeleteItemImageResult.Success => Ok("Item image deleted successfully"),
            DeleteItemImageResult.TokenInvalid => BadRequest("Token invalid"),
            DeleteItemImageResult.UserNotFound => NotFound("User not found"),
            DeleteItemImageResult.ShopNotFound  => NotFound("Shop not found"),
            DeleteItemImageResult.ItemNotFound => NotFound("Item not found"),
            DeleteItemImageResult.ItemImageItemNotFound => NotFound("ItemImage not found"),
            _ => StatusCode(500, "DatabaseError"),
        };
    }

    [HttpGet("set_is_avatar")]
    [Authorize(Roles = $"{nameof(UserRole.Seller) }" )]
    public async Task<IActionResult> SetIsAvatar([FromForm] SetIsAvatarRequest request)
    {
        var result = await itemImageService.SetIsAvatar(request);
        return result switch
        {
            SetIsAvatarResult.Success => Ok("Avatar set successfully"),
            SetIsAvatarResult.TokenInvalid => BadRequest("Token invalid"),
            SetIsAvatarResult.UserNotFound => NotFound("User not found"),
            SetIsAvatarResult.ShopNotFound => NotFound("Shop not found"),
            SetIsAvatarResult.ItemNotFound => NotFound("Item not found"),
            SetIsAvatarResult.ItemImageItemNotFound => NotFound("ItemImage not found"),
            _ => StatusCode(500, "DatabaseError"),
        };
    }
}