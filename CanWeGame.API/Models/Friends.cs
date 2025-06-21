namespace CanWeGame.API.Models
{
    // This model represents a friendship relationship between two users.
    // It's a many-to-many relationship handled via an intermediary table.
    // Note: We'll enforce a unique constraint on (UserId1, UserId2) pair
    // to prevent duplicate friendships, and ensure UserId1 < UserId2
    // to prevent (A,B) and (B,A) from both existing.
    public class Friends
    {
        // Composite primary key (UserId1, UserId2) will be configured in DbContext
        public int UserId1 { get; set; } // The ID of the first user in the friendship (lower ID)
        public int UserId2 { get; set; } // The ID of the second user in the friendship (higher ID)

        public DateTime EstablishedDate { get; set; } = DateTime.UtcNow; // When the friendship was established

        // Navigation properties to the User entities involved in the friendship
        public User User1 { get; set; } = null!; // The first user
        public User User2 { get; set; } = null!; // The second user
    }
}