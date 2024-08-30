using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
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
    public class AdminAccountsController : ControllerBase
    {
        // private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly OfflineDatabase _offlineDatabase;

        public AdminAccountsController(OfflineDatabase offlineDatabase, IConfiguration configuration)
        {
            _offlineDatabase = offlineDatabase;
            _configuration = configuration;

        }

        // GET: api/AdminAccounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminAccount>>> GetAdminAccounts()
        {
            if (_offlineDatabase.AdminAccounts == null)
            {
                return NotFound();
            }
            var adminAccounts = await _offlineDatabase.GetAdminAccountsAsync();
            return Ok(adminAccounts);
        }

        // GET: api/AdminAccounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminAccount>> GetAdminAccount(string id)
        {
            if (_offlineDatabase.AdminAccounts == null)
            {
                return NotFound();
            }
            var adminAccount = await _offlineDatabase.FindAdminAccountAsync(id);

            if (adminAccount == null)
            {
                return NotFound();
            }

            return Ok(adminAccount);
        }

        // PUT: api/AdminAccounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAdminAccount(string id, AdminAccount adminAccount)
        {
            if (id != adminAccount.Username)
            {
                return BadRequest();
            }

            var existingAccount = await _offlineDatabase.FindAdminAccountAsync(id);
            if (existingAccount == null)
            {
                return NotFound();
            }

            existingAccount.Password = adminAccount.Password;

            //await _offlineDatabase.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/AdminAccounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AdminAccount>> PostAdminAccount(AdminAccount adminAccount)
        {
            if (_offlineDatabase.AdminAccounts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.AdminAccounts' is null.");
            }

            await _offlineDatabase.AddAdminAccountAsync(adminAccount);
           //await _offlineDatabase.SaveChangesAsync();

            return CreatedAtAction("GetAdminAccount", new { id = adminAccount.Username }, adminAccount);
        }

    // POST: api/AdminAccounts/authenticate
    [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AdminAccount adminAccount)
        {
            if (adminAccount == null || string.IsNullOrEmpty(adminAccount.Username) || string.IsNullOrEmpty(adminAccount.Password))
            {
                return BadRequest("Invalid client request");
            }

            var user = _offlineDatabase.AdminAccounts.FirstOrDefault(u => u.Username == adminAccount.Username && u.Password == adminAccount.Password);
            if (user == null)
            {
                return Unauthorized();
            }

            // Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("gFcJCxAlDKg8G5i06vEFk2aee7L6fk8O"); // Use the same key as in Program.cs
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString });
        }

        // DELETE: api/AdminAccounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdminAccount(string id)
        {
            if (_offlineDatabase.AdminAccounts == null)
            {
                return NotFound();
            }
            var adminAccount = await _offlineDatabase.FindAdminAccountAsync(id);
            if (adminAccount == null)
            {
                return NotFound();
            }

            await _offlineDatabase.RemoveAdminAccountAsync(adminAccount);
            //await _offlineDatabase.SaveChangesAsync();

            return NoContent();
        }

        private bool AdminAccountExists(string id)
        {
            return (_offlineDatabase.AdminAccounts?.Any(e => e.Username == id)).GetValueOrDefault();
        }
    }
}
