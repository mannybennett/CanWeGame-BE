using System.ComponentModel.DataAnnotations;

namespace CanWeGame.API.Dtos.Schedules
{
    // This DTO can be similar to CreateDto, but all fields can be optional
    // if you want to allow partial updates (PATCH verb).
    // For PUT (full replacement), it would typically require all fields as in CreateDto.
    // For simplicity, let's make them required for PUT.
    public class ScheduleUpdateDto
    {
        [Required(ErrorMessage = "Game title is required.")]
        public string GameTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start time is required.")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeOnly EndTime { get; set; }

        [Required(ErrorMessage = "At least one day of the week is required.")]
        [MinLength(1, ErrorMessage = "At least one day of the week must be selected.")]
        public List<string> DaysOfWeek { get; set; } = [];

        [Required(ErrorMessage = "Weekly preference is required ('y' or 'n').")]
        public bool Weekly { get; set; }

        public string? Description { get; set; } // Optional -> "string?" means "can be null"
    }
}