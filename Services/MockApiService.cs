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
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<TransactionDto>>(json);
    }
}