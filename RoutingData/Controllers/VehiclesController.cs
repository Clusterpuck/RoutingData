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
    public class VehiclesController : ControllerBase
    {
#if OFFLINE_DATA
        private readonly OfflineDatabase _offlineDatabase;

        public VehiclesController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            if (_offlineDatabase.Vehicles == null)
            {
                return NotFound();
            }
            return _offlineDatabase.Vehicles;
        }

        [HttpPost]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            if (_offlineDatabase.Vehicles == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Drivers'  is null.");
            }
            int newId = _offlineDatabase.Vehicles.Last().Id + 1;
            vehicle.Id = newId;
            _offlineDatabase.Vehicles.Add(vehicle);

            return Ok(vehicle);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            if (_offlineDatabase.Vehicles == null)
            {
                return NotFound();
            }
            var vehicle = _offlineDatabase.Vehicles.FirstOrDefault(x => x.Id == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            _offlineDatabase.Vehicles.Remove(vehicle);

            return NoContent();
        }






#else
        private readonly ApplicationDbContext _context;

        public VehiclesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Vehicles
        [Authorize]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
          if (_context.Vehicles == null)
          {
              return NotFound();
          }
            return await _context.Vehicles.ToListAsync();
        }


        /// <summary>
        /// Returns the number of available vehicles on that given date. 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        // GET: api/Vehicles
        [HttpGet("num-on-date/{date}")]
        [Authorize]
        public async Task<ActionResult<int>> GetVehiclesOnDate( DateTime date )
        {
            if (_context.Vehicles == null)
            {
                return NotFound();
            }
            //get number of routes for this date
            int routeCount = await _context.DeliveryRoutes.Where(route => (route.DeliveryDate.Date == date.Date)).CountAsync();

            //get number or drivers and vehicles that are active, take minimum of the two
            int activeVehicles = await GetMaxVehicles();

            //subtract the number of routes and return value
            return activeVehicles - routeCount;
        }


        /// <summary>
        /// Determines the maximum vehicles that can be used in one day
        /// Factors in number of drivers as well
        /// </summary>
        /// <returns>maxVehicles int</returns>
        private async Task<int> GetMaxVehicles()
        {
            // Get the count of drivers (Accounts with Role "Driver")
            var driverCount = await _context.Accounts
                .Where(account => (account.Role == Account.ACCOUNT_ROLES[0]) && //Only selecting driver role
                    (account.Status == Account.ACCOUNT_STATUSES[0])) //That is active
                .CountAsync();

            // Get the count of vehicles
            var vehicleCount = await _context.Vehicles
                .Where(vehicle => vehicle.Status == Vehicle.VEHICLE_STATUSES[0]) //Only selecting active vehicle
                .CountAsync();

            // Find the minimum of the driver count and vehicle count
            int maxVehicles = Math.Min(driverCount, vehicleCount);

            return maxVehicles;
        }

        // GET: api/Vehicles/{licensePlate}
        [HttpGet("{licensePlate}")]
        [Authorize]
        public async Task<ActionResult<Vehicle>> GetVehicle(string licensePlate)
        {
            if (_context.Vehicles == null)
            {
                return NotFound();
            }

            // Search for the vehicle using the licensePlate
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);

            if (vehicle == null)
            {
                return NotFound();
            }

            return vehicle;
        }


        // PUT: api/Vehicles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutVehicle(String id, VehicleInDTO vehicle)
        {
            if (id != vehicle.LicensePlate)
            {
                return BadRequest();
            }
            Vehicle newVehicle = new Vehicle()
            {
                LicensePlate = vehicle.LicensePlate,
                Status = Vehicle.VEHICLE_STATUSES[0]
            };

            _context.Entry(newVehicle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", vehicle);
        }

        // POST: api/Vehicles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Vehicle>> PostVehicle(VehicleInDTO vehicle)
        {
          if (_context.Vehicles == null)
          {
              return Problem("Entity set 'ApplicationDbContext.Vehicles'  is null.");
          }
            Vehicle newVehicle = new Vehicle()
            {
                LicensePlate = vehicle.LicensePlate,
                Status = Vehicle.VEHICLE_STATUSES[0]
            };

            _context.Vehicles.Add(newVehicle);

            try
            {
                await _context.SaveChangesAsync();

            }
            catch (DbUpdateException ex)
            {
                // Check for specific error related to unique constraint violation
                if (ex.InnerException != null && ex.InnerException.Message.Contains("PRIMARY KEY constraint"))
                {
                    return Conflict($"Vehicle with plate '{vehicle.LicensePlate}' already exists.");
                }

                return Problem($"An error occurred while trying to add");
                // Log and return a general error message the vehicle. {ex.InnerException}");
            }

            return CreatedAtAction(nameof(GetVehicle), new { licensePlate = vehicle.LicensePlate }, vehicle);
        }

        // DELETE: api/Vehicles/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteVehicle(string id)
        {
            if (_context.Vehicles == null)
            {
                return NotFound();
            }

            // Find vehicle by LicensePlate (which is now the string 'id')
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            // Find any routes that are associated with this vehicle
            List<DeliveryRoute> routes = await _context.DeliveryRoutes
                .Where(route => route.VehicleLicense == vehicle.LicensePlate)
                .ToListAsync();

            // Return an error if the vehicle is associated with any active routes
            if (routes.Any())
            {
                return BadRequest("Vehicle is associated with active route");
            }

            // Mark the vehicle status as 'deleted' (or equivalent status)
            vehicle.Status = Vehicle.VEHICLE_STATUSES[2]; // Assuming index 2 is the "deleted" status
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vehicle deleted successfully" });
        }

        // Check if vehicle exists using string (LicensePlate)
        private bool VehicleExists(string id)
        {
            return (_context.Vehicles?.Any(e => e.LicensePlate == id)).GetValueOrDefault();
        }

#endif
    }
}
