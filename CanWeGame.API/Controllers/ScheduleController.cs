using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Still needed for User.FindFirstValue, though now it's for the commented-out original logic
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Schedules; // Corrected namespace for DTOs

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // REMOVED/COMMENTED OUT THIS LINE as requested
    public class ScheduleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to get the current user's ID.
        // TEMPORARY: Since authentication is disabled, this will return a fixed ID for testing.
        // Ensure a user with this ID exists in your database (e.g., by registering one first).
        // Once JWT authentication is re-introduced, this method will be restored to read from JWT claims.
        private int GetCurrentUserId()
        {
            // You MUST register a user first (e.g., via /api/Auth/register)
            // and then use their actual ID here for testing.
            // For example, if the first registered user gets ID 1, then use 1.
            return 1; // <--- Set this to a known User ID from your database for testing.
            // Original logic (for JWT-based authentication):
            // var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            // {
            //     // Changed to BadRequest as it's not an authentication failure without JWT setup
            //     throw new InvalidOperationException("User ID context not available. Please ensure a user is logged in or for testing, ensure GetCurrentUserId returns a valid ID.");
            // }
            // return userId;
        }

        // GET: api/Schedule/my
        // Get schedules for the current user (based on the fixed GetCurrentUserId for now)
        [HttpGet("my")]
        public async Task<IActionResult> GetMySchedules()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var schedules = await _context.Schedules // Assuming DbSet is named 'Schedules' in your DbContext
                    .Where(s => s.UserId == currentUserId)
                    // Include the User navigation property if you need user details (e.g., email)
                    // .Include(s => s.User)
                    .Select(s => new ScheduleResponseDto // Map to DTO
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        Username = s.Username, // Assuming this is kept updated
                        GameTitle = s.GameTitle,
                        ScheduledTime = s.ScheduledTime,
                        ScheduleDate = s.ScheduledTime.Date, // Use the date part of ScheduledTime
                        IsWeekly = s.IsWeekly,
                        Description = s.Description,
                        CreatedDate = s.CreatedDate
                    })
                    .ToListAsync();

                return Ok(schedules);
            }
            catch (InvalidOperationException ex)
            {
                // Changed from Unauthorized to BadRequest as no JWT authentication is set up
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using ILogger)
                return StatusCode(500, "An error occurred while retrieving schedules.");
            }
        }

        // GET: api/Schedule/user/{userId}
        // Get schedules for a specific user (e.g., a friend)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserSchedules(int userId)
        {
            // In a real application, you'd add authorization logic here
            // to check if the requesting user is allowed to view this user's schedules.

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var schedules = await _context.Schedules // Assuming DbSet is named 'Schedules' in your DbContext
                .Where(s => s.UserId == userId)
                .Select(s => new ScheduleResponseDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Username = s.Username,
                    GameTitle = s.GameTitle,
                    ScheduledTime = s.ScheduledTime,
                    ScheduleDate = s.ScheduledTime.Date, // Use the date part of ScheduledTime
                    IsWeekly = s.IsWeekly,
                    Description = s.Description,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync();

            return Ok(schedules);
        }

        // POST: api/Schedule
        // Add a new gaming schedule for the current user
        [HttpPost]
        public async Task<IActionResult> AddSchedule([FromBody] ScheduleCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId(); // Will use temporary fixed ID
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser == null)
                {
                    // Changed from Unauthorized to BadRequest as no JWT authentication is set up
                    return BadRequest("User not found for the specified ID. Please ensure user with this ID exists.");
                }

                // Convert "y"/"n" string to boolean
                bool isWeeklyBool = model.Weekly.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);

                var newSchedule = new Schedule
                {
                    UserId = currentUserId,
                    Username = currentUser.Username, // Get the current username from the User model
                    GameTitle = model.GameTitle,
                    ScheduledTime = model.ScheduledTime,
                    ScheduleDate = model.ScheduledTime.Date, // Extract just the date part
                    IsWeekly = isWeeklyBool,
                    Description = model.Description,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Schedules.Add(newSchedule); // Assuming DbSet is named 'Schedules' in your DbContext
                await _context.SaveChangesAsync();

                // Return a DTO of the newly created schedule
                var responseDto = new ScheduleResponseDto
                {
                    Id = newSchedule.Id,
                    UserId = newSchedule.UserId,
                    Username = newSchedule.Username,
                    GameTitle = newSchedule.GameTitle,
                    ScheduledTime = newSchedule.ScheduledTime,
                    ScheduleDate = newSchedule.ScheduleDate,
                    IsWeekly = newSchedule.IsWeekly,
                    Description = newSchedule.Description,
                    CreatedDate = newSchedule.CreatedDate
                };

                return CreatedAtAction(nameof(GetMySchedules), new { id = newSchedule.Id }, responseDto);
            }
            catch (InvalidOperationException ex)
            {
                // Changed from Unauthorized to BadRequest
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while adding the schedule.");
            }
        }

        // PUT: api/Schedule/{id}
        // Update an existing schedule for the current user
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId(); // Will use temporary fixed ID

                // Find the schedule by ID and ensure it belongs to the current user
                var schedule = await _context.Schedules // Assuming DbSet is named 'Schedules' in your DbContext
                    .SingleOrDefaultAsync(s => s.Id == id && s.UserId == currentUserId);

                if (schedule == null)
                {
                    return NotFound("Schedule not found or you do not have permission to update it.");
                }

                // Convert "y"/"n" string to boolean
                bool isWeeklyBool = model.Weekly.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);

                // Update properties
                schedule.GameTitle = model.GameTitle;
                schedule.ScheduledTime = model.ScheduledTime;
                schedule.ScheduleDate = model.ScheduledTime.Date; // Update date part too
                schedule.IsWeekly = isWeeklyBool;
                schedule.Description = model.Description;
                // CreatedDate should not change on update

                _context.Schedules.Update(schedule); // Mark as modified
                await _context.SaveChangesAsync();

                return NoContent(); // 204 No Content for successful update
            }
            catch (InvalidOperationException ex)
            {
                // Changed from Unauthorized to BadRequest
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the schedule.");
            }
        }

        // DELETE: api/Schedule/{id}
        // Delete a schedule for the current user
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId(); // Will use temporary fixed ID

                // Find the schedule by ID and ensure it belongs to the current user
                var schedule = await _context.Schedules // Assuming DbSet is named 'Schedules' in your DbContext
                    .SingleOrDefaultAsync(s => s.Id == id && s.UserId == currentUserId);

                if (schedule == null)
                {
                    return NotFound("Schedule not found or you do not have permission to delete it.");
                }

                _context.Schedules.Remove(schedule); // Mark for removal
                await _context.SaveChangesAsync();

                return NoContent(); // 204 No Content for successful deletion
            }
            catch (InvalidOperationException ex)
            {
                // Changed from Unauthorized to BadRequest
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while deleting the schedule.");
            }
        }
    }
}
