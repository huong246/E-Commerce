namespace SaleManagementRewrite.Schemas;

public record DepositIntoBalanceRequest(decimal Amount);
public record CreatePaymentRequest(Guid OrderId, Guid BuyerId, decimal Amount);
public record CreatePayOutRequest(Guid OrderShopId, Guid SellerId, decimal Amount);
public record CreateRefundRequest(Guid BuyerId, Guid ReturnOrderId, decimal Amount);
public record CreateRefundWhenCancelRequest(Guid BuyerId, Guid CancelRequestId, decimal Amount);
public record CreateRefundWhenSellerCancelRequest( Guid OrderShopId, decimal Amount);
public record GetTransactionForOrderRequest(Guid OrderId);
public record CreatePaymentVnPayRequest(Guid OrderId, string IpAddress);
public record VnpayIpnResponse(string RspCode, string Message);
