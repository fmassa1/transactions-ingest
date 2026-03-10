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
        var audits = new List<TransactionAudit>();


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
                if (existingTrans.Amount != trans.Amount)
                {
                    audits.Add(new TransactionAudit
                    {
                        TransactionId = trans.TransactionId,
                        ChangeType = "Updated",
                        ChangedField = "Amount",
                        OldValue = existingTrans.Amount.ToString(),
                        NewValue = trans.Amount.ToString(),
                        UpdatedAt = curTime
                    });
                    existingTrans.Amount = trans.Amount;
                }
                if (existingTrans.LocationCode != trans.LocationCode)
                {
                    audits.Add(new TransactionAudit
                    {
                        TransactionId = trans.TransactionId,
                        ChangeType = "Updated",
                        ChangedField = "LocationCode",
                        OldValue = existingTrans.LocationCode,
                        NewValue = trans.LocationCode,
                        UpdatedAt = curTime
                    });
                    existingTrans.LocationCode = trans.LocationCode;
                }
                if (existingTrans.ProductName != trans.ProductName)
                {
                    audits.Add(new TransactionAudit
                    {
                        TransactionId = trans.TransactionId,
                        ChangeType = "Updated",
                        ChangedField = "ProductName",
                        OldValue = existingTrans.ProductName,
                        NewValue = trans.ProductName,
                        UpdatedAt = curTime
                    });
                    existingTrans.ProductName = trans.ProductName;
                }

                if (audits.Any(a => a.TransactionId == trans.TransactionId))
                    existingTrans.LastUpdated = curTime;
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

                audits.Add(new TransactionAudit
                {
                    TransactionId = trans.TransactionId,
                    ChangeType = "Created",
                    ChangedField = "All",
                    OldValue = null,
                    NewValue = $"Amount={trans.Amount}, Location={trans.LocationCode}, Product={trans.ProductName}",
                    UpdatedAt = curTime
                });
            }
        }

        var twentyFourHoursAgo = curTime.AddHours(-24);

        // Revokes any transactions missing within the last 24 hours
        var recentTransactions = await _db.Transactions
            .Where(t => t.Timestamp >= twentyFourHoursAgo && t.Status == TransactionStatus.Active)
            .ToListAsync();
        
        foreach (var trans in recentTransactions)
        {
            if (!transactionIds.Contains(trans.TransactionId))
            {
                audits.Add(new TransactionAudit
                {
                    TransactionId = trans.TransactionId,
                    ChangeType = "StatusChange",
                    ChangedField = "Status",
                    OldValue = TransactionStatus.Active.ToString(),
                    NewValue = TransactionStatus.Revoked.ToString(),
                    UpdatedAt = curTime
                });
    
                trans.Status = TransactionStatus.Revoked;
                trans.LastUpdated = curTime;
            }
        }

        // Finalizes active transactions that are over 24 hours old
        var toFinalizeTransactions = await _db.Transactions
            .Where(t => t.Timestamp < twentyFourHoursAgo && t.Status == TransactionStatus.Active)
            .ToListAsync();

        foreach (var trans in toFinalizeTransactions) 
        {
            audits.Add(new TransactionAudit
            {
                TransactionId = trans.TransactionId,
                ChangeType = "StatusChange",
                ChangedField = "Status",
                OldValue = TransactionStatus.Active.ToString(),
                NewValue = TransactionStatus.Finalized.ToString(),
                UpdatedAt = curTime
            });

            trans.Status = TransactionStatus.Finalized;
            trans.LastUpdated = curTime;
        }
        if (audits.Count > 0)
            await _db.Audits.AddRangeAsync(audits);

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }
}