using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/OrderProducts
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderProduct>>> GetOrderProducts()
        {
          if (_context.OrderProducts == null)
          {
              return NotFound();
          }
            return await _context.OrderProducts.ToListAsync();
        }

        // GET: api/OrderProducts/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderProduct>> GetOrderProduct(int id)
        {
          if (_context.OrderProducts == null)
          {
              return NotFound();
          }
            var orderProduct = await _context.OrderProducts.FindAsync(id);

            if (orderProduct == null)
            {
                return NotFound();
            }

            return orderProduct;
        }

        // PUT: api/OrderProducts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutOrderProduct(int id, OrderProduct orderProduct)
        {
            if (id != orderProduct.OrderId)
            {
                return BadRequest();
            }

            _context.Entry(orderProduct).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", orderProduct);
        }

        // POST: api/OrderProducts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrderProduct>> PostOrderProduct(OrderProduct orderProduct)
        {
          if (_context.OrderProducts == null)
          {
              return Problem("Entity set 'ApplicationDbContext.OrderProducts'  is null.");
          }
            _context.OrderProducts.Add(orderProduct);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (OrderProductExists(orderProduct.OrderId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetOrderProduct", new { id = orderProduct.OrderId }, orderProduct);
        }

        // DELETE: api/OrderProducts/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrderProduct(int id)
        {
            if (_context.OrderProducts == null)
            {
                return NotFound();
            }
            var orderProduct = await _context.OrderProducts.FindAsync(id);
            if (orderProduct == null)
            {
                return NotFound();
            }

            _context.OrderProducts.Remove(orderProduct);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderProductExists(int id)
        {
            return (_context.OrderProducts?.Any(e => e.OrderId == id)).GetValueOrDefault();
        }
    }
}
