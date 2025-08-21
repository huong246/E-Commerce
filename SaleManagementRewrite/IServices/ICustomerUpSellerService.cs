using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

 
public interface ICustomerUpSellerService
{
    Task<Result<CustomerUpSeller>> CreateCustomerUpSellerAsync();
    Task<Result<CustomerUpSeller>> GetCustomerUpSellerAsync();
    Task<Result<CustomerUpSeller>> ApproveCustomerUpSellerAsync(ApproveRequest request);
    Task<Result<CustomerUpSeller>> RejectCustomerUpSellerAsync(RejectRequest request);
}