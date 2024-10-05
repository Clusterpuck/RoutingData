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
    /// <summary>
    ///  Class <c>ProductsController</c> provides API interactions for Product model
    /// </summary>

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

        /// <summary>
        /// Method <c>GetProducts</c> returns a list of all active products (products with active status)
        /// </summary>
        /// <returns></returns>
        // GET: api/Products
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
          if (_context.Products == null)
          {
              return NotFound();
          }
            return await _context.Products
                .Where( prod => prod.Status == Product.PRODUCT_STATUSES[0])
                .ToListAsync();
        }


        /// <summary>
        /// Method <c>GetProduct</c> gets a product by it's specific ID. 
        /// Will return an Inactive product for historical uses
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET: api/Products/5
        [HttpGet("{id}")]
        [Authorize]
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


        /// <summary>
        /// Method <c>PutProduct</c> Edits an existing product if the provided id matches the id in the provided product object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutProduct(int id, ProductInDTO inProduct)
        {
            Product dbProduct = await _context.Products.FindAsync(id);
            if (dbProduct == null)
            {
                return NotFound();
            }

            dbProduct.Name = inProduct.Name;
            dbProduct.UnitOfMeasure = inProduct.UnitOfMeasure;

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

            return Created("", dbProduct);
        }


        /// <summary>
        /// Method <c>PostProduct</c> adds a product to the database
        /// Sets status to starting default of "Active" 
        /// Usis InProduct DTO to allow sent inobject o only need to send minimal fields
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Product>> PostProduct(ProductInDTO inProduct)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Products'  is null.");
            }

            Product product = new Product()
            {
                Name = inProduct.Name,
                UnitOfMeasure = inProduct.UnitOfMeasure,
                Status = Product.PRODUCT_STATUSES[0]
            };

            //All products start as active. 
            product.Status = Product.PRODUCT_STATUSES[0];
        
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }


        /// <summary>
        /// Method <c>DeleteProduct</c> assigns the product the inactive status in the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (_context.Products == null)
            {
                return NotFound();
            }
            var product = await _context.Products.FindAsync(id);
            if (product == null || ( product.Status == Product.PRODUCT_STATUSES[1] ) )
            {//Null product or a product already set to InActive is considered deleted already
                return NotFound();
            }
            //first check orders associated in valid state
            Boolean canDelete =  await RemoveProductFromOrders(id);

            //Instead of removing, simply set the product status to InActive
            //confirm any orders the product is on are either planned or cancelled
            if (canDelete)
            {
                product.Status = Product.PRODUCT_STATUSES[1];
                await _context.SaveChangesAsync();
            
                //Also need to update any order that is "Planned" to remove this product.

                return Ok(new { message = "Product deleted successfully" }); // Return a success message
            }
            else
            {
                return BadRequest("Some product are on active orders");
            }

        }


        /// <summary>
        /// Method <c>RemoveProductFromOrders</c> removes entries from OrderProduct table that match the product id
        /// Only if the order isn't already loaded for deliver. i.e. planned
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        private async Task<Boolean> RemoveProductFromOrders(int productId)
        {
            // Check if any orders related to the product have a status other than 0 or 3
            var hasInvalidStatus = await (
                from op in _context.OrderProducts
                join o in _context.Orders on op.OrderId equals o.Id
                where op.ProductId == productId && o.Status != Order.ORDER_STATUSES[0] && o.Status != Order.ORDER_STATUSES[3]
                select o
            ).AnyAsync();

            // If any order has an invalid status, return false
            if (hasInvalidStatus)
            {
                return false;
            }

            // Find all OrderProducts for the given product where the related order is in "Planned" status
            var orderProductsToRemove = await (
                from op in _context.OrderProducts
                join o in _context.Orders on op.OrderId equals o.Id
                where o.Status == Order.ORDER_STATUSES[0] && op.ProductId == productId
                select op
            ).ToListAsync();

            if (!orderProductsToRemove.Any())
            {//no invlaid status, not valid status other, no orders
                return true;
            }

            // Remove the found OrderProducts
            _context.OrderProducts.RemoveRange(orderProductsToRemove);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Method <c>ProductExists</c> confirms the product is in the database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool ProductExists(int id)
        {
            return (_context.Products?.Any(e => e.Id == id)).GetValueOrDefault();
        }
#endif
    }
}
