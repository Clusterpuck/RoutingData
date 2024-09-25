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

            return Created("", adminAccount);
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


        /// <summary>
        /// Method <c>PostAccount</c> confirms the provided account has all valid details
        /// Then adds the Active status and adds the product to the database
        /// </summary>
        /// <param name="inAccount"></param>
        /// <returns></returns>
        // POST: api/Accounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Account>> PostAccount(AccountInDTO inAccount)
        {
            if (_context.Accounts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Accounts' is null.");
            }
            StringBuilder sb  = new StringBuilder();
            Account account = ValidateAndMakeNewAccount(inAccount, sb);
            if( account == null )
            {
                return Problem($"Invalid details provided: {sb.ToString()}");
            }
            try
            {
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Check for specific error related to unique constraint violation
                if (ex.InnerException != null && ex.InnerException.Message.Contains("PRIMARY KEY constraint"))
                {
                    return Conflict($"Account with username '{account.Username}' already exists.");
                }

                // Log and return a general error message
                return Problem($"An error occurred while trying to add the account. {ex.InnerException}" );
            }

            return CreatedAtAction("GetAccount", new { id = account.Username }, account);
        }

        /// <summary>
        /// Method <c>ValidateAndMakeNewAccount</c> confirms the details of Username, Password
        /// And Role are within the set requirements
        /// Returns null if they are not.
        /// </summary>
        /// <param name="inAccount"></param>
        /// <returns></returns>
        /// TODO Update to change to throw exception when invalid and not to assign null object
        private Account ValidateAndMakeNewAccount( AccountInDTO inAccount, StringBuilder sb )
        {
            Account newAccount = null;
            bool isValid = true;
            if( !IsValidEmail( inAccount.Username ) )
            {
                isValid = false;
                sb.Append("Invalid Username/Email provided, ");

            }
            if(inAccount.Password.Length < Account.PASSWORD_LENGTH)
            {
                isValid = false;
                sb.Append("Password is too short, ");

            }
            //Convert to all upper case to overcome case issues. 
            inAccount.Role = inAccount.Role.ToUpper();
            if (!Account.ACCOUNT_ROLES.Contains(inAccount.Role))
            {
                isValid = false;
                sb.Append($"{inAccount.Role} is not a valid role ");
            }
            if( isValid)
            {
                newAccount = new Account()
                {
                    Username = inAccount.Username,
                    Name = inAccount.Name,
                    Phone = inAccount.Phone,
                    Password = inAccount.Password,
                    Role = inAccount.Role,
                    Address = inAccount.Address,
                    Status = Account.ACCOUNT_STATUSES[0]
                };

            }
            return newAccount;

        }


        /// <summary>
        /// Method <c>IsValidEmail</c> determines if a provided string is a valid email form
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Method <c>GetAccounts</c> Determines if accounts are active
        /// Returns list of only active accounts
        /// </summary>
        /// <returns></returns>
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
            var accounts = await _context.Accounts
                .Where(a => a.Status == "Active")
                .ToListAsync();


            // check if the accounts list is empty
            if (!accounts.Any())
            {
                return NotFound("No accounts found.");
            }

            return Ok(accounts);
        }


        /// <summary>
        /// Method <c>GetAccount</c> returns an account if found
        /// Includes returning an Inactive account
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET: api/Accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(string id)
        {
            // check if the Accounts table exists in the database context
            if( !IsValidEmail(id))
            {
                return BadRequest("Not a valid email");
            }
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


        /// <summary>
        /// Method <c>PutAccount</c> Checks if the provided Username is in database
        /// Confirms all details of the new Account object are valid
        /// Then applies all fields except Status to the dbAccount record
        /// </summary>
        /// <param name="id"></param>
        /// <param name="inAccount"></param>
        /// <returns></returns>
        // PUT: api/Accounts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAccount(string id, AccountInDTO inAccount)
        {
            if( !IsValidEmail(id) || !IsValidEmail(inAccount.Username) )
            {
                return BadRequest("Not a valid email");
            }
            //check if the account id exists in database
            StringBuilder sb = new StringBuilder();
            Account updatedAccount = ValidateAndMakeNewAccount(inAccount, sb);
            
            if( updatedAccount == null )
            {
                return BadRequest($"Details not valid in provided account: {sb.ToString()}");
            }
            Account dbAccount = await _context.Accounts.FindAsync(id);
            if (dbAccount == null )
            {
                return NotFound($"No such account in database with id {id}.");
            }

            dbAccount.Username = updatedAccount.Username;
            dbAccount.Name = updatedAccount.Name;
            dbAccount.Phone = updatedAccount.Phone;
            dbAccount.Password = updatedAccount.Password;
            dbAccount.Role = updatedAccount.Role;
            dbAccount.Address = updatedAccount.Address;

            // update the account in the context and save changes to the database
            _context.Entry(dbAccount).State = EntityState.Modified;

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

            return Created("", dbAccount);
        }


        /// <summary>
        /// Method <c>Authenticate</c> determines if the Authentication values provided are valid
        /// If so, creates and returns a JWT token
        /// Otherwise returns the UnAuthorised State
        /// </summary>
        /// <param name="loginDetails"></param>
        /// <returns></returns>
        // POST: api/Accounts/authenticate
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequestDTO loginDetails)
        {
            // validate the login details (empty & null checks)
            if (loginDetails == null || string.IsNullOrEmpty(loginDetails.Username) || string.IsNullOrEmpty(loginDetails.Password))
            {
                return BadRequest("Invalid client request");
            }

            if (!IsValidEmail(loginDetails.Username) || loginDetails.Password.Length < Account.PASSWORD_LENGTH)
            {
                return BadRequest("Invalid request data");
            }

            // check if the user exists in the database with matching username and password
            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.Username == loginDetails.Username && u.Password == loginDetails.Password);

            if (user == null)
            {
                // return as unauthorized if the user does not exist or credentials are wrong
                return Unauthorized();
            }

            // generate JWT Token and include the user's role in the token claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role) // add role to JWT claims
            }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return the token along with the user's role
            return Ok(new { Token = tokenString, Role = user.Role });
        }


        /// <summary>
        /// Method <c>DoesAccountExist</c> helper method to determine existance of Account in database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // check if an account exists in the database (online db version)
        private bool DoesAccountExist(string id)
        {
            return _context.Accounts.Any(e => e.Username == id);
        }

        /// <summary>
        /// Method <c>DeleteAccount</c> If found, sets the Account status to inactive
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

            //never actually deleting accounts, just sets status to InActive
            account.Status = Account.ACCOUNT_STATUSES[1];
            // update the account in the context and save changes to the database
            _context.Entry(account).State = EntityState.Modified;

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

            return Ok(new { message = "Account deleted successfully" }); // Return a success message
        }



        /// <summary>
        /// <class>AuthController</class>
        /// Used to authenticate users and generate the JWT tokens. 
        /// </summary>
        [ApiController]
        [Route("api/[controller]")]
        //[Authorize]
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

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
        {
            // Validate input data
            if (string.IsNullOrEmpty(changePasswordDTO.Username) ||
                string.IsNullOrEmpty(changePasswordDTO.CurrentPassword) ||
                string.IsNullOrEmpty(changePasswordDTO.NewPassword))
            {
                return BadRequest("Invalid request data");
            }

            // Validate if the username is a valid email (optional depending on your system requirements)
            if (!IsValidEmail(changePasswordDTO.Username))
            {
                return BadRequest("Invalid username/email format.");
            }

            // Check if the user exists in the database
            var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Username == changePasswordDTO.Username);
            if (user == null)
            {
                return NotFound("Account not found.");
            }

            // Verify that the current password matches the one in the database
            if (user.Password != changePasswordDTO.CurrentPassword)
            {
                return Unauthorized(new
                {
                    message = "Current password is incorrect."
                });
            }

            // Validate the new password (e.g., length check, complexity, etc.)
            if (changePasswordDTO.NewPassword.Length < 6)
            {
                return BadRequest("New password is too short.");
            }

            // Update the password
            user.Password = changePasswordDTO.NewPassword;
            _context.Entry(user).State = EntityState.Modified;

            try
            {
                // Save the changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle potential concurrency issues
                if (!DoesAccountExist(changePasswordDTO.Username))
                {
                    return NotFound("Account not found during update.");
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Password changed successfully." });
        }

        // change account state back to active
        [HttpPost("reactivate/{id}")]
        public async Task<IActionResult> ReactivateAccount(string id)
        {
            if (_context.Accounts == null)
            {
                return NotFound("Accounts data is not available.");
            }

            // Check if the account exists in the database
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return NotFound($"Account with ID {id} not found.");
            }

            // Check if the account is already active
            if (account.Status == Account.ACCOUNT_STATUSES[0])
            {
                return BadRequest("Account is already active.");
            }

            // Reactivate the account by setting its status to Active
            account.Status = Account.ACCOUNT_STATUSES[0]; // Assuming ACCOUNT_STATUSES[0] corresponds to "Active"

            // Update the account in the context and save changes to the database
            _context.Entry(account).State = EntityState.Modified;

            try
            {
                // Save changes asynchronously
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Check if the account still exists in the database
                if (!DoesAccountExist(id))
                {
                    return NotFound($"Account with ID {id} no longer exists.");
                }
                else
                {
                    throw; // If it's a different concurrency issue
                }
            }

            return Ok(new { message = "Account reactivated successfully." });
        }




#endif


    }
}


