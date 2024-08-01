using Microsoft.EntityFrameworkCore;

namespace RoutingData.Models;
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
