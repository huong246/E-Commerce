using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum RegisterRequestResult
{
    Success,
    DatabaseError,
    UsernameExists,
    PasswordLengthNotEnough,
}
public enum LoginUserResultType
{
    Success,
    InvalidCredentials
}
public enum LogoutUserResultType
{
    Success,
    DatabaseError,
    UserNotFound,
    TokenInvalid,
}
public record LoginUserResult(LoginUserResultType LonginUserResultType, string? AccessToken, string? RefreshToken);
public interface IUserService
{
    Task<RegisterRequestResult> RegisterUser(RegisterRequest request);
    Task<LoginUserResult> LoginUser(LoginRequest request);
    Task<LogoutUserResultType> LogOutUser();
}