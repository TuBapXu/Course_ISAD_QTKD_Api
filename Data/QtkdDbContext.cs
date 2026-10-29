using Microsoft.EntityFrameworkCore;
using QTKD.Api.Entities;

namespace QTKD.Api.Data
{
    public class QtkdDbContext : DbContext
    {
        public QtkdDbContext(DbContextOptions<QtkdDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<PriceList> PriceLists { get; set; }
        public DbSet<PriceItem> PriceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Customer)
                .WithMany(cu => cu.Contracts)
                .HasForeignKey(c => c.CustomerId);

            modelBuilder.Entity<PriceItem>()
                .HasOne(pi => pi.PriceList)
                .WithMany(pl => pl.PriceItems)
                .HasForeignKey(pi => pi.PriceListId);

            // Dữ liệu mẫu
            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "Công ty Minh Phát", Status = "Active" },
                new Customer { Id = 2, Name = "Công ty ABC", Status = "Active" }
            );

            // SỬA Ở ĐÂY: Thêm 0, 0, 0, DateTimeKind.Utc
            modelBuilder.Entity<Contract>().HasData(
                new Contract { 
                    Id = 1, 
                    ContractNo = "HD-2026-018", 
                    CustomerId = 1, 
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 
                    EndDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), 
                    Status = "Draft" 
                }
            );

            // SỬA Ở ĐÂY: Thêm 0, 0, 0, DateTimeKind.Utc
            modelBuilder.Entity<PriceList>().HasData(
                new PriceList { 
                    Id = 1, 
                    Name = "Bảng giá 2026", 
                    EffectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) 
                }
            );

            modelBuilder.Entity<PriceItem>().HasData(
                new PriceItem { Id = 1, PriceListId = 1, ServiceCode = "SV01", UnitPrice = 100000 },
                new PriceItem { Id = 2, PriceListId = 1, ServiceCode = "SV02", UnitPrice = 200000 }
            );
        }
    }
}