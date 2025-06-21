using System.ComponentModel.DataAnnotations;

namespace CanWeGame.API.Dtos.Users
{
    // DTO for adding/deleting friends by username or ID
    public class FriendRequestDto
    {
        [Required(ErrorMessage = "Friend's username or ID is required.")]
        public string FriendIdentifier { get; set; } = string.Empty; // Could be username or ID string
    }
}