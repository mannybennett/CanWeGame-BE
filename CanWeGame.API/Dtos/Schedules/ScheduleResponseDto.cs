namespace CanWeGame.API.Dtos.Schedules
{
    // This DTO is what your API will send back to the frontend.
    // It's good practice to not expose all internal model properties.
    public class ScheduleResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty; // Include username for display
        public string GameTitle { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public DateTime ScheduleDate { get; set; }
        public bool IsWeekly { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}