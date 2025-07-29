using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.Success, result);
        var item1 = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id == item.Id);
        Assert.NotNull(item1);
        Assert.Equal(item1.Stock, 8);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.TokenInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.UserNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([Guid.NewGuid()], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.CartItemNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.AddressNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, Guid.NewGuid());
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.AddressNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.OutOfStock, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
            PhoneNumber = "08818888888",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.InsufficientStock, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.VoucherExpired, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.MinSpendNotMet, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        await dbContext.Object.Database.EnsureCreatedAsync(); 
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext.Object);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.ConcurrencyConflict, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        await dbContext.Object.Database.EnsureCreatedAsync(); 
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext.Object);
        var request = new CreateOrderRequest([cartItem.Id], voucherData, null, voucherShipping.Id, null,
            null, null, null);
        var result = await orderService.CreateOrderAsync(request);
        Assert.Equal(CreateOrderResult.DatabaseError, result);
    }

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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.Success, result);
        var item1 = await dbContext.Items.FirstOrDefaultAsync(i=>i.Id == item.Id);
        Assert.NotNull(item1);
        Assert.Equal(item1.Stock, 12);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.TokenInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.UserNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CancelMainOrderRequest(Guid.NewGuid(), "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.OrderNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
        var order = new Order()
        {
            Id = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            DiscountProductAmount = 30,
            DiscountShippingAmount = 0,
            TotalAmount = 170,
            TotalShippingFee = 0,
            TotalSubtotal = 200,
            Status = OrderStatus.Completed,
            VoucherProductId = null,
            VoucherShippingId = voucherShipping.Id,
            UserAddress = address1,
            User = customer,
            UserId = customer.Id,
            UserAddressId = address1.Id,
            OrderShops = new List<OrderShop>()
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.NotPermitted, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Object.Database.EnsureCreatedAsync(); 
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.ConcurrencyConflict, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Object.Database.EnsureCreatedAsync(); 
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.Shops.AddAsync(shop);
        await dbContext.Object.Addresses.AddRangeAsync(address, address1);
        await dbContext.Object.Items.AddAsync(item);
        await dbContext.Object.Users.AddAsync(customer);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext.Object);
        var request = new CancelMainOrderRequest(order.Id, "testReason");
        var result = await orderService.CancelMainOrderAsync(request);
        Assert.Equal(CancelMainOrderResult.DatabaseError, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
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
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { item.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.Equal(ReturnOrderItemResult.Success, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
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
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { item.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.Equal(ReturnOrderItemResult.TokenInvalid, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { item.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.Equal(ReturnOrderItemResult.UserNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { item.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(Guid.NewGuid(), itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.Equal(ReturnOrderItemResult.OrderNotFound, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
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
        var customer= new User()
        {
            Id = Guid.NewGuid(),
            Username = "1232314567",
            Password = BCrypt.Net.BCrypt.HashPassword("12345116789"),
            FullName = "Joh1n Doe",
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
            {
                new OrderShop()

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
                    OrderItems = new List<OrderItem>()
                    {
                        new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            Quantity = 2,
                            Item = item,
                            ItemId = item.Id,
                            Price = item.Price,
                            ShopId = shop.Id,
                            Shop = shop,
                            Status = OrderItemStatus.Pending,
                            TotalAmount = 200,
                            OrderShop = null,
                        },
                    },
                },
            }
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddRangeAsync(address, address1);
        await dbContext.Items.AddAsync(item);
        await dbContext.Users.AddAsync(customer);
        await dbContext.CartItems.AddAsync(cartItem);
        await dbContext.Orders.AddAsync(order);
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
        var orderService = new OrderService(mockHttpContextAccessor.Object, dbContext);
        var itemReturn = new Dictionary<Guid, int>()
        {
            { item.Id, 1 }
        };
        var request = new ReturnOrderItemRequest(order.Id, itemReturn, "testReason");
        var result = await orderService.ReturnOrderItemAsync(request);
        Assert.Equal(ReturnOrderItemResult.NotPermitted, result);
    }
}