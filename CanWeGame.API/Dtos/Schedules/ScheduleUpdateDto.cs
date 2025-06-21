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

        [Required(ErrorMessage = "Scheduled time is required.")]
        public DateTime ScheduledTime { get; set; }

        [Required(ErrorMessage = "Weekly preference is required ('y' or 'n').")]
        public string Weekly { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}