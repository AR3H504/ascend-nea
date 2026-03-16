using System;
using System.Collections.Generic;

// This class acts as a container for all user accounts.
// Unity's JSON system cannot directly store a raw list,
// so I wrap the list inside this class.

[Serializable]
public class UserDatabase
{
    // List containing every user account registered in the game
    public List<UserAccount> users = new List<UserAccount>();
}