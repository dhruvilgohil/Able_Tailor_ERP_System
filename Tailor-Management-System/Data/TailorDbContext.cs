using Microsoft.EntityFrameworkCore;
using Tailor_Management_System.Models;

namespace Tailor_Management_System.Data
{
    public class TailorDbContext : DbContext
    {
        public TailorDbContext(DbContextOptions<TailorDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Tailor> Tailors { get; set; } = null!;
        public DbSet<Measurement> Measurements { get; set; } = null!;
        public DbSet<InventoryItem> Inventory { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<IncomeItem> Incomes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<Tailor>().Property(t => t.Salary).HasPrecision(18, 2);
            modelBuilder.Entity<Tailor>().Property(t => t.ContractRate).HasPrecision(18, 2);
            modelBuilder.Entity<InventoryItem>().Property(i => i.StockQty).HasPrecision(18, 2);
            modelBuilder.Entity<InventoryItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TailorContractPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.CalculatedTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.UserDefinedTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<IncomeItem>().Property(i => i.Amount).HasPrecision(18, 2);

            // Handle multiple cascade paths in SQL Server
            modelBuilder.Entity<Measurement>()
                .HasOne(m => m.Customer)
                .WithMany()
                .HasForeignKey(m => m.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Measurement)
                .WithMany()
                .HasForeignKey(o => o.MeasurementId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
