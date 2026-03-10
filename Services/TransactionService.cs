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
                HandleUpdateTransaction(existingTrans, trans, audits, curTime);
            }
            else 
            {
                CreateNewTransaction(trans, audits, curTime);
            }
        }

        await HandleRevokedTransactions(transactionIds, audits, curTime);
        await HandleFinalizedTransactions(audits, curTime);
        
        if (audits.Count > 0)
            await _db.Audits.AddRangeAsync(audits);

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }

    private void CreateNewTransaction(TransactionDto trans, List<TransactionAudit> audits, DateTime curTime)
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

        audits.Add(CreateAudit(
            trans.TransactionId, 
            "Created", 
            "All",
            null, 
            $"Amount={trans.Amount}, Location={trans.LocationCode}, Product={trans.ProductName}", 
            curTime
        ));
    }
    
    private void HandleUpdateTransaction(Transaction existing, TransactionDto updated, List<TransactionAudit> audits, DateTime curTime)
    {
        bool changed = false;
        if (existing.Amount != updated.Amount)
        {
            audits.Add(CreateAudit(
                existing.TransactionId,
                "Updated",
                "Amount",
                existing.Amount.ToString(),
                updated.Amount.ToString(),
                curTime
            ));

            existing.Amount = updated.Amount;
            changed = true;
        }
        if (existing.LocationCode != updated.LocationCode)
        {
            audits.Add(CreateAudit(
                existing.TransactionId,
                "Updated",
                "LocationCode",
                existing.LocationCode,
                updated.LocationCode,
                curTime
            ));
            
            existing.LocationCode = updated.LocationCode;
            changed = true;
        }

        if (existing.ProductName != updated.ProductName)
        {
            audits.Add(CreateAudit(
                existing.TransactionId,
                "Updated",
                "ProductName",
                existing.ProductName,
                updated.ProductName,
                curTime
            ));

            existing.ProductName = updated.ProductName;
            changed = true;
        }

        if (changed)
            existing.LastUpdated = curTime;
    }
    
    private async Task HandleRevokedTransactions(List<string> transactionIds, List<TransactionAudit> audits, DateTime curTime)
    {
        var cutOffTime = curTime.AddHours(-24);

        var recentTransactions = await _db.Transactions
            .Where(t => t.Timestamp >= cutOffTime && t.Status == TransactionStatus.Active)
            .ToListAsync();
        
        foreach (var trans in recentTransactions)
        {
            if (!transactionIds.Contains(trans.TransactionId))
            {
                audits.Add(CreateAudit(
                    trans.TransactionId,
                    "StatusChange",
                    "Status",
                    TransactionStatus.Active.ToString(),
                    TransactionStatus.Revoked.ToString(),
                    curTime
                ));
    
                trans.Status = TransactionStatus.Revoked;
                trans.LastUpdated = curTime;
            }
        }
    }

    private async Task HandleFinalizedTransactions(List<TransactionAudit> audits, DateTime curTime)
    {
        var cutOffTime = curTime.AddHours(-24);

        var toFinalizeTransactions = await _db.Transactions
            .Where(t => t.Timestamp < cutOffTime && t.Status == TransactionStatus.Active)
            .ToListAsync();

        foreach (var trans in toFinalizeTransactions) 
        {
            audits.Add(CreateAudit(
                trans.TransactionId,
                "StatusChange",
                "Status",
                TransactionStatus.Active.ToString(),
                TransactionStatus.Finalized.ToString(),
                curTime
            ));

            trans.Status = TransactionStatus.Finalized;
            trans.LastUpdated = curTime;
        }
    }

    private TransactionAudit CreateAudit(
        string transactionId,
        string changeType,
        string field,
        string? oldValue,
        string? newValue,
        DateTime timestamp)
    {
        return new TransactionAudit
        {
            TransactionId = transactionId,
            ChangeType = changeType,
            ChangedField = field,
            OldValue = oldValue,
            NewValue = newValue,
            UpdatedAt = timestamp
        };
    }
}