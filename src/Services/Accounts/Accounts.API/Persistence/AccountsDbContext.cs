using Accounts.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Accounts.API.Data;

public class AccountsDbContext : DbContext
{
    public AccountsDbContext(DbContextOptions<AccountsDbContext> options) :
        base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

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
            // одна учётная запись на пользователя в одной валюте
            entity.HasIndex(a => new { a.UserId, a.Currency }).IsUnique();
        });

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("ledger_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Direction).HasColumnName("direction");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after");
            entity.HasIndex(e => new { e.AccountId, e.Id }); // выписка по счёту
            entity.HasIndex(e => e.TransactionId);           // все записи одной проводки
        });
    }
}