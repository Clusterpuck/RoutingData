using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;
using RoutingData;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;
using RoutingData.Controllers;
using System.Threading.Tasks;

namespace RoutingDataTest
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());
            // Ensures that each test gets a unique in-memory database.

            await using var context = new ApplicationDbContext(optionsBuilder.Options);

            var customersController = new CustomersController(context);
            await customersController.PostCustomer(new Customer { Name = "Amira Moriff", Phone = "0420717695" });

            Assert.Single(context.Customers);

        }
    }
}