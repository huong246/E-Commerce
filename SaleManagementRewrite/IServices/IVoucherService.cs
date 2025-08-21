using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;
 
public interface IVoucherService
{
    Task<Result<Voucher>> CreateVoucher(CreateVoucherRequest request);
    Task<Voucher?> GetVoucherByIdAsync(Guid id);
    Task<Result<bool>> DeleteVoucher(DeleteVoucherRequest request);
    Task<Result<Voucher>> UpdateVoucher(UpdateVoucherRequest request);
}