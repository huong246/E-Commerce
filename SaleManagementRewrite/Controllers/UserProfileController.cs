using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfileController(IUserProfileService userProfileService) : ControllerBase
{

    [HttpGet("get_user_profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var result = await userProfileService.GetUserProfileAsync();
        return HandleResult(result);
    }

    [HttpPost("update_user_profile")]
    public async Task<IActionResult> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        var result = await userProfileService.UpdateUserProfileAsync(request);
        return HandleResult(result);
    }

    [HttpPost("update_password")]
    public async Task<IActionResult> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var result = await userProfileService.UpdatePasswordAsync(request);
        return HandleResult(result);
    }
    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

         
        if (typeof(T) == typeof(bool))
        {
            return NoContent(); // HTTP 204
        }
        
        return Ok(result.Value);
    }

    private IActionResult HandleFailure<T>(Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(result.Error),
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            ErrorType.BadRequest  => BadRequest(result.Error),
            _ => StatusCode(500, result.Error)  
        };
    }
}