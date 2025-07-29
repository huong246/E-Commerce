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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.Equal(CreateCustomerUpSellerResult.Success, result);
        var request = await dbContext.CustomerUpSellers.FirstOrDefaultAsync(c=>c.UserId == userId);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            UserRole = UserRole.Customer,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.Equal(CreateCustomerUpSellerResult.TokenInvalid, result);
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.Equal(CreateCustomerUpSellerResult.UserNotFound, result);
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.Equal(CreateCustomerUpSellerResult.NotPermitted, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            UserRole = UserRole.Customer,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.Equal(CreateCustomerUpSellerResult.RequestExists, result);
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
            Username = "123234567",
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            UserRole = UserRole.Customer,
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
        
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext.Object);
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        Assert.Equal(CreateCustomerUpSellerResult.DatabaseError, result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result =  await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.Equal(customerUpSeller, result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.Null(result);
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.Null(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        Assert.Null(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Admin,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.True(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Admin,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Seller,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Admin,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new ApproveRequest(Guid.NewGuid());
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Admin,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new ApproveRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Admin,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.True(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Admin,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Seller,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        var adminId = Guid.NewGuid();
        var admin = new User()
        {
            Id = adminId,
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Seller,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new RejectRequest(Guid.NewGuid());
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result);
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
            Username = "123234567",
            UserRole = UserRole.Customer,
            Password = BCrypt.Net.BCrypt.HashPassword("123456789"), 
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
            Username = "123234567890",
            Password = BCrypt.Net.BCrypt.HashPassword("1223435677878"),
            FullName = "John Doe1",
            PhoneNumber = "088888888811",
            UserRole = UserRole.Seller,
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
        var customerUpSellerService = new CustomerUpSellerService(
            mockHttpContextAccessor.Object, dbContext);
        var request = new RejectRequest(customerUpSeller.Id);
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        Assert.False(result);
    }
}