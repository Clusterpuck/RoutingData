using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RoutingData.DTO;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class CustomersController : ControllerBase
    {

#if OFFLINE_DATA
    private readonly OfflineDatabase _offlineDatabase;

        public CustomersController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }

#else
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

#endif

#if OFFLINE_DATA
        // GET: api/Customers
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {

            return _offlineDatabase.Customers;
        }

        // POST: api/Customers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            int newID = _offlineDatabase.Customers.Last().Id + 1;
            customer.Id = newID;
            _offlineDatabase.Customers.Add(customer);

            return Created("", customer);
        }
#else

        /// <summary>
        /// Method <c>GetCustomers</c> retrieves all active customers
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            if (_context.Customers == null)
              {
                  return NotFound();
              }
            return await _context.Customers
                .Where(cust => cust.Status == Customer.CUSTOMER_STATUSES[0])
                .ToListAsync();
        }


        /// <summary>
        /// Method <c>GetCustomer</c> returns a customer if exists in database
        /// Will return inactive customers
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {

            if (_context.Customers == null)
          {
              return NotFound();
          }
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
    }

        /// <summary>
        /// Method <c>PutCustomer</c> Edits an existing customer if the customer found in database
        /// Can not be used to change status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inCustomer"></param>
        /// <returns></returns>
        // PUT: api/Customers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, CustomerInDTO inCustomer)
        {
            Customer dbCustomer = await _context.Customers.FindAsync(id);
            if (dbCustomer == null) {
                return BadRequest("No such Customer");
            }
            if(inCustomer.Name.IsNullOrEmpty() || inCustomer.Phone.IsNullOrEmpty() )
            {
                return BadRequest("Values missing for customer");
            }

            dbCustomer.Name = inCustomer.Name;
            dbCustomer.Phone =inCustomer.Phone;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", dbCustomer);
        }


        /// <summary>
        /// Method <c>PostCustomer</c> Adds the customer to the database
        /// Rejects if any values are null or empty
        /// Status is automatically set to Active
        /// </summary>
        /// <param name="inCustomer"></param>
        /// <returns></returns>
        // POST: api/Customers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(CustomerInDTO inCustomer)
        {
            if (_context.Customers == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Customers'  is null.");
            }
            if (inCustomer.Name.IsNullOrEmpty() || inCustomer.Phone.IsNullOrEmpty())
            {
                return BadRequest("Values missing for customer");
            }
            Customer customer = new Customer()
            {
                Name = inCustomer.Name,
                Phone = inCustomer.Phone,
                Status = Customer.CUSTOMER_STATUSES[0]
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        /// <summary>
        /// Method <c>DeleteCustomer</c> Sets the Status of the customer to active
        /// Any active orders for the customer should be removed.
        /// And therefore also the associated OrderProducts
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/Customers/5
        //Should not be able to delete customers that are assigned any active orders at all
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (_context.Customers == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            //Check for any orders associated that aren't cancelled
            //Sets customer to Inactive instead of deleting
            List<Order> activeOrders = await _context.Orders.
                Where(order => order.CustomerId == customer.Id && 
                    order.Status != Order.ORDER_STATUSES[4] && //not cancelled
                    order.Status != Order.ORDER_STATUSES[5]).//not delivered
                    ToListAsync(); 
            //any associated orders that aren't cancelled or delviered should not allow customer to delete
            if( activeOrders.Any() )
            {
                return BadRequest("Customer has aassociated active orders");
            }
            
            //set status to inactive
            customer.Status = Customer.CUSTOMER_STATUSES[1];
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return (_context.Customers?.Any(e => e.Id == id)).GetValueOrDefault();
        }
#endif
    }
}
