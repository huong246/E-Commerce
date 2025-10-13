using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaleManagement.Services;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Services;

var builder = WebApplication.CreateBuilder(args); //trình xây dựng ứng dụng web
var configuration = builder.Configuration; //đối tượng cấu hinh cho phep truy cap vao trong file
//tao duong noi den co so du lieu
//var dbPath = Path.Combine(builder.Environment.ContentRootPath, "SaleManagementRewrite.db");
// Lấy đường dẫn đến thư mục gốc của dự án backend
var projectRootPath = builder.Environment.ContentRootPath;

// Nối đường dẫn đó với tên file CSDL để tạo ra một đường dẫn đầy đủ và duy nhất
var dbPath = System.IO.Path.Combine(projectRootPath, "SaleManagementRewrite.db");

// Tạo ra chuỗi kết nối đầy đủ, ví dụ: "DataSource=C:\Users\...\SaleManagementRewrite\SaleManagementRewrite.db"
var connectionString = $"DataSource={dbPath}";

// Đăng ký ApiDbContext với chuỗi kết nối đầy đủ
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") 
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});
//tao nut authorization tren moi header cua request
builder.Services.AddSwaggerGen(options =>
{
    //dinh nghia cau hinh cua nut authorization
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    //dinh nghia chi can xac thuc token 1 lan o bat ky request nao thi tat ca cac request deu duoc xac thuc
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            [] //o vua tao la o trong
        }
    });
});

//Phan xac thuc nguoi dung bang bearer Token tuong ung
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = configuration["JWT:ValidAudience"],
        ValidIssuer = configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty)),
        RoleClaimType = ClaimTypes.Role,
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // 1️⃣ Ưu tiên header Authorization
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
            }
            else
            {
                // 2️⃣ Nếu không có, thử lấy từ cookie
                var cookie = context.Request.Cookies["jwt"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    context.Token = cookie;
                }
            }

            return Task.CompletedTask;
        }
    };

});
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); 
        options.Lockout.MaxFailedAccessAttempts = 5; 
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApiDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IItemImageService, ItemImageService>();
builder.Services.AddScoped<ICartItemService, CartItemService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerUpSellerService, CustomerUpSellerService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddHostedService<CompleteOrderService>();
builder.Services.AddHostedService<VoucherUpdateStatusService>();
var app = builder.Build();

// ham tu dong cap nhat migration de khong can chay lenh dotnet ef database update thu cong
//nghia la khi them moi vao entity thi van can chay ham dotnet ef migrations add... truoc

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApiDbContext>();
        await dbContext.Database.MigrateAsync();
       
    }
    catch (Exception ex) 
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogError(ex, "An error occurred during database initialization.");
    }
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles(); 
app.UseStaticFiles(); //dung cho itemImage
app.UseCors(myAllowSpecificOrigins);
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization(); //Phan quyen
app.MapControllers(); // dua yeu cau toi controller tuong ung
app.MapHub<SaleManagementRewrite.Hubs.NotificationHubs>("/NotificationHubs");
app.MapHub<SaleManagementRewrite.Hubs.ChatHubs>("/ChatHubs");
app.Run();