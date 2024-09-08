using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.DTO;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ProductsController : ControllerBase
    {
#if OFFLINE_DATA
        private readonly OfflineDatabase _offlineDatabase;

        public ProductsController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return _offlineDatabase.Products;
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            int newID = _offlineDatabase.Products.Last().Id + 1;
            product.Id = newID;
            _offlineDatabase.Products.Add(product);

            return Created("", product);
        }

#else

        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
          if (_context.Products == null)
          {
              return NotFound();
          }
            return await _context.Products.ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
          if (_context.Products == null)
          {
              return NotFound();
          }
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", product);
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Products'  is null.");
            }

            //All products start as active. 
            product.Status = Product.PRODUCT_STATUSES[0];
        
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (_context.Products == null)
            {
                return NotFound();
            }
            var product = await _context.Products.FindAsync(id);
            if (product == null || ( product.Status == Product.PRODUCT_STATUSES[2] ) )
            {//Null product or a product already set to InActive is considered delted already
                return NotFound();
            }
            //Instead of removing, simply set the product status to InActive
            product.Status = Product.PRODUCT_STATUSES[2];
            await _context.SaveChangesAsync();
            
            //Also need to update any order that is "Planned" to remove this product.
            await RemoveProductFromPlannedOrders(id);

            return NoContent();
        }

        private async Task<IActionResult> RemoveProductFromPlannedOrders(int productId)
        {
            // Find all OrderProducts for the given product where the related order is in "Planned" status
            var orderProductsToRemove = await (
                from op in _context.OrderProducts
                join o in _context.Orders on op.OrderId equals o.Id
                where o.Status == Order.ORDER_STATUSES[0] && op.ProductId == productId
                select op
            ).ToListAsync();

            if (!orderProductsToRemove.Any())
            {
                return NotFound();
            }

            // Remove the found OrderProducts
            _context.OrderProducts.RemoveRange(orderProductsToRemove);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return NoContent();
        }


        private bool ProductExists(int id)
        {
            return (_context.Products?.Any(e => e.Id == id)).GetValueOrDefault();
        }
#endif
    }
}
