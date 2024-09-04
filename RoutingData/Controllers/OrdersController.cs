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
    [Authorize]
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
        public async Task<ActionResult<IEnumerable<OrderDetail>>> GetOrders()
        {
            Dictionary<int, OrderDetail> detailDict = _offlineDatabase.MakeOrdersDictionary();
            List<Order> orders = _offlineDatabase.Orders;
            List<OrderDetail> orderDetails = new List<OrderDetail>();
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
                return NoContent();
            }

        }


     
    }
#else


        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<List<OrderDetailsDto>> GetOrders()
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
                    (combined, product) => new { combined.order, combined.location, combined.customer, product })
                .GroupBy(g => new { g.order, g.location, g.customer })
                .Select(g => new OrderDetailsDto
                {
                    OrderID = g.Key.order.Id,
                    OrderNotes = g.Key.order.OrderNotes,
                    DateOrdered = g.Key.order.DateOrdered,
                    Address = g.Key.location.Address,
                    Latitude = g.Key.location.Latitude,
                    Longitude = g.Key.location.Longitude,
                    CustomerName = g.Key.customer.Name,
                    CustomerPhone = g.Key.customer.Phone,
                    ProductNames = g.Select(x => x.product.Name).ToList()
                })
                .ToListAsync();

            return orderDetails;
        }

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

            return NoContent();
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(OrderWithProductsDTO orderDTO)
        {
            if (_context.Orders == null || _context.OrderProducts == null)
            {
                return Problem("Entity sets 'ApplicationDbContext.Orders' or 'ApplicationDbContext.OrderProducts' are null.");
            }

            // Add the Order
            var order = orderDTO.Order;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var product in orderDTO.Products)
            {
                product.OrderId = order.Id;
                _context.OrderProducts.Add(product);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
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

            _context.Orders.Remove(order);
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
