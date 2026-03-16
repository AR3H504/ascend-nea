using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script controls the login and sign-up user interface.
// It communicates with the AccountManager to create accounts
// and log users into the game.

public class LoginUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject signUpPanel;

    [Header("Login Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text loginFeedbackText;

    [Header("Sign Up Inputs")]
    public TMP_InputField signUpUsernameInput;
    public TMP_InputField signUpPasswordInput;
    public TMP_InputField signUpEmailInput;
    public TMP_Text signUpFeedbackText;

    // Switches to login screen
    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    // Switches to sign-up screen
    public void ShowSignUpPanel()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
    }

    // Called when login button is pressed
    public void OnLoginButtonPressed()
    {
        bool success = AccountManager.Instance.Login(
            usernameInput.text,
            passwordInput.text,
            out string message
        );

        // Display result message
        loginFeedbackText.text = message;

        // If login successful load main menu
        if (success)
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Called when create account button is pressed
    public void OnCreateAccountButtonPressed()
    {
        bool success = AccountManager.Instance.SignUp(
            signUpUsernameInput.text,
            signUpPasswordInput.text,
            signUpEmailInput.text,
            out string message
        );

        // Show result message
        signUpFeedbackText.text = message;

        // If account created successfully return to login
        if (success)
        {
            ShowLoginPanel();
        }
    }
}