namespace CanWeGame.API.Dtos.Users
{
    // DTO for returning user information, excluding sensitive data like password hash/salt
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}