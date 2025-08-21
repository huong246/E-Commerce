using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class OrderService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext, UserManager<User> userManager, ITransactionService transactionService)
    : IOrderService
{
    private const double EarthRadiusKm = 6371.0;
    private const double ShippingFeePerKm = 1000;

    public async Task<Result<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<CreateOrderResponse>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<CreateOrderResponse>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, (UserRoles.Customer)))
        {
            return Result<CreateOrderResponse>.Failure("UserRole not permitted", ErrorType.Conflict);
        }

        var cartItems = await dbContext.CartItems.Include(ci => ci.Item).ThenInclude(item => item.Shop)
            .ThenInclude(shop => shop.Address).Include(cartItem => cartItem.Item).ThenInclude(item => item.Shop)
            .ThenInclude(shop => shop.Vouchers)
            .Where(ci => ci.UserId == userId && request.CartItemId.Contains(ci.Id)).ToListAsync();
        if (cartItems.Count == 0 || cartItems.Count != request.CartItemId.Count)
        {
            return Result<CreateOrderResponse>.Failure("One or more CartItems not found", ErrorType.NotFound);
        }

        var itemsGroupedByShop = cartItems.GroupBy(ci => ci.Item?.Shop);
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            Address? address;
            if (request.AddressId != null)
            {
                address = await dbContext.Addresses.FirstOrDefaultAsync(a =>
                    a.Id == request.AddressId && a.UserId == userId);
                if (address == null)
                {
                    return Result<CreateOrderResponse>.Failure("Address not found", ErrorType.NotFound);
                }
            }
            else if (request is { Latitude: not null, Longitude: not null, AddressName: not null })
            {
                address = new Address()
                {
                    Id = Guid.NewGuid(),
                    User = user,
                    UserId = userId,
                    Latitude = (double)request.Latitude,
                    Longitude = (double)request.Longitude,
                    Name = request.AddressName,
                    IsDefault = false
                };
                dbContext.Addresses.Add(address);
            }
            else
            {
                address = await dbContext.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault == true);
                if (address == null)
                {
                    return Result<CreateOrderResponse>.Failure("Address not found", ErrorType.NotFound);
                }
            }

            double userLatitude = address.Latitude;
            double userLongitude = address.Longitude;
            var order = new Order()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = user,
                DiscountProductAmount = 0,
                DiscountShippingAmount = 0,
                Status = OrderStatus.PendingPayment,
                OrderShops = new List<OrderShop>(),
                TotalAmount = 0,
                TotalSubtotal = 0,
                OrderDate = DateTime.UtcNow,
                VoucherProductId = null,
                VoucherShippingId = null,
                TotalShippingFee = 0,
                UserAddress = address,
                UserAddressId = address.Id,
            };
            dbContext.Orders.Add(order);
            foreach (var groupItemInShop in itemsGroupedByShop)
            {
                var shop = groupItemInShop.Key;
                var itemsForThisShop = groupItemInShop.ToList();
                if (shop == null) continue;
                var orderShop = new OrderShop()
                {
                    Id = Guid.NewGuid(),
                    ShopId = shop.Id,
                    Shop = shop,
                    ShippingFee = 0,
                    DeliveredDate = null,
                    DiscountShopAmount = 0,
                    Notes = null,
                    OrderItems = new List<OrderItem>(),
                    Status = OrderShopStatus.PendingConfirmation,
                    SubTotalShop = 0,
                    TotalShopAmount = 0,
                    VoucherShopId = null,
                    VoucherShopCode = null,
                    VoucherShop = null,
                    Order = order,
                    OrderId = order.Id,
                    TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
                };
                dbContext.OrderShops.Add(orderShop);
                order.OrderShops.Add(orderShop);
                foreach (var itemInShop in itemsForThisShop)
                {
                    var item = itemInShop.Item;
                    if (item == null)
                    {
                        continue;
                    }

                    if (item.Stock <= 0)
                    {
                        return Result<CreateOrderResponse>.Failure("Out of stock", ErrorType.Conflict);
                    }

                    if (itemInShop.Quantity > item.Stock && item.Stock > 0)
                    {
                        return Result<CreateOrderResponse>.Failure("InsufficientStock", ErrorType.Conflict);
                    }

                    if (itemInShop.Item != null)
                    {
                        var orderItem = new OrderItem()
                        {
                            Id = Guid.NewGuid(),
                            OrderShop = orderShop,
                            OrderShopId = orderShop.Id,
                            Quantity = itemInShop.Quantity,
                            Price = itemInShop.Item.Price,
                            Item = itemInShop.Item,
                            ItemId = itemInShop.Item.Id,
                            Status = OrderItemStatus.Pending,
                            ShopId = shop.Id,
                            Shop = shop,
                            TotalAmount = itemInShop.Item.Price * itemInShop.Quantity,
                        };
                        dbContext.OrderItems.Add(orderItem);
                        orderShop.OrderItems.Add(orderItem);
                    }

                    orderShop.SubTotalShop += item.Price * itemInShop.Quantity;
                    item.Stock -= itemInShop.Quantity;

                }

                order.TotalSubtotal += orderShop.SubTotalShop;
                if (request.VoucherShop != null)
                { 
                    Guid? voucherId = null;
                    foreach (var voucherShopId in request.VoucherShop.Keys.Where(voucherShopId => shop.Id == voucherShopId))
                    {
                        voucherId = request.VoucherShop[voucherShopId];
                    }
                    var voucherShop =
                        await dbContext.Vouchers.FirstOrDefaultAsync(v =>
                            v.Id == voucherId && v.ShopId == shop.Id);
                    if (voucherShop == null || voucherShop.Quantity == 0)
                    {
                        return Result<CreateOrderResponse>.Failure("VoucherShop expired", ErrorType.Conflict);
                    }

                    if (voucherShop.MinSpend.HasValue && voucherShop.MinSpend.Value > orderShop.SubTotalShop)
                    {
                        return Result<CreateOrderResponse>.Failure("MinSpend voucherShop not met", ErrorType.Conflict);
                    }

                    if (voucherShop.VoucherMethod == Method.Percentage)
                    {
                        orderShop.DiscountShopAmount = orderShop.SubTotalShop * (voucherShop.Value) / 100;
                    }
                    else
                    {
                        orderShop.DiscountShopAmount = voucherShop.Value;
                    }

                    if (voucherShop.Maxvalue.HasValue && voucherShop.Maxvalue.Value < orderShop.DiscountShopAmount)
                    {
                        orderShop.DiscountShopAmount = voucherShop.Maxvalue.Value;
                    }

                    if (orderShop.DiscountShopAmount > orderShop.SubTotalShop)
                    {
                        orderShop.DiscountShopAmount = orderShop.SubTotalShop;
                    }

                    voucherShop.Quantity -= 1;
                    order.DiscountProductAmount += orderShop.DiscountShopAmount;
                    orderShop.VoucherShopId = voucherShop.Id;
                    orderShop.VoucherShopCode = voucherShop.Code;
                    orderShop.VoucherShop = voucherShop;
                    
                }

                if (shop.Address == null) continue;
                var shippingFeeForShop = (decimal)CalculateShippingFee(userLatitude, userLongitude,
                    shop.Address.Latitude, shop.Address.Longitude);
                order.TotalShippingFee += shippingFeeForShop;
                orderShop.ShippingFee = shippingFeeForShop;
            }

            // VoucherProduct
            if (request.VoucherProductId.HasValue)
            {
                decimal valueVoucher;
                var voucherProduct =
                    await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == request.VoucherProductId);
                if (voucherProduct is not { IsActive: true } || voucherProduct.Quantity == 0 ||
                    voucherProduct.EndDate < DateTime.UtcNow)
                {
                    return Result<CreateOrderResponse>.Failure("VoucherProduct expired", ErrorType.Conflict);
                }

                if (voucherProduct.MinSpend.HasValue && order.TotalSubtotal < voucherProduct.MinSpend)
                {
                    return Result<CreateOrderResponse>.Failure("MinSpend voucherProduct not met", ErrorType.Conflict);
                }

                if (voucherProduct.VoucherMethod == Method.Percentage)
                {
                    valueVoucher = (order.TotalSubtotal * voucherProduct.Value / 100);
                }
                else
                {
                    valueVoucher = voucherProduct.Value;
                }

                if (voucherProduct.Maxvalue.HasValue && valueVoucher > voucherProduct.Maxvalue.Value)
                {
                    valueVoucher = voucherProduct.Maxvalue.Value;
                }

                if (valueVoucher > order.TotalSubtotal)
                {
                    valueVoucher = order.TotalSubtotal;
                }

                order.DiscountProductAmount += valueVoucher;
                voucherProduct.Quantity -= 1;
                order.VoucherProductId = voucherProduct.Id;
                dbContext.Vouchers.Update(voucherProduct);
            }

            //tinh voucherShipping
            if (request.VoucherShippingId.HasValue)
            {
                var voucherShipping =
                    await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == request.VoucherShippingId);
                if (voucherShipping is not { IsActive: true } || voucherShipping.Quantity == 0 ||
                    voucherShipping.EndDate < DateTime.UtcNow)
                {
                    return Result<CreateOrderResponse>.Failure("VoucherShipping expired", ErrorType.Conflict);
                }

                if (voucherShipping.MinSpend.HasValue && order.TotalSubtotal < voucherShipping.MinSpend)
                {
                    return Result<CreateOrderResponse>.Failure("MinSpend voucherShipping not met", ErrorType.Conflict);
                }

                if (voucherShipping.VoucherMethod == Method.Percentage)
                {
                    order.DiscountShippingAmount = (order.TotalShippingFee * voucherShipping.Value / 100);
                }
                else
                {
                    order.DiscountShippingAmount = voucherShipping.Value;
                }

                if (voucherShipping.Maxvalue.HasValue && voucherShipping.Maxvalue < order.DiscountShippingAmount)
                {
                    order.DiscountShippingAmount = voucherShipping.Maxvalue.Value;
                }

                if (order.DiscountShippingAmount > order.TotalShippingFee)
                {
                    order.DiscountShippingAmount = order.TotalShippingFee;
                }

                voucherShipping.Quantity -= 1;
                order.VoucherShippingId  = voucherShipping.Id;
                dbContext.Vouchers.Update(voucherShipping);
            }

            order.TotalAmount = order.TotalSubtotal + order.TotalShippingFee - order.DiscountProductAmount -
                                order.DiscountShippingAmount;
            var orderHistory = new OrderHistory()
            {
                Id = Guid.NewGuid(),
                CreateAt = DateTime.UtcNow,
                OrderId = order.Id,
                Note = "Create order",
                FromStatus = null,
                ToStatus = nameof(OrderStatus.PendingPayment),
                ChangedByUserId = userId,
            };
            dbContext.OrderHistories.Add(orderHistory);
            foreach (var cart in cartItems)
            {
                dbContext.CartItems.Remove(cart);
            }

            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            var successResponse = new CreateOrderResponse(order.Id, order.TotalAmount);
            return Result<CreateOrderResponse>.Success(successResponse);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<CreateOrderResponse>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<CreateOrderResponse>.Failure("Database error", ErrorType.Conflict);
        }
    }

    private static double CalculateShippingFee(double lat1, double lon1, double lat2, double lon2)
    {
        var latRad1 = ToRadians(lat1);
        var lonRad1 = ToRadians(lon1);
        var latRad2 = ToRadians(lat2);
        var lonRad2 = ToRadians(lon2);

        var deltaLat = latRad2 - latRad1;
        var deltaLon = lonRad2 - lonRad1;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(latRad1) * Math.Cos(latRad2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var distance = EarthRadiusKm * c;
        var shippingFee = distance * ShippingFeePerKm;

        return shippingFee;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
    
    public async Task<Order?> GetOrderByIdAsync(Guid id)
    {
        return await dbContext.Orders
            .Include(o => o.OrderShops)
            .ThenInclude(os => os.OrderItems)
            .Include(o => o.UserAddress)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    public async Task<Result<bool>> CancelMainOrderAsync(CancelMainOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<bool>.Failure("User not permitted", ErrorType.Conflict);
        }
        var order = await dbContext.Orders.Include(order => order.OrderShops)
            .ThenInclude(orderShop => orderShop.OrderItems).ThenInclude(orderItem => orderItem.Item)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
        if (order == null)
        {
            return Result<bool>.Failure("Order not found",  ErrorType.NotFound);
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return Result<bool>.Failure("Status invalid", ErrorType.Conflict);
        }
        var fromStatus = order.Status;

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            order.Status = OrderStatus.Canceled;
            var toStatus = order.Status;
            foreach (var orderShop in order.OrderShops)
            {
                orderShop.Status = OrderShopStatus.Cancelled;
                foreach (var orderItem in orderShop.OrderItems)
                {
                    orderItem.Status = OrderItemStatus.Cancelled;
                    if (orderItem.Item != null)
                    {
                        orderItem.Item.Stock += orderItem.Quantity;
                    }
                }

                if (!orderShop.VoucherShopId.HasValue) continue;
                var voucher = await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == orderShop.VoucherShopId.Value);
                if (voucher == null)
                {
                    continue;
                }

                voucher.Quantity += 1;
                dbContext.Vouchers.Update(voucher);
            }

            if (order.VoucherProductId.HasValue)
            {
                var voucherProduct =
                    await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == order.VoucherProductId.Value);
                if (voucherProduct != null)
                {
                    voucherProduct.Quantity += 1;
                    dbContext.Vouchers.Update(voucherProduct);
                }

            }

            if (order.VoucherShippingId.HasValue)
            {
                var voucherShipping =
                    await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == order.VoucherShippingId.Value);
                if (voucherShipping != null)
                {
                    voucherShipping.Quantity += 1;
                    dbContext.Vouchers.Update(voucherShipping);
                }
            }

            var orderHistory = new OrderHistory()
            {
                Id = Guid.NewGuid(),
                CreateAt = DateTime.UtcNow,
                OrderId = order.Id,
                Note = request.Reason,
                FromStatus = fromStatus.ToString(),
                ToStatus = toStatus.ToString(),
                ChangedByUserId = userId,
            };
            dbContext.OrderHistories.Add(orderHistory);
            dbContext.Orders.Update(order);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> CancelAPaidOrderAsync(CancelAPaidOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user =  await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }
        if (!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<bool>.Failure("User not permitted", ErrorType.Conflict);
        }
        var orderShop = await dbContext.OrderShops.Include(os=>os.OrderItems).FirstOrDefaultAsync(os => os.Id == request.OrderShopId);
        if (orderShop == null)
        {
            return Result<bool>.Failure("OrderShop not found",  ErrorType.NotFound);
        }

        if (orderShop.Status != OrderShopStatus.Processing)
        {
            return Result<bool>.Failure("OrderShop not processing", ErrorType.Conflict);
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var cancelRequest = new CancelRequest()
            {
                OrderId = orderShop.OrderId,
                OrderShopId = orderShop.Id,
                Reason = request.Reason,
                RequestAt = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                UserId = userId,
                Amount = orderShop.TotalShopAmount,
            };
            dbContext.CancelRequests.Add(cancelRequest);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> ApproveCancelAPaidOrderAsync(ApproveCancelAPaidOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return  Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<bool>.Failure("UserRole not permitted",  ErrorType.Conflict);
        }

        var cancelRequest = await dbContext.CancelRequests.FirstOrDefaultAsync(c => c.Id == request.CancelRequestId);
        if (cancelRequest == null)
        {
            return Result<bool>.Failure("CancelRequest not found",  ErrorType.NotFound);
        }

        if (cancelRequest.Status != RequestStatus.Pending)
        {
            return Result<bool>.Failure("Status invalid", ErrorType.Conflict);
        }
        var orderShop = await dbContext.OrderShops.Include(os=>os.OrderItems).ThenInclude(ot => ot.Item).FirstOrDefaultAsync(os=>os.Id == cancelRequest.OrderShopId);
        if (orderShop == null)
        {
            return Result<bool>.Failure("OrderShop not found", ErrorType.NotFound);
        }

        if (orderShop.Status != OrderShopStatus.Processing)
        {
            return Result<bool>.Failure("OrderShop not processing", ErrorType.Conflict);
        }
        const RequestStatus fromStatus = RequestStatus.Pending;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            orderShop.Status = OrderShopStatus.Cancelled;
            foreach (var  orderItem in orderShop.OrderItems)
            {
                orderItem.Status = OrderItemStatus.Cancelled;
                if (orderItem.Item != null) orderItem.Item.Stock += orderItem.Quantity;
            }

            cancelRequest.Status = RequestStatus.Approved;
            var orderHistory = new OrderHistory()
            {
                ChangedByUserId = userId,
                CreateAt = DateTime.UtcNow,
                FromStatus = fromStatus.ToString(),
                ToStatus = orderShop.Status.ToString(),
                Note = null,
                OrderId = orderShop.OrderId,
                OrderShopId = orderShop.Id,
            };
            dbContext.OrderHistories.Add(orderHistory);
            var refundRequest =
                new CreateRefundWhenCancelRequest(cancelRequest.UserId, cancelRequest.Id, cancelRequest.Amount);
            var refundResult = await transactionService.CreateRefundWhenCancelAsync(refundRequest);
            if (!refundResult.IsSuccess)
            {
                await dbTransaction.RollbackAsync();
                return Result<bool>.Failure("Refund request failed", ErrorType.Conflict);
            }
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> RejectCancelAPaidOrderAsync(RejectCancelAPaidOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return  Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            return Result<bool>.Failure("UserRole not permitted",  ErrorType.Conflict);
        }

        var cancelRequest = await dbContext.CancelRequests.FirstOrDefaultAsync(c => c.Id == request.CancelRequestId);
        if (cancelRequest == null)
        {
            return Result<bool>.Failure("CancelRequest not found",  ErrorType.NotFound);
        }

        if (cancelRequest.Status != RequestStatus.Pending)
        {
            return Result<bool>.Failure("Status invalid", ErrorType.Conflict);
        }
        var orderShop = await dbContext.OrderShops.Include(os=>os.OrderItems).ThenInclude(ot => ot.Item).FirstOrDefaultAsync(os=>os.Id == cancelRequest.OrderShopId);
        if (orderShop == null)
        {
            return Result<bool>.Failure("OrderShop not found", ErrorType.NotFound);
        }

        if (orderShop.Status != OrderShopStatus.Processing)
        {
            return Result<bool>.Failure("OrderShop not processing", ErrorType.Conflict);
        }
        const RequestStatus fromStatus = RequestStatus.Pending;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            cancelRequest.Status = RequestStatus.Rejected;
            var orderHistory = new OrderHistory()
            {
                ChangedByUserId = userId,
                CreateAt = DateTime.UtcNow,
                FromStatus = fromStatus.ToString(),
                ToStatus = cancelRequest.Status.ToString(),
                Note = request.Reason,
                OrderId = orderShop.OrderId,
                OrderShopId = orderShop.Id,
            };  
            dbContext.OrderHistories.Add(orderHistory);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }
    public async Task<Result<ReturnOrderItemResponse>> ReturnOrderItemAsync(ReturnOrderItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<ReturnOrderItemResponse>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<ReturnOrderItemResponse>.Failure("User not found", ErrorType.NotFound);
        }
        if(!await userManager.IsInRoleAsync(user, UserRoles.Customer))
        {
            return Result<ReturnOrderItemResponse>.Failure("UserRole not permitted",  ErrorType.Conflict);
        }
        var order = await dbContext.Orders.FirstOrDefaultAsync(o =>
            o.Id == request.OrderId && o.UserId == userId );
        if (order == null)
        {
            return Result<ReturnOrderItemResponse>.Failure("Order not found", ErrorType.NotFound);
        }

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var returnOrder = new ReturnOrder()
            {
                Id = Guid.NewGuid(),
                OrderId = request.OrderId,
                Order = order,
                RequestAt = DateTime.UtcNow,
                ReviewAt = null,
                ReturnOrderItems = new List<ReturnOrderItem>(),
                Status = ReturnStatus.Pending,
                User = user,
                UserId = userId,
            };
            dbContext.ReturnOrders.Add(returnOrder);
            var orderItemIdsToReturn = request.ItemsReturn.Keys.ToList();

            var orderItems = await dbContext.OrderItems
                .Include(oi => oi.OrderShop).ThenInclude(os => os.Order)
                .Where(oi => orderItemIdsToReturn.Contains(oi.Id))
                .ToListAsync();
            var returnOrderItems = new Dictionary<Guid, string>();
            foreach (var orderItem in orderItems)
            {
                var orderShop = orderItem.OrderShop;
                if (orderShop is not { Status: OrderShopStatus.Delivered } ||
                    orderItem.Status != OrderItemStatus.Delivered)
                {
                    return Result<ReturnOrderItemResponse>.Failure("OrderShopStatus invalid", ErrorType.Conflict);
                }

                DateTime? dateTimeReturn = null;
                if (orderShop is { DeliveredDate: not null })
                {
                    dateTimeReturn = orderShop.DeliveredDate.Value.AddDays(7);
                }

                if (dateTimeReturn < DateTime.UtcNow || !orderShop.DeliveredDate.HasValue)
                {
                    return Result<ReturnOrderItemResponse>.Failure("Return period expired", ErrorType.Conflict);
                }

                var quantityReturn = request.ItemsReturn[orderItem.Id];
                if (quantityReturn > orderItem.Quantity || quantityReturn <= 0)
                {
                    return Result<ReturnOrderItemResponse>.Failure("Quantity returnOrder invalid",
                        ErrorType.Conflict);
                }

                var returnOrderItem = new ReturnOrderItem()
                {
                    Id = Guid.NewGuid(),
                    OrderItem = orderItem,
                    OrderItemId = orderItem.Id,
                    Quantity = quantityReturn,
                    Reason = request.Reason,
                    ReturnOrder = returnOrder,
                    ReturnOrderId = returnOrder.Id,
                    ReturnShippingTrackingCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                };
                var orderHistory = new OrderHistory()
                {
                    Id = Guid.NewGuid(),
                    CreateAt = DateTime.UtcNow,
                    OrderItemId = orderItem.Id,
                    OrderShopId = orderShop.Id,
                    OrderId = order.Id,
                    Note = nameof(OrderItemStatus.ReturnRequest),
                    ChangedByUserId = userId,
                    FromStatus = nameof(OrderItemStatus.Delivered),
                    ToStatus = nameof(OrderItemStatus.ReturnRequest),
                };
                orderItem.Status = OrderItemStatus.ReturnRequest;
                dbContext.OrderHistories.Add(orderHistory);
                returnOrder.ReturnOrderItems.Add(returnOrderItem);
                dbContext.ReturnOrderItems.Add(returnOrderItem);
                returnOrderItems[returnOrderItem.Id] = returnOrderItem.ReturnShippingTrackingCode;
            }

            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            var  response = new ReturnOrderItemResponse(returnOrder.Id, returnOrderItems);
            return Result<ReturnOrderItemResponse>.Success(response);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<ReturnOrderItemResponse>.Failure("Concurrency Conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<ReturnOrderItemResponse>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> ApproveReturnOrderItemAsync(ApproveReturnOrderItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            return Result<bool>.Failure("UserRole invalid", ErrorType.Conflict);
        }
        
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return Result<bool>.Failure("Shop not found",  ErrorType.NotFound);
            }

            var returnOrder = await dbContext.ReturnOrders.Include(returnOrder => returnOrder.ReturnOrderItems)
                .ThenInclude(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .Include(returnOrder => returnOrder.Order)
                .FirstOrDefaultAsync(o => o.Id == request.ReturnOrderId);
            if (returnOrder == null)
            {
                return Result<bool>.Failure("ReturnOrder not found",  ErrorType.NotFound);
            }
            if (returnOrder.Status != ReturnStatus.Pending)
            {
                return Result<bool>.Failure("StatusReturnOrder invalid", ErrorType.Conflict);
            }
            var isOwner = returnOrder.ReturnOrderItems.Any(roi => roi.OrderItem.OrderShop != null && roi.OrderItem.OrderShop.ShopId == shop.Id);
            if (!isOwner)
            {
                return Result<bool>.Failure("ReturnOrderItem not found", ErrorType.NotFound);
            }

            var fromStatus = returnOrder.Status.ToString();
            var returnOrderItems = await dbContext.ReturnOrderItems
                .Include(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.Item)
                .Include(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .Where(r => r.ReturnOrderId == request.ReturnOrderId && request.ReturnOrderItemsId.Contains(r.Id))
                .ToListAsync();

            if (returnOrderItems.Count == 0)
            {
                return Result<bool>.Failure("ReturnOrderItems not found",  ErrorType.NotFound);
            }

            foreach (var returnOrderItem in returnOrderItems)
            {
                if (returnOrderItem.Status != ReturnStatus.Pending)
                {
                    return Result<bool>.Failure("StatusReturnOrderItem invalid", ErrorType.Conflict);
                }
                var orderItem = returnOrderItem.OrderItem;
                Debug.Assert(orderItem != null, nameof(orderItem) + " != null");
                orderItem.Status = OrderItemStatus.ReturnApproved;
                returnOrderItem.Status = ReturnStatus.Approved;
                var orderHistory = new OrderHistory()
                {
                    Id = Guid.NewGuid(),
                    CreateAt = DateTime.UtcNow,
                    ChangedByUserId = userId,
                    FromStatus = fromStatus,
                    ToStatus = nameof(ReturnStatus.Approved),
                    Note = "Return order approved",
                    OrderItemId =  orderItem.Id,
                    OrderShopId = orderItem.OrderShop?.Id,
                    Order = returnOrder.Order,
                    OrderId = returnOrder.OrderId,
                };
                dbContext.OrderHistories.Add(orderHistory);
            }
            
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency Conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("DatabaseError", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> RejectReturnOrderItemAsync(RejectReturnOrderItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var user =  await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            return Result<bool>.Failure("UserRole invalid", ErrorType.Conflict);
        }
        
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return Result<bool>.Failure("Shop not found", ErrorType.NotFound);
            }

            var returnOrder = await dbContext.ReturnOrders.Include(returnOrder => returnOrder.Order).Include(returnOrder => returnOrder.ReturnOrderItems)
                .ThenInclude(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .FirstOrDefaultAsync(o => o.Id == request.ReturnOrderId);
            if (returnOrder == null)
            {
                return Result<bool>.Failure("ReturnOrder not found", ErrorType.NotFound);
            }
            if (returnOrder is not { Status: ReturnStatus.Pending })
            {
                return Result<bool>.Failure("StatusReturnOrder invalid", ErrorType.Conflict);
            }
            var isOwner = returnOrder.ReturnOrderItems.Any(roi => roi.OrderItem.OrderShop != null && roi.OrderItem.OrderShop.ShopId == shop.Id);
            if (!isOwner)
            {
                return Result<bool>.Failure("ReturnOrder not found", ErrorType.NotFound);
            }

            var fromStatus = returnOrder.Status.ToString();
            var returnOrderItems = await dbContext.ReturnOrderItems
                .Where(r => r.ReturnOrderId == request.ReturnOrderId && request.RejectReturnOrderItems.Keys.Contains(r.Id))
                .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.OrderShop)
                .ToListAsync();

            if (returnOrderItems.Count == 0)
            {
                return Result<bool>.Failure("ReturnOrderItem not found", ErrorType.NotFound);
            }
            foreach (var returnOrderItem in returnOrderItems)
            {
                if (returnOrderItem.Status != ReturnStatus.Pending)
                {
                    return Result<bool>.Failure("Invalid state", ErrorType.Conflict);
                }
                var orderItem = returnOrderItem.OrderItem;
                Debug.Assert(orderItem != null, nameof(orderItem) + " != null");
                orderItem.Status = OrderItemStatus.ReturnRejected;
                returnOrderItem.Status = ReturnStatus.Rejected;
                returnOrderItem.Reason = request.RejectReturnOrderItems[returnOrderItem.Id];
                var orderHistory = new OrderHistory()
                {
                    Id = Guid.NewGuid(),
                    CreateAt = DateTime.UtcNow,
                    ChangedByUserId = userId,
                    FromStatus = fromStatus,
                    ToStatus = nameof(ReturnStatus.Rejected),
                    Note = request.RejectReturnOrderItems[returnOrderItem.Id],
                    OrderItemId =  orderItem.Id,
                    OrderShopId = orderItem.OrderShop?.Id,
                    Order = returnOrder.Order,
                    OrderId = returnOrder.OrderId,
                };
                returnOrderItem.Reason  = request.RejectReturnOrderItems[returnOrderItem.Id];
                dbContext.OrderHistories.Add(orderHistory);
            }
            
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }
    

    public async Task<Result<IEnumerable<OrderHistory>>> GetOrderHistoryAsync(GetOrderHistoryRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<IEnumerable<OrderHistory>>.Failure("Token invalid", ErrorType.Unauthorized);
        }
        var orderExists = await dbContext.Orders.AnyAsync(o => o.Id == request.OrderId && o.UserId == userId);
        if (!orderExists)
        {
            return Result<IEnumerable<OrderHistory>>.Failure("Order not found or permission denied.", ErrorType.NotFound);
        }
        var histories = await dbContext.OrderHistories
            .Where(h => h.OrderId == request.OrderId)
            .OrderBy(h => h.CreateAt)
            .ToListAsync();
        return Result<IEnumerable<OrderHistory>>.Success(histories);
    }

    public async Task<Result<bool>> ShipOrderShopAsync(ShipOrderShopRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found",  ErrorType.NotFound);
        }
        if(!await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            return Result<bool>.Failure("User not permitted", ErrorType.NotFound);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
        if (shop == null)
        {
            return Result<bool>.Failure("Shop not found",  ErrorType.NotFound);
        }
        var orderShop = await dbContext.OrderShops.Include(os => os.OrderItems)
            .FirstOrDefaultAsync(os => os.Id == request.OrderShopId && os.ShopId == shop.Id); 
        if (orderShop == null)
        {
            return Result<bool>.Failure("Order shop not found",  ErrorType.NotFound);
        }

        if (orderShop.Status != OrderShopStatus.Processing && orderShop.Status != OrderShopStatus.ReadyToShop)
        {
            return Result<bool>.Failure("StatusOrderShop invalid", ErrorType.Conflict);
        }

        await using var dbTransaction= await dbContext.Database.BeginTransactionAsync();
        try
        {
            OrderItemStatus newOrderItemStatus;
            switch (orderShop.Status)
            {
                case OrderShopStatus.Processing:
                    orderShop.Status = OrderShopStatus.ReadyToShop;
                    newOrderItemStatus = OrderItemStatus.ReadyToShop; 
                    break;
                case OrderShopStatus.ReadyToShop:
                    orderShop.Status = OrderShopStatus.Shipped;
                    newOrderItemStatus = OrderItemStatus.Shipped;
                    break;
                default:
                    return Result<bool>.Failure("OrderShopStatus not permitted", ErrorType.Conflict);
            }
            foreach (var orderItem in orderShop.OrderItems)
            {
                orderItem.Status = newOrderItemStatus;
            }
            dbContext.OrderShops.Update(orderShop);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> SellerCancelOrderAsync(SellerCancelOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found",   ErrorType.NotFound);
        }
        if (!await userManager.IsInRoleAsync(user, UserRoles.Seller))
        {
            return Result<bool>.Failure("UserRole invalid",  ErrorType.NotFound);
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
        if (shop == null)
        {
            return Result<bool>.Failure("Shop not found",  ErrorType.NotFound);
        }
        var orderShop = await dbContext.OrderShops.Include(os => os.OrderItems).ThenInclude(orderItem => orderItem.Item)
            .FirstOrDefaultAsync(os=> os.Id == request.OrderShopId && os.ShopId == shop.Id);
        if (orderShop == null)
        {
            return Result<bool>.Failure("OrderShop not found",  ErrorType.NotFound);
        }

        if (orderShop.Status != OrderShopStatus.PendingConfirmation && orderShop.Status != OrderShopStatus.Processing)
        {
            return Result<bool>.Failure("Status OrderShop invalid",  ErrorType.Conflict);
        }
        var order = await dbContext.Orders.Include(o => o.OrderShops).Include(order => order.User).FirstOrDefaultAsync(o=>o.Id == orderShop.OrderId);
        if (order == null) 
        { 
            return Result<bool>.Failure("Order not found",  ErrorType.NotFound);
        }

        var customer = order.User;
        if (customer == null)
        {
            return Result<bool>.Failure("Customer not found",  ErrorType.NotFound);
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var fromStatus = orderShop.Status;
            orderShop.Status = OrderShopStatus.Cancelled;
            foreach (var orderItem in orderShop.OrderItems)
            {
                if(orderItem.Status!= OrderItemStatus.Pending && orderItem.Status!= OrderItemStatus.Processing)
                orderItem.Status = OrderItemStatus.Cancelled;
                if (orderItem.Item != null)
                {
                    orderItem.Item.Stock += orderItem.Quantity;
                }
            }

            if (order.Status == OrderStatus.Paid)
            {
                var refundRequest = new CreateRefundWhenSellerCancelRequest(orderShop.Id, orderShop.TotalShopAmount);
                var refundResult = await transactionService.CreateRefundWhenSellerCancelAsync(refundRequest);
                if (!refundResult.IsSuccess)
                {
                    await dbTransaction.RollbackAsync();
                    return Result<bool>.Failure("Refund request failed", ErrorType.Conflict);
                }
            }
            var check = true;
            foreach (var orderShopOther in order.OrderShops)
            {
                if (orderShopOther.Status != OrderShopStatus.Cancelled)
                {
                    check = false;
                }
            }

            if (check)
            {
                order.Status = OrderStatus.Canceled;
            }
            var orderHistory = new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderShopId = orderShop.Id,
                ChangedByUserId = userId,
                FromStatus = fromStatus.ToString(),
                ToStatus = orderShop.Status.ToString(),
                CreateAt = DateTime.UtcNow,
                Note = request.Reason,
                OrderId = order.Id,
                Order = order,
            };
            dbContext.OrderHistories.Add(orderHistory);
            dbContext.OrderShops.Update(orderShop);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict",  ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error",  ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> MarkShopOrderAsDeliveredAsync(MarkShopOrderAsDeliveredRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("Shop not found",  ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Admin))
        {
            return Result<bool>.Failure("UserRole invalid",  ErrorType.Conflict);
        }
        var orderShop = await dbContext.OrderShops.Include(os => os.OrderItems).Include(os=>os.Order).ThenInclude(o => o.OrderShops).FirstOrDefaultAsync(o => o.Id == request.OrderShopId);
        if (orderShop == null)
        {
            return Result<bool>.Failure("OrderShop not found",  ErrorType.NotFound);
        }

        if (orderShop.Status != OrderShopStatus.Shipped)
        {
            return Result<bool>.Failure("OrderShopStatus invalid",  ErrorType.Conflict);
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var fromStatus = orderShop.Status;
            orderShop.Status = OrderShopStatus.Delivered;
            orderShop.DeliveredDate = DateTime.UtcNow;
            foreach (var orderItem in orderShop.OrderItems)
            {
                orderItem.Status = OrderItemStatus.Delivered;
            }

            var order = orderShop.Order;
            if (order != null && order.OrderShops.All(os => os.Status == OrderShopStatus.Delivered))
            {
                order.Status = OrderStatus.Delivered;
            }

            if (order != null)
            {
                var orderHistory = new OrderHistory
                {
                    Id = Guid.NewGuid(),
                    OrderShopId = orderShop.Id,
                    ChangedByUserId = userId,
                    FromStatus = fromStatus.ToString(),
                    ToStatus = orderShop.Status.ToString(),
                    CreateAt = DateTime.UtcNow,
                    Note = "Delivery successfully",
                    OrderId = order.Id,
                    Order = order,
                };
                dbContext.OrderHistories.Add(orderHistory);
            }

            dbContext.OrderShops.Update(orderShop);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict",  ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error",  ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> MarkOrderShopAsCompletedAsync(MarkOrderShopAsCompletedRequest request) 
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out var adminId))
        {
            return Result<bool>.Failure("Token invalid", ErrorType.Unauthorized);
        }

        var adminUser = await userManager.FindByIdAsync(adminIdString);
        if (adminUser == null || !await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
        {
            return Result<bool>.Failure("User is not an admin.", ErrorType.Forbidden);
        }
        
        var orderShop = await dbContext.OrderShops
            .Include(os => os.Shop)
            .Include(os => os.Order)
            .FirstOrDefaultAsync(os => os.Id == request.OrderShopId);
            
        if (orderShop == null)
        {
            return Result<bool>.Failure("OrderShop not found.", ErrorType.NotFound);
        }
        if (orderShop.Status != OrderShopStatus.Delivered)
        {
            return Result<bool>.Failure("OrderShop must be in 'Delivered' status to be completed.", ErrorType.Conflict);
        }
        if (orderShop.DeliveredDate == null || orderShop.DeliveredDate.Value.AddDays(7) > DateTime.UtcNow)
        {
            return Result<bool>.Failure("The 7-day return period has not expired yet.", ErrorType.Conflict);
        }
        
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var fromStatus = orderShop.Status;
            orderShop.Status = OrderShopStatus.Completed;
            
            if (orderShop.Shop == null)
            {
                await dbTransaction.RollbackAsync();
                return Result<bool>.Failure("Shop information is missing for this order.", ErrorType.NotFound);
            }
            var payoutRequest = new CreatePayOutRequest(orderShop.Shop.UserId, orderShop.Id, orderShop.TotalShopAmount);
            var payoutResult = await transactionService.CreatePayOutAsync(payoutRequest);
            if (!payoutResult.IsSuccess)
            {
                await dbTransaction.RollbackAsync();
                return Result<bool>.Failure("Payout fail", ErrorType.Conflict);
            }
            
            var orderHistory = new OrderHistory
            {
                Order = orderShop.Order,
                OrderId = orderShop.OrderId,
                OrderShopId = orderShop.Id,
                Note = "Order shop marked as completed and payout processed.",
                FromStatus = fromStatus.ToString(),
                ToStatus = orderShop.Status.ToString(),
                ChangedByUserId = adminId,
                CreateAt = DateTime.UtcNow,
            };
            await dbContext.OrderHistories.AddAsync(orderHistory);
            
            
            var mainOrder = await dbContext.Orders.Include(o => o.OrderShops).FirstOrDefaultAsync(o => o.Id == orderShop.OrderId);
            if (mainOrder != null && mainOrder.OrderShops.All(os => os.Status == OrderShopStatus.Completed))
            {
                mainOrder.Status = OrderStatus.Completed;
            }

            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error", ErrorType.Conflict);
        }
    }
    public async Task<Result<bool>> CancelEntireOrderAsync(CancelEntireOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }
        var user = await userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return Result<bool>.Failure("User not found",  ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(user, UserRoles.Admin))
        {
            return Result<bool>.Failure("UserRole invalid",  ErrorType.Conflict);
        }
        var order = await dbContext.Orders.Include(o => o.OrderShops).ThenInclude(os => os.OrderItems)
            .ThenInclude(ot => ot.Shop).Include(order => order.OrderShops)
            .ThenInclude(orderShop => orderShop.OrderItems).ThenInclude(orderItem => orderItem.Item)
            .Include(order => order.OrderShops).ThenInclude(orderShop => orderShop.VoucherShop).FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order == null)
        {
            return Result<bool>.Failure("Order not found",  ErrorType.NotFound);
        }

        if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.Paid)
        {
            return Result<bool>.Failure("OrderStatus invalid",  ErrorType.Conflict);
        }
        var fromStatus = order.Status;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            order.Status = OrderStatus.Canceled;
            foreach (var orderShop in order.OrderShops)
            {
                if (orderShop.Status != OrderShopStatus.PendingConfirmation)
                {
                    return Result<bool>.Failure("OrderShopStatus invalid", ErrorType.Conflict);
                }
                orderShop.Status = OrderShopStatus.Cancelled;
                foreach (var orderItem in orderShop.OrderItems)
                {
                    if (orderItem.Status != OrderItemStatus.Pending)
                    {
                        return Result<bool>.Failure("OrderItemStatus invalid", ErrorType.Conflict);
                    }
                    orderItem.Status = OrderItemStatus.Cancelled;
                    if (orderItem.Item != null)
                    {
                        orderItem.Item.Stock += orderItem.Quantity;
                    }
                }

                if (orderShop.VoucherShop != null)
                {
                    orderShop.VoucherShop.Quantity += 1;
                }
            }

            if (order.VoucherProductId.HasValue)
            {
                var voucherProduct = await dbContext.Vouchers.FirstOrDefaultAsync(v=>v.Id == order.VoucherProductId.Value);
                if (voucherProduct != null)
                {
                    voucherProduct.Quantity += 1;
                }
            }

            if (order.VoucherShippingId.HasValue)
            {
                var voucherShipping =
                    await dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == order.VoucherShippingId.Value);
                if (voucherShipping != null)
                {
                    voucherShipping.Quantity += 1;
                } 
            }

            var orderHistory = new OrderHistory
            {
                Id = Guid.NewGuid(),
                Order = order,
                OrderId = order.Id,
                Note = request.Reason,
                FromStatus = fromStatus.ToString(),
                ToStatus = nameof(OrderStatus.Canceled),
                ChangedByUserId = userId,
                CreateAt = DateTime.UtcNow,
            };
            await dbContext.OrderHistories.AddAsync(orderHistory);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict",  ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error",  ErrorType.Conflict);
        }
    }

    public async Task<Result<bool>> ProcessReturnRequestAsync(ProcessReturnRequestRequest request)
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out var adminId))
        {
            return Result<bool>.Failure("Token invalid",  ErrorType.Unauthorized);
        }

        var admin = await userManager.FindByIdAsync(adminIdString);
        if (admin == null)
        {
            return Result<bool>.Failure("Admin not found",  ErrorType.NotFound);
        }

        if (!await userManager.IsInRoleAsync(admin, UserRoles.Admin))
        {
            return Result<bool>.Failure("UserRole invalid",  ErrorType.Conflict);
        }

        var returnOrder = await dbContext.ReturnOrders.Include(ro => ro.ReturnOrderItems)
            .ThenInclude(rot => rot.OrderItem).ThenInclude(orderItem => orderItem.Item).Include(returnOrder => returnOrder.Order).FirstOrDefaultAsync(ro => ro.Id == request.ReturnOrderId);
        if (returnOrder == null)
        {
            return Result<bool>.Failure("ReturnOrder not found",  ErrorType.NotFound);
        }

        if (returnOrder.Status is ReturnStatus.Approved or ReturnStatus.Completed)
        {
            return Result<bool>.Failure("ReturnOrderStatus invalid",  ErrorType.Conflict);
        }
        var fromStatus = returnOrder.Status;
        await using var  dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            if (request.IsApproved)
            {
                returnOrder.Status = ReturnStatus.Approved;
                foreach (var returnOderItem in returnOrder.ReturnOrderItems)
                {
                    if (returnOderItem.Status == ReturnStatus.Approved ||
                        returnOderItem.Status == ReturnStatus.Completed)
                    {
                        return Result<bool>.Failure("ReturnOrderItemStatus invalid",   ErrorType.Conflict);
                    }
                    returnOderItem.OrderItem.Status = OrderItemStatus.ReturnApproved;
                    returnOderItem.Status = ReturnStatus.Approved;
                    if (returnOderItem.OrderItem.Item != null)
                    {
                        returnOderItem.OrderItem.Item.Stock += returnOderItem.Quantity;
                    }
                }
            }
            else
            {
                returnOrder.Status = ReturnStatus.Rejected;
                foreach (var returnOderItem in returnOrder.ReturnOrderItems)
                {
                    returnOderItem.Status = ReturnStatus.Rejected;
                    returnOderItem.OrderItem.Status = OrderItemStatus.ReturnRejected;
                    returnOderItem.Reason = request.Reason;
                }
            }

            var orderHistory = new OrderHistory
            {
                Id = Guid.NewGuid(),
                Order = returnOrder.Order,
                OrderId = returnOrder.OrderId,
                CreateAt = DateTime.UtcNow,
                FromStatus = fromStatus.ToString(),
                ToStatus = returnOrder.Status.ToString(),
                Note = request.Reason,
                ChangedByUserId = adminId,
            };
            await dbContext.OrderHistories.AddAsync(orderHistory);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Concurrency conflict",  ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return Result<bool>.Failure("Database error",  ErrorType.Conflict);
        }
    }
}