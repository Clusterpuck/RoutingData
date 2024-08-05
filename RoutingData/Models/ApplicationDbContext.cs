using Microsoft.EntityFrameworkCore;

namespace RoutingData.Models;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
    {
    }

    public DbSet<Person> Person { get; set; } = null!; // to be deleted
    public DbSet<QuantumFacts> QuantumFacts { get; set; } = null!; // to be deleted
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Driver> Drivers { get; set; } = null!;
    public DbSet<Admin> Admins { get; set; } = null!;
    public DbSet<DriverAccount> DriverAccounts { get; set; } = null!;
    public DbSet<AdminAccount> AdminAccounts { get; set; } = null!;
    public DbSet<OrderProduct> OrderProducts { get; set; } = null!;

}
