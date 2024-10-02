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
        public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
        {
          if (_context.Locations == null)
          {
              return NotFound();
          }
            return await _context.Locations.ToListAsync();
        }

        // GET: api/Locations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Location>> GetLocation(int id)
        {
          if (_context.Locations == null)
          {
              return NotFound();
          }
            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            return location;
        }

        // PUT: api/Locations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLocation(int id, LocationInDTO location)
        {
            Location dbLocation = await _context.Locations.FindAsync(id);
            if (dbLocation == null)
            {
                return BadRequest("Location ID does not exist");
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

            _context.Entry(location).State = EntityState.Modified;

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
        public async Task<ActionResult<Location>> PostLocation(LocationInDTO location)
        {
            if (_context.Locations == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Locations'  is null.");
            }
            Location dbLocation = new Location();
                dbLocation.Longitude = location.Longitude;
                dbLocation.Latitude = location.Latitude;
                dbLocation.Address = location.Address;
                dbLocation.Suburb = location.Suburb;
                dbLocation.State = location.State;
                dbLocation.PostCode = location.PostCode;
                dbLocation.Country = location.Country;
                dbLocation.Description = location.Description;
                dbLocation.Status = Location.LOCATION_STATUSES[0];
            _context.Locations.Add(dbLocation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLocation", new { id = dbLocation.Id }, dbLocation);
        }
        // DELETE: api/Locations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            // first check location table isnt empty
            if (_context.Locations == null)
            {
                return NotFound("Location data not available.");
            }

            // then find the location by the given ID
            var location = await _context.Locations.FindAsync(id);
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
            location.Status = Location.LOCATION_STATUSES[1]; 
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
