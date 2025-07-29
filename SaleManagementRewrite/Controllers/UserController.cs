using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.IServices;
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
        return result switch
        {
            RegisterRequestResult.Success => Ok("Register Success"),
            RegisterRequestResult.UsernameExists => Conflict("Username already exists"),
            RegisterRequestResult.PasswordLengthNotEnough => Conflict("Password length not enough"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("login_user")]
    public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
    {
        var result = await userService.LoginUser(request);
        if (result.LonginUserResultType == LoginUserResultType.Success)
        {
            return Ok(new{token = result.AccessToken});
        }
        return Unauthorized("username or password is incorrect");
    }

    [HttpPost("logout_user")]
    [Authorize]
    public async Task<IActionResult> LogoutUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest("Invalid user id");
        }

        await userService.LogOutUser();
        return Ok("Logged out successfully");
    }
    
}