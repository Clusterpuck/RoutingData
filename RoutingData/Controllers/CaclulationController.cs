using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaclulationController : Controller
    {
        public readonly ApplicationDbContext _context;

        public CaclulationController( ApplicationDbContext context)
        {

            _context = context; 
        }

        // GET: api/CalculationStatus/{id}
        //To be used to determine if any calculations at all are running
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<int>> GetCalculationsCount()
        {
            if (_context.Calculations == null)
            {
                return NotFound();
            }
            //return if any calculation is running the number of calculations
            int count =  await _context.Calculations.
                Where( calculation => calculation.Status == Calculation.CALCULATION_STATUS[1]).
                CountAsync();

            return count;
        }

        // POST: api/Calculation
        // Initiates a new calculation and returns the unique request ID
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Calculation>> PostCalculation()
        {
            var calculation = new Calculation();
            //constructor should make id and start time for me

            _context.Calculations.Add(calculation);
            await _context.SaveChangesAsync();

            // Return the created calculation object, including its unique ID
            return CreatedAtAction(nameof(GetCalculationById), new { id = calculation.ID }, calculation);
        }

        // GET: api/Calculation/{id}
        // Retrieves the status of a specific calculation by ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Calculation>> GetCalculationById(string id)
        {
            if (_context.Calculations == null)
            {
                return NotFound();
            }

            var calculation = await _context.Calculations.FindAsync(id);

            if (calculation == null)
            {
                return NotFound();
            }

            return calculation;
        }

        // PUT: api/Calculation/{id}
        // Updates the specified calculation object
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCalculation(string id, Calculation updatedCalculation)
        {
            if (id != updatedCalculation.ID)
            {
                return BadRequest();
            }

            var existingCalculation = await _context.Calculations.FindAsync(id);
            if (existingCalculation == null)
            {
                return NotFound();
            }

            // Update the properties as needed
            existingCalculation.Status = updatedCalculation.Status;
            existingCalculation.EndTime = updatedCalculation.EndTime; 
            // No other properties should change

            // Save changes to the database
            _context.Entry(existingCalculation).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent(); // Return 204 No Content
        }
    }
}


