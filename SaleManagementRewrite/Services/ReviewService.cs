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

public class ReviewService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User> userManager) : IReviewService
{
    
    public async Task<Result<Review>> CreateReviewAsync(CreateReviewRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var  userId))
        {
            return Result<Review>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<Review>.Failure("User not found", ErrorType.NotFound);
        }
        var item = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id == request.ItemId);
        if (item == null)
        {
            return Result<Review>.Failure("Item not found", ErrorType.NotFound);
        }
        var orderItem = await dbContext.OrderItems.Include(orderItem => orderItem.OrderShop)
            .ThenInclude(orderShop => orderShop.Order).FirstOrDefaultAsync(i => i.ItemId == request.ItemId && i.OrderShop != null && i.OrderShop.Order != null && i.OrderShop.Order.UserId == user.Id);
        if (orderItem == null)
        {
            return Result<Review>.Failure("OrderItem not found", ErrorType.NotFound);
        }
        if (orderItem.Status != OrderItemStatus.Completed)
        {
            return Result<Review>.Failure("OrderItemStatus invalid", ErrorType.Conflict);
        }

        if (request.Rating < 1 || request.Rating > 5)
        {
            return Result<Review>.Failure("Rating invalid", ErrorType.Conflict);
        }
        var review = await dbContext.Reviews.FirstOrDefaultAsync(i => i.ItemId == request.ItemId && i.Comment == request.Comment);
        if (review != null)
        {
            return Result<Review>.Failure("Review existed",  ErrorType.Conflict);
        }
        
        var newReview = new Review()
        {
            Id = Guid.NewGuid(),
            Item = item,
            ItemId = item.Id,
            Comment = request.Comment,
            Rating = request.Rating,
            Order = orderItem.OrderShop?.Order,
            OrderId = orderItem.OrderShop.OrderId,
            ReviewAt = DateTime.UtcNow,
            User = user,
            UserId = user.Id
        };
        dbContext.Reviews.Add(newReview);
        await dbContext.SaveChangesAsync();
        return Result<Review>.Success(newReview);
    }

    public async Task<Result<bool>> DeleteReviewAsync(DeleteReviewRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var  userId))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }
        var review = await dbContext.Reviews.FirstOrDefaultAsync(r=>r.Id == request.ReviewId && r.UserId == user.Id);
        if (review == null)
        {
            return Result<bool>.Failure("Review not found", ErrorType.NotFound);
        }
        dbContext.Reviews.Remove(review);
        await dbContext.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<Review>> UpdateReviewAsync(UpdateReviewRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var  userId))
        {
            return Result<Review>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<Review>.Failure("User not found", ErrorType.NotFound);
        }
        var review = await dbContext.Reviews.FirstOrDefaultAsync(r=>r.Id == request.ReviewId && r.UserId == user.Id);
        if (review == null)
        {
            return Result<Review>.Failure("Review not found", ErrorType.NotFound);
        }
        var noChange = request.Rating == null || request.Rating == review.Rating && review.Comment == request.Comment || request.Comment == null;
        if (noChange)
        {
            return Result<Review>.Failure("Duplicate value", ErrorType.Conflict);
        }
        if (request.Rating is < 1 or > 5 && request.Rating.HasValue)
        {
            return Result<Review>.Failure("Rating invalid",  ErrorType.Conflict);
        }
        review.Rating = request.Rating ??  review.Rating;
        review.Comment = request.Comment ??   review.Comment;
        await dbContext.SaveChangesAsync();
        return Result<Review>.Success(review);
    }
}