using System.Security.Cryptography;
using System.Text;

// This static class is responsible for hashing passwords
// before they are stored in the database.
// Hashing ensures that passwords are not saved in plain text.

public static class PasswordHasher
{
    // Converts a plain text password into a SHA-256 hash
    public static string HashPassword(string password)
    {
        // Create SHA256 hashing object
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convert password string to bytes
            byte[] bytes = Encoding.UTF8.GetBytes(password);

            // Compute hash
            byte[] hash = sha256.ComputeHash(bytes);

            // Convert hash bytes into hexadecimal string
            StringBuilder builder = new StringBuilder();

            foreach (byte b in hash)
            {
                builder.Append(b.ToString("x2"));
            }

            // Return hashed password
            return builder.ToString();
        }
    }
}