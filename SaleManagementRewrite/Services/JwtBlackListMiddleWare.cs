using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;

namespace SaleManagementRewrite.Services;
public class JwtBlacklistMiddleware(RequestDelegate next, IMemoryCache cache)
{
    public async Task InvokeAsync(HttpContext context )
    {
        var user = context.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && cache.TryGetValue(jti, out _))
            {
                // Token is blacklisted
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("This token has been revoked.");
                return;
            }
        }

        await next(context);
    }
}