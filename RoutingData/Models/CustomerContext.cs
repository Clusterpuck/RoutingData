using Microsoft.EntityFrameworkCore;

namespace RoutingData.Models
{
    public class CustomerContext : DbContext
    {
        public CustomerContext(DbContextOptions<CustomerContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
    }
}
