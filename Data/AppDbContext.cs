using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Models;

public class AppDbContext : DbContext 
{
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionAudit> Audits { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) {}
}