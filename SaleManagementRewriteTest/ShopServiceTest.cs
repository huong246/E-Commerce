using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.Results;
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
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            UserRole = UserRoles.Seller,
            PhoneNumber = "0888888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.True(result.IsSuccess);
        var shop = await dbContext.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        Assert.NotNull(shop);
        Assert.Equal("testShop", shop.Name);
        var addressInDb = await dbContext.Addresses.FindAsync(shop.AddressId);
        Assert.NotNull(addressInDb);
        Assert.Equal("AddressShop", addressInDb.Name);
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync("null")).ReturnsAsync((User)null!);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
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
            Name = "AddressShop",
            UserId = userId,
            User = user
        };
        var shop = new Shop()
        {
            Id = Guid.NewGuid(),
            Name = "TestShop",
            AddressId = address.Id,
            Address = address,
            PrepareTime = 10,
            UserId = userId,
            User = user,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Addresses.AddAsync(address);
        await dbContext.Shops.AddAsync(shop);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new CreateShopRequest("testShop", 10.0, 11.0, "AddressShop", 10);
        var result = await shopService.CreateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
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
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
             
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);
        var result = await shopService.CreateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
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
            UserName = "123234567",
            PasswordHash = "123456789",
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "newName", address.Id, 10.0, 11.0, "AddressShop", 11);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.True(result.IsSuccess);
        Assert.Equal("newName", shop.Name);
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
            UserName = "123234567",
            PasswordHash = "123456789",
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "newName", address.Id, 10.0, 11.0, "AddressShop", 11);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(Guid.NewGuid(), "newName", Guid.NewGuid(), 10.0, 11.0, "AddressShop", 11);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(Guid.NewGuid().ToString())).ReturnsAsync((User)null!);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
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
            UserName = "123234567",
            PasswordHash = "123456789",
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(Guid.NewGuid(), "newName", address.Id, 10.0, 11.0, "AddressShop", 11);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
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
            UserName = "123234567",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456789"),
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "newName", Guid.NewGuid(), 10.0, 11.0, "AddressShop", 11);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
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
            UserName = "123234567",
            PasswordHash = "123456789",
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

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new UpdateShopRequest(shop.Id, "TestShop", address.Id, 10.0, 11.0, "AddressShop", 10);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Seller)).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
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
            UserName = "123234567",
            PasswordHash = "123456789",
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
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        var shopService = new ShopService(mockHttpContextAccessor.Object, mockDbContext.Object, mockUserManager.Object);
        var result = await shopService.UpdateShop(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
