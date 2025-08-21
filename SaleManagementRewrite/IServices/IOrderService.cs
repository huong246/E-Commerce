using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;
public interface IOrderService
{
    Task<Result<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);
    Task<Order?> GetOrderByIdAsync(Guid id); 
    Task<Result<bool>> CancelMainOrderAsync(CancelMainOrderRequest request);
    Task<Result<bool>> CancelAPaidOrderAsync(CancelAPaidOrderRequest request);
    Task<Result<bool>> ApproveCancelAPaidOrderAsync(ApproveCancelAPaidOrderRequest request);
    Task<Result<bool>> RejectCancelAPaidOrderAsync(RejectCancelAPaidOrderRequest request);
    Task<Result<ReturnOrderItemResponse>> ReturnOrderItemAsync(ReturnOrderItemRequest request);
    Task<Result<bool>> ApproveReturnOrderItemAsync(ApproveReturnOrderItemRequest request);
    Task<Result<bool>> RejectReturnOrderItemAsync(RejectReturnOrderItemRequest request);
    Task<Result<IEnumerable<OrderHistory>>>  GetOrderHistoryAsync(GetOrderHistoryRequest request);
    Task<Result<bool>> ShipOrderShopAsync(ShipOrderShopRequest request);
    Task<Result<bool>> SellerCancelOrderAsync(SellerCancelOrderRequest request);
    Task<Result<bool>> MarkShopOrderAsDeliveredAsync(MarkShopOrderAsDeliveredRequest request);
    Task<Result<bool>> MarkOrderShopAsCompletedAsync(MarkOrderShopAsCompletedRequest request);
    Task<Result<bool>> CancelEntireOrderAsync(CancelEntireOrderRequest request);
    Task<Result<bool>> ProcessReturnRequestAsync(ProcessReturnRequestRequest request);

}