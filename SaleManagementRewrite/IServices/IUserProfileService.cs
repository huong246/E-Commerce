using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum UpdateUserProfileResult
{
    Success,
    DatabaseError,
    DuplicateValue,
    UserNotFound,
    TokenInvalid,
}

public enum UpdatePasswordResult
{
    Success,
    DatabaseError,
    DuplicateValue,
    UserNotFound,
    OldPasswordWrong,
    TokenInvalid,
}

public interface IUserProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync();
    Task<UpdateUserProfileResult> UpdateUserProfileAsync(UpdateUserProfileRequest request);
    Task<UpdatePasswordResult> UpdatePasswordAsync(UpdatePasswordRequest request);
}