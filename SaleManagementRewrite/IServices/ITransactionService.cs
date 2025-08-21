using System.Transactions;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using Transaction = SaleManagementRewrite.Entities.Transaction;

namespace SaleManagementRewrite.IServices;
 
public interface ITransactionService
{
    Task<Result<Transaction>> DepositIntoBalanceAsync(DepositIntoBalanceRequest request);
    Task<Result<Transaction>> CreatePaymentAsync(CreatePaymentRequest request);
    Task<Result<Transaction>> CreatePayOutAsync(CreatePayOutRequest request);
    Task<Result<Transaction>> CreateRefundAsync(CreateRefundRequest request);
    Task<Result<Transaction>> CreateRefundWhenCancelAsync(CreateRefundWhenCancelRequest request);
    Task<Result<Transaction>> CreateRefundWhenSellerCancelAsync(CreateRefundWhenSellerCancelRequest request);
    Task<Result<IEnumerable<Transaction?>>> GetTransactionAsync(GetTransactionForOrderRequest request);
    Task<string> CreatePaymentVnPayUrlAsync(CreatePaymentVnPayRequest request );
    Task<VnpayIpnResponse> ProcessIpnAsync(IQueryCollection vnPayData);
}