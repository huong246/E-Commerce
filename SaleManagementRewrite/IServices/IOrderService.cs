using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public enum CreateOrderResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    CartItemNotFound,
    CartItemIsEmpty,
    InsufficientStock,
    VoucherExpired,
    MinSpendNotMet,
    AddressNotFound,
    ConcurrencyConflict,
    OutOfStock,
}

public enum CancelMainOrderResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ConcurrencyConflict,
}

public enum ReturnOrderItemResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ReturnPeriodExpired,
    ConcurrencyConflict,
    QuantityReturnInvalid
}

public enum ShipOrderShopResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ShopNotFound,
    ConcurrencyConflict,
}

public enum SellerCancelOrderResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ShopNotFound,
    CustomerNotFound,
    ConcurrencyConflict,
}

public enum MarkShopOrderAsDeliveredResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ConcurrencyConflict,
}

public enum MarkEntireOrderAsCompletedResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ConcurrencyConflict,
    ReturnPeriodNotExpired
}

public enum CancelEntireOrderResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    OrderNotFound,
    ConcurrencyConflict,
    
}

public enum ProcessReturnRequestResult
{
    Success,
    DatabaseError,
    TokenInvalid,
    UserNotFound,
    NotPermitted,
    ReturnOrderNotFound,
    AlreadyProcessed,
    ReasonIsRequiredForRejection,
    ConcurrencyConflict,
}
public interface IOrderService
{
    Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request);
    Task<CancelMainOrderResult> CancelMainOrderAsync(CancelMainOrderRequest request);
    Task<ReturnOrderItemResult> ReturnOrderItemAsync(ReturnOrderItemRequest request);
    Task<bool> ApproveReturnOrderItemAsync(ApproveReturnOrderItemRequest request);
    Task<bool> RejectReturnOrderItemAsync(RejectReturnOrderItemRequest request);
    Task<IEnumerable<OrderHistory>>  GetOrderHistoryAsync(GetOrderHistoryRequest request);
    Task<ShipOrderShopResult> ShipOrderShopAsync(ShipOrderShopRequest request);
    Task<SellerCancelOrderResult> SellerCancelOrderAsync(SellerCancelOrderRequest request);
    Task<MarkShopOrderAsDeliveredResult> MarkShopOrderAsDeliveredAsync(MarkShopOrderAsDeliveredRequest request);
    Task<MarkEntireOrderAsCompletedResult> MarkEntireOrderAsCompletedAsync(MarkEntireOrderAsCompletedRequest request);
    Task<CancelEntireOrderResult> CancelEntireOrderAsync(CancelEntireOrderRequest request);
    Task<ProcessReturnRequestResult> ProcessReturnRequestAsync(ProcessReturnRequestRequest request);

}