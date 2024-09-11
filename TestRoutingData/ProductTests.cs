using Microsoft.AspNetCore.Mvc;
using RoutingData.Controllers;
using RoutingData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestRoutingData
{
    public class ProductTests
    {
        private readonly ProductsController _productController;

#if OFFLINE_DATA
        public ProductTests()
        {
            _productController = new ProductsController(TestServiceProvider.OfflineDatabaseInstance);
        }


        [Fact]
        public async Task Get_Products()
        {
            // Act
            var result = await _productController.GetProducts();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Product>>>(result);

            Assert.NotNull(actionResult.Value);

            var products = Assert.IsAssignableFrom<IEnumerable<Product>>(actionResult.Value);

        }

        [Fact]

        public async Task Post_Product()
        {
            Product product = new Product
            {
                Name = "Xunit Product"
            };

            var result = await _productController.PostProduct(product);

            var actionResult = Assert.IsType<ActionResult<Product>>(result);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);


            var returnedProduct = Assert.IsType<Product>(createdResult.Value);
            Assert.Equal(TestServiceProvider.OfflineDatabaseInstance.Products.Last().Id, returnedProduct.Id);
            Assert.Equal(product.Name, returnedProduct.Name);

            var storedResult = await _productController.GetProducts();
            var productsList = Assert.IsType<List<Product>>(storedResult.Value);
            var storedProduct = productsList.Last();

            Assert.Equal(product.Id, storedProduct.Id);
            Assert.Equal(product.Name, storedProduct.Name);
        }


#endif
    }

}
