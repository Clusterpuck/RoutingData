using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.CompilerServices;
using RoutingData.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connection = String.Empty;

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
}
else
{
    //temporary literal to bypass env issues. 
    connection = "Server=tcp:quantumsqlserver.database.windows.net,1433;Initial Catalog=quantumDelivery;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default";
    //connection = Environment.GetEnvironmentVariable("SQLCONNSTR_AZURE_SQL_CONNECTIONSTRING");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connection));

var app = builder.Build();

// Configure the HTTP request pipeline.
//TODO: Uncomment condition for proper deployment
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/Person", (ApplicationDbContext context) =>
{
    return context.Person.ToList();
})
.WithName("GetPersons")
.WithOpenApi();

app.MapPost("/Person", (Person person, ApplicationDbContext context) =>
{
    context.Add(person);
    context.SaveChanges();
})
.WithName("CreatePerson")
.WithOpenApi();


app.MapGet("/QuantumFact", (ApplicationDbContext context) =>
{
    var facts = context.QuantumFacts.ToList();
    if (facts.Count == 0)
    {
        return "No quantum facts found.";
    }

    var random = new Random();
    int index = random.Next(facts.Count);
    return facts[index].FactText;
}
).WithName("GetQuantumFact")
.WithOpenApi();

app.MapPost("/QuantumFact", (QuantumFacts fact, ApplicationDbContext context) =>
{
    context.Add(fact);
    context.SaveChanges();
})
.WithName("CreateFact")
.WithOpenApi();


app.Run();


/*public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}


public class QuantumFacts
{
    public int Id { get; set; }
    public string FactText { get; set; }
}



public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> Person { get; set; } = null!;
    public DbSet<QuantumFacts> QuantumFacts { get; set; } = null!;

    public DbSet<Customer> Customers { get; set; } = null!;


}

*/