using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // NOW REQUIRED AGAIN
using System.Security.Claims;
using CanWeGame.API.Data;
using CanWeGame.API.Models;
using CanWeGame.API.Dtos.Users;

namespace CanWeGame.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // RE-ENABLED: All endpoints in this controller now require authentication
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

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

        // GET: api/Users/all
        // Get a list of all registered users (for searching friends, etc.)
        // NOTE: This endpoint might ideally be public or have different authorization rules.
        // For now, it requires authentication as per the controller's [Authorize] attribute.
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
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
        [HttpGet("friends")]
        public async Task<IActionResult> GetMyFriends()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var friendships = await _context.Friendships // Using Friendships DbSet
                    .Where(f => f.UserId1 == currentUserId || f.UserId2 == currentUserId)
                    .Include(f => f.User1)
                    .Include(f => f.User2)
                    .ToListAsync();

                var friends = new List<UserResponseDto>();

                foreach (var friendship in friendships)
                {
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
                    else
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
        [HttpPost("friends/add")]
        public async Task<IActionResult> AddFriend([FromBody] FriendRequestDto model)
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

                if (currentUserId == friendId)
                {
                    return BadRequest("Cannot add yourself as a friend.");
                }

                var user1Id = Math.Min(currentUserId, friendId);
                var user2Id = Math.Max(currentUserId, friendId);

                var existingFriendship = await _context.Friendships // Using Friendships DbSet
                    .AnyAsync(f => f.UserId1 == user1Id && f.UserId2 == user2Id);

                if (existingFriendship)
                {
                    return Conflict("Friendship already exists.");
                }

                var newFriendship = new Friendship // Using Friendship model
                {
                    UserId1 = user1Id,
                    UserId2 = user2Id,
                    EstablishedDate = DateTime.UtcNow
                };

                _context.Friendships.Add(newFriendship); // Using Friendships DbSet
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

                var user1Id = Math.Min(currentUserId, friendId);
                var user2Id = Math.Max(currentUserId, friendId);

                var friendshipToRemove = await _context.Friendships // Using Friendships DbSet
                    .SingleOrDefaultAsync(f => f.UserId1 == user1Id && f.UserId2 == user2Id);

                if (friendshipToRemove == null)
                {
                    return NotFound("Friendship not found.");
                }

                _context.Friendships.Remove(friendshipToRemove); // Using Friendships DbSet
                await _context.SaveChangesAsync();

                return NoContent();
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