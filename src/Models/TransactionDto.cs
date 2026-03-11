using System;
using System.Text.Json.Serialization;

namespace TransactionsIngest.Models;

public class TransactionDto
{
    [JsonPropertyName("transactionId")]
    public required int TransactionId { get; set; }

    [JsonPropertyName("cardNumber")]
    public required string CardNumber { get; set; }

    [JsonPropertyName("locationCode")]
    public required string LocationCode { get; set; }

    [JsonPropertyName("productName")]
    public required string ProductName { get; set; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
