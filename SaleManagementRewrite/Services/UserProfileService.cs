using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiDbContext _dbContext;

    public UserProfileService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }
    
    public async Task<UserProfileDto?> GetUserProfileAsync()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }

        return await _dbContext.Users.Where(u => u.Id == userId)
            .Select(u => new UserProfileDto(u.Id, u.Username, u.FullName, u.PhoneNumber, u.Birthday, u.Gender))
            .FirstOrDefaultAsync();
    }

    public async Task<UpdateUserProfileResult> UpdateUserProfileAsync(UpdateUserProfileRequest request)
    {
        var userIdString =  _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdateUserProfileResult.TokenInvalid;
        }
        var user =  await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return UpdateUserProfileResult.UserNotFound;
        }

        bool noChanges = request.Fullname == user.FullName &&
                         request.Email == user.Email &&
                         request.PhoneNumber == user.PhoneNumber &&
                         request.Birthday == user.Birthday &&
                         request.Gender == user.Gender;

        if (noChanges)
        {
            return UpdateUserProfileResult.DuplicateValue;
        }
        user.FullName = request.Fullname;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.Birthday = request.Birthday;
        user.Gender = request.Gender;
        try
        { 
            _dbContext.Update(user); 
            await _dbContext.SaveChangesAsync();
            return UpdateUserProfileResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdateUserProfileResult.DatabaseError;
        }
    }

    public async Task<UpdatePasswordResult> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return UpdatePasswordResult.TokenInvalid;
        }
        var user =  await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return UpdatePasswordResult.UserNotFound;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.Password))
        {
            return UpdatePasswordResult.OldPasswordWrong;
        }

        if (request.OldPassword == request.NewPassword)
        {
            return UpdatePasswordResult.DuplicateValue;
        }
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        try
        {
            _dbContext.Update(user);
            await _dbContext.SaveChangesAsync();
            return UpdatePasswordResult.Success;
        }
        catch (DbUpdateException)
        {
            return UpdatePasswordResult.DatabaseError;
        }
    }
}