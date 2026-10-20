using Microsoft.EntityFrameworkCore;
using Transfers.API.Domain;

namespace Transfers.API.Persistence
{
    public class TransfersDbContext : DbContext
    {
        public TransfersDbContext(DbContextOptions<TransfersDbContext> options) 
            : base(options) { }

        public DbSet<Transfer> Transfers => Set<Transfer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transfer>(entity =>
            {
                entity.ToTable("transfers");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.FromAccountId).HasColumnName("from_account_id");
                entity.Property(t => t.ToAccountId).HasColumnName("to_account_id");
                entity.Property(t => t.Amount).HasColumnName("amount");
                entity.Property(t => t.Status).HasColumnName("status").HasConversion<string>();
                entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
