namespace CanWeGame.API.Models
{
    public class Schedule 
    {
        public int Id { get; set; } // Primary Key

        public int UserId { get; set; } // Foreign Key to User.Id
        public string Username { get; set; } = string.Empty; // User's username at the time of schedule creation/update

        public string GameTitle { get; set; } = string.Empty; // Game title (your 'game' column)
        public DateTime StartTime { get; set; } // Stores the start time of the game
        public DateTime EndTime { get; set; }   // Stores the end time of the game
        // This will be stored as a JSON string in the database using a ValueConverter
        public List<string> DaysOfWeek { get; set; } = new List<string>(); // e.g., ["M", "T", "W"]

        public bool IsWeekly { get; set; } // Your 'weekly' column (true or false)

        public string? Description { get; set; } // Optional description

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Automatically set creation date

        // Navigation property
        // Each gaming schedule belongs to one user
        public User User { get; set; } = null!; // Non-nullable navigation property
    }
}