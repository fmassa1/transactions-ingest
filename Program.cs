using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TransactionsIngest.Models;


var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json");

var config = builder.Build();

Console.WriteLine("Connection String:");
Console.WriteLine(config.GetConnectionString("DefaultConnection"));

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(config.GetConnectionString("DefaultConnection"))
    .Options;

using var db = new AppDbContext(options);
db.Database.EnsureCreated();

Console.WriteLine("Done");
