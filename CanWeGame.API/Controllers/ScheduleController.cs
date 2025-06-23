using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // NOW REQUIRED AGAIN
using System.Security.Claims; // Required for User.FindFirstValue
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Schedules;

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // RE-ENABLED: All endpoints in this controller now require authentication
    public class ScheduleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
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
                        ScheduledTime = s.ScheduledTime,
                        ScheduleDate = s.ScheduledTime.Date,
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
                    ScheduledTime = s.ScheduledTime,
                    ScheduleDate = s.ScheduledTime.Date,
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

            try
            {
                var currentUserId = GetCurrentUserId(); // Gets ID from JWT claims
                var currentUser = await _context.Users.FindAsync(currentUserId);
                if (currentUser == null)
                {
                    return Unauthorized("Authenticated user not found in the database.");
                }

                bool isWeeklyBool = model.Weekly.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);

                var newSchedule = new Schedule // Use GamingSchedule model
                {
                    UserId = currentUserId,
                    Username = currentUser.Username,
                    GameTitle = model.GameTitle,
                    ScheduledTime = model.ScheduledTime,
                    ScheduleDate = model.ScheduledTime.Date,
                    IsWeekly = isWeeklyBool,
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

            try
            {
                var currentUserId = GetCurrentUserId(); // Gets ID from JWT claims

                var schedule = await _context.Schedules // Use Schedules DbSet
                    .SingleOrDefaultAsync(s => s.Id == id && s.UserId == currentUserId);

                if (schedule == null)
                {
                    return NotFound("Schedule not found or you do not have permission to update it.");
                }

                bool isWeeklyBool = model.Weekly.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);

                schedule.GameTitle = model.GameTitle;
                schedule.ScheduledTime = model.ScheduledTime;
                schedule.ScheduleDate = model.ScheduledTime.Date;
                schedule.IsWeekly = isWeeklyBool;
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
