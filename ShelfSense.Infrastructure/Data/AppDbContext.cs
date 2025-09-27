//using Microsoft.EntityFrameworkCore;
//using ShelfSense.Domain.Entities;

//namespace ShelfSense.Infrastructure.Data
//{
//    public class ShelfSenseDbContext : DbContext
//    {
//        public ShelfSenseDbContext(DbContextOptions<ShelfSenseDbContext> options) : base(options) { }



//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            modelBuilder.Entity<Category>(entity =>
//            {
//                entity.HasKey(e => e.CategoryId);
//                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
//                entity.HasIndex(e => e.CategoryName).IsUnique();
//                entity.Property(e => e.Description).HasMaxLength(255);
//                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
//            });

//            // Product constraints
//            modelBuilder.Entity<Product>(entity =>
//            {
//                entity.HasKey(e => e.ProductId);
//                entity.Property(e => e.StockKeepingUnit).IsRequired().HasMaxLength(50);
//                entity.HasIndex(e => e.StockKeepingUnit).IsUnique();
//                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
//                entity.Property(e => e.PackageSize).HasMaxLength(50);
//                entity.Property(e => e.Unit).HasMaxLength(20);
//                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
//                entity.HasOne(e => e.Category)
//                      .WithMany()
//                      .HasForeignKey(e => e.CategoryId)
//                      .OnDelete(DeleteBehavior.Cascade);
//            });

//            modelBuilder.Entity<Store>(entity =>
//            {
//                entity.HasKey(e => e.StoreId);
//                entity.Property(e => e.StoreName).IsRequired().HasMaxLength(100);
//                entity.HasIndex(e => e.StoreName).IsUnique();
//                entity.Property(e => e.Address).HasMaxLength(255);
//                entity.Property(e => e.City).HasMaxLength(50);
//                entity.Property(e => e.State).HasMaxLength(50);
//                entity.Property(e => e.PostalCode).HasMaxLength(10);
//                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
//            });

//            modelBuilder.Entity<Shelf>(entity =>
//            {
//                entity.HasKey(e => e.ShelfId);
//                entity.Property(e => e.ShelfCode).IsRequired().HasMaxLength(50);
//                entity.HasIndex(e => e.ShelfCode).IsUnique();
//                entity.Property(e => e.LocationDescription).HasMaxLength(100);
//                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
//                entity.HasOne(e => e.Store)
//                      .WithMany()
//                      .HasForeignKey(e => e.StoreId)
//                      .OnDelete(DeleteBehavior.Cascade);
//            });

//            modelBuilder.Entity<Shelf>(entity =>
//            {
//                entity.HasKey(e => e.ShelfId);
//                entity.Property(e => e.ShelfCode).IsRequired().HasMaxLength(50);
//                entity.HasIndex(e => e.ShelfCode).IsUnique();
//                entity.Property(e => e.LocationDescription).HasMaxLength(100);
//                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
//                entity.HasOne(e => e.Store)
//                      .WithMany()
//                      .HasForeignKey(e => e.StoreId)
//                      .OnDelete(DeleteBehavior.Cascade);
//            });

//            modelBuilder.Entity<ProductShelf>(entity =>
//            {
//                entity.HasKey(e => e.ProductShelfId);

//                entity.HasIndex(e => new { e.ProductId, e.ShelfId }).IsUnique();

//                entity.Property(e => e.Quantity).IsRequired();
//                entity.Property(e => e.LastRestockedAt).HasDefaultValueSql("GETDATE()");

//                entity.HasOne(e => e.Product)
//                      .WithMany()
//                      .HasForeignKey(e => e.ProductId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.HasOne(e => e.Shelf)
//                      .WithMany()
//                      .HasForeignKey(e => e.ShelfId)
//                      .OnDelete(DeleteBehavior.Cascade);
//            });


//            modelBuilder.Entity<ReplenishmentAlert>(entity =>
//            {
//                entity.HasKey(e => e.AlertId);

//                entity.Property(e => e.UrgencyLevel)
//                      .IsRequired()
//                      .HasMaxLength(20);

//                entity.Property(e => e.Status)
//                      .IsRequired()
//                      .HasMaxLength(20)
//                      .HasDefaultValue("open");

//                entity.Property(e => e.CreatedAt)
//                      .HasDefaultValueSql("GETDATE()");

//                entity.HasOne(e => e.Product)
//                      .WithMany()
//                      .HasForeignKey(e => e.ProductId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.HasOne(e => e.Shelf)
//                      .WithMany()
//                      .HasForeignKey(e => e.ShelfId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.ToTable("ReplenishmentAlert");
//            });

//            modelBuilder.Entity<Staff>(entity =>
//            {
//                entity.HasKey(e => e.StaffId);

//                entity.Property(e => e.Role)
//                      .IsRequired()
//                      .HasMaxLength(50)
//                      .HasDefaultValue("staff");

//                entity.Property(e => e.Email)
//                      .IsRequired()
//                      .HasMaxLength(150);

//                entity.HasIndex(e => e.Email)
//                      .IsUnique();

//                entity.Property(e => e.CreatedAt)
//                      .HasDefaultValueSql("GETDATE()");

//                entity.HasOne(e => e.Store)
//                      .WithMany()
//                      .HasForeignKey(e => e.StoreId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.ToTable("Staff");
//            });

//            modelBuilder.Entity<RestockTask>(entity =>
//            {
//                entity.HasKey(e => e.TaskId);

