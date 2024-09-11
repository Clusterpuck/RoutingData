using System;
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
        public async Task<List<OrderDetailsDTO>> GetOrders()
        {
            var orderDetails = await _context.Orders
                .Join(_context.Locations,
                    order => order.LocationId,
                    location => location.Id,
                    (order, location) => new { order, location })
                .Join(_context.Customers,
                    combined => combined.order.CustomerId,
                    customer => customer.Id,
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
                    ProductNames = g.Select(x => x.product.Name).ToList()
                })
                .ToList();
            return groupedOrderDetails;
        }
        
        //TODO Can add get order by status
        // GET: api/Orders/5
        [HttpGet("{id}")]
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
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", order);
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
        //[Authorize]
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
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == orderDTO.Order.CustomerId);
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

        private Order orderDtoToOrder(OrderInDTO orderInDTO)
        {
            Order order = new Order()
            {
                Status = Order.ORDER_STATUSES[0],
                DateOrdered = orderInDTO.DateOrdered,
                OrderNotes = orderInDTO.OrderNotes,
                CustomerId = orderInDTO.CustomerId,
                LocationId = orderInDTO.LocationId,
                DeliveryRouteId = -1,
                PositionNumber = -1,
                DeliveryDate = orderInDTO.DeliveryDate,

            };
            return order;
            
        }


        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
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
            if (order.Status == Order.ORDER_STATUSES[3])//check if delivered
            {
                return BadRequest("Cannot delete a delivered order.");
            }
            //if is anything other than planned then should not delete
            else if( order.Status != Order.ORDER_STATUSES[0])
            {
                return BadRequest("Cannot delete on on route order. Cancel route first");
            }

            // remove the order if it hasn't been delivered on on delivery
            order.Status = Order.ORDER_STATUSES[4];
            List<OrderProduct> orderProducts = await _context.OrderProducts.
                Where(orderProd => orderProd.OrderId == order.Id).
                ToListAsync();
            foreach (var product in orderProducts)
            {//set each to inactive
                product.Status = OrderProduct.ORDERPRODUCTS_STATUSES[1];
            }
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return (_context.Orders?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }

#endif

}
