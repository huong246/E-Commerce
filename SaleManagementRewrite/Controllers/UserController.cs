using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [HttpPost("register_user")]
    public async Task<IActionResult> RegisterUser([FromBody]RegisterRequest request)
    {
        var result = await userService.RegisterUser(request);
        return HandleResult(result);
    }

    [HttpPost("login_user")]
    public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
    {
        var result = await userService.LoginUser(request);
        return HandleResult(result);
    }

    [HttpPost("logout_user")]
    [Authorize]
    public async Task<IActionResult> LogoutUser()
    {
        var result = await userService.LogOutUser();
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
            return NoContent(); 
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
            _ => StatusCode(500, result.Error)  
        };
    }

    [HttpPost("refresh_token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await userService.RefreshToken(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorType);
        }
        return Ok(result.Value);
    }
    [HttpPost("{userId}/assign-admin-role")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> AssignAdminRole(Guid userId)
    {
        var result = await userService.AssignRoleAsync(userId, UserRoles.Admin);

        if (result.Succeeded)
        {
            return Ok($"Successfully assigned Admin role to user.");
        }

        return BadRequest(result.Errors);
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await userService.ForgotPasswordAsync(request);
        return Ok(new { message = "If an account with that email address exists, a password reset link has been sent." });
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await userService.ResetPasswordAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(new { error = ErrorType.Conflict });
        }
        return Ok(new { message = "Your password has been reset successfully." });
    }
}