using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class CustomerUpSellerService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User> userManager)
    : ICustomerUpSellerService
{
    public async Task<Result<CustomerUpSeller>> CreateCustomerUpSellerAsync()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CustomerUpSeller>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CustomerUpSeller>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<CustomerUpSeller>.Failure("UserRole invalid", ErrorType.Conflict);
        }

        var request = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(a => a.UserId == userId);
        if (request != null)
        {
            return Result<CustomerUpSeller>.Failure("Request Exist", ErrorType.Conflict);
        }

        request = new CustomerUpSeller()
        {
            UserId = userId,
            User = user,
            RequestAt = DateTime.UtcNow,
            Status = RequestStatus.Pending,
        };
        try
        {
            await dbContext.CustomerUpSellers.AddAsync(request);
            await dbContext.SaveChangesAsync();
            return Result<CustomerUpSeller>.Success(request);
        }
        catch (DbUpdateException)
        {
            return Result<CustomerUpSeller>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<CustomerUpSeller>> GetCustomerUpSellerAsync()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CustomerUpSeller>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CustomerUpSeller>.Failure("User not found", ErrorType.NotFound);
        }
        var request = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(a => a.UserId == userId);
        return request == null ? Result<CustomerUpSeller>.Failure("Request not found", ErrorType.NotFound) : Result<CustomerUpSeller>.Success(request);
    }

    public async Task<Result<CustomerUpSeller>> ApproveCustomerUpSellerAsync(ApproveRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out _))
        {
            return Result<CustomerUpSeller>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CustomerUpSeller>.Failure("User not found",  ErrorType.NotFound);
        }
        if (!await userManager.IsInRoleAsync(user, UserRoles.Admin))
        {
            return Result<CustomerUpSeller>.Failure("User not permitted", ErrorType.Conflict);
        }
        var authorizationRequest = await dbContext.CustomerUpSellers
            .Include(customerUpSellerRequest => customerUpSellerRequest.User).FirstOrDefaultAsync(c=>c.Id == request.CustomerUpSellerId);
        if (authorizationRequest == null)
        {
            return Result<CustomerUpSeller>.Failure("CustomerUpSeller not found", ErrorType.NotFound);
        }

        if (authorizationRequest.User == null)
        {
            return Result<CustomerUpSeller>.Failure("Not find its user",  ErrorType.NotFound);
        }

        if (authorizationRequest.Status != RequestStatus.Pending)
        {
            return Result<CustomerUpSeller>.Failure("RequestStatus invalid", ErrorType.Conflict);
        }
        if (await userManager.IsInRoleAsync(authorizationRequest.User, UserRoles.Seller))
        {
            return Result<CustomerUpSeller>.Failure("This user is seller", ErrorType.Conflict);
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var addToRoleResult = await userManager.AddToRoleAsync(authorizationRequest.User, UserRoles.Seller);
            if (!addToRoleResult.Succeeded)
            {
                await transaction.RollbackAsync(); 
                var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                return Result<CustomerUpSeller>.Failure($"Error {errors}", ErrorType.BadRequest);
            }
            authorizationRequest.Status = RequestStatus.Approved;
            authorizationRequest.ReviewAt = DateTime.UtcNow;
            dbContext.CustomerUpSellers.Update(authorizationRequest);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result<CustomerUpSeller>.Success(authorizationRequest);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Result<CustomerUpSeller>.Failure($"Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<CustomerUpSeller>> RejectCustomerUpSellerAsync(RejectRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CustomerUpSeller>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CustomerUpSeller>.Failure("User not found", ErrorType.NotFound);
        }
        if (!await userManager.IsInRoleAsync(user, UserRoles.Admin))
        {
            return Result<CustomerUpSeller>.Failure("User not permitted", ErrorType.Conflict);
        }
        var authorizationRequest = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(c=>c.Id == request.CustomerUpSellerId);
        if (authorizationRequest == null)
        {
            return Result<CustomerUpSeller>.Failure("CustomerUpSeller not found", ErrorType.NotFound);
        }

        if (authorizationRequest.Status != RequestStatus.Pending)
        {
            return Result<CustomerUpSeller>.Failure("RequestStatus invalid", ErrorType.Conflict);
        }

        authorizationRequest.Status = RequestStatus.Rejected;
        authorizationRequest.ReviewAt = DateTime.UtcNow;
        dbContext.CustomerUpSellers.Update(authorizationRequest);
        await dbContext.SaveChangesAsync();
        return Result<CustomerUpSeller>.Success(authorizationRequest);
    }
}
