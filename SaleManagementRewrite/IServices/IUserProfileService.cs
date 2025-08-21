using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

 

public interface IUserProfileService
{
    Task<Result<User>> GetUserProfileAsync();
    Task<Result<User>> UpdateUserProfileAsync(UpdateUserProfileRequest request);
    Task<Result<bool>> UpdatePasswordAsync(UpdatePasswordRequest request);
}