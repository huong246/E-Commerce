using Microsoft.AspNetCore.Identity;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;
 
public record LoginResponse(string AccessToken, string RefreshToken);
public interface IUserService
{
    Task<Result<User>> RegisterUser(RegisterRequest request);
    Task<Result<LoginResponse>> LoginUser(LoginRequest request);
    Task<Result<LoginResponse>> RefreshToken(RefreshTokenRequest request);
    Task<Result<bool>> LogOutUser();
    Task<IdentityResult> AssignRoleAsync(Guid UserId, string roleName);
    Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request);
}