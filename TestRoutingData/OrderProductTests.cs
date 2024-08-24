using Microsoft.AspNetCore.Mvc;
using RoutingData.Controllers;
using RoutingData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRoutingData
{
    public class OrderProductTests
    {
       /* private readonly OrderProductsController _orderProductController;

        public OrderProductTests()
        {
            _orderProductController = new OrderProductsController(TestServiceProvider.OfflineDatabaseInstance);
        }

#if OFFLINE_DATA

        [Fact]
        public async Task Get_OrderProducts()
        {
            // Act
            var result = await _orderProductController.GetOrderProducts();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<OrderProduct>>>(result);

            Assert.NotNull(actionResult.Value);

            var orderProducts = Assert.IsAssignableFrom<IEnumerable<OrderProduct>>(actionResult.Value);

        }

        [Fact]

        public async Task Post_OrderProduct()
        {
            OrderProduct orderProduct = new OrderProduct
            {
                Name = "Xunit OrderProduct"
            };

            var result = await _orderProductController.PostOrderProduct(orderProduct);

            var actionResult = Assert.IsType<ActionResult<OrderProduct>>(result);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);


            var returnedOrderProduct = Assert.IsType<OrderProduct>(createdResult.Value);
            Assert.Equal(TestServiceProvider.OfflineDatabaseInstance.OrderProducts.Last().Id, returnedOrderProduct.Id);
            Assert.Equal(orderProduct.Name, returnedOrderProduct.Name);

            var storedResult = await _orderProductController.GetOrderProducts();
            var orderProductsList = Assert.IsType<List<OrderProduct>>(storedResult.Value);
            var storedOrderProduct = orderProductsList.Last();

            Assert.Equal(orderProduct.Id, storedOrderProduct.Id);
            Assert.Equal(orderProduct.Name, storedOrderProduct.Name);
        }


#endif
       */
    }
}
