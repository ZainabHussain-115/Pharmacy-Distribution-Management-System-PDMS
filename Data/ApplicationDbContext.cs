using Microsoft.EntityFrameworkCore;
using PDMS.Models;

namespace PDMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- DbSets (one per table) ---
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Medicine>     Medicines     { get; set; }
        public DbSet<Warehouse>    Warehouses    { get; set; }
        public DbSet<Batch>        Batches       { get; set; }
        public DbSet<Customer>     Customers     { get; set; }
        public DbSet<User>         Users         { get; set; }
        public DbSet<Sale>         Sales         { get; set; }
        public DbSet<SaleDetail>   SaleDetails   { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Manufacturer ─────────────────────────────────────────────
            modelBuilder.Entity<Manufacturer>()
                .HasIndex(m => m.LicenseNumber)
                .IsUnique();

            // ── Medicine → Manufacturer ───────────────────────────────────
            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Manufacturer)
                .WithMany(mf => mf.Medicines)
                .HasForeignKey(m => m.ManufacturerID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Batch → Medicine ──────────────────────────────────────────
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(b => b.MedicineID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Batch → Warehouse ─────────────────────────────────────────
            modelBuilder.Entity<Batch>()
                .HasOne(b => b.Warehouse)
                .WithMany(w => w.Batches)
                .HasForeignKey(b => b.WarehouseID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Customer ──────────────────────────────────────────────────
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.LicenseNumber)
                .IsUnique();

            // ── User ──────────────────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // ── Sale → Customer ───────────────────────────────────────────
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Sale → User ───────────────────────────────────────────────
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ── SaleDetail → Sale ─────────────────────────────────────────
            modelBuilder.Entity<SaleDetail>()
                .HasOne(sd => sd.Sale)
                .WithMany(s => s.SaleDetails)
                .HasForeignKey(sd => sd.SaleID)
                .OnDelete(DeleteBehavior.Cascade);

            // ── SaleDetail → Batch ────────────────────────────────────────
            modelBuilder.Entity<SaleDetail>()
                .HasOne(sd => sd.Batch)
                .WithMany(b => b.SaleDetails)
                .HasForeignKey(sd => sd.BatchID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Unique: one batch per sale ────────────────────────────────
            modelBuilder.Entity<SaleDetail>()
                .HasIndex(sd => new { sd.SaleID, sd.BatchID })
                .IsUnique();
        }
    }
}
