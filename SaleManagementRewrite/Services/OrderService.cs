using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Services;

public class OrderService(IHttpContextAccessor httpContextAccessor, ApiDbContext dbContext)
    : IOrderService
{
    private const double EarthRadiusKm = 6371.0;
    private const double ShippingFeePerKm = 1000;

    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CreateOrderResult.TokenInvalid;
        }

        var user = await dbContext.Users.Include(user => user.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return CreateOrderResult.UserNotFound;
        }

        var cartItems = await dbContext.CartItems.Include(ci => ci.Item).ThenInclude(item => item.Shop)
            .ThenInclude(shop => shop.Address).Include(cartItem => cartItem.Item).ThenInclude(item => item.Shop)
            .ThenInclude(shop => shop.Vouchers)
            .Where(ci => ci.UserId == userId && request.CartItemId.Contains(ci.Id)).ToListAsync();
        if (cartItems.Count == 0 || cartItems.Count != request.CartItemId.Count)
        {
            return CreateOrderResult.CartItemNotFound;
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
                    return CreateOrderResult.AddressNotFound;
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
                    return CreateOrderResult.AddressNotFound;
                }
            }

            var userLatitude = address.Latitude;
            var userLongitude = address.Longitude;
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
                        return CreateOrderResult.OutOfStock;
                    }

                    if (itemInShop.Quantity > item.Stock && item.Stock > 0)
                    {
                        return CreateOrderResult.InsufficientStock;
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
                        return CreateOrderResult.VoucherExpired;
                    }

                    if (voucherShop.MinSpend.HasValue && voucherShop.MinSpend.Value > orderShop.SubTotalShop)
                    {
                        return CreateOrderResult.MinSpendNotMet;
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
                    return CreateOrderResult.VoucherExpired;
                }

                if (voucherProduct.MinSpend.HasValue && order.TotalSubtotal < voucherProduct.MinSpend)
                {
                    return CreateOrderResult.MinSpendNotMet;
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
                    return CreateOrderResult.VoucherExpired;
                }

                if (voucherShipping.MinSpend.HasValue && order.TotalSubtotal < voucherShipping.MinSpend)
                {
                    return CreateOrderResult.MinSpendNotMet;
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
            return CreateOrderResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CreateOrderResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CreateOrderResult.DatabaseError;
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


    public async Task<CancelMainOrderResult> CancelMainOrderAsync(CancelMainOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CancelMainOrderResult.TokenInvalid;
        }

        var user = await dbContext.Users.Include(user => user.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return CancelMainOrderResult.UserNotFound;
        }

        var order = await dbContext.Orders.Include(order => order.OrderShops)
            .ThenInclude(orderShop => orderShop.OrderItems).ThenInclude(orderItem => orderItem.Item)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
        if (order == null)
        {
            return CancelMainOrderResult.OrderNotFound;
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            return CancelMainOrderResult.NotPermitted;
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
            return CancelMainOrderResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CancelMainOrderResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CancelMainOrderResult.DatabaseError;
        }
    }


    public async Task<ReturnOrderItemResult> ReturnOrderItemAsync(ReturnOrderItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return ReturnOrderItemResult.TokenInvalid;
        }

        var user = await dbContext.Users.Include(user => user.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return ReturnOrderItemResult.UserNotFound;
        }

        var order = await dbContext.Orders.FirstOrDefaultAsync(o =>
            o.Id == request.OrderId && o.UserId == userId );
        if (order == null)
        {
            return ReturnOrderItemResult.OrderNotFound;
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
                .Include(oi => oi.OrderShop).ThenInclude(os=>os.Order)
                .Where(oi => orderItemIdsToReturn.Contains(oi.Id))
                .ToListAsync();
            foreach (var orderItem in orderItems)
            {
                var orderShop = orderItem.OrderShop;
                if (orderShop.Status != OrderShopStatus.Delivered && orderItem.Status != OrderItemStatus.Delivered )
                {
                    return ReturnOrderItemResult.NotPermitted;
                }
                DateTime? dateTimeReturn = null;
                if (orderShop.DeliveredDate.HasValue)
                { 
                    dateTimeReturn =  orderShop.DeliveredDate.Value.AddDays(7);
                }
                if (dateTimeReturn < DateTime.UtcNow || !orderShop.DeliveredDate.HasValue)
                {
                    return ReturnOrderItemResult.ReturnPeriodExpired;
                }
                var quantityReturn = request.ItemsReturn[orderItem.Id];
                if (quantityReturn > orderItem.Quantity || quantityReturn <= 0)
                {
                    return ReturnOrderItemResult.QuantityReturnInvalid;
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
            }
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return ReturnOrderItemResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return ReturnOrderItemResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return ReturnOrderItemResult.DatabaseError;
        }
    }

    public async Task<bool> ApproveReturnOrderItemAsync(ApproveReturnOrderItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return false;
        }
        var user =  await dbContext.Users.Include(user => user.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        if (user.UserRole != UserRole.Seller)
        {
            return false;
        }
        
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return false;
            }

            var returnOrder = await dbContext.ReturnOrders.Include(returnOrder => returnOrder.ReturnOrderItems)
                .ThenInclude(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .FirstOrDefaultAsync(o => o.Id == request.ReturnOrderId);
            if (returnOrder is not { Status: ReturnStatus.Pending })
            {
                return false;
            }
            var isOwner = returnOrder.ReturnOrderItems.Any(roi => roi.OrderItem.OrderShop.ShopId == shop.Id);
            if (!isOwner)
            {
                return false; 
            }

            var fromStatus = returnOrder.Status.ToString();
            var returnOrderItems = await dbContext.ReturnOrderItems
                .Include(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.Item)
                .Include(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .Where(r => r.ReturnOrderId == request.ReturnOrderId && request.ReturnOrderItemsId.Contains(r.Id))
                .ToListAsync();

            if (returnOrderItems.Count == 0)
            {
                return false;
            }

            foreach (var returnOrderItem in returnOrderItems)
            {
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
                    OrderShopId = orderItem.OrderShop.Id,
                };
                dbContext.OrderHistories.Add(orderHistory);
            }
            
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> RejectReturnOrderItemAsync(RejectReturnOrderItemRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return false;
        }
        var user =  await dbContext.Users.Include(user => user.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        if (user.UserRole != UserRole.Seller)
        {
            return false;
        }
        
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null)
            {
                return false;
            }

            var returnOrder = await dbContext.ReturnOrders.Include(returnOrder => returnOrder.ReturnOrderItems)
                .ThenInclude(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .FirstOrDefaultAsync(o => o.Id == request.ReturnOrderId);
            if (returnOrder is not { Status: ReturnStatus.Pending })
            {
                return false;
            }
            var isOwner = returnOrder.ReturnOrderItems.Any(roi => roi.OrderItem.OrderShop.ShopId == shop.Id);
            if (!isOwner)
            {
                return false; 
            }

            var fromStatus = returnOrder.Status.ToString();
            var returnOrderItems = await dbContext.ReturnOrderItems
                .Include(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.Item)
                .Include(returnOrderItem => returnOrderItem.OrderItem).ThenInclude(orderItem => orderItem.OrderShop)
                .Where(r => r.ReturnOrderId == request.ReturnOrderId && request.RejectReturnOrderItems.Keys.Contains(r.Id))
                .ToListAsync();

            if (returnOrderItems.Count == 0)
            {
                return false;
            }

            foreach (var returnOrderItem in returnOrderItems)
            {
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
                    OrderShopId = orderItem.OrderShop.Id,
                };
                returnOrderItem.Reason  = request.RejectReturnOrderItems[returnOrderItem.Id];
                dbContext.OrderHistories.Add(orderHistory);
            }
            
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }
    

    public async Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(GetOrderHistoryRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return [];
        }
        var query = dbContext.OrderHistories
            .Where(h => h.OrderId == request.OrderId && h.Order != null && h.Order.UserId == userId)
            .OrderBy(h => h.CreateAt);
        
        return await query.ToListAsync();
    }

    public async Task<ShipOrderShopResult> ShipOrderShopAsync(ShipOrderShopRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return ShipOrderShopResult.TokenInvalid;
        }
        var user = await dbContext.Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return ShipOrderShopResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
        if (shop == null)
        {
            return ShipOrderShopResult.ShopNotFound;
        }
        var orderShop = await dbContext.OrderShops.Include(os => os.OrderItems)
            .FirstOrDefaultAsync(os => os.Id == request.OrderShopId && os.ShopId == shop.Id); 
        if (orderShop == null)
        {
            return ShipOrderShopResult.OrderNotFound;
        }

        if (orderShop.Status != OrderShopStatus.Processing && orderShop.Status != OrderShopStatus.ReadyToShop)
        {
            return ShipOrderShopResult.NotPermitted;
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
                    return ShipOrderShopResult.NotPermitted;
            }
            foreach (var orderItem in orderShop.OrderItems)
            {
                orderItem.Status = newOrderItemStatus;
            }
            dbContext.OrderShops.Update(orderShop);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return ShipOrderShopResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return ShipOrderShopResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return ShipOrderShopResult.DatabaseError;
        }
    }

    public async Task<SellerCancelOrderResult> SellerCancelOrderAsync(SellerCancelOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return SellerCancelOrderResult.TokenInvalid;
        }
        var user = await dbContext.Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is not { UserRole: UserRole.Seller })
        {
            return SellerCancelOrderResult.UserNotFound;
        }
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
        if (shop == null)
        {
            return SellerCancelOrderResult.ShopNotFound;
        }
        var orderShop = await dbContext.OrderShops.Include(os => os.OrderItems).ThenInclude(orderItem => orderItem.Item)
            .FirstOrDefaultAsync(os=> os.Id == request.OrderShopId && os.ShopId == shop.Id);
        if (orderShop == null)
        {
            return SellerCancelOrderResult.OrderNotFound;
        }

        if (orderShop.Status != OrderShopStatus.PendingConfirmation && orderShop.Status != OrderShopStatus.Processing)
        {
            return SellerCancelOrderResult.NotPermitted;
        }
        var order = await dbContext.Orders.Include(o => o.OrderShops).Include(order => order.User).FirstOrDefaultAsync(o=>o.Id == orderShop.OrderId);
        if (order == null) 
        { 
            return SellerCancelOrderResult.OrderNotFound;
        }

        var customer = order.User;
        if (customer == null)
        {
            return SellerCancelOrderResult.CustomerNotFound;
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var fromStatus = orderShop.Status;
            orderShop.Status = OrderShopStatus.Cancelled;
            foreach (var orderItem in orderShop.OrderItems)
            {
                orderItem.Status = OrderItemStatus.Cancelled;
                if (orderItem.Item != null)
                {
                    orderItem.Item.Stock += orderItem.Quantity;
                }
                
                //check again..................................
                
                customer.Balance += orderItem.TotalAmount;
                user.Balance -= orderItem.TotalAmount;
            }
            
            bool check = true;
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
            return SellerCancelOrderResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return SellerCancelOrderResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return SellerCancelOrderResult.DatabaseError;
        }
    }

    public async Task<MarkShopOrderAsDeliveredResult> MarkShopOrderAsDeliveredAsync(MarkShopOrderAsDeliveredRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return MarkShopOrderAsDeliveredResult.TokenInvalid;
        }
        var user = await dbContext.Users.Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return MarkShopOrderAsDeliveredResult.UserNotFound;
        }

        if (user.UserRole != UserRole.Admin)
        {
            return MarkShopOrderAsDeliveredResult.NotPermitted;
        }
        var orderShop = await dbContext.OrderShops.Include(os => os.OrderItems).Include(os=>os.Order).ThenInclude(o => o.OrderShops).FirstOrDefaultAsync(o => o.Id == request.OrderShopId);
        if (orderShop == null)
        {
            return MarkShopOrderAsDeliveredResult.OrderNotFound;
        }

        if (orderShop.Status != OrderShopStatus.Shipped)
        {
            return MarkShopOrderAsDeliveredResult.NotPermitted;
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
            if (order.OrderShops.All(os => os.Status == OrderShopStatus.Delivered))
            {
                order.Status = OrderStatus.Delivered;
            }

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
            dbContext.OrderShops.Update(orderShop);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return MarkShopOrderAsDeliveredResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return MarkShopOrderAsDeliveredResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return MarkShopOrderAsDeliveredResult.DatabaseError;
        }
    }


    public async Task<MarkEntireOrderAsCompletedResult> MarkEntireOrderAsCompletedAsync(
        MarkEntireOrderAsCompletedRequest request)
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out var adminId))
        {
            return MarkEntireOrderAsCompletedResult.TokenInvalid;
        }

        var adminUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == adminId);
        if (adminUser == null)
        {
            return MarkEntireOrderAsCompletedResult.UserNotFound;
        }

        if (adminUser.UserRole != UserRole.Admin)
        {
            return MarkEntireOrderAsCompletedResult.NotPermitted;
        }

        var order = await dbContext.Orders.Include(o => o.OrderShops).FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order == null)
        {
            return MarkEntireOrderAsCompletedResult.OrderNotFound;
        }

        if (order.Status != OrderStatus.Delivered)
        {
            return MarkEntireOrderAsCompletedResult.NotPermitted;
        }

        foreach (var orderShop in order.OrderShops)
        {
            if (orderShop.DeliveredDate == null || orderShop.DeliveredDate.Value.AddDays(7) > DateTime.UtcNow)
            {
                return MarkEntireOrderAsCompletedResult.ReturnPeriodNotExpired;
            }
        }
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var fromStatus = order.Status;
            order.Status = OrderStatus.Completed;
            foreach (var orderShop in order.OrderShops)
            {
                orderShop.Status = OrderShopStatus.Completed;
            }

            // tien tu nen tang sang seller
            var orderHistory = new OrderHistory
            {
                Id = Guid.NewGuid(),
                Order = order,
                OrderId = order.Id,
                Note = "Order marked as completed",
                FromStatus = fromStatus.ToString(),
                ToStatus = nameof(OrderStatus.Completed),
                ChangedByUserId = adminId,
                CreateAt = DateTime.UtcNow,
            };
            await dbContext.OrderHistories.AddAsync(orderHistory);
            await dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return MarkEntireOrderAsCompletedResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return MarkEntireOrderAsCompletedResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return MarkEntireOrderAsCompletedResult.DatabaseError;
        }
    }

    public async Task<CancelEntireOrderResult> CancelEntireOrderAsync(CancelEntireOrderRequest request)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return CancelEntireOrderResult.TokenInvalid;
        }
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return CancelEntireOrderResult.UserNotFound;
        }

        if (user.UserRole != UserRole.Admin)
        {
            return CancelEntireOrderResult.NotPermitted;
        }
        var order = await dbContext.Orders.Include(o => o.OrderShops).ThenInclude(os => os.OrderItems)
            .ThenInclude(ot => ot.Shop).Include(order => order.OrderShops)
            .ThenInclude(orderShop => orderShop.OrderItems).ThenInclude(orderItem => orderItem.Item)
            .Include(order => order.OrderShops).ThenInclude(orderShop => orderShop.VoucherShop).FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order == null)
        {
            return CancelEntireOrderResult.OrderNotFound;   
        }

        if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.Paid)
        {
            return  CancelEntireOrderResult.NotPermitted;
        }
        var fromStatus = order.Status;
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            order.Status = OrderStatus.Canceled;
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
            return CancelEntireOrderResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return CancelEntireOrderResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return CancelEntireOrderResult.DatabaseError;
        }
    }

    public async Task<ProcessReturnRequestResult> ProcessReturnRequestAsync(ProcessReturnRequestRequest request)
    {
        var adminIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(adminIdString, out var adminId))
        {
            return ProcessReturnRequestResult.TokenInvalid;
        }
        var admin =  await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == adminId);
        if (admin == null)
        {
            return  ProcessReturnRequestResult.UserNotFound;
        }

        if (admin.UserRole != UserRole.Admin)
        {
            return ProcessReturnRequestResult.NotPermitted;
        }

        var returnOrder = await dbContext.ReturnOrders.Include(ro => ro.ReturnOrderItems)
            .ThenInclude(rot => rot.OrderItem).ThenInclude(orderItem => orderItem.Item).Include(returnOrder => returnOrder.Order).FirstOrDefaultAsync(ro => ro.Id == request.ReturnOrderId);
        if (returnOrder == null)
        {
            return ProcessReturnRequestResult.ReturnOrderNotFound;
        }

        if (returnOrder.Status == ReturnStatus.Approved || returnOrder.Status == ReturnStatus.Completed)
        {
            return ProcessReturnRequestResult.AlreadyProcessed;
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
                if (string.IsNullOrEmpty(request.Reason))
                {
                    return ProcessReturnRequestResult.ReasonIsRequiredForRejection;
                }

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
            return ProcessReturnRequestResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            return ProcessReturnRequestResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            await dbTransaction.RollbackAsync();
            return ProcessReturnRequestResult.DatabaseError;
        }
    }
}