using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class VoucherServiceTest
{
    [Fact]
    public async Task CreateVoucher_WhenRequestValid_ReturnsSuccess()
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
            UserRole = UserRole.Seller,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(shop.Id, null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.Success, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenTokenInvalid_ReturnsTokenInvalid()
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
            UserRole = UserRole.Seller,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, "null")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(shop.Id, null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.TokenInvalid, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenUserNotFound_ReturnsUserNotFound()
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
            UserRole = UserRole.Seller,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(shop.Id, null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.Success, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenNotPermitted_ReturnsNotPermitted()
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
            UserRole = UserRole.Customer,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(shop.Id, null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.NotPermitted, result);
    }
    [Fact]
    public async Task CreateVoucher_WhenQuantityInvalid_ReturnsQuantityInvalid()
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
            UserRole = UserRole.Seller,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(shop.Id, null, 10, -100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.QuantityInvalid, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenShopNotFound_ReturnsShopNotFound()
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
            UserRole = UserRole.Seller,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(Guid.NewGuid(), null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.ShopNotFound, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenItemNotFound_ReturnsItemNotFound()
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
            UserRole = UserRole.Seller,
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
            Stock = 20,
            ShopId = shop.Id,
            Shop = shop,
            Description = "TestItem",
            Color = "blue",
            Size = "100",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Items.AddAsync(item);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateVoucherRequest(shop.Id, Guid.NewGuid(), 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.ItemNotFound, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            UserRole = UserRole.Seller,
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
        await mockDbContext.Object.Database.EnsureCreatedAsync(); 
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        await mockDbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new CreateVoucherRequest(shop.Id, null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.ConcurrencyConflict, result);
    }

    [Fact]
    public async Task CreateVoucher_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            UserRole = UserRole.Seller,
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
        await mockDbContext.Object.Database.EnsureCreatedAsync(); 
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        await mockDbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new CreateVoucherRequest(shop.Id, null, 10, 100, 250, Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, true);
        var result = await voucherService.CreateVoucher(request);
        Assert.Equal(CreateVoucherResult.DatabaseError, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenRequestValid_ReturnsSuccess()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteVoucherRequest(voucher.Id);
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.Success, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenTokenInvalid_ReturnsTokenInvalid()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, "null")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteVoucherRequest(voucher.Id);
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.TokenInvalid, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenUserNotFound_ReturnsUserNotFound()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteVoucherRequest(voucher.Id);
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.UserNotFound, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenNotPermitted_ReturnNotPermitted()
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
            UserRole = UserRole.Customer,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteVoucherRequest(voucher.Id);
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.NotPermitted, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenShopNotFound_ReturnsShopNotFound()
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
            UserRole = UserRole.Seller,
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
    
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteVoucherRequest(Guid.NewGuid());
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.ShopNotFound, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenVoucherNotFound_ReturnsVoucherNotFound()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new DeleteVoucherRequest(Guid.NewGuid());
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.VoucherNotFound, result);
    }
    [Fact]
    public async Task DeleteVoucher_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await mockDbContext.Object.Database.EnsureCreatedAsync(); 
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Vouchers.Add(voucher);
        await mockDbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new DeleteVoucherRequest(voucher.Id);
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.ConcurrencyConflict, result);
    }

    [Fact]
    public async Task DeleteVoucher_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await mockDbContext.Object.Database.EnsureCreatedAsync(); 
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Vouchers.Add(voucher);
        await mockDbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new DeleteVoucherRequest(voucher.Id);
        var result = await voucherService.DeleteVoucher(request);
        Assert.Equal(DeleteVoucherResult.DatabaseError, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenRequestValid_ReturnsSuccess()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.Success, result);
        var voucherUd = await dbContext.Vouchers.FindAsync(voucher.Id);
        Assert.NotNull(voucherUd);
        Assert.Equal(10, voucherUd.Quantity);
    }

    [Fact]
    public async Task UpdateVoucher_WhenTokenInvalid_ReturnsTokenInvalid()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, "null")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.TokenInvalid, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenUserNotFound_ReturnsUserNotFound()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.UserNotFound, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenNotPermitted_ReturnNotPermitted()
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
            UserRole = UserRole.Customer,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.NotPermitted, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenShopNotFound_ReturnsShopNotFound()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
        {
            Id = Guid.NewGuid(),
            Code = "ABCDEFGHJK",
            EndDate = DateTime.Now.AddDays(2),
            StartDate = DateTime.UtcNow,
            IsActive = true,
            ShopId = null,
            ItemId = null,
            Maxvalue = 100,
            MinSpend = 25,
            VoucherMethod = Method.FixAmount,
            VoucherTarget = Target.Shop,
            Value = 30,
            Quantity = 100,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.ShopNotFound, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenVoucherNotFound_ReturnsVoucherNotFound()
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
            UserRole = UserRole.Seller,
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
   
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(Guid.NewGuid(), null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, null);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.VoucherNotFound, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenDuplicateValue_ReturnsDuplicateValue()
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
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Vouchers.AddAsync(voucher);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, dbContext);
        var request = new UpdateVoucherRequest(voucher.Id, voucher.ItemId, voucher.Quantity, voucher.Value, voucher.VoucherMethod, voucher.VoucherTarget, voucher.Maxvalue,
            voucher.StartDate, voucher.EndDate, voucher.MinSpend, voucher.IsActive, null);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.DuplicateValue, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await mockDbContext.Object.Database.EnsureCreatedAsync(); 
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Vouchers.Add(voucher);
        await mockDbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.ConcurrencyConflict, result);
    }

    [Fact]
    public async Task UpdateVoucher_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            UserRole = UserRole.Seller,
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
        var voucher = new Voucher()
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
        await mockDbContext.Object.Database.EnsureCreatedAsync(); 
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Shops.Add(shop);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Vouchers.Add(voucher);
        await mockDbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { 
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        var voucherService = new VoucherService(mockHttpContextAccessor.Object, mockDbContext.Object);
        var request = new UpdateVoucherRequest(voucher.Id, null, 10, 100,  Method.FixAmount, Target.Shop, 1000,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(2), 5, true, voucher.RowVersion);
        var result = await voucherService.UpdateVoucher(request);
        Assert.Equal(UpdateVoucherResult.DatabaseError, result);
    }
}