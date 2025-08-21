using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public interface IReviewService
{
    Task<Result<Review>> CreateReviewAsync(CreateReviewRequest request);
    Task<Result<bool>> DeleteReviewAsync(DeleteReviewRequest request);
    Task<Result<Review>> UpdateReviewAsync(UpdateReviewRequest request);
}