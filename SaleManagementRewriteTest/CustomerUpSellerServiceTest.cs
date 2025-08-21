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

public class CustomerUpSellerServiceTest
{
    [Fact]
    public async Task CreateCustomerUpSeller_WhenUserExist_ReturnsSuccess()
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
            UserRole = UserRoles.Customer,
            PasswordHash = "123456789",
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.True(result.IsSuccess);
        var request = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(c => c.UserId == userId);
        Assert.NotNull(request);
    }

    [Fact]
    public async Task CreateCustomerUpSeller_WhenTokenInvalid_ReturnsTokenInvalid()
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
            UserRole = UserRoles.Customer,
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
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
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CreateCustomerUpSeller_WhenUserNotFound_ReturnsUserNotFound()
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateCustomerUpSeller_WhenNotPermitted_ReturnsNotPermitted()
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
            UserRole = UserRoles.Seller,
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(false);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateCustomerUpSeller_WhenRequestExist_ReturnsRequestExists()
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
            UserRole = UserRoles.Customer,
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var request = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.CustomerUpSellers.AddAsync(request);
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
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreateCustomerUpSeller_WhenDatabaseError_ReturnsDatabaseError()
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
            UserRole = UserRoles.Customer,
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        await dbContext.Object.Database.EnsureCreatedAsync();
        dbContext.Object.Users.Add(user);
        await dbContext.Object.SaveChangesAsync();

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        dbContext.Setup(db => db.Users).ReturnsDbSet(new List<User> { user });
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));

        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext.Object, mockUserManager.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task GetCustomerUpSeller_WhenCustomerUpSellerExist_ReturnsCustomerUpSeller()
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
            UserRole = UserRoles.Customer,
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
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
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetCustomerUpSeller_WhenTokenInvalid_ReturnTokenInvalid()
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
            UserRole = UserRoles.Customer,
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
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
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task GetCustomerUpSeller_WhenUserNotFoundReturnsUserNotFound()
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetCustomerUpSeller_WhenCustomerUpSellerNotFound_ReturnsNull()
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
            UserRole = UserRoles.Customer,
            PasswordHash = "123456789",
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
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Customer)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Customer)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ApproveCustomerUpSeller_WhenRequestValid_ReturnsTrue()
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
            UserRole = UserRoles.Customer,
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRoles.Admin,
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddRangeAsync(user,admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ApproveCustomerUpSeller_WhenTokenInvalid_ReturnsFalse()
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
            UserRole = UserRoles.Customer,
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRoles.Admin,
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task ApproveCustomerUpSeller_WhenUserNotFound_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ApproveCustomerUpSeller_WhenUserNotPermitted_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(false);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ApproveCustomerUpSeller_WhenCustomerUpSellerNotFound_ReturnsFalse()
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
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
             
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new ApproveRequest(Guid.NewGuid());
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ApproveCustomerUpSeller_WhenCustomerUpSellerNotPending_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Approved,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
             
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task RejectCustomerUpSeller_WhenRequestValid_ReturnsTrue()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
             
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RejectCustomerUpSeller_WhenTokenInvalid_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
           
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RejectCustomerUpSeller_WhenUserNotFound_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task RejectCustomerUpSeller_WhenUserNotPermitted_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Pending,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(false);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task RejectCustomerUpSeller_WhenCustomerUpSellerNotFound_ReturnsFalse()
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
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new RejectRequest(Guid.NewGuid());
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task RejectCustomerUpSeller_WhenCustomerUpSellerNotPending_ReturnsFalse()
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
        var customerUpSeller = new CustomerUpSeller()
        {
            Id = Guid.NewGuid(),
            RequestAt = DateTime.UtcNow,
            UserId = userId,
            User = user,
            Status = RequestStatus.Rejected,
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            UserName = "123234567890",
            PasswordHash = "1223435677878",
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            
            Balance = 0,
            Birthday = new DateTime(2000, 1, 1),
        };
        await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Users.AddAsync(user);
        await dbContext.Users.AddAsync(admin);
        await dbContext.CustomerUpSellers.AddAsync(customerUpSeller);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);
        mockUserManager.Setup(x => x.IsInRoleAsync(admin, UserRoles.Admin)).ReturnsAsync(true);
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext, mockUserManager.Object);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
