using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;
using RoutingData;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;
using RoutingData.Controllers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RoutingDataTest
{
    public class UnitTest1
    {
        // TEST POST CUSTOMER
        [Fact]
        public async Task Test_PostCustomer()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());
            // Ensures that each test gets a unique in-memory database.

            await using var context = new ApplicationDbContext(optionsBuilder.Options);

            var customersController = new CustomersController(context);
            await customersController.PostCustomer(new Customer { Name = "Amira Moriff", Phone = "0420717695" });

            Assert.Single(context.Customers);
        }

        // TEST GET CUSTOMERS
        [Fact]
        public async Task Test_GetCustomers()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            context.Customers.AddRange(
                new Customer { Name = "Amira", Phone = "12345678" },
                new Customer { Name = "Tyler", Phone = "87687654" }
            );
            await context.SaveChangesAsync();

            var customersController = new CustomersController(context);
            var result = await customersController.GetCustomers(); 

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Customer>>>(result); // check that the result is of type ActionResult<IEnumerable<Customer>>
            var returnValue = Assert.IsType<List<Customer>>(actionResult.Value);  // check that the Value property of the ActionResult is of type List<Customer>
            Assert.Equal(2, returnValue.Count); // check that the count of the returned list is 2
        }

        // TEST GET CUSTOMER
        [Fact]
        public async Task Test1_GetCustomer()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            var customer = new Customer { Name = "Fishy", Phone = "134454355" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var customersController = new CustomersController(context);
            var result = await customersController.GetCustomer(customer.Id);

            var actionResult = Assert.IsType<ActionResult<Customer>>(result);
            var returnValue = Assert.IsType<Customer>(actionResult.Value);
            Assert.Equal(customer.Name, returnValue.Name);
            Assert.Equal(customer.Phone, returnValue.Phone);
        }

        [Fact]
        public async Task Test2_GetCustomer()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            await using var context = new ApplicationDbContext(optionsBuilder.Options);
            var customersController = new CustomersController(context);
            var result = await customersController.GetCustomer(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}