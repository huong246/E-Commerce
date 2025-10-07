using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Entities;

namespace SaleManagementRewrite.Data;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : IdentityDbContext<User, IdentityRole<Guid>,  Guid>(options)
{
    public virtual DbSet<Address> Addresses { get; set; }
    public virtual DbSet<Shop> Shops { get; set; }
    public virtual DbSet<Item> Items { get; set; }
    public virtual DbSet<ItemImage> ItemImages { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<Conversation> Conversations { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderShop> OrderShops { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<OrderHistory> OrderHistories { get; set; }
    public virtual DbSet<ReturnOrder> ReturnOrders { get; set; }
    public virtual DbSet<ReturnOrderItem> ReturnOrderItems { get; set; }
    public virtual DbSet<CustomerUpSeller> CustomerUpSellers{ get; set; }
    public virtual DbSet<Voucher> Vouchers { get; set; }
    public virtual DbSet<CartItem> CartItems { get; set; }
    public virtual DbSet<Transaction> Transactions { get; set; }
    public virtual DbSet<CancelRequest> CancelRequests { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<ItemFts>  ItemFts { get; set; }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        
        configurationBuilder.Properties<decimal>()
            .HaveColumnType("NUMERIC");

        base.ConfigureConventions(configurationBuilder);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.Entity<CustomerUpSeller>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); 
        });
        modelBuilder.Entity<Item>()
            .Property(p => p.Version)
            .IsConcurrencyToken();
        base.OnModelCreating(modelBuilder);
    }
}