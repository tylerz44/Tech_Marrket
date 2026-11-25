
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechMarket.Models;

// Alias tránh trùng tên namespace Store
using StoreEntity = TechMarket.Models.Store;

namespace TechMarket.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Quản lý cửa hàng
        public DbSet<StoreEntity> Stores { get; set; }

        // Yêu cầu đăng ký mở cửa hàng
        public DbSet<StoreRegistrationRequest>
            StoreRegistrationRequests
        { get; set; }

        // Quản lý danh mục
        public DbSet<Category> Categories { get; set; }

        // Quản lý sản phẩm
        public DbSet<Product> Products { get; set; }

        // Quản lý biến thể sản phẩm
        public DbSet<ProductVariant> ProductVariants { get; set; }

        // Quản lý giỏ hàng
        public DbSet<Cart> Carts { get; set; }

        public DbSet<CartItem> CartItems { get; set; }
        // Quản lý đơn hàng
        public DbSet<Order> Orders { get; set; }

        public DbSet<StoreOrder> StoreOrders { get; set; }
        // Chi tiết đơn hàng
        public DbSet<OrderDetail> OrderDetails { get; set; }
        // Quản lý địa chỉ nhận hàng
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }

        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentStatus)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentTransactionNo)
                .HasMaxLength(100);

            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentTransactionRef)
                .HasMaxLength(100);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.PaymentTransactionRef)
                .IsUnique()
                .HasFilter("[PaymentTransactionRef] IS NOT NULL");

            modelBuilder.Entity<DiscountCode>(entity =>
            {
                entity.ToTable("DiscountCodes");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.DiscountType).HasMaxLength(20).IsRequired();
                entity.Property(x => x.DiscountValue).HasPrecision(18, 2);
                entity.Property(x => x.MaxDiscountAmount).HasPrecision(18, 2);
                entity.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<UserCoupon>(entity =>
            {
                entity.ToTable("UserCoupons");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.UserId, x.DiscountCodeId }).IsUnique();
                entity.HasOne(x => x.DiscountCode).WithMany(x => x.UserCoupons)
                    .HasForeignKey(x => x.DiscountCodeId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>().Property(x => x.DiscountAmount).HasPrecision(18, 2);

            // =====================================
            // CẤU HÌNH SHIPPING ADDRESS
            // =====================================

            modelBuilder.Entity<ShippingAddress>(entity =>
            {
                entity.ToTable("ShippingAddresses");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.AddressName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.ReceiverName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Phone)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(x => x.Address)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.UserId);
            });

            modelBuilder.Entity<StoreOrder>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.ToTable("StoreOrder");
                entity.HasOne(x => x.Order)
                    .WithMany()
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Store)
                    .WithMany()
                    .HasForeignKey(x => x.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => new { x.OrderId, x.StoreId })
                    .IsUnique();
            });
            // =====================================
            // CẤU HÌNH CART ITEM
            // =====================================

            // Một giỏ hàng có nhiều CartItems
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Một sản phẩm có thể xuất hiện trong nhiều CartItems
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Một biến thể có thể xuất hiện trong nhiều giỏ hàng
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.ProductVariant)
                .WithMany()
                .HasForeignKey(ci => ci.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trong cùng một giỏ hàng,
            // một biến thể chỉ được xuất hiện một lần
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new
                {
                    ci.CartId,
                    ci.ProductVariantId
                })
                .IsUnique();

            // =====================================
            // CẤU HÌNH ORDER
            // =====================================

            // Một khách hàng có nhiều đơn hàng
            modelBuilder.Entity<Order>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Định dạng tiền
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            // =====================================
            // CẤU HÌNH ORDER DETAIL
            // =====================================

            // Một đơn hàng có nhiều chi tiết
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Liên kết sản phẩm
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Liên kết biến thể
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.ProductVariant)
                .WithMany()
                .HasForeignKey(od => od.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Liên kết cửa hàng
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Store)
                .WithMany()
                .HasForeignKey(od => od.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // Định dạng tiền
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.TotalPrice)
                .HasPrecision(18, 2);


        }
    }
}
