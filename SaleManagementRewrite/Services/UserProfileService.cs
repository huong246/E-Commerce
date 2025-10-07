using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class UserProfileService(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
    : IUserProfileService
{
    private async Task<Result<User>> GetCurrentUserAsync()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out _))
        {
            return Result<User>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        return user == null ? Result<User>.Failure("User not found",  ErrorType.NotFound) : Result<User>.Success(user);
    }
    public async Task<Result<User>> GetUserProfileAsync()
    {
        return await GetCurrentUserAsync();
    }

    public async Task<Result<User>> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        var userIdString =  httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out _))
        {
            return Result<User>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user =  await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<User>.Failure("User not found", ErrorType.NotFound);
        }
        
        var noChanges = request.FullName == user.FullName &&
                        request.Email == user.Email &&
                        request.PhoneNumber == user.PhoneNumber &&
                        request.Birthday == user.Birthday &&
                        request.Gender == user.Gender;

        if (noChanges)
        {
            return Result<User>.Failure("Duplicate value", ErrorType.Conflict);
        }

        user.FullName = request.FullName ?? user.FullName;
        user.Birthday = request.Birthday ?? user.Birthday;
        user.Gender = request.Gender ?? user.Gender;
        if (request.Email != null && !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return Result<User>.Failure("Email exist by other user", ErrorType.Conflict);
            }
            var setEmailResult = await userManager.SetEmailAsync(user, request.Email);
            if(!setEmailResult.Succeeded)
            {
                return Result<User>.Failure("Failed to update email.", ErrorType.Conflict);
            }
        }
        if (user.PhoneNumber != request.PhoneNumber)
        {
            var setPhoneResult = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if(!setPhoneResult.Succeeded)
            {
                return Result<User>.Failure("Failed to update phone number.", ErrorType.Conflict);
            }
        } 
        var updateResult = await userManager.UpdateAsync(user);
        if (updateResult.Succeeded)
        {
            return Result<User>.Success(user);
        }
        var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
        return Result<User>.Failure(errors, ErrorType.Conflict);
    }

    public async Task<Result<bool>> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out _))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user =  await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }
        if (request.OldPassword == null || request.NewPassword == null)
        {
            return Result<bool>.Failure("Cannot be empty.",  ErrorType.Conflict);
        }
        var checkPasswordResult = await userManager.CheckPasswordAsync(user, request.OldPassword);
        if (!checkPasswordResult)
        {
            return Result<bool>.Failure("OldPassword wrong", ErrorType.Conflict);
        }
        if (request.OldPassword == request.NewPassword)
        {
            return Result<bool>.Failure("New password cannot be the same as the old password", ErrorType.Conflict);
        }
        var changePasswordResult = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (changePasswordResult.Succeeded) return Result<bool>.Success(true);
        var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
        return Result<bool>.Failure(errors, ErrorType.Conflict);

    }
}