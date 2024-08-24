using RoutingData.Controllers;
using RoutingData.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;


namespace TestRoutingData
{
    public class CustomerControllersTests
    {
        private readonly CustomersController _customerController;

        public CustomerControllersTests()
        {
            _customerController = new CustomersController(TestServiceProvider.OfflineDatabaseInstance);
        }

        [Fact]
        public void Non_Database_Endpoint()
        {
            // Arrange
            var controller = new QuantumFactsController();

            // Act
            var result = controller.Get();

            var fieldInfo = typeof(QuantumFactsController).GetField("Summaries", BindingFlags.NonPublic | BindingFlags.Static);
            var summaries = (string[])fieldInfo.GetValue(null);

            // Assert
            Assert.Contains(result, summaries);
        }

#if OFFLINE_DATA

        [Fact]
        public async Task Get_Customers()
        {
            // Act
            var result = await _customerController.GetCustomers();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Customer>>>(result);

            Assert.NotNull(actionResult.Value);

            var customers = Assert.IsAssignableFrom<IEnumerable<Customer>>(actionResult.Value);

        }

        [Fact]

        public async Task Post_Customer()
        {
            Customer customer = new Customer();
            customer.Name = "Xunit Customer";
            customer.Phone = "666";

            var result = await _customerController.PostCustomer(customer);

            var actionResult = Assert.IsType<ActionResult<Customer>>(result);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);


            var returnedCustomer = Assert.IsType<Customer>(createdResult.Value);
            Assert.Equal(TestServiceProvider.OfflineDatabaseInstance.Customers.Last().Id, returnedCustomer.Id);
            Assert.Equal(customer.Name, returnedCustomer.Name);
            Assert.Equal(customer.Phone, returnedCustomer.Phone);

            var storedResult = await _customerController.GetCustomers();
            var customersList = Assert.IsType<List<Customer>>(storedResult.Value);
            var storedCustomer = customersList.Last();

            Assert.Equal(customer.Id, storedCustomer.Id);
            Assert.Equal(customer.Name, storedCustomer.Name);
            Assert.Equal(customer.Phone, storedCustomer.Phone);
        }


#endif
    }


}