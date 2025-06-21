using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For AnyAsync
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Auth;
using CanWeGame.API.Services; // For PasswordHasher

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")] // Sets the base route for this controller (e.g., /api/Auth)
    [ApiController] // Indicates that this class is an API controller
    public class AuthController : ControllerBase // Base class for MVC controllers without view support
    {
        private readonly ApplicationDbContext _context;

        // Constructor for Dependency Injection.
        // ASP.NET Core will automatically provide an instance of ApplicationDbContext.
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto model)
        {
            // Basic validation provided by [Required] and other attributes in DTO
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Returns detailed validation errors
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                return Conflict("Username already exists."); // 409 Conflict
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return Conflict("Email already exists."); // 409 Conflict
            }

            // Hash the password using BCrypt
            string hashedPassword = PasswordHasher.HashPassword(model.Password);

            // Create a new User entity
            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = hashedPassword, // Store the hashed password
                CreatedDate = DateTime.UtcNow
            };

            // Add the new user to the database context
            _context.Users.Add(newUser);
            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return a 201 Created status code with the location of the newly created resource
            // and the user data (excluding password hash)
            // In a real app, you might return a stripped-down UserResponseDto here.
            return StatusCode(201, new { Message = "Registration successful!", UserId = newUser.Id });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by username
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);

            // If user not found OR password doesn't match
            if (user == null || !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                // Return 401 Unauthorized for either case for security reasons (don't reveal if username exists)
                return Unauthorized("Invalid username or password.");
            }

            // Placeholder for JWT Token Generation (we'll implement this later)
            // For now, just indicate success
            return Ok(new { Message = "Login successful!", Username = user.Username, UserId = user.Id });
        }
    }
}
