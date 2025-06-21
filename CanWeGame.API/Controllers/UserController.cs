using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Users;

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All endpoints in this controller require authentication
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                throw new InvalidOperationException("User ID claim not found or invalid.");
            }
            return userId;
        }

        // GET: api/Users/all
        // Get a list of all registered users (for searching friends, etc.)
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto // Map to DTO to avoid exposing password hash
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    CreatedDate = u.CreatedDate
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/Users/{id}
        // Get details of a specific user by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedDate = user.CreatedDate
            };

            return Ok(responseDto);
        }


        // GET: api/Users/friends
        // Get the list of friends for the current user
        [HttpGet("friends")]
        public async Task<IActionResult> GetMyFriends()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Get friendships where the current user is User1 or User2
                var friendships = await _context.Friends
                    .Where(f => f.UserId1 == currentUserId || f.UserId2 == currentUserId)
                    .Include(f => f.User1) // Eager load User1 details
                    .Include(f => f.User2) // Eager load User2 details
                    .ToListAsync();

                var friends = new List<UserResponseDto>();

                foreach (var friendship in friendships)
                {
                    // Add the "other" user in the friendship to the friends list
                    if (friendship.UserId1 == currentUserId)
                    {
                        friends.Add(new UserResponseDto
                        {
                            Id = friendship.User2.Id,
                            Username = friendship.User2.Username,
                            Email = friendship.User2.Email,
                            CreatedDate = friendship.User2.CreatedDate
                        });
                    }
                    else // friendship.UserId2 == currentUserId
                    {
                        friends.Add(new UserResponseDto
                        {
                            Id = friendship.User1.Id,
                            Username = friendship.User1.Username,
                            Email = friendship.User1.Email,
                            CreatedDate = friendship.User1.CreatedDate
                        });
                    }
                }

                return Ok(friends);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving friends.");
            }
        }

        // POST: api/Users/friends/add
        // Add a new friend for the current user
        [HttpPost("friends/add")]
        public async Task<IActionResult> AddFriend([FromBody] FriendRequestDto model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                int friendId;
                User? friendUser;

                // Try to parse as ID first, then search by username
                if (int.TryParse(model.FriendIdentifier, out int parsedId))
                {
                    friendUser = await _context.Users.FindAsync(parsedId);
                    friendId = parsedId;
                }
                else
                {
                    friendUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.FriendIdentifier);
                    friendId = friendUser?.Id ?? 0; // Use 0 if not found
                }

                if (friendUser == null)
                {
                    return NotFound("Friend user not found.");
                }

                if (currentUserId == friendId)
                {
                    return BadRequest("Cannot add yourself as a friend.");
                }

                // Ensure UserId1 is always the smaller ID and UserId2 is the larger ID
                // This prevents duplicate entries like (1,2) and (2,1)
                var user1Id = Math.Min(currentUserId, friendId);
                var user2Id = Math.Max(currentUserId, friendId);

                // Check if friendship already exists
                var existingFriendship = await _context.Friends
                    .AnyAsync(f => f.UserId1 == user1Id && f.UserId2 == user2Id);

                if (existingFriendship)
                {
                    return Conflict("Friendship already exists.");
                }

                var newFriendship = new Friends
                {
                    UserId1 = user1Id,
                    UserId2 = user2Id,
                    EstablishedDate = DateTime.UtcNow
                };

                _context.Friends.Add(newFriendship);
                await _context.SaveChangesAsync();

                return StatusCode(201, new { Message = $"Friendship established with {friendUser.Username}." });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while adding friend.");
            }
        }

        // DELETE: api/Users/friends/remove
        // Remove a friend for the current user
        [HttpDelete("friends/remove")]
        public async Task<IActionResult> RemoveFriend([FromBody] FriendRequestDto model)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                int friendId;
                User? friendUser;

                if (int.TryParse(model.FriendIdentifier, out int parsedId))
                {
                    friendUser = await _context.Users.FindAsync(parsedId);
                    friendId = parsedId;
                }
                else
                {
                    friendUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.FriendIdentifier);
                    friendId = friendUser?.Id ?? 0;
                }

                if (friendUser == null)
                {
                    return NotFound("Friend user not found.");
                }

                // Determine the correct UserId1 and UserId2 for lookup
                var user1Id = Math.Min(currentUserId, friendId);
                var user2Id = Math.Max(currentUserId, friendId);

                var friendshipToRemove = await _context.Friends
                    .SingleOrDefaultAsync(f => f.UserId1 == user1Id && f.UserId2 == user2Id);

                if (friendshipToRemove == null)
                {
                    return NotFound("Friendship not found.");
                }

                _context.Friends.Remove(friendshipToRemove);
                await _context.SaveChangesAsync();

                return NoContent(); // 204 No Content
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while removing friend.");
            }
        }
    }
}