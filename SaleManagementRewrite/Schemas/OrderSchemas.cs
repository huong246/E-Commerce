using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
 
namespace SaleManagementRewrite.Schemas;

public record CreateOrderRequest(List<Guid> CartItemId, Dictionary<Guid,Guid>? VoucherShop,Guid? VoucherProductId, Guid? VoucherShippingId, double? Latitude, double? Longitude,string? AddressName, Guid? AddressId);
public record CreateOrderResponse(Guid? OrderId, decimal TotalAmount);
public record CancelMainOrderRequest(Guid OrderId, string Reason);
public record CancelAPaidOrderRequest(Guid OrderShopId, string Reason);
public record ApproveCancelAPaidOrderRequest(Guid CancelRequestId);
public record RejectCancelAPaidOrderRequest(Guid CancelRequestId, string Reason);
public record ReturnOrderItemRequest(Guid OrderId, Dictionary<Guid, int> ItemsReturn,string Reason);
public record ReturnOrderItemResponse(Guid ReturnOrderId, Dictionary<Guid, string> ReturnOrderItems);
public record ApproveReturnOrderItemRequest(Guid ReturnOrderId, List<Guid> ReturnOrderItemsId);
public record RejectReturnOrderItemRequest(Guid ReturnOrderId,Dictionary<Guid, string> RejectReturnOrderItems);
public record GetOrderHistoryRequest(Guid OrderId);

public record ShipOrderShopRequest(Guid OrderShopId);

public record SellerCancelOrderRequest(Guid OrderShopId, string Reason);

public record MarkShopOrderAsDeliveredRequest(Guid OrderShopId, string? Note); //cac shipped...
public record MarkOrderShopAsCompletedRequest(Guid OrderShopId);
public record CancelEntireOrderRequest(Guid OrderId, string Reason);
public record ProcessReturnRequestRequest(Guid ReturnOrderId, bool IsApproved, string? Reason);
