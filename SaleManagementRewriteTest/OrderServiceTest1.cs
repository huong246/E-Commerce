using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class OrderServiceTest1
{
    [Fact]
    public async Task CancelMainOrder_WhenRequestValid_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CancelMainOrder_WhenTokenInvalid_ReturnsTokenInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems = [orderItem];
        order.OrderShops = [orderShop];
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddRangeAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, "null")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CancelMainOrder_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems = [orderItem];
        order.OrderShops = [orderShop];
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddRangeAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CancelMainOrder_WhenOrderNotFound_ReturnsOrderNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems = [orderItem];
        order.OrderShops = [orderShop];
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(Guid.NewGuid(), "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CancelMainOrder_WhenNotPermitted_ReturnsNotPermitted()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems = [orderItem];
        order.OrderShops = [orderShop];
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(false);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CancelMainOrder_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems = [orderItem];
        order.OrderShops = [orderShop];
        await dbContext.Object.Database.EnsureCreatedAsync();
        await dbContext.Object.Users.AddRangeAsync(user, customer);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Categories.AddAsync(category);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.CartItems.AddAsync(cartItem);
        await dbContext.Object.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Object.Orders.AddAsync(order);
        await dbContext.Object.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext.Object,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CancelMainOrder_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems = [orderItem];
        order.OrderShops = [orderShop];
        await dbContext.Object.Database.EnsureCreatedAsync();
        await dbContext.Object.Users.AddRangeAsync(user, customer);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Categories.AddAsync(category);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.CartItems.AddAsync(cartItem);
        await dbContext.Object.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Object.Orders.AddAsync(order);
        await dbContext.Object.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext.Object,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenRequestValid_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.Delivered,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.Delivered,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-2),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.Delivered,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.True(result.IsSuccess);
        var returnOrderItem = await dbContext.ReturnOrderItems.FirstOrDefaultAsync(r => r.OrderItemId == orderItem.Id);
        Assert.NotNull(returnOrderItem);
        Assert.Equal(1, returnOrderItem.Quantity);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenTokenInvalid_ReturnsTokenInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, "null")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenOrderNotFound_ReturnsOrderNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(Guid.NewGuid(), itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenNotPermitted_ReturnsNotPermitted()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenReturnPeriodExpired_ReturnsReturnPeriodExpired()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash =  "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller,
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.Delivered,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.Delivered,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-8),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.Delivered,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenQuantityReturnInvalid_ReturnsQuantityReturnInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash =  "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash =  "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.Delivered,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.Delivered,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.Delivered,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
        await dbContext.OrderShops.AddAsync(orderShop);
        await dbContext.OrderItems.AddAsync(orderItem);
        await dbContext.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 10 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Name = "category",
            Items = new List<Item>()
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.Delivered,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.Delivered,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.Delivered,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        await dbContext.Object.Database.EnsureCreatedAsync();
        await dbContext.Object.Users.AddRangeAsync(user, customer);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Categories.AddAsync(category);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.CartItems.AddAsync(cartItem);
        await dbContext.Object.Orders.AddAsync(order);
        await dbContext.Object.OrderShops.AddAsync(orderShop);
        await dbContext.Object.OrderItems.AddAsync(orderItem);
        await dbContext.Object.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Object.SaveChangesAsync();


        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext.Object,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ReturnOrderItem_ReturnDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRoles.Seller
        };
        var address = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = userId,
            User = user,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Address = address,
            AddressId = address.Id,
            Name = "shop",
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        var category = new Category()
        {
            Id = Guid.NewGuid(),
            Items = new List<Item>(),
            Name = "category"
        };
        var item = new Item()
        {
            Id = Guid.NewGuid(),
            Name = "item",
            Price = 100,
            Stock = 10,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
            Category = category,
            CategoryId = category.Id,
        };
        var voucherShop = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = shop.Id,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        var voucherShipping = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK123",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shipping,
            Value = 30,
            Quantity = 100,
        };
        var customer = new User()
        {
            Id = Guid.NewGuid(),
            UserName = "1232314567",
            PasswordHash = "12345116789",
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
            UserRole = UserRoles.Customer
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            User = customer,
            Item = item,
            ItemId = item.Id,
            UserId = userId,
            ShopId = shop.Id,
            Shop = shop,
        };
        var address1 = new Address()
        {
            Id = Guid.NewGuid(),
            IsDefault = true,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.PendingPayment,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
        };
        var orderShop = new OrderShop()
        {
            Id = Guid.NewGuid(),
            DiscountShopAmount = 30,
            ShopId = shop.Id,
            Shop = shop,
            ShippingFee = 0,
            Status = OrderShopStatus.PendingConfirmation,
            SubTotalShop = 200,
            TotalShopAmount = 170,
            VoucherShop = voucherShop,
            VoucherShopCode = "1234567890",
            VoucherShopId = voucherShop.Id,
            TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
            OrderItems = new List<OrderItem>(),
            Order = order,
            OrderId = order.Id,
            DeliveredDate = DateTime.UtcNow.AddDays(-1),
            Notes = null,
        };
        var orderItem = new OrderItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 2,
            Item = item,
            ItemId = item.Id,
            Price = item.Price,
            ShopId = shop.Id,
            Shop = shop,
            Status = OrderItemStatus.ReturnRequest,
            TotalAmount = 200,
            OrderShop = orderShop,
            OrderShopId = orderShop.Id,
        };
        orderShop.OrderItems.Add(orderItem);
        order.OrderShops.Add(orderShop);
        await dbContext.Object.Database.EnsureCreatedAsync();
        await dbContext.Object.Users.AddRangeAsync(user, customer);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Categories.AddAsync(category);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.CartItems.AddAsync(cartItem);
        await dbContext.Object.Orders.AddAsync(order);
        await dbContext.Object.OrderShops.AddAsync(orderShop);
        await dbContext.Object.OrderItems.AddAsync(orderItem);
        await dbContext.Object.Vouchers.AddRangeAsync(voucherShipping, voucherShop);
        await dbContext.Object.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(customer.Id.ToString())).ReturnsAsync(customer);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(customer, UserRoles.Customer)).ReturnsAsync(true);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext.Object,
            mockUserManager.Object,
            mockTransactionService.Object);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { orderItem.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

