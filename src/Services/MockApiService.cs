using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TransactionsIngest.Models;

namespace TransactionsIngest.Services;

public class MockApiService
{
    private readonly IConfiguration _config;

    public MockApiService(IConfiguration config)
    {
        _config = config;
    }

    public List<TransactionDto> GetTransactions()
    {
        var filePath = _config["MockAPI:JSONPath"];
        
        if (string.IsNullOrEmpty(filePath))
            throw new Exception("MockAPI:JSONPath is missing in configuration");

        var json = File.ReadAllText(filePath);
        
        var transactions = JsonSerializer.Deserialize<List<TransactionDto>>(json);    
        
        return transactions ?? new List<TransactionDto>();
    }
}