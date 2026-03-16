using System;

// This class represents a single user account in the system.
// It acts like a record in the Users table from the database design.

[Serializable]
public class UserAccount
{
    // Unique ID assigned to each user
    public int userId;

    // Username chosen by the player during sign-up
    public string username;

    // Hashed password (not stored as plain text for security)
    public string passwordHash;

    // Optional email associated with the account
    public string email;

    // Date the account was created
    public string dateCreated;

    // Last time the user successfully logged in
    public string lastLogin;
}