//                entity.Property(e => e.Status)
//                      .IsRequired()
//                      .HasMaxLength(20)
//                      .HasDefaultValue("pending");

//                entity.Property(e => e.AssignedAt)
//                      .HasDefaultValueSql("GETDATE()");

//                entity.HasOne(e => e.Alert)
//                      .WithMany()
//                      .HasForeignKey(e => e.AlertId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.HasOne(e => e.Product)
//                      .WithMany()
//                      .HasForeignKey(e => e.ProductId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.HasOne(e => e.Shelf)
//                      .WithMany()
//                      .HasForeignKey(e => e.ShelfId)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.HasOne(e => e.Staff)
//                      .WithMany()
//                      .HasForeignKey(e => e.AssignedTo)
//                      .OnDelete(DeleteBehavior.Cascade);

//                entity.ToTable("RestockTask");
//            });






//        }

//        public DbSet<Category> Categories { get; set; }
//        public DbSet<Product> Products { get; set; }

//        public DbSet<Store> Stores { get; set; }
//        public DbSet<Shelf> Shelves { get; set; }

//        public DbSet<ProductShelf> ProductShelves { get; set; }

//        public DbSet<ReplenishmentAlert> ReplenishmentAlerts { get; set; }
//        public DbSet<Staff> Staffs { get; set; }

//        public DbSet<RestockTask> RestockTasks { get; set; }


//        }
//}

using Microsoft.EntityFrameworkCore;
using ShelfSense.Domain.Entities;

namespace ShelfSense.Infrastructure.Data
{
    public class ShelfSenseDbContext : DbContext
    {
        public ShelfSenseDbContext(DbContextOptions<ShelfSenseDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Shelf> Shelves { get; set; }
        public DbSet<ProductShelf> ProductShelves { get; set; }
        public DbSet<ReplenishmentAlert> ReplenishmentAlerts { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<RestockTask> RestockTasks { get; set; }
        public DbSet<InventoryReport> InventoryReports { get; set; }
        public DbSet<StockRequest> StockRequests { get; set; }
        public DbSet<SalesHistory> SalesHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.CategoryName).IsUnique();
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.StockKeepingUnit).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.StockKeepingUnit).IsUnique();
                entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PackageSize).HasMaxLength(50);
                entity.Property(e => e.Unit).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Category)
                      .WithMany()
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Store
            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.StoreId);
                entity.Property(e => e.StoreName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.StoreName).IsUnique();
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.PostalCode).HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Shelf
            modelBuilder.Entity<Shelf>(entity =>
            {
                entity.HasKey(e => e.ShelfId);
                entity.Property(e => e.ShelfCode).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.ShelfCode).IsUnique();
                entity.Property(e => e.LocationDescription).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Store)
                      .WithMany()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                      .WithMany()
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ProductShelf
            modelBuilder.Entity<ProductShelf>(entity =>
            {
                entity.HasKey(e => e.ProductShelfId);
                entity.HasIndex(e => new { e.ProductId, e.ShelfId }).IsUnique();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.LastRestockedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Shelf)
                      .WithMany()
                      .HasForeignKey(e => e.ShelfId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ReplenishmentAlert
            modelBuilder.Entity<ReplenishmentAlert>(entity =>
            {
                entity.HasKey(e => e.AlertId);
                entity.Property(e => e.UrgencyLevel).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("open");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Shelf)
                      .WithMany()
                      .HasForeignKey(e => e.ShelfId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable("ReplenishmentAlert");
            });

            // Staff
            modelBuilder.Entity<Staff>(entity =>
            {
                entity.HasKey(e => e.StaffId);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50).HasDefaultValue("staff");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Store)
                      .WithMany()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable("Staff");
            });

            // RestockTask
            modelBuilder.Entity<RestockTask>(entity =>
            {
                entity.HasKey(e => e.TaskId);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Alert)
                      .WithMany()
                      .HasForeignKey(e => e.AlertId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent cascade conflict
                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Shelf)
                      .WithMany()
                      .HasForeignKey(e => e.ShelfId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Staff)
                      .WithMany()
                      .HasForeignKey(e => e.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.ToTable("RestockTask");
            });


            modelBuilder.Entity<InventoryReport>(entity =>
            {
                entity.HasKey(e => e.ReportId);

                entity.Property(e => e.ReportDate)
                      .IsRequired();

                entity.Property(e => e.QuantityOnShelf)
                      .IsRequired();

                entity.Property(e => e.AlertTriggered)
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => new { e.ProductId, e.ShelfId, e.ReportDate })
                      .IsUnique();

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Shelf)
                      .WithMany()
                      .HasForeignKey(e => e.ShelfId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("InventoryReport");
            });

            modelBuilder.Entity<StockRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);

                entity.Property(e => e.Quantity)
                      .IsRequired();

                entity.Property(e => e.DeliveryStatus)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("requested");

                entity.Property(e => e.RequestDate)
                      .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Store)
                      .WithMany()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("StockRequest");
            });


            modelBuilder.Entity<SalesHistory>(entity =>
            {
                entity.HasKey(e => e.SaleId);

                entity.Property(e => e.Quantity)
                      .IsRequired();

                entity.Property(e => e.SaleTime)
                      .IsRequired();

                entity.HasOne(e => e.Store)
                      .WithMany()
                      .HasForeignKey(e => e.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("SalesHistory");
            });



        }
    }
}