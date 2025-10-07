using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace SaleManagementRewrite.Services;

public class UserService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IMemoryCache cache,
    UserManager<User> userManager,
    SignInManager<User> signInManager, IEmailService emailService)
    : IUserService
{
    public async Task<Result<User>> RegisterUser(RegisterRequest request)
    {
        var normalizedUsername = request.Username.ToLowerInvariant();
        var normalizedEmail = request.Email.ToLowerInvariant();
        var existingUserByName = await userManager.FindByNameAsync(request.Username);
        if (existingUserByName != null)
        {
           return Result<User>.Failure("Username already exists", ErrorType.Conflict); 
        }
        var existingUserByEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail != null)
        {
            return Result<User>.Failure("Email already exists", ErrorType.Conflict);
        }
        if (request.Password.Length < 8)
        {
            return Result<User>.Failure("PasswordLength invalid",  ErrorType.Conflict);
        }

        var user = new User()
        {
            Balance = 0,
            FullName = request.FullName,
            UserName = request.Username, 
            NormalizedUserName = normalizedUsername.ToUpperInvariant(), 
            Email = normalizedEmail,
            NormalizedEmail = normalizedEmail.ToUpperInvariant(),
            PhoneNumber = request.PhoneNumber,
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return Result<User>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)), ErrorType.Conflict);
        }
        await userManager.AddToRoleAsync(user, UserRoles.Customer);
        return Result<User>.Success(user);
    }

    public async Task<Result<LoginResponse>> LoginUser(LoginRequest request)
    { 
        var result = await signInManager.PasswordSignInAsync(request.Username, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Result<LoginResponse>.Failure("Account locked out.", ErrorType.Forbidden);
            }
            return Result<LoginResponse>.Failure("Invalid username or password.", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return Result<LoginResponse>.Failure("User not found", ErrorType.NotFound);
        }

        var userRoles = await userManager.GetRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };

        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }
        var accessToken = GenerateAccessToken(authClaims);
        var refreshToken = GenerateRefreshToken();
        _ = int.TryParse(configuration["Jwt:RefreshTokenExpiresInDays"], out var refreshTokenExpiresInDays);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays);
        await userManager.UpdateAsync(user);
        var response = new LoginResponse(accessToken, refreshToken);
        return Result<LoginResponse>.Success(response);
    }
    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));
        var tokenExpires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpiresInMinutes"]));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:ValidIssuer"],
            audience: configuration["Jwt:ValidAudience"],
            expires: tokenExpires,
            claims: claims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<Result<bool>> LogOutUser()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Result<bool>.Failure("HttpContext not available.", ErrorType.Conflict);
        }
        var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }
        var jti = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expClaim = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (!string.IsNullOrEmpty(jti) && long.TryParse(expClaim, out long expSeconds))
        {
            var expiryTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            var cacheExpiry = expiryTime - DateTimeOffset.UtcNow;

            if (cacheExpiry > TimeSpan.Zero)
            {
                cache.Set(jti, "blacklisted", cacheExpiry);
            }
        }
        await userManager.UpdateSecurityStampAsync(user);
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        var updateResult = await userManager.UpdateAsync(user);
        return !updateResult.Succeeded ? Result<bool>.Failure("Failed to update user on logout.", ErrorType.Conflict) : Result<bool>.Success(true);
    }

    public async Task<Result<LoginResponse>> RefreshToken(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal?.Identity?.Name == null)
        {
            return Result<LoginResponse>.Failure("Invalid access token", ErrorType.Conflict);
        }

        var username = principal.Identity.Name;
        var user = await userManager.FindByNameAsync(username);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result<LoginResponse>.Failure("Invalid refresh token", ErrorType.Conflict);
        }
        var newAccessToken = GenerateAccessToken(principal.Claims);
        var newRefreshToken = GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        _= int.TryParse(configuration["Jwt:RefreshTokenExpiresInDays"], out var refreshTokenExpiresInDays);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays);
        await userManager.UpdateAsync(user);
        var response = new LoginResponse(newAccessToken, newRefreshToken);
        return Result<LoginResponse>.Success(response);
    }
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty)),
            ValidAudience = configuration["Jwt:ValidAudience"],
            ValidIssuer = configuration["Jwt:ValidIssuer"],
            ValidateLifetime = false,
            RoleClaimType = ClaimTypes.Role,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IdentityResult> AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return IdentityResult.Failed();
        }
        if (roleName == UserRoles.Admin)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }
        }
        if (await userManager.IsInRoleAsync(user, roleName))
        {
            return IdentityResult.Success; 
        }
        return await userManager.AddToRoleAsync(user, roleName);
    }

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user =  await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<bool>.Success(true);
            // do bao mat
        }
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"https://your-frontend-app.com/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(request.Email)}";
        var emailBody = $"Please reset your password by <a href='{resetLink}'>clicking here</a>.";
        var sendEmail = new SendEmailRequest(request.Email, "Reset Your Password", emailBody);
        await emailService.SendEmailAsync(sendEmail);
        return Result<bool>.Success(true);
        
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<bool>.Failure("Invalid", ErrorType.Conflict);
        }
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(x => x.Description));
            return Result<bool>.Failure(errors, ErrorType.Conflict);
        }
        await userManager.UpdateSecurityStampAsync(user);
        return Result<bool>.Success(true);
    }
}