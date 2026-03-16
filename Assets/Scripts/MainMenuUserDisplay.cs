using TMPro;
using UnityEngine;

// This script displays the currently logged-in user's username
// and basic save information on the main menu screen.

public class MainMenuUserDisplay : MonoBehaviour
{
    // Reference to the welcome text
    public TMP_Text welcomeText;

    // Reference to the progress text
    public TMP_Text progressInfoText;

    void Start()
    {
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentUser != null)
        {
            welcomeText.text = "Welcome, " + AccountManager.Instance.CurrentUser.username;

            if (AccountManager.Instance.CurrentProgress != null)
            {
                ProgressData progress = AccountManager.Instance.CurrentProgress;

                progressInfoText.text =
                    "Deaths: " + progress.deaths +
                    " | Best Time: " + progress.bestTime;
            }
            else
            {
                progressInfoText.text = "No save data found.";
            }
        }
        else
        {
            welcomeText.text = "Welcome, Guest";
            progressInfoText.text = "No user logged in.";
        }
    }
}