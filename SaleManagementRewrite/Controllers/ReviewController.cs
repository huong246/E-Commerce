using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewController(IReviewService reviewService):ControllerBase
{
    [HttpPost("create_review")]
    [Authorize(Roles = nameof(UserRoles.Customer))]
    public async Task<IActionResult> CreateReview(CreateReviewRequest request)
    {
        var result = await reviewService.CreateReviewAsync(request);
        return HandleResult(result);
    }
    [HttpDelete("delete_review")]
    [Authorize(Roles = $"{nameof(UserRoles.Customer)},{nameof(UserRoles.Admin)}")]
    public async Task<IActionResult> DeleteReview(DeleteReviewRequest request)
    {
        var result = await reviewService.DeleteReviewAsync(request);
        return HandleResult(result);
    }
    [HttpPut("update_review")]
    [Authorize(Roles = nameof(UserRoles.Customer))]
    public async Task<IActionResult> UpdateReview(UpdateReviewRequest request)
    {
        var result = await reviewService.UpdateReviewAsync(request);
        return HandleResult(result);
    }
    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }
        if (typeof(T) == typeof(bool))
        {
            return NoContent(); // HTTP 204
        }
        return Ok(result.Value);
    }

    private IActionResult HandleFailure<T>(Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(result.Error),
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            ErrorType.BadRequest => BadRequest(result.Error),
            _ => StatusCode(500, result.Error)
        };
    }
}