using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace SaleManagementRewrite.Services;

public class UserService(
    ApiDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IMemoryCache cache)
    : IUserService
{
    public async Task<RegisterRequestResult> RegisterUser(RegisterRequest request)
    {
       var user = await dbContext.Users.FirstOrDefaultAsync(u =>u.Username == request.Username);
       if (user != null)
       {
           return RegisterRequestResult.UsernameExists;
       }

       if (request.Password.Length < 8)
       {
           return RegisterRequestResult.PasswordLengthNotEnough;
       }

       user = new User()
       {
           Id = Guid.NewGuid(),
           Balance = 0,
           FullName = request.FullName,
           Username = request.Username,
           Password =BCrypt.Net.BCrypt.HashPassword(request.Password),
           PhoneNumber = request.PhoneNumber,
           UserRole = UserRole.Customer,
       };
       try
       { 
           await dbContext.Users.AddAsync(user);
           await dbContext.SaveChangesAsync();
           return RegisterRequestResult.Success;
       }
       catch (DbUpdateException)
       {
           return RegisterRequestResult.DatabaseError;
       }
    }

    public async Task<LoginUserResult> LoginUser(LoginRequest request)
    {
    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
    {
        return new LoginUserResult(LoginUserResultType.InvalidCredentials, null, null);
    }

    var authClaim = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };
        foreach (UserRole role in Enum.GetValues(typeof(UserRole)))
    {
        if (user.UserRole.HasFlag(role))
        {
            authClaim.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }
    }

    var accessToken = GenerateAccessToken(authClaim);
    var refreshToken = GenerateRefreshToken();
        
    _= int.TryParse(configuration["Jwt:RefreshTokenExpiresInDays"], out var refreshTokenExpiresInDays);
    user.RefreshToken = refreshToken;
    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays);
        
    await dbContext.SaveChangesAsync();
        return new LoginUserResult(LoginUserResultType.Success, accessToken, refreshToken); 
        
    }
    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
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

    public async Task<LogoutUserResultType> LogOutUser()
    {
        var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (username == null)
        {
            return LogoutUserResultType.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return LogoutUserResultType.UserNotFound;
        }
        var jti = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var exp =  httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (jti != null || exp != null)
        {
            var expiryTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));
            var cacheExpiry = expiryTime - DateTimeOffset.UtcNow;

            if (cacheExpiry > TimeSpan.Zero)
            {
                cache.Set(jti, "blacklisted", cacheExpiry);
            }
            if (cacheExpiry > TimeSpan.Zero)
            {
                cache.Set(jti, "blacklisted", cacheExpiry);
            }
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        try
        {
            await dbContext.SaveChangesAsync();
            return LogoutUserResultType.Success;
        }
        catch (DbUpdateException)
        {
            return LogoutUserResultType.DatabaseError;
        }
    }
}