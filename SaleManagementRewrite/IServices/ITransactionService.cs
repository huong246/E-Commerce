using System.Transactions;
using SaleManagementRewrite.Schemas;
using Transaction = SaleManagementRewrite.Entities.Transaction;

namespace SaleManagementRewrite.IServices;

public enum DepositIntoBalanceResult
{
    Success,
    DatabaseError,
    UserNotFound,
    AmountInvalid,
    TokenInvalid,
    ConcurrencyConflict,
}
public enum CreatePaymentResult
{
    Success,
    DatabaseError,
    UserNotFound,
    AmountInvalid,
    OrderNotFound,
    ConcurrencyConflict,
    NotPermitted,
    InvalidState,
    PlatformWalletNotFound,
    AmountMismatch,
    BalanceNotEnough,
}

public enum CreatePayOutResult
{
    Success,
    DatabaseError,
    UserNotFound,
    AmountInvalid,
    OrderNotFound,
    ConcurrencyConflict,
    PlatformWalletNotFound,
    ShopNotFound,
    OrderNotOfShop,
    InvalidState,
    AmountMismatch,
}

public enum CreateRefundResult
{
    Success,
    DatabaseError,
    UserNotFound,
    AmountInvalid,
    ReturnOrderNotFound,
    ConcurrencyConflict,
    PlatformWalletNotFound,
    AmountMismatch,
    InvalidState,
}
public interface ITransactionService
{
    Task<DepositIntoBalanceResult> DepositIntoBalanceAsync(DepositIntoBalanceRequest request);
    Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request);
    Task<CreatePayOutResult> CreatePayOutAsync(CreatePayOutRequest request);
    Task<CreateRefundResult> CreateRefundAsync(CreateRefundRequest request);
    Task<IEnumerable<Transaction?>> GetTransactionAsync(GetTransactionForOrderRequest request);
}