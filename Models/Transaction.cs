namespace TransactionsIngest.Models;

public class Transaction
{
    public int Id { get; set; }
    public string TransactionId { get; set; }
    public string CardNumber { get; set; }
    public string LocationCode { get; set; }
    public string ProductName { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }

}