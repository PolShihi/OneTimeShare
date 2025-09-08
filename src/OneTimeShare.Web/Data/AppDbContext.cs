using Microsoft.EntityFrameworkCore;
using OneTimeShare.Web.Models;

namespace OneTimeShare.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<StoredFile> StoredFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserAccount configuration
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.LastLoginAtUtc).IsRequired();
        });

        // StoredFile configuration
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(255).IsRequired();
            entity.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OneTimeTokenHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TokenSalt).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ConcurrencyStamp).HasMaxLength(255).IsRequired();

            // Indexes
            entity.HasIndex(e => e.OneTimeTokenHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAtUtc);
            entity.HasIndex(e => e.DeletedAtUtc);
            entity.HasIndex(e => e.OwnerId);

            // Foreign key relationship
            entity.HasOne(e => e.Owner)
                  .WithMany(u => u.StoredFiles)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Concurrency token
            entity.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
        });
    }
}