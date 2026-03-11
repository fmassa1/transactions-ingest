using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TransactionsIngest.Models;
using TransactionsIngest.Services; 
using Xunit; 

public class TransactionServiceTests 
{ 
    private AppDbContext CreateDb() 
    { 
        var options = new DbContextOptionsBuilder<AppDbContext>() 
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
            return new AppDbContext(options);
    } 
    
    [Fact] 
    public async Task Should_Insert_New_Transaction() 
    { 
        var db = CreateDb(); 
        var service = new TransactionService(db);
        var transactions = new List<TransactionDto> { 
            new TransactionDto 
            { 
                TransactionId = 1, 
                CardNumber = "1234567812345678", 
                LocationCode = "LOC1", 
                ProductName = "Keyboard", 
                Amount = 100, 
                Timestamp = DateTime.UtcNow 
            } 
        }; 
        await service.ProcessTransactions(transactions);

        var result = await db.Transactions.FirstOrDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.TransactionId);
        Assert.Equal("5678", result.CardNumberLast4); 
    }
    [Fact]
    public async Task Should_Update_Transaction_When_Fields_Change()
    {
        var db = CreateDb();

        db.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            CardNumberLast4 = "5678",
            LocationCode = "LOC1",
            ProductName = "Keyboard",
            Amount = 100,
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.Active,
            LastUpdated = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        var transactions = new List<TransactionDto>
        {
            new TransactionDto
            {
                TransactionId = 1,
                CardNumber = "1234567812345678",
                LocationCode = "LOC2",
                ProductName = "Keyboard",
                Amount = 150,
                Timestamp = DateTime.UtcNow
            }
        };

        await service.ProcessTransactions(transactions);

        var updated = await db.Transactions.FirstAsync();

        Assert.Equal(150, updated.Amount);
        Assert.Equal("LOC2", updated.LocationCode);

        var audits = await db.Audits.ToListAsync();
        Assert.Equal(2, audits.Count);
    }

    [Fact]
    public async Task Should_Not_Update_When_No_Fields_Change()
    {
        var db = CreateDb();

        db.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            CardNumberLast4 = "5678",
            LocationCode = "LOC1",
            ProductName = "Keyboard",
            Amount = 100,
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.Active,
            LastUpdated = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        var transactions = new List<TransactionDto>
        {
            new TransactionDto
            {
                TransactionId = 1,
                CardNumber = "1234567812345678",
                LocationCode = "LOC1",
                ProductName = "Keyboard",
                Amount = 100,
                Timestamp = DateTime.UtcNow
            }
        };

        await service.ProcessTransactions(transactions);

        var audits = await db.Audits.ToListAsync();

        Assert.Empty(audits);
    }

    [Fact]
    public async Task Should_Revoke_Missing_Transactions()
    {
        var db = CreateDb();

        db.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            CardNumberLast4 = "5678",
            LocationCode = "LOC1",
            ProductName = "Keyboard",
            Amount = 100,
            Timestamp = DateTime.UtcNow,
            Status = TransactionStatus.Active,
            LastUpdated = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        await service.ProcessTransactions(new List<TransactionDto>());

        var trans = await db.Transactions.FirstAsync();

        Assert.Equal(TransactionStatus.Revoked, trans.Status);
    }

    [Fact]
    public async Task Should_Finalize_Old_Transactions()
    {
        var db = CreateDb();

        db.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            CardNumberLast4 = "5678",
            LocationCode = "LOC1",
            ProductName = "Keyboard",
            Amount = 100,
            Timestamp = DateTime.UtcNow.AddDays(-2),
            Status = TransactionStatus.Active,
            LastUpdated = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        await service.ProcessTransactions(new List<TransactionDto>());

        var trans = await db.Transactions.FirstAsync();

        Assert.Equal(TransactionStatus.Finalized, trans.Status);
    }

    [Fact]
    public async Task Should_Not_Update_Finalized_Transaction()
    {
        var db = CreateDb();

        db.Transactions.Add(new Transaction
        {
            TransactionId = 1,
            CardNumberLast4 = "5678",
            LocationCode = "LOC1",
            ProductName = "Keyboard",
            Amount = 100,
            Timestamp = DateTime.UtcNow.AddDays(-2),
            Status = TransactionStatus.Finalized,
            LastUpdated = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var service = new TransactionService(db);

        var input = new List<TransactionDto>
        {
            new TransactionDto
            {
                TransactionId = 1,
                CardNumber = "1234567812345678",
                LocationCode = "LOC2",
                ProductName = "Keyboard",
                Amount = 200,
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        };

        await service.ProcessTransactions(input);

        var trans = await db.Transactions.FirstAsync();

        Assert.Equal(100, trans.Amount);
        Assert.Equal("LOC1", trans.LocationCode);
    }

    [Fact]
    public async Task Should_Be_Idempotent_When_Input_Is_Same()
    {
        var db = CreateDb();
        var service = new TransactionService(db);

        var transactions = new List<TransactionDto>
        {
            new TransactionDto
            {
                TransactionId = 1,
                CardNumber = "1234567812345678",
                LocationCode = "LOC1",
                ProductName = "Keyboard",
                Amount = 100,
                Timestamp = DateTime.UtcNow
            }
        };

        await service.ProcessTransactions(transactions);
        await service.ProcessTransactions(transactions);

        var count = await db.Transactions.CountAsync();
        var audits = await db.Audits.CountAsync();

        Assert.Equal(1, count);
        Assert.Equal(1, audits);
    }
}