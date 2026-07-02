using CentralBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace CentralBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure index on OrderId and CustomerEmail
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderId);
                
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerEmail);
        }
    }
}
