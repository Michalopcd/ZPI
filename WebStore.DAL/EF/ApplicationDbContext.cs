using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebStore.Model.DataModels;

namespace WebStore.DAL.EF
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Customer> Customers { get; set; } = default!;
        public virtual DbSet<Supplier> Suppliers { get; set; } = default!;
        public virtual DbSet<StationaryStoreEmployee> StationaryStoreEmployees { get; set; } = default!;
        public virtual DbSet<Product> Products { get; set; } = default!;
        public virtual DbSet<ProductStock> ProductStocks { get; set; } = default!;
        public virtual DbSet<Category> Categories { get; set; } = default!;
        public virtual DbSet<Order> Orders { get; set; } = default!;
        public virtual DbSet<OrderProduct> OrderProducts { get; set; } = default!;
        public virtual DbSet<Invoice> Invoices { get; set; } = default!;
        public virtual DbSet<Address> Addresses { get; set; } = default!;
        public virtual DbSet<StationaryStore> StationaryStores { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<User>(nameof(User))
                .HasValue<Customer>(nameof(Customer))
                .HasValue<Supplier>(nameof(Supplier))
                .HasValue<StationaryStoreEmployee>(nameof(StationaryStoreEmployee));

            modelBuilder.Entity<Customer>(customer =>
            {
                customer.HasMany(c => c.Orders)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                customer.HasMany(c => c.Addresses)
                    .WithOne(a => a.Customer)
                    .HasForeignKey(a => a.CustomerId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                customer.HasOne(c => c.BillingAddress)
                    .WithMany()
                    .HasForeignKey(c => c.BillingAddressId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                customer.HasOne(c => c.ShippingAddress)
                    .WithMany()
                    .HasForeignKey(c => c.ShippingAddressId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Products)
                .WithOne(p => p.Supplier)
                .HasForeignKey(p => p.SupplierId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StationaryStore>(store =>
            {
                store.HasMany(s => s.Addresses)
                    .WithOne(a => a.StationaryStore)
                    .HasForeignKey(a => a.StationaryStoreId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                store.HasMany(s => s.Employees)
                    .WithOne(e => e.StationaryStore)
                    .HasForeignKey(e => e.StationaryStoreId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>(product =>
            {
                product.Property(p => p.Price).HasPrecision(18, 2);

                product.HasMany(p => p.ProductStocks)
                    .WithOne(ps => ps.Product)
                    .HasForeignKey(ps => ps.ProductId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Invoice>(invoice =>
            {
                invoice.Property(i => i.TotalAmount).HasPrecision(18, 2);

                invoice.HasMany(i => i.Orders)
                    .WithOne(o => o.Invoice)
                    .HasForeignKey(o => o.InvoiceId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Order>()
                .Ignore(o => o.TotalAmount);

            modelBuilder.Entity<OrderProduct>(orderProduct =>
            {
                orderProduct.HasKey(op => new { op.OrderId, op.ProductId });

                orderProduct.HasOne(op => op.Order)
                    .WithMany(o => o.OrderProducts)
                    .HasForeignKey(op => op.OrderId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                orderProduct.HasOne(op => op.Product)
                    .WithMany(p => p.OrderProducts)
                    .HasForeignKey(op => op.ProductId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
