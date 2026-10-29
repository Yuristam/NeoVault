using Accounts.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Accounts.API.Persistence;

public class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options) :
        base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Operation> Operations => Set<Operation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.UserId).HasColumnName("user_id");
            entity.Property(a => a.Currency).HasColumnName("currency").HasMaxLength(3);
            entity.Property(a => a.Balance).HasColumnName("balance");
            entity.Property(a => a.Hold).HasColumnName("hold");
            entity.Property(a => a.CreatedAt).HasColumnName("created_at");
            // одна учётная запись на пользователя в одной валюте
            entity.HasIndex(a => new { a.UserId, a.Currency }).IsUnique();
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.ToTable("operations");
            entity.HasKey(e => e.Id).HasName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
