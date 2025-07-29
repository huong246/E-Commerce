using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class CustomerUpSellerService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    : ICustomerUpSellerService
{
    public async Task<CreateCustomerUpSellerResult> CreateCustomerUpSellerAsync()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CreateCustomerUpSellerResult.TokenInvalid;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return CreateCustomerUpSellerResult.UserNotFound;
        }

        if (user.UserRole != UserRole.Customer)
        {
            return CreateCustomerUpSellerResult.NotPermitted;
        }

        var request = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(a => a.UserId == userId);
        if (request != null)
        {
            return CreateCustomerUpSellerResult.RequestExists;
        }

        request = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = user,
            RequestAt = DateTime.UtcNow,
            Status = RequestStatus.Pending,
        };
        try
        {
            await dbContext.CustomerUpSellers.AddAsync(request);
            await dbContext.SaveChangesAsync();
            return CreateCustomerUpSellerResult.Success;
        }
        catch (DbUpdateException)
        {
            return CreateCustomerUpSellerResult.DatabaseError;
        }
    }

    public async Task<CustomerUpSeller?> GetCustomerUpSellerAsync()
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Id == userId);
        if (user == null)
        {
            return null;
        }
        var request = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(a => a.UserId == userId);
        return request;
    }

    public async Task<bool> ApproveCustomerUpSellerAsync(ApproveRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return false;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is not { UserRole: UserRole.Admin })
        {
            return false;
        }
        var authorizationRequest = await dbContext.CustomerUpSellers
            .Include(customerUpSellerRequest => customerUpSellerRequest.User).FirstOrDefaultAsync(c=>c.Id == request.CustomerUpSellerId);
        if (authorizationRequest == null)
        {
            return false;
        }

        if (authorizationRequest.Status != RequestStatus.Pending)
        {
            return false;
        }

        authorizationRequest.Status = RequestStatus.Approved;
        var userRequest = authorizationRequest.User;
        if (userRequest != null)
        {
            userRequest.UserRole |= UserRole.Seller;
        }
        authorizationRequest.ReviewAt = DateTime.UtcNow;
        dbContext.CustomerUpSellers.Update(authorizationRequest);
        if (userRequest != null) dbContext.Users.Update(userRequest);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectCustomerUpSellerAsync(RejectRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return false;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is not { UserRole: UserRole.Admin })
        {
            return false;
        }
        var authorizationRequest = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(c=>c.Id == request.CustomerUpSellerId);
        if (authorizationRequest == null)
        {
            return false;
        }

        if (authorizationRequest.Status != RequestStatus.Pending)
        {
            return false;
        }

        authorizationRequest.Status = RequestStatus.Rejected;
        authorizationRequest.ReviewAt = DateTime.UtcNow;
        dbContext.CustomerUpSellers.Update(authorizationRequest);
        await dbContext.SaveChangesAsync();
        return true;

    }
}
