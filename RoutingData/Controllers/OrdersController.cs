﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;
using RoutingData.DTO;
using Microsoft.AspNetCore.Authorization;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class OrdersController : ControllerBase
    {
#if OFFLINE_DATA
        private readonly OfflineDatabase _offlineDatabase;
        //private readonly ApplicationDbContext _context;

        public OrdersController(OfflineDatabase offlineDatabase) //, ApplicationDbContext context)
        {
            _offlineDatabase = offlineDatabase;
           // _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDetailsDTO>>> GetOrders()
        {
            Dictionary<int, OrderDetailsDTO> detailDict = _offlineDatabase.MakeOrdersDictionary();
            List<Order> orders = _offlineDatabase.Orders;
            List<OrderDetailsDTO> orderDetails = new List<OrderDetailsDTO>();
            foreach (var order in orders)
            {
                orderDetails.Add(detailDict[order.Id]);
            }
         
            return orderDetails;
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Order>> PostOrder(OrderWithProductsDTO orderDTO)
        {
            
            // Add the Order
            Order order = orderDTO.Order;
            if( order.Status == null )
            {
                order.Status = "planned";
            }
            int orderID = _offlineDatabase.Orders.Last().Id + 1;
            order.Id = orderID;
            _offlineDatabase.Orders.Add(order);

            foreach (var product in orderDTO.Products)
            {
                product.OrderId = order.Id;
                _offlineDatabase.OrderProducts.Add(product);
            }

            return Created("", order);
        }


        // PUT: api/Order/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("DeliveryDate/{id}")]
        [Authorize]
        public async Task<IActionResult> PutOrder(int id, DateTime deliveryDate)
        {
            Order orderToUpdate = _offlineDatabase.Orders.FirstOrDefault(o => o.Id == id);

            // Check if the order was found
            if (orderToUpdate == null)
            {
                return NotFound($"Order with Id {id} not found.");
            }
            else
            {
                orderToUpdate.DeliveryDate = deliveryDate;
                return Created("", deliveryDate);
            }

        }


     
    }
#else


        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        //TODO Get Orders can potentially just return Not cancelled orders review options
        // GET: api/Orders
        [HttpGet]
        [Authorize]
        public async Task<List<OrderDetailsDTO>> GetOrders()
        {
            var orderDetails = await _context.Orders
                .Join(_context.Locations,
                    order => order.LocationId,
                    location => location.Id,
                    (order, location) => new { order, location })
                .Join(_context.Customers,
                    combined => combined.order.CustomerName,
                    customer => customer.Name,
                    (combined, customer) => new { combined.order, combined.location, customer })
                .Join(_context.OrderProducts,
                    combined => combined.order.Id,
                    orderProduct => orderProduct.OrderId,
                    (combined, orderProduct) => new { combined.order, combined.location, combined.customer, orderProduct })
                .Join(_context.Products,
                    combined => combined.orderProduct.ProductId,
                    product => product.Id,
                    (combined, product) => new { combined.order, combined.location, combined.customer, product }).ToListAsync();

            var groupedOrderDetails = orderDetails
                .GroupBy(g => new { g.order, g.location, g.customer })
                .Select(g => new OrderDetailsDTO
                {
                    OrderID = g.Key.order.Id,
                    OrderNotes = g.Key.order.OrderNotes,
                    DateOrdered = g.Key.order.DateOrdered,
                    Address = g.Key.location.Address,
                    Latitude = g.Key.location.Latitude,
                    Longitude = g.Key.location.Longitude,
                    CustomerName = g.Key.customer.Name,
                    CustomerPhone = g.Key.customer.Phone,
                    Status = g.Key.order.Status,//NickW Added
                    Delayed = g.Key.order.Delayed, // amira added
                    ProductNames = g.Select(x => x.product.Name).ToList(),
                    DeliveryDate = g.Key.order.DeliveryDate,
                })
                .ToList();
            return groupedOrderDetails;
        }
        // GET: api/Orders/with-products
        // returns all orders including product info --> product id, product name, quantity and unit of measure
        [HttpGet("with-products")]
        [Authorize]
        public async Task<List<OrderDetailsWithProductsDTO>> GetOrdersWithProducts()
        {
            var orderDetails = await _context.Orders
                .Join(_context.Locations,
                    order => order.LocationId,
                    location => location.Id,
                    (order, location) => new { order, location })
                .Join(_context.Customers,
                    combined => combined.order.CustomerName,
                    customer => customer.Name,
                    (combined, customer) => new { combined.order, combined.location, customer })
                .Join(_context.OrderProducts,
                    combined => combined.order.Id,
                    orderProduct => orderProduct.OrderId,
                    (combined, orderProduct) => new { combined.order, combined.location, combined.customer, orderProduct })
                .Join(_context.Products,
                    combined => combined.orderProduct.ProductId,
                    product => product.Id,
                    (combined, product) => new { combined.order, combined.location, combined.customer, combined.orderProduct, product })
                .ToListAsync();

            var groupedOrderDetails = orderDetails
                .GroupBy(g => new { g.order, g.location, g.customer })
                .Select(g => new OrderDetailsWithProductsDTO
                {
                    OrderID = g.Key.order.Id,
                    OrderNotes = g.Key.order.OrderNotes,
                    DateOrdered = g.Key.order.DateOrdered,
                    Address = g.Key.location.Address,
                    Latitude = g.Key.location.Latitude,
                    Longitude = g.Key.location.Longitude,
                    CustomerName = g.Key.customer.Name,
                    CustomerPhone = g.Key.customer.Phone,
                    Status = g.Key.order.Status,
                    Delayed = g.Key.order.Delayed,
                    Products = g.Select(x => new ProductGetOrderDTO
                    {
                        ProductID = x.product.Id,
                        Name = x.product.Name,
                        Quantity = x.orderProduct.Quantity,
                        UnitOfMeasure = x.product.UnitOfMeasure
                    }).ToList(),
                    DeliveryDate = g.Key.order.DeliveryDate,
                })
                .ToList();

            return groupedOrderDetails;
        }

        // GET: api/Orders/issues
        // returns all orders where the status is ISSUE
        [HttpGet("issues")]
        [Authorize]
        public async Task<List<OrderDetailsWithProductsDTO>> GetIssueOrders()
        {
            var orderDetails = await _context.Orders
                .Join(_context.Locations,
                    order => order.LocationId,
                    location => location.Id,
                    (order, location) => new { order, location })
                .Join(_context.Customers,
                    combined => combined.order.CustomerName,
                    customer => customer.Name,
                    (combined, customer) => new { combined.order, combined.location, customer })
                .Join(_context.OrderProducts,
                    combined => combined.order.Id,
                    orderProduct => orderProduct.OrderId,
                    (combined, orderProduct) => new { combined.order, combined.location, combined.customer, orderProduct })
                .Join(_context.Products,
                    combined => combined.orderProduct.ProductId,
                    product => product.Id,
                    (combined, product) => new { combined.order, combined.location, combined.customer, combined.orderProduct, product })
                .Where(x => x.order.Status == Order.ORDER_STATUSES[5]) // filter orders by status "ISSUE"
                .ToListAsync();

            var groupedOrderDetails = orderDetails
                .GroupBy(g => new { g.order, g.location, g.customer })
                .Select(g => new OrderDetailsWithProductsDTO
                {
                    OrderID = g.Key.order.Id,
                    OrderNotes = g.Key.order.OrderNotes,
                    DateOrdered = g.Key.order.DateOrdered,
                    Address = g.Key.location.Address,
                    Latitude = g.Key.location.Latitude,
                    Longitude = g.Key.location.Longitude,
                    CustomerName = g.Key.customer.Name,
                    CustomerPhone = g.Key.customer.Phone,
                    Status = g.Key.order.Status, 
                    Delayed = g.Key.order.Delayed,
                    Products = g.Select(x => new ProductGetOrderDTO
                    {
                        ProductID = x.product.Id,
                        Name = x.product.Name,
                        Quantity = x.orderProduct.Quantity,
                        UnitOfMeasure = x.product.UnitOfMeasure
                    }).ToList(),
                    DeliveryDate = g.Key.order.DeliveryDate,
                })
                .ToList();

            return groupedOrderDetails;
        }



        //TODO Can add get order by status
        // GET: api/Orders/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            if (_context.Orders == null)
            {
                return NotFound();
            }
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        //TODO Edit to use OrderWithProductsDTO
        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutOrder(int id, EditOrderDTO orderDTO)
        {
            if (id != orderDTO.OrderId)
            {
                return BadRequest(" Order ID mismatch");
            }

            if (!Order.ORDER_STATUSES.Contains(orderDTO.Status))
            {
                string availableStatuses = string.Join(", ", Order.ORDER_STATUSES);
                return BadRequest($"Invalid status sent of {orderDTO.Status} +" +
                    $" Must be either one of: {availableStatuses}");
            }

            var productIds = orderDTO.Products.Select(p => p.ProductId).ToList();

            var existingProductIds = await _context.Products.Where(p => productIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();

            var invalidProductIds = productIds.Except(existingProductIds).ToList();

            if (invalidProductIds.Any())
            {
                string invalidIds = string.Join(", ", invalidProductIds);
                return BadRequest($"The following Product IDs do not exist: {invalidIds}");
            }

            var discontinuedProducts = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.Status == Product.PRODUCT_STATUSES[1])
                .ToListAsync();

            if (discontinuedProducts.Any())
            {
                return BadRequest("Some products are discontinued and cannot be ordered.");
            }
            // check if the customer is inactive
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Name == orderDTO.CustomerName);
            if (customer == null || customer.Status == Customer.CUSTOMER_STATUSES[1])
            {
                return BadRequest("The customer is inactive and cannot place orders.");
            }
            // check if the location is inactive
            var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == orderDTO.LocationId);
            if (location == null || location.Status == Location.LOCATION_STATUSES[1])
            {
                return BadRequest("The location is inactive and cannot be used for deliveries.");
            }
            // check if the delivery date is not in the past
            if (orderDTO.DeliveryDate <= DateTime.Today)
            {
                return BadRequest("Cannot change delivery date to a past date.");
            }
            if (_context.Orders == null)
            {
                return NotFound("Orders not found.");
            }
            // Find the order by the provided OrderId
            var order = await _context.Orders.FindAsync(orderDTO.OrderId);

            if (order == null)
            { // return not found if not found
                return NotFound($"Order with ID {orderDTO.OrderId} not found.");
            }

            if (order.Status == Order.ORDER_STATUSES[5])  // if an order is issue status
            {
                // If the order is set to cancelled
                if (orderDTO.Status == Order.ORDER_STATUSES[3]) 
                {
                    order.DeliveryRouteId = -1; // remove order from route
                }

                // If the order is set to "Planned"
                if (orderDTO.Status == Order.ORDER_STATUSES[0])  
                {
                    order.DeliveryRouteId = -1;
                }

                // If the delivery date is changed AND user tries to mark is as delivered
                if ( (order.DeliveryDate.Date != orderDTO.DeliveryDate.Date) && (orderDTO.Status == Order.ORDER_STATUSES[2])) 
                {
                    return BadRequest("Cannot set order to delivered and change delivery date.");
                }
                if (order.DeliveryDate.Date != orderDTO.DeliveryDate.Date) // if delivery date is changed
                {
                    order.DeliveryRouteId = -1; // remove order from route
                    orderDTO.Status = Order.ORDER_STATUSES[0]; // set order status to planned
                }
            }

            try
            {
                order.ChangeStatus(orderDTO.Status);
            }
            catch(ArgumentException ex)
            {
                return BadRequest($"Error in editing order's state: {ex.Message}");
            }

            order.CustomerName = orderDTO.CustomerName;
            order.LocationId = orderDTO.LocationId;
            order.DeliveryDate = orderDTO.DeliveryDate;
            order.OrderNotes = orderDTO.OrderNotes;


            var existingOrderProducts = await _context.OrderProducts.Where(op => op.OrderId == orderDTO.OrderId).ToListAsync();

            // Find and remove products that are no longer in the orderDTO.Products list
            var productsToRemove = existingOrderProducts
                .Where(op => !productIds.Contains(op.ProductId))
                .ToList();

            foreach (var productToRemove in productsToRemove)
            {
                _context.OrderProducts.Remove(productToRemove);
            }

            // Add or update products
            foreach (var productDTO in orderDTO.Products)
            {
                var existingProduct = existingOrderProducts
                    .FirstOrDefault(op => op.ProductId == productDTO.ProductId);

                if (existingProduct != null)
                {
                    // Update the quantity if the product is already in the order
                    existingProduct.Quantity = productDTO.Quantity;
                }
                else
                {
                    // Add a new product to the order
                    var newOrderProduct = new OrderProduct
                    {
                        OrderId = orderDTO.OrderId,
                        ProductId = productDTO.ProductId,
                        Quantity = productDTO.Quantity,
                        Status = OrderProduct.ORDERPRODUCTS_STATUSES[0]  // Set the appropriate status
                    };
                    _context.OrderProducts.Add(newOrderProduct);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Problem($"An error occurred while updating the order: {ex.Message}");
            }

            return NoContent();
        }

        /// <summary>
        /// Adds a new order to the system.
        /// </summary>
        /// <param name="orderDTO">The data transfer object containing the order and its associated products.</param>
        /// <returns>
        /// Returns the created order along with the route to retrieve the order if successful. 
        /// Returns a <see cref="BadRequestResult"/> if:
        /// <list type="bullet">
        /// <item><description>Any product in the order is discontinued.</description></item>
        /// <item><description>The customer is inactive.</description></item>
        /// <item><description>The location is inactive.</description></item>
        /// <item><description>The delivery date is not in the future.</description></item>
        /// </list>
        /// Returns a <see cref="UnauthorizedResult"/> if the user token is invalid.
        /// </returns>
        /// <remarks>
        /// This method also sets the customer ID from the user's authentication token.
        /// </remarks>


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Order>> PostOrder(OrderWithProductsDTO orderDTO)
        {
            if (_context.Orders == null || _context.OrderProducts == null)
            {
                return Problem("Entity sets 'ApplicationDbContext.Orders' or 'ApplicationDbContext.OrderProducts' are null.");
            }

            // retrieve user information from token
           /* var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID");
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token or user not found.");
            }
            var userId = int.Parse(userIdClaim.Value);*/

            // check if any product is discontinued
            var productIds = orderDTO.Products.Select(p => p.ProductId).ToList();
            var discontinuedProducts = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.Status == Product.PRODUCT_STATUSES[1])
                .ToListAsync();

            if (discontinuedProducts.Any())
            {
                return BadRequest("Some products are discontinued and cannot be ordered.");
            }

            // check if the customer is inactive
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Name == orderDTO.Order.CustomerName);
            if (customer == null || customer.Status == Customer.CUSTOMER_STATUSES[1])
            {
                return BadRequest("The customer is inactive and cannot place orders.");
            }

            // check if the location is inactive
            var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == orderDTO.Order.LocationId);
            if (location == null || location.Status == Location.LOCATION_STATUSES[1])
            {
                return BadRequest("The location is inactive and cannot be used for deliveries.");
            }

            // check if the delivery date is greater than today
            if (orderDTO.Order.DeliveryDate <= DateTime.Today.AddDays(-1))
            {
                return BadRequest("Delivery date must be greater than today.");
            }

            // add the Order
            var order = orderDTO.Order;
            Order dbOrder = orderDtoToOrder(order);
            //order.CustomerId = userId;  // set account by token
            _context.Orders.Add(dbOrder);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Check for specific error related to unique constraint violation
                if (ex.InnerException != null )
                {
                    return Conflict($"Error adding order '{order.OrderNotes}'. {ex.InnerException}");
                }

                // Log and return a general error message
                return Problem($"An error occurred while trying to add the order.");
            }

            // add the products
            foreach (var product in orderDTO.Products)
            {
                OrderProduct dbOrderProd = new OrderProduct()
                {
                    OrderId = dbOrder.Id,
                    Status = OrderProduct.ORDERPRODUCTS_STATUSES[0],
                    Quantity = product.Quantity,
                    ProductId = product.ProductId,

                };
                _context.OrderProducts.Add(dbOrderProd);
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null)
                {
                    return Conflict($"Error adding products: {ex.InnerException}"); 
                }
                return Problem("Unknown error adding products");

            }


            return CreatedAtAction("GetOrder", new { id = dbOrder.Id }, dbOrder);
        }

        [HttpPost("update-order-status")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus( UpdateOrderStatusDTO orderStatusDTO)
        {
            //Check if a valid status provided before using ay database requests
            if (!Order.ORDER_STATUSES.Contains(orderStatusDTO.Status))
            {
                string availableStatuses = string.Join(", ", Order.ORDER_STATUSES);
                return BadRequest($"Invalid status sent of {orderStatusDTO.Status} +" +
                    $" Must be either one of: {availableStatuses}");
            }

            if (_context.Orders == null)
            {
                return NotFound("Orders not found.");
            }

            // Find the order by the provided OrderId
            var order = await _context.Orders.FindAsync(orderStatusDTO.OrderId);

            if (order == null)
            { // return not found if not found
                return NotFound($"Order with ID {orderStatusDTO.OrderId} not found.");
            }

            // Update the order status
            try
            {
                order.ChangeStatus(orderStatusDTO.Status);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Error in updating order's state: {ex.Message}");
            }

            // Save the changes to the database
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Problem($"An error occurred while updating the order status: {ex.Message}");
            }

            return Ok(new { message = "Order status updated successfully", orderId = order.Id, newStatus = order.Status });
        }

        private Order orderDtoToOrder(OrderInDTO orderInDTO)
        {
            Order order = new Order()
            {
                DateOrdered = orderInDTO.DateOrdered,
                OrderNotes = orderInDTO.OrderNotes,
                CustomerName = orderInDTO.CustomerName,
                LocationId = orderInDTO.LocationId,
                DeliveryRouteId = -1,
                PositionNumber = -1,
                DeliveryDate = orderInDTO.DeliveryDate,
                Delayed = false //amira added

            };
            return order;
            
        }


        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            // first check if rhere are any orders
            if (_context.Orders == null)
            {
                return NotFound();
            }

            // then try and find the order by the given ID
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // check order status before deleting, make sure it is not delivered
            if (order.Status == Order.ORDER_STATUSES[2])//check if delivered
            {
                return BadRequest("Cannot delete a delivered order.");
            }
            // if the order has status issue, remove it from the route
            else if (order.Status == Order.ORDER_STATUSES[5])
            { 
                order.DeliveryRouteId = -1;
            }
            //if is anything other than planned then should not delete
            else if (order.Status != Order.ORDER_STATUSES[0])
            {
                return BadRequest("Cannot delete on on route order. Cancel route first");
            }

            // remove the order if it hasn't been delivered on on delivery
            try
            {
                order.ChangeStatus(Order.ORDER_STATUSES[3]);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Error in setting order's state: {ex.Message}");
            }

            List<OrderProduct> orderProducts = await _context.OrderProducts.
                Where(orderProd => orderProd.OrderId == order.Id).
                ToListAsync();
            foreach (var product in orderProducts)
            {//set each to inactive
                product.Status = OrderProduct.ORDERPRODUCTS_STATUSES[1];
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully" }); // Return a success message
        }

        private bool OrderExists(int id)
        {
            return (_context.Orders?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }

#endif

}
