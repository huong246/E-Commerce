using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemImageController(IItemImageService itemImageService) : ControllerBase
{
    [HttpPost("upload_item_image")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> UploadItemImage([FromForm] UploadItemImageRequest request)
    {
        var result = await itemImageService.UploadItemImage(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Accepted(result);
    }

    [HttpDelete("delete_item_image")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Seller}")]
    public async Task<IActionResult> DeleteItemImage([FromForm] DeleteItemImageRequest request)
    {
        var result = await itemImageService.DeleteItemImage(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message ="ItemImage deleted successfully"});
    }

    [HttpGet("set_is_avatar")]
    [Authorize(Roles = "UserRoles.Seller")]
    public async Task<IActionResult> SetIsAvatar([FromForm] SetIsAvatarRequest request)
    {
        var result = await itemImageService.SetIsAvatar(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Accepted(result.Value);
    }
}