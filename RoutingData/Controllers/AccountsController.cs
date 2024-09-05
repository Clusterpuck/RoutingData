using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
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
    public class AccountsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

#if OFFLINE_DATA

        private readonly OfflineDatabase _offlineDatabase;

        public AccountsController(IConfiguration configuration, OfflineDatabase offlineDatabase)
        {
            _configuration = configuration;
            _offlineDatabase = offlineDatabase;
        }

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            if (_offlineDatabase.Accounts == null)
            {
                return NotFound();
            }
            var adminAccounts = await _offlineDatabase.GetAccountsAsync();
            return Ok(adminAccounts);
        }

        // GET: api/Accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(string id)
        {
            if (_offlineDatabase.Accounts == null)
            {
                return NotFound();
            }
            var adminAccount = await _offlineDatabase.FindAccountAsync(id);

            if (adminAccount == null)
            {
                return NotFound();
            }

            return Ok(adminAccount);
        }

        // PUT: api/Accounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAccount(string id, Account adminAccount)
        {
            if (id != adminAccount.Username)
            {
                return BadRequest();
            }

            var existingAccount = await _offlineDatabase.FindAccountAsync(id);
            if (existingAccount == null)
            {
                return NotFound();
            }

            existingAccount.Password = adminAccount.Password;

            //await _offlineDatabase.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Accounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Account>> PostAccount(Account adminAccount)
        {
            if (_offlineDatabase.Accounts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Accounts' is null.");
            }

            await _offlineDatabase.AddAccountAsync(adminAccount);
            //await _offlineDatabase.SaveChangesAsync();

            return CreatedAtAction("GetAccount", new { id = adminAccount.Username }, adminAccount);
        }

        // POST: api/Accounts/authenticate
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequestDTO loginDetails)
        {
            if (loginDetails == null || string.IsNullOrEmpty(loginDetails.Username) || string.IsNullOrEmpty(loginDetails.Password))
            {
                return BadRequest("Invalid client request");
            }

            var user = _offlineDatabase.Accounts.FirstOrDefault(u => u.Username == loginDetails.Username && u.Password == loginDetails.Password);
            if (user == null)
            {
                return Unauthorized();
            }

            // Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]); // Use the same key as in Program.cs
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

        // DELETE: api/Accounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            if (_offlineDatabase.Accounts == null)
            {
                return NotFound();
            }
            var adminAccount = await _offlineDatabase.FindAccountAsync(id);
            if (adminAccount == null)
            {
                return NotFound();
            }

            await _offlineDatabase.RemoveAccountAsync(adminAccount);
            //await _offlineDatabase.SaveChangesAsync();

            return NoContent();
        }

        private bool AccountExists(string id)
        {
            return (_offlineDatabase.Accounts?.Any(e => e.Username == id)).GetValueOrDefault();
        }

        [ApiController]
        [Route("api/[controller]")]
        [Authorize]
        public class AuthController : ControllerBase
        {
            private readonly OfflineDatabase _offlineDatabase;

            public AuthController(OfflineDatabase offlineDatabase)
            {
                _offlineDatabase = offlineDatabase;
            }

            [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] AuthenticateRequestDTO request)
            {
                var admin = await _offlineDatabase.FindAccountAsync(request.Username);
                if (admin != null && admin.Password == request.Password)
                {
                    // Generate and return a token
                    return Ok(new { Token = "your-generated-token" });
                }
                return Unauthorized();
            }
        }

    }

#else
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }


#endif

    }
}


