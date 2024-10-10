using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using RoutingData.DTO;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class LocationsController : ControllerBase
    {
#if OFFLINE_DATA
        private readonly OfflineDatabase _offlineDatabase;

        public LocationsController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
        {

            return _offlineDatabase.Locations;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Location>> PostLocation(Location location)
        {
            int newID = _offlineDatabase.Locations.Last().Id + 1;
            location.Id = newID;
            _offlineDatabase.Locations.Add(location);

            return Created("", location);
        }



#else
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // GET: api/Locations
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<RoutingData.Models.Location>>> GetLocations()
        {
          if (_context.Locations == null)
          {
              return NotFound();
          }
            return await _context.Locations.
                Where( location => location.Status == RoutingData.Models.Location.LOCATION_STATUSES[0]).
                //return active locations only
                ToListAsync();
        }

        // GET: api/Locations
        [HttpGet("for-customer/{customerName}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<RoutingData.Models.Location>>> GetLocationsForCustomer( string customerName)
        {
            if (_context.Locations == null)
            {
                return NotFound();
            }
            Customer locCustomer = await GetCustomerIfValid(customerName);
            if (locCustomer == null)
            {
                return BadRequest("Customer ID is not valid");
            }

            return await _context.Locations.
                Where( location => (location.CustomerName == customerName) && location.Status == RoutingData.Models.Location.LOCATION_STATUSES[0] ).
                //return active locations only
                ToListAsync();
        }

        // GET: api/Locations
        [HttpGet("depots")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<RoutingData.Models.Location>>> GetDepots()
        {
            if (_context.Locations == null)
            {
                return NotFound();
            }
            return await _context.Locations.Where(location => location.IsDepot).ToListAsync();
        }

        // GET: api/Locations/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<RoutingData.Models.Location>> GetLocation(int id)
        {
          if (_context.Locations == null)
          {
              return NotFound();
          }
            var location = await _context.Locations.
                FirstOrDefaultAsync( location => location.Id == id && location.Status == RoutingData.Models.Location.LOCATION_STATUSES[0]);
            //return active location only

            if (location == null)
            {
                return NotFound();
            }

            return location;
        }

        private async Task<Customer> GetCustomerIfValid( string customerName )
        {
            Console.WriteLine("Checking for valid customer with name " + customerName);
            Customer locCustomer = await _context.Customers.
              FirstOrDefaultAsync(customer => (customer.Name == customerName && //found customer
                  customer.Status == Customer.CUSTOMER_STATUSES[0])); //customer is active
            return locCustomer;
        }

        // PUT: api/Locations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutLocation(int id, LocationInDTO location)
        {//TODO Restrict to only locations not associated with active orders
            RoutingData.Models.Location dbLocation = await _context.Locations.
                Where( location => location.Id == id && location.Status == RoutingData.Models.Location.LOCATION_STATUSES[0]).
                //Get active locations only
                FirstOrDefaultAsync();
            if (dbLocation == null)
            {
                return BadRequest("Location ID does not exist");
            }
            Customer locCustomer = await GetCustomerIfValid(location.CustomerName);
            if ( !dbLocation.IsDepot && locCustomer == null)//depots don't need customers
            {
                return BadRequest("Customer ID is not valid");
            }

            // before updating, make sure location isnt apart of any active delivery
            bool hasOngoingOrders = await _context.Orders
                .AnyAsync(order => order.LocationId == id &&
                    (order.Status != Order.ORDER_STATUSES[2] && order.Status != Order.ORDER_STATUSES[3])); //Any that are not Delivered or cancelled
            //Unless cancelled or delivered, should not be able to set location to inactive
            if (hasOngoingOrders)
            {
                return BadRequest("Location is part of an active delivery");
            }


            //Update database object with new details
            dbLocation.Longitude = location.Longitude;
            dbLocation.Latitude = location.Latitude;
            dbLocation.Address = location.Address;
            dbLocation.Suburb = location.Suburb;
            dbLocation.State = location.State;
            dbLocation.PostCode = location.PostCode;
            dbLocation.Country = location.Country;
            dbLocation.Description = location.Description;
            dbLocation.CustomerName = location.CustomerName;

            _context.Entry(dbLocation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", location);
        }

        // POST: api/Locations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<RoutingData.Models.Location>> PostLocation(LocationInDTO location)
        {
            if (_context.Locations == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Locations'  is null.");
            }
            Customer locCustomer = await GetCustomerIfValid(location.CustomerName);
            if ( locCustomer == null)
            {
                return BadRequest("Customer ID is not valid");
            }
            RoutingData.Models.Location dbLocation = new RoutingData.Models.Location();
                dbLocation.Longitude = location.Longitude;
                dbLocation.Latitude = location.Latitude;
                dbLocation.Address = location.Address;
                dbLocation.Suburb = location.Suburb;
                dbLocation.State = location.State;
                dbLocation.PostCode = location.PostCode;
                dbLocation.Country = location.Country;
                dbLocation.Description = location.Description;
                dbLocation.Status = RoutingData.Models.Location.LOCATION_STATUSES[0];
                dbLocation.CustomerName = location.CustomerName;
            _context.Locations.Add(dbLocation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLocation", new { id = dbLocation.Id }, dbLocation);
        }


        // DELETE: api/Locations/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            // first check location table isnt empty
            if (_context.Locations == null)
            {
                return NotFound("Location data not available.");
            }

            // then find the location by the given ID
            var location = await _context.Locations.
                Where( location => location.Id == id && location.Status == RoutingData.Models.Location.LOCATION_STATUSES[0]).
                FirstOrDefaultAsync();
            if (location == null)
            {
                return NotFound("Location not found.");
            }

            // before deleteing, make sure location isnt apart of any active delivery
            bool hasOngoingOrders = await _context.Orders
                .AnyAsync(order => order.LocationId == id && 
                    ( order.Status != Order.ORDER_STATUSES[2] && order.Status != Order.ORDER_STATUSES[3]  ) ); //Any that are not Delivered or cancelled
            //Unless cancelled or delivered, should not be able to set location to inactive

            if (hasOngoingOrders)
            {
                return BadRequest("Cannot delete location as it is associated with ongoing orders, please wait for orders to be completed and then try again.");
            }

            // finally, remove location (set to inactive)
            location.Status = RoutingData.Models.Location.LOCATION_STATUSES[1]; 
            _context.Entry(location).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Location deleted successfully" }); // Return a success message
        }

        private bool LocationExists(int id)
        {
            return (_context.Locations?.Any(e => e.Id == id)).GetValueOrDefault();
        }

#endif
    }
}
