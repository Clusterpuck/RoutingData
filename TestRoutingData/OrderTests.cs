using Microsoft.AspNetCore.Mvc;
using RoutingData.Controllers;
using RoutingData.Models;
using RoutingData.DTO;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRoutingData
{
    public class OrderTests
    {
        private readonly OrdersController _orderController;
        private readonly List<OrderProduct> _products;

        public OrderTests()
        {
            _orderController = new OrdersController(TestServiceProvider.OfflineDatabaseInstance);
            _products = TestServiceProvider.OfflineDatabaseInstance.OrderProducts.ToList();
        }

#if OFFLINE_DATA

        [Fact]
        public async Task Get_Orders()
        {
            // Act
            var result = await _orderController.GetOrders();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<OrderDetail>>>(result);

            Assert.NotNull(actionResult.Value);

            var orders = Assert.IsAssignableFrom<IEnumerable<OrderDetail>>(actionResult.Value);

        }

        [Fact]

        public async Task Post_Order()
        {

            Order order = new Order
            {
                DateOrdered = DateTime.Now,
                OrderNotes = "This is a test order",
                CustomerId = 1,
                LocationId = 2,
                DeliveryRouteId = 3,
                PositionNumber = 4,
            };

            OrderWithProductsDTO orderProductsDTO = new OrderWithProductsDTO
            {
                Order = order,
                //Order products has no controller, therefor populated direct from database
                Products = _products,
            };

            var result = await _orderController.PostOrder(orderProductsDTO);

            var actionResult = Assert.IsType<ActionResult<Order>>(result);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);


            var returnedOrder = Assert.IsType<Order>(createdResult.Value);
            Assert.Equal(TestServiceProvider.OfflineDatabaseInstance.Orders.Last().Id, returnedOrder.Id);
            AssertOrderEqual(order, returnedOrder);

            var storedResult = await _orderController.GetOrders();
            var ordersList = Assert.IsType<List<Order>>(storedResult.Value);
            var storedOrder = ordersList.Last();

            Assert.Equal(order.Id, storedOrder.Id);
            AssertOrderEqual(order, storedOrder);
        }

        private void AssertOrderEqual(Order expected, Order actual)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            Assert.Equal(expected.DateOrdered, actual.DateOrdered);
            Assert.Equal(expected.OrderNotes, actual.OrderNotes);
            Assert.Equal(expected.CustomerId, actual.CustomerId);
            Assert.Equal(expected.LocationId, actual.LocationId);
            Assert.Equal(expected.DeliveryRouteId, actual.DeliveryRouteId);
            Assert.Equal(expected.PositionNumber, actual.PositionNumber);
        }


#endif
    }
}
