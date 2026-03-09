using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using TransactionsIngest.Services;
using TransactionsIngest.Models;


var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json");

var config = builder.Build();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(config.GetConnectionString("DefaultConnection"))
    .Options;

using var db = new AppDbContext(options);
db.Database.EnsureCreated();


var mockApi = new MockApiService(config);
var transactions = mockApi.GetTransactions();

Console.WriteLine("Loaded transactions:");
foreach (var t in transactions)
{
    Console.WriteLine($"{t.TransactionId} | {t.ProductName} | {t.Amount:C} | {t.Timestamp}");
}

Console.WriteLine("Done");
