using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UserProfileController(IUserProfileService userProfileService) : ControllerBase
{
    private readonly IUserProfileService _userProfileService = userProfileService;

    [HttpGet("get_user_profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var result =  await _userProfileService.GetUserProfileAsync();
        return Ok(result);
    }

    [HttpPost("update_user_profile")]
    public async Task<IActionResult> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        var result = await _userProfileService.UpdateUserProfileAsync(request);
        return result switch
        {
            UpdateUserProfileResult.Success => Ok("User Profile Updated Success"),
            UpdateUserProfileResult.TokenInvalid => BadRequest("Invalid Token"),
            UpdateUserProfileResult.DuplicateValue => BadRequest("Duplicate Value"),
            UpdateUserProfileResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("update_password")]
    public async Task<IActionResult> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var result = await _userProfileService.UpdatePasswordAsync(request);
        return result switch
        {
            UpdatePasswordResult.Success => Ok("Password Updated Success"),
            UpdatePasswordResult.TokenInvalid => BadRequest("Invalid Token"),
            UpdatePasswordResult.DuplicateValue => BadRequest("Duplicate Value"),
            UpdatePasswordResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "Database Error"),
        };
    }
}