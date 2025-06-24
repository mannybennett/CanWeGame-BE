using System.ComponentModel.DataAnnotations;

namespace CanWeGame.API.Dtos.Schedules
{
    public class ScheduleCreateDto
    {
        [Required(ErrorMessage = "Game title is required.")]
        public string GameTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start time is required.")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeOnly EndTime { get; set; }

        [Required(ErrorMessage = "At least one day of the week is required.")]
        [MinLength(1, ErrorMessage = "At least one day of the week must be selected.")]
        public List<string> DaysOfWeek { get; set; } = []; // e.g., ["M", "T", "W"]

        [Required(ErrorMessage = "Weekly preference is required ('y' or 'n').")]
        public bool Weekly { get; set; }

        public string? Description { get; set; } // Optional
    }
}