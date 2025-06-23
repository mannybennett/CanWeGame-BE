namespace CanWeGame.API.Models
{
    public class User
    {
        public int Id { get; set; } // Primary Key

        public string Username { get; set; } = string.Empty; // Non-nullable string, initialized to avoid warnings
        public string Email { get; set; } = string.Empty; // Non-nullable string
        public string PasswordHash { get; set; } = string.Empty; // Stores the hashed password
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Automatically set creation date

        // Navigation properties for relationships
        // A user can have many gaming schedules
        public ICollection<Schedule>? Schedules { get; set; }

        // A user can initiate many friendships (as the first user in the relationship)
        public ICollection<Friendship>? SentFriendships { get; set; }

        // A user can be the recipient in many friendships (as the second user in the relationship)
        public ICollection<Friendship>? ReceivedFriendships { get; set; }
    }
}