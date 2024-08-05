using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverAccountsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DriverAccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/DriverAccounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DriverAccount>>> GetDriverAccounts()
        {
          if (_context.DriverAccounts == null)
          {
              return NotFound();
          }
            return await _context.DriverAccounts.ToListAsync();
        }

        // GET: api/DriverAccounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DriverAccount>> GetDriverAccount(string id)
        {
          if (_context.DriverAccounts == null)
          {
              return NotFound();
          }
            var driverAccount = await _context.DriverAccounts.FindAsync(id);

            if (driverAccount == null)
            {
                return NotFound();
            }

            return driverAccount;
        }

        // PUT: api/DriverAccounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDriverAccount(string id, DriverAccount driverAccount)
        {
            if (id != driverAccount.Username)
            {
                return BadRequest();
            }

            _context.Entry(driverAccount).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DriverAccountExists(id))
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

        // POST: api/DriverAccounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DriverAccount>> PostDriverAccount(DriverAccount driverAccount)
        {
          if (_context.DriverAccounts == null)
          {
              return Problem("Entity set 'ApplicationDbContext.DriverAccounts'  is null.");
          }
            _context.DriverAccounts.Add(driverAccount);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (DriverAccountExists(driverAccount.Username))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetDriverAccount", new { id = driverAccount.Username }, driverAccount);
        }

        // DELETE: api/DriverAccounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDriverAccount(string id)
        {
            if (_context.DriverAccounts == null)
            {
                return NotFound();
            }
            var driverAccount = await _context.DriverAccounts.FindAsync(id);
            if (driverAccount == null)
            {
                return NotFound();
            }

            _context.DriverAccounts.Remove(driverAccount);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DriverAccountExists(string id)
        {
            return (_context.DriverAccounts?.Any(e => e.Username == id)).GetValueOrDefault();
        }
    }
}
