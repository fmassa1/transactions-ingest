namespace TransactionsIngest.Models;

public class Transaction
{
    public int Id { get; set; }
    public required int TransactionId { get; set; }
    public required string CardNumberLast4 { get; set; }
    public required string LocationCode { get; set; }
    public required string ProductName { get; set; }
    public required decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }

}