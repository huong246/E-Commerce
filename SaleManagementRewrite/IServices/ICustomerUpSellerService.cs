using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum CreateCustomerUpSellerResult
{
    Success,
    DatabaseError,
    RequestExists,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
}
public interface ICustomerUpSellerService
{
    Task<CreateCustomerUpSellerResult> CreateCustomerUpSellerAsync();
    Task<CustomerUpSeller?> GetCustomerUpSellerAsync();
    Task<bool> ApproveCustomerUpSellerAsync(ApproveRequest request);
    Task<bool> RejectCustomerUpSellerAsync(RejectRequest request);
}