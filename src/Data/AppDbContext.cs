using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Models;

public class AppDbContext : DbContext 
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionAudit> Audits { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionId)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .Property(t => t.TransactionId)
            .HasMaxLength(20);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.LocationCode)
            .HasMaxLength(20);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.ProductName)
            .HasMaxLength(20);
    }
}