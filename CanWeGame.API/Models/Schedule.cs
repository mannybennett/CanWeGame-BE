namespace CanWeGame.API.Models
{
    public class Schedule 
    {
        public int Id { get; set; } // Primary Key

        public int UserId { get; set; } // Foreign Key to User.Id
        public string Username { get; set; } = string.Empty; // User's username at the time of schedule creation/update

        public string GameTitle { get; set; } = string.Empty; // Game title (your 'game' column)
        public DateTime ScheduledTime { get; set; } // Specific time (your 'times' column)
        public DateTime ScheduleDate { get; set; } // Specific date (your 'days' column)

        // Using a boolean for 'weekly' is more robust than "y" or "n" strings.
        // It maps directly to true/false in C# and 0/1 in SQLite.
        public bool IsWeekly { get; set; } // Your 'weekly' column ("y" or "n")

        public string? Description { get; set; } // Optional description

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Automatically set creation date

        // Navigation property
        // Each gaming schedule belongs to one user
        public User User { get; set; } = null!; // Non-nullable navigation property
    }
}