using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;
using RoutingData.DTO;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
#if OFFLINE_DATA
        private readonly OfflineDatabase _offlineDatabase;

        public OrdersController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }

        // GET: api/Orders
        [HttpGet]
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
        public async Task<ActionResult<Order>> PostOrder(OrderWithProductsDTO orderDTO)
        {
            
            // Add the Order
            Order order = orderDTO.Order;
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

#else
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
          if (_context.Orders == null)
          {
              return NotFound();
          }
            return await _context.Orders.ToListAsync();
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
#endif
    }
}
