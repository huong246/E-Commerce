using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class ShopServiceTest
{
    [Fact]
    public async Task CreateShop_WhenRequestValid_ReturnsSuccess()
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
            UserRole = UserRole.Seller,
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.Equal(CreateShopResult.Success, result);
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s=>s.UserId == userId);
        Assert.NotNull(shop);
        Assert.Equal("testShop", shop.Name);
        var address = dbContext.Addresses.Count();
        Assert.Equal(1, address);
    }
    
    [Fact]
    public async Task CreateShop_WhenTokenInvalid_ReturnsTokenInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.Equal(CreateShopResult.TokenInvalid, result);
    }
    
    [Fact]
    public async Task CreateShop_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.Equal(CreateShopResult.UserNotFound, result);
    }

    [Fact]
    public async Task CreateShop_WhenUserHasShop_ReturnsUserHasShop()
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            Address = address,
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.Equal(CreateShopResult.UserHasShop, result);
    }
    
    [Fact]
    public async Task CreateShop_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            UserRole = UserRole.Seller,
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };
        mockDbContext.Object.Users.Add(user);
        await mockDbContext.Object.SaveChangesAsync();
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        mockDbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var shopService = new ShopService( mockHttpContextAccessor.Object, mockDbContext.Object);
        var result =  await shopService.CreateShop(request);
        Assert.Equal(CreateShopResult.DatabaseError, result);
    }

    [Fact]
    public async Task UpdateShop_WhenRequestValid_ReturnsSuccess()
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            Address = address,
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "newName", address.Id, 10.0, 11.0, "AddressShop", 11);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.Success, result);
    }

    [Fact]
    public async Task UpdateShop_WhenTokenInvalid_ReturnsTokenInvalid()
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            Address = address,
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,"null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "newName", address.Id, 10.0, 11.0, "AddressShop", 11);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.TokenInvalid, result);
    }
    [Fact]
    public async Task UpdateShop_WhenUserNotFound_ReturnsShopNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(Guid.NewGuid(),"newName", Guid.NewGuid(), 10.0, 11.0, "AddressShop", 11);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.UserNotFound, result);
    }
    [Fact]
    public async Task UpdateShop_WhenShopNotFound_ReturnsShopNotFound()
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(Guid.NewGuid(), "newName", address.Id, 10.0, 11.0, "AddressShop", 11);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.ShopNotFound, result);
    }

    [Fact]
    public async Task UpdateShop_WhenAddressNotFound_ReturnsAddressNotFound()
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            PrepareTime = 10,
            User = user,
            UserId = userId,
            Address = null,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Users.AddAsync(user);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "newName", Guid.NewGuid(), 10.0, 11.0, "AddressShop", 11);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.AddressNotFound, result);
    }

    [Fact]
    public async Task UpdateShop_WhenDuplicateValue_ReturnsDuplicateValue()
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            Address = address,
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();
        
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "TestShop", address.Id, 10.0, 11.0, "AddressShop", 10);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext);
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.DuplicateValue, result);
    }

    [Fact]
    public async Task UpdateShop_WhenDatabaseError_ReturnsDatabaseError()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
        var mockDbContext = new Mock<ApiDbContext>(options) { CallBase = true };
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
            Name = "AddressShop",
            User = user,
            UserId = userId,
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            Address = address,
            PrepareTime = 10,
            User = user,
            UserId = userId,
        };
        mockDbContext.Object.Users.Add(user);
        mockDbContext.Object.Addresses.Add(address);
        mockDbContext.Object.Shops.Add(shop);
        await mockDbContext.Object.SaveChangesAsync();
        var request = new UpdateShopRequest(shop.Id, "TestShop111", address.Id, 10.0, 11.0, "AddressShop", 10);
        mockDbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var shopService = new ShopService( mockHttpContextAccessor.Object, mockDbContext.Object);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var result = await shopService.UpdateShop(request);
        Assert.Equal(UpdateShopResult.DatabaseError, result);
    }
}