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

public class OrderServiceTest
{
    [Fact]
    public async Task CreateOrderAsync_WhenRequestValid_ReturnsSuccess()
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
            UserRole = UserRoles.Seller,
            PhoneNumber = "0888888888",
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
            Name = "1232344"
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
            CategoryId = category.Id,
            Category = category,
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
            UserRole = UserRoles.Customer,
            PhoneNumber = "08818888888",
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateOrder_WhenTokenInvalid_ReturnsTokenInvalid()
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
            UserRole = UserRoles.Customer,
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenUserNotFound_ReturnsUserNotFound()
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
            Items = new List<Item>(),
            Name = "category",
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
        };
        mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var mockTransactionService = new Mock<ITransactionService>();
        mockTransactionService
            .Setup(x => x.CreateRefundWhenCancelAsync(It.IsAny<CreateRefundWhenCancelRequest>()))
            .ReturnsAsync(Result<Transaction>.Success(new Transaction()));
        var orderService = new OrderService(
            mockHttpContextAccessor.Object,
            dbContext,
            mockUserManager.Object,
            mockTransactionService.Object);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenCartItemNotFound_ReturnsCartItemNotFound()
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([Guid.NewGuid()], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenAddressNotFound_ReturnsAddressNotFound()
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
            IsDefault = false,
            Latitude = 10.0,
            Longitude = 11.0,
            Name = "address",
            UserId = customer.Id,
            User = customer,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenAddressNotFound1_ReturnsAddressNotFound()
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, Guid.NewGuid());
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenOutOfStock_ReturnsOutOfStock()
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
            Stock = 0,
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
            UserRole = UserRoles.Customer,
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenInsufficientStock_ReturnsInsufficientStock()
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
            Items = new List<Item>(),
            Name = "category",
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
            UserRole = UserRoles.Customer,
        };
        var cartItem = new CartItem()
        {
            Id = Guid.NewGuid(),
            Quantity = 20,
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenVoucherExpired_ReturnsVoucherExpired()
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
            Items = new List<Item>(),
            Name = "category",
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
            Quantity = 0,
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
            UserRole = UserRoles.Customer,
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user, customer);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenMinSpendNotMet_ReturnsMinSpendNotMet()
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
            MinSpend = 2500,
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
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
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
            Items = new List<Item>(),
            Name = "category",
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
        await dbContext.Object.Database.EnsureCreatedAsync();
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Categories.AddAsync(category);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.Users.AddAsync(customer);
        await dbContext.Object.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateOrder_WhenDatabaseError_ReturnsDatabaseError()
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
        await dbContext.Object.Database.EnsureCreatedAsync();
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Categories.AddAsync(category);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.Users.AddAsync(customer);
        await dbContext.Object.CartItems.AddAsync(cartItem);
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
        var voucherData = new Dictionary<Guid, Guid>
        {
            { shop.Id, voucherShop.Id }
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
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}