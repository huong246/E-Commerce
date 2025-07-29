using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum CreateVoucherResult
{
    Success,
    DatabaseError,
    TokenInvalid, 
    UserNotFound,
    NotPermitted,
    QuantityInvalid,
    ShopNotFound,
    ItemNotFound,
    ConcurrencyConflict,
}

public enum DeleteVoucherResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    VoucherNotFound,
    ConcurrencyConflict,
    ShopNotFound,
}

public enum UpdateVoucherResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    VoucherNotFound,
    DuplicateValue,
    ConcurrencyConflict,
    ShopNotFound
}
public interface IVoucherService
{
    Task<CreateVoucherResult> CreateVoucher(CreateVoucherRequest request);
    Task<DeleteVoucherResult> DeleteVoucher(DeleteVoucherRequest request);
    Task<UpdateVoucherResult> UpdateVoucher(UpdateVoucherRequest request);
}