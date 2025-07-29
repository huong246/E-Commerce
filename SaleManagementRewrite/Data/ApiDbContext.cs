using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Entities;

namespace SaleManagementRewrite.Data;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public virtual DbSet<User> Users { get; set; }
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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Voucher>()
            .Property(v => v.RowVersion)
            .IsRowVersion(); 
        modelBuilder.Entity<Item>()
            .Property(i => i.RowVersion)
            .IsRowVersion();
    }
}