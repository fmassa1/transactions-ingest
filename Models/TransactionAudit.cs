namespace TransactionsIngest.Models;

public class TransactionAudit 
{
    public int Id { get; set; }
    public string TransactionId { get; set; }
    public string ChangeType { get; set; }
    public string ChangedField { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public DateTime UpdatedAt { get; set; }
}