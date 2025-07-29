namespace SaleManagementRewrite.Schemas;

public record DepositIntoBalanceRequest(decimal Amount);
public record CreatePaymentRequest(Guid OrderId, Guid BuyerId, decimal Amount);
public record CreatePayOutRequest(Guid OrderShopId, Guid SellerId, decimal Amount);
public record CreateRefundRequest(Guid BuyerId, Guid ReturnOrderId, decimal Amount);
public record GetTransactionForOrderRequest(Guid OrderId);