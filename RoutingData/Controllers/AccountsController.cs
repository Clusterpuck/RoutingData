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

        public AccountsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        // POST: api/Accounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Account>> PostAccount(Account account)
        {
            if (_context.Accounts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Accounts' is null.");
            }

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAccount", new { id = account.Username }, account);
        }

        // GET: api/Accounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            // check if the accounts table exists and has data
            if (_context.Accounts == null)
            {
                return NotFound("Accounts data is not available.");
            }

            // fetch the accounts from the database asynchronously
            var accounts = await _context.Accounts.ToListAsync();

            // check if the accounts list is empty
            if (!accounts.Any())
            {
                return NotFound("No accounts found.");
            }

            return Ok(accounts);
        }

        // GET: api/Accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(string id)
        {
            // check if the Accounts table exists in the database context
            if (_context.Accounts == null)
            {
                return NotFound("Accounts data is not available.");
            }

            // retrieve the account with the specified ID from the database
            var account = await _context.Accounts.FindAsync(id);

            // check if the account was found
            if (account == null)
            {
                return NotFound($"Account with ID {id} not found.");
            }

            return Ok(account);
        }

        // PUT: api/Accounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAccount(string id, Account adminAccount)
        {
            // check if the provided ID matches the account's username
            if (id != adminAccount.Username)
            {
                return BadRequest("ID does not match the account username.");
            }

            // retrieve the existing account from the database
            var existingAccount = await _context.Accounts.FindAsync(id);
            if (existingAccount == null)
            {
                return NotFound($"Account with ID {id} not found.");
            }

            existingAccount.Password = adminAccount.Password;

            // update the account in the context and save changes to the database
            _context.Entry(existingAccount).State = EntityState.Modified;

            try
            {
                // save changes asynchronously
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // check if the account still exists in the database
                if (!DoesAccountExist(id))
                {
                    return NotFound($"Account with ID {id} no longer exists.");
                }
                else
                {
                    throw; // if it's a different concurrency issue
                }
            }

            return NoContent(); // return 204 No Content on success
        }

        // POST: api/Accounts/authenticate
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequestDTO loginDetails)
        {
            // validate the login details (empty & null checks)
            if (loginDetails == null || string.IsNullOrEmpty(loginDetails.Username) || string.IsNullOrEmpty(loginDetails.Password))
            {
                return BadRequest("Invalid client request");
            }

            // check if the user exists in the database with matching username and password
            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == loginDetails.Username && u.Password == loginDetails.Password);

            if (user == null)
            {
                // return as unauthorized if the user does not exist or credentials are wrong
                return Unauthorized();
            }

            // generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]); // this key matches the configuration (the key in Program.cs) !! has to match !!
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

            // return the generated token
            return Ok(new { Token = tokenString });
        }


        // check if an account exists in the database (online db version)
        private bool DoesAccountExist(string id)
        {
            return _context.Accounts.Any(e => e.Username == id);
        }


        // DELETE: api/Accounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            if (_context.Accounts == null)
            {
                return NotFound();
            }
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AccountExists(string id)
        {
            return (_context.Accounts?.Any(e => e.Username == id)).GetValueOrDefault();
        }

        [ApiController]
        [Route("api/[controller]")]
        [Authorize]
        public class AuthController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public AuthController(ApplicationDbContext context)
            {
                _context = context;
            }

            [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] AuthenticateRequestDTO request)
            {
                var admin = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == request.Username);
                if (admin != null && admin.Password == request.Password)
                {
                    // Generate and return a token
                    return Ok(new { Token = "your-generated-token" });
                }
                return Unauthorized();
            }
        }


#endif

    }
}


