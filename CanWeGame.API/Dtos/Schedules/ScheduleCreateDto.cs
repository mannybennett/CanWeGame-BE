using System.ComponentModel.DataAnnotations;

namespace CanWeGame.API.Dtos.Schedules
{
    public class ScheduleCreateDto
    {
        [Required(ErrorMessage = "Game title is required.")]
        public string GameTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scheduled time is required.")]
        public DateTime ScheduledTime { get; set; } // Will include both date and time

        // Note: We use bool for IsWeekly, but for the user input, it might be string "y"/"n"
        // We'll convert this in the controller.
        [Required(ErrorMessage = "Weekly preference is required ('y' or 'n').")]
        public string Weekly { get; set; } = string.Empty; // User input as "y" or "n"

        public string? Description { get; set; } // Optional
    }
}