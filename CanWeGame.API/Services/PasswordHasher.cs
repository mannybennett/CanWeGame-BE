namespace CanWeGame.API.Services
{
    // A simple utility class for hashing and verifying passwords using BCrypt.
    // This abstracts away the BCrypt implementation details from your controllers.
    public static class PasswordHasher
    {
        // Hashes a plain text password and returns the hashed string.
        // BCrypt automatically generates a salt internally and includes it in the hash.
        public static string HashPassword(string password)
        {
            // Default work factor is 10. You can adjust this for more security (higher number)
            // or faster hashing (lower number), but generally 10-12 is a good starting point.
            // Higher work factors increase CPU cost, making brute-force attacks harder.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Verifies a plain text password against a stored hashed password.
        // BCrypt automatically extracts the salt from the hash for verification.
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}