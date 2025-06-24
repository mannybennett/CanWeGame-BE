using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Required for User.FindFirstValue
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Schedules;

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        // ApplicationDbContext is a class that inherits from Microsoft.EntityFrameworkCore.DbContext.
        // DbContext is the primary class in Entity Framework Core (EF Core) that represents a session with the database.
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context) // an instance of the database
        {
            _context = context;
        }

        // Helper method to get the current authenticated user's ID from the JWT claims
        private int GetCurrentUserId()
        {
            // This will now correctly read the user ID from the authenticated JWT token.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                // If this exception is thrown, it means the [Authorize] attribute failed,
                // or the JWT token somehow didn't contain the NameIdentifier claim.
                throw new InvalidOperationException("User ID claim not found or invalid in authenticated context.");
            }
            return userId;
        }

        // GET: api/Schedule/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMySchedules()
        {
            try
            {
                var currentUserId = GetCurrentUserId(); // Gets ID from JWT claims

                var schedules = await _context.Schedules // Use Schedules DbSet
                    .Where(s => s.UserId == currentUserId)
                    .Select(s => new ScheduleResponseDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        Username = s.Username,
                        GameTitle = s.GameTitle,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        DaysOfWeek = s.DaysOfWeek, // List<string> will be serialized
                        IsWeekly = s.IsWeekly,
                        Description = s.Description,
                        CreatedDate = s.CreatedDate
                    })
                    .ToListAsync();

                return Ok(schedules);
            }
            catch (InvalidOperationException ex)
            {
                // This error now implies an issue with authentication context/claims
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving schedules.");
            }
        }

        // GET: api/Schedule/user/{userId} (for friends' schedules)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserSchedules(int userId)
        {
            // You would add explicit authorization logic here if needed (e.g., check if current user is friends with {userId})
            // For now, it's accessible by any authenticated user.

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            var schedules = await _context.Schedules // Use Schedules DbSet
                .Where(s => s.UserId == userId)
                .Select(s => new ScheduleResponseDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Username = s.Username,
                    GameTitle = s.GameTitle,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DaysOfWeek = s.DaysOfWeek,
                    IsWeekly = s.IsWeekly,
                    Description = s.Description,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync();

            return Ok(schedules);
        }

        // POST: api/Schedule
        [HttpPost]
        public async Task<IActionResult> AddSchedule([FromBody] ScheduleCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Input validation for days of week
            if (model.DaysOfWeek == null || model.DaysOfWeek.Count == 0)
            {
                return BadRequest("At least one day of the week must be provided.");
            }
            // Optional: Validate that days are from "M, T, W, Thu, F, Sat, Sun"
            var validDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "M", "T", "W", "THU", "F", "SAT", "SUN" };
            if (model.DaysOfWeek.Any(day => !validDays.Contains(day.ToUpper())))
            {
                return BadRequest("Invalid day of week provided. Valid options are M, T, W, Thu, F, Sat, Sun.");
            }

            try
            {
                var currentUserId = GetCurrentUserId(); // Gets ID from JWT claims
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser == null)
                {
                    return Unauthorized("Authenticated user not found in the database.");
                }

                var newSchedule = new Schedule // Use Schedule model
                {
                    UserId = currentUserId,
                    Username = currentUser.Username,
                    GameTitle = model.GameTitle,
                    StartTime = model.StartTime, // Assign new properties
                    EndTime = model.EndTime,     // Assign new properties
                    DaysOfWeek = model.DaysOfWeek, // Assign the list of strings
                    IsWeekly = model.Weekly,
                    Description = model.Description,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Schedules.Add(newSchedule); // Use Schedules DbSet
                await _context.SaveChangesAsync();

                var responseDto = new ScheduleResponseDto
                {
                    Id = newSchedule.Id,
                    UserId = newSchedule.UserId,
                    Username = newSchedule.Username,
                    GameTitle = newSchedule.GameTitle,
                    StartTime = newSchedule.StartTime,
                    EndTime = newSchedule.EndTime,
                    DaysOfWeek = newSchedule.DaysOfWeek,
                    IsWeekly = newSchedule.IsWeekly,
                    Description = newSchedule.Description,
                    CreatedDate = newSchedule.CreatedDate
                };

                return CreatedAtAction(nameof(GetMySchedules), new { id = newSchedule.Id }, responseDto);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while adding the schedule.");
            }
        }

        // PUT: api/Schedule/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Input validation for days of week
            if (model.DaysOfWeek == null || model.DaysOfWeek.Count == 0)
            {
                return BadRequest("At least one day of the week must be provided.");
            }
            var validDays = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "M", "T", "W", "THU", "F", "SAT", "SUN" };
            if (model.DaysOfWeek.Any(day => !validDays.Contains(day.ToUpper())))
            {
                return BadRequest("Invalid day of week provided. Valid options are M, T, W, Thu, F, Sat, Sun.");
            }

            try
            {
                var currentUserId = GetCurrentUserId(); // Gets ID from JWT claims

                var schedule = await _context.Schedules // Use Schedules DbSet
                    .SingleOrDefaultAsync(s => s.Id == id && s.UserId == currentUserId);

                if (schedule == null)
                {
                    return NotFound("Schedule not found or you do not have permission to update it.");
                }

                schedule.GameTitle = model.GameTitle;
                schedule.StartTime = model.StartTime;
                schedule.EndTime = model.EndTime;
                schedule.DaysOfWeek = model.DaysOfWeek;
                schedule.IsWeekly = model.Weekly;
                schedule.Description = model.Description;

                _context.Schedules.Update(schedule); // Use Schedules DbSet
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the schedule.");
            }
        }

        // DELETE: api/Schedule/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId(); // Gets ID from JWT claims

                var schedule = await _context.Schedules // Use Schedules DbSet
                    .SingleOrDefaultAsync(s => s.Id == id && s.UserId == currentUserId);

                if (schedule == null)
                {
                    return NotFound("Schedule not found or you do not have permission to delete it.");
                }

                _context.Schedules.Remove(schedule); // Use Schedules DbSet
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while deleting the schedule.");
            }
        }
    }
}