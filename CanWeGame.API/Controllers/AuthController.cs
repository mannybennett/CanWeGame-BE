using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt; // For JwtSecurityToken, JwtSecurityTokenHandler
using System.Security.Claims; // For Claims, ClaimTypes
using System.Text; // For Encoding
using Microsoft.IdentityModel.Tokens; // For SymmetricSecurityKey, SigningCredentials
using Microsoft.Extensions.Configuration; // For IConfiguration
using System.ComponentModel.DataAnnotations;
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Auth;
using CanWeGame.API.Services; // For PasswordHasher

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // To access appsettings.json for JWT settings

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
        {
            // Initial Model State Validation (from Data Annotations)
            // This handles [Required], [EmailAddress] (for basic format), [StringLength]
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Trim leading/trailing whitespace from inputs
            //    This is crucial for preventing subtle issues with copy-pasting
            var username = model.Username.Trim();
            var email = model.Email.Trim();
            var password = model.Password; // Password will be trimmed below if needed

            // Specific Backend Validation for Internal Spaces (Username)
            // Even with client-side, always re-validate critical rules on backend.
            if (username.Contains(' '))
            {
                // Add a model state error for consistency with other validation errors
                ModelState.AddModelError("Username", "Username cannot contain spaces.");
                return BadRequest(ModelState);
            }

            // Validate Email (After trimming, ensure it's still valid if trimming somehow affected it,
            // though [EmailAddress] should catch most internal space issues already).
            // This is a redundant check if [EmailAddress] works as expected, but safe.
            // A simple EmailAddressAttribute instance can be used for runtime validation.
            if (!new EmailAddressAttribute().IsValid(email))
            {
                 ModelState.AddModelError("Email", "Invalid email format after trimming.");
                 return BadRequest(ModelState);
            }


            // Check for existing Username (using the trimmed version for consistency)
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return Conflict("Username already exists.");
            }

            // Check for existing Email (using the trimmed version for consistency)
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict("Email already exists.");
            }
            string hashedPassword = PasswordHasher.HashPassword(password.Trim()); // Trim leading/trailing spaces for password

            // Create new User object
            var newUser = new User
            {
                Username = username, // Use the trimmed username
                Email = email,       // Use the trimmed email
                PasswordHash = hashedPassword,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { Message = "Registration successful!", UserId = newUser.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            // --- JWT Token Generation ---
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var expiryMinutesString = _configuration["Jwt:ExpiryMinutes"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(expiryMinutesString))
            {
                throw new InvalidOperationException("JWT configuration is missing in appsettings.json.");
            }

            int expiryMinutes = int.Parse(expiryMinutesString);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Define claims to be included in the token (e.g., User ID, Username)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Standard claim for user ID
                new Claim(ClaimTypes.Name, user.Username), // Standard claim for username
                new Claim(JwtRegisteredClaimNames.Sub, user.Username), // Subject claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes), // Token expiration
                signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);
            // --- End JWT Token Generation ---

            return Ok(new { Token = tokenString, Username = user.Username, UserId = user.Id });
        }
    }
}