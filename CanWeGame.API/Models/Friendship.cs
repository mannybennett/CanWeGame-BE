namespace CanWeGame.API.Models // Namespace adjusted to CanWeGame.API
{
    public class Friendship
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