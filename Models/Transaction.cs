namespace TransactionsIngest.Models;

public class Transaction
{
    public int Id { get; set; }
    public string TransactionId { get; set; }
    public string CardNumberLast4 { get; set; }
    public string LocationCode { get; set; }
    public string ProductName { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
    public TransactionStatus Status { get; set; }
}