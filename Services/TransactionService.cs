using Microsoft.EntityFrameworkCore;
using TransactionsIngest.Models;

namespace TransactionsIngest.Services;

public class TransactionService
{
    private readonly AppDbContext _db;

    public TransactionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task ProcessTransactions(List<TransactionDto> transactions)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();

        var curTime = DateTime.UtcNow;

        // Loads existing transactions to compare to new transactions
        var transactionIds = transactions.Select(t => t.TransactionId).ToList();
        var existing = await _db.Transactions
            .Where(t => transactionIds.Contains(t.TransactionId))
            .ToDictionaryAsync(t => t.TransactionId);

        foreach (var trans in transactions)
        {
            // If it exists checks if it has been updated
            if(existing.TryGetValue(trans.TransactionId, out var existingTrans))
            {

            }
            // Adds new transactions
            else 
            {
                _db.Transactions.Add(new Transaction
                {
                    TransactionId = trans.TransactionId,
                    CardNumber = trans.CardNumber,
                    LocationCode = trans.LocationCode,
                    ProductName = trans.ProductName,
                    Amount = trans.Amount,
                    Timestamp = trans.Timestamp,
                    Status = TransactionStatus.Active,
                    LastUpdated = curTime
                });
            }
        }
        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }
}