using UnityEngine;
using UnityEngine.SceneManagement;

// This script controls the pause menu system.
// It allows the player to pause the game, resume,
// restart from checkpoint, restart the level,
// or return to the main menu.

public class PauseMenu : MonoBehaviour
{
    // Reference to the pause menu UI panel
    // This is the PauseBackground object we created
    public GameObject pauseMenuUI;

    // Reference to the GameManager so we can access
    // the checkpoint respawn function
    public GameManager gameManager;

    // Reference to the main pause panel (Resume, Restart etc)
    public GameObject pausePanel;

    // Reference to the options panel
    public GameObject optionsPanel;

    // Tracks whether the game is currently paused
    private bool isPaused = false;

    void Update()
    {
        // Detect when the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If the game is already paused, resume it
            if (isPaused)
            {
                ResumeGame();
            }
            // Otherwise pause the game
            else
            {
                PauseGame();
            }
        }
    }

    // Pauses the game and shows the pause menu
    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);

        // Show pause buttons and hide options panel
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
    }

    // Resumes the game and hides the pause menu
    public void ResumeGame()
    {
        // Hide the pause menu UI
        pauseMenuUI.SetActive(false);

        // Resume normal game time
        Time.timeScale = 1f;

        // Update pause state
        isPaused = false;
    }

    // Opens the options panel
    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    // Returns from options to the pause menu
    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    // Sends the player back to their last checkpoint
    public void RestartFromCheckpoint()
    {
        // Ensure the game resumes before teleporting player
        Time.timeScale = 1f;

        // Hide the pause menu
        pauseMenuUI.SetActive(false);

        // Update pause state
        isPaused = false;

        // Call the GameManager checkpoint respawn function
        if (gameManager != null)
        {
            gameManager.RespawnAtCheckpoint();
        }
    }

    // Reloads the current level from the beginning
    public void RestartLevel()
    {
        Time.timeScale = 1f;

        if (gameManager != null)
        {
            gameManager.ResetToLevelStart();
        }

        GameManager.ForceExplicitLevelStartOnNextLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Loads the main menu scene
    public void ExitToMainMenu()
    {
        // Resume time before switching scenes
        Time.timeScale = 1f;

        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
