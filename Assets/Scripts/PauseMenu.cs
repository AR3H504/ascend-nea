using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    // Optional pause-menu volume slider. If not assigned, it is found automatically.
    public Slider volumeSlider;

    // Optional pause-menu toggle for enabling/disabling checkpoints.
    public Toggle checkpointsToggle;

    // Optional checkpoint restart button so it can be disabled when checkpoints are off.
    public Button checkpointButton;

    // Optional button and label for toggling checkpoints from the options panel.
    public Button checkpointsToggleButton;
    public Graphic checkpointsToggleStatusGraphic;

    // Tracks whether the game is currently paused
    private bool isPaused = false;

    private void Start()
    {
        InitializeVolumeControls();
        InitializeCheckpointControls();
    }

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

        // Allow a quick keyboard restart from the latest checkpoint.
        if (Input.GetKeyDown(KeyCode.F))
        {
            RestartFromCheckpoint();
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
        RefreshCheckpointControls();
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
        InitializeVolumeControls();
        InitializeCheckpointControls();
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
        if (!Checkpoint.AreCheckpointsEnabled())
        {
            return;
        }

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

    private void InitializeVolumeControls()
    {
        if (volumeSlider == null && optionsPanel != null)
        {
            Transform sliderTransform = optionsPanel.transform.Find("VolumeSlider");
            if (sliderTransform != null)
            {
                volumeSlider = sliderTransform.GetComponent<Slider>();
            }

            if (volumeSlider == null)
            {
                volumeSlider = optionsPanel.GetComponentInChildren<Slider>(true);
            }
        }

        if (volumeSlider == null)
        {
            return;
        }

        volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", AudioListener.volume);
        volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);

        ApplySavedVolume();
    }

    private void HandleVolumeChanged(float newVolume)
    {
        AudioListener.volume = newVolume;
        PlayerPrefs.SetFloat("Volume", newVolume);
        PlayerPrefs.SetInt("Muted", newVolume <= 0.001f ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySavedVolume()
    {
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        float savedVolume = PlayerPrefs.GetFloat("Volume", 1f);
        AudioListener.volume = isMuted ? 0f : savedVolume;
    }

    private void InitializeCheckpointControls()
    {
        if (optionsPanel == null)
        {
            return;
        }

        if (checkpointsToggle == null)
        {
            Transform existingToggle = optionsPanel.transform.Find("CheckpointsToggle");
            if (existingToggle != null)
            {
                checkpointsToggle = existingToggle.GetComponent<Toggle>();
            }
        }

        if (checkpointsToggleButton == null)
        {
            Transform existingButton = optionsPanel.transform.Find("CheckpointsToggleButton");
            if (existingButton == null)
            {
                existingButton = optionsPanel.transform.Find("DisableCheckpointsButton");
            }

            if (existingButton == null)
            {
                existingButton = optionsPanel.transform.Find("CheckpointToggleButton");
            }

            if (existingButton != null)
            {
                checkpointsToggleButton = existingButton.GetComponent<Button>();
            }
        }

        if (checkpointsToggleStatusGraphic == null && checkpointsToggleButton != null)
        {
            checkpointsToggleStatusGraphic = checkpointsToggleButton.GetComponentInChildren<Graphic>(true);
        }

        if (checkpointsToggle != null)
        {
            checkpointsToggle.onValueChanged.RemoveListener(HandleCheckpointsToggleChanged);
            checkpointsToggle.isOn = Checkpoint.AreCheckpointsEnabled();
            checkpointsToggle.onValueChanged.AddListener(HandleCheckpointsToggleChanged);
        }

        if (checkpointsToggleButton != null)
        {
            checkpointsToggleButton.onClick.RemoveListener(ToggleCheckpointsEnabled);
            checkpointsToggleButton.onClick.AddListener(ToggleCheckpointsEnabled);
        }

        RefreshCheckpointControls();
    }

    private void RefreshCheckpointControls()
    {
        if (checkpointButton == null && pausePanel != null)
        {
            Transform existingButton = pausePanel.transform.Find("CheckpointButton");
            if (existingButton != null)
            {
                checkpointButton = existingButton.GetComponent<Button>();
            }
        }

        bool areCheckpointsEnabled = Checkpoint.AreCheckpointsEnabled();

        if (checkpointsToggle == null)
        {
            if (checkpointButton != null)
            {
                checkpointButton.interactable = areCheckpointsEnabled;
            }
        }
        else
        {
            checkpointsToggle.SetIsOnWithoutNotify(areCheckpointsEnabled);
        }

        if (checkpointButton != null)
        {
            checkpointButton.interactable = areCheckpointsEnabled;
        }

        if (checkpointsToggleStatusGraphic != null)
        {
            checkpointsToggleStatusGraphic.color = areCheckpointsEnabled
                ? new Color(0.15f, 0.85f, 0.35f, 1f)
                : new Color(0.85f, 0.2f, 0.2f, 1f);
        }
    }

    private void HandleCheckpointsToggleChanged(bool areEnabled)
    {
        Checkpoint.SetCheckpointsEnabled(areEnabled);

        if (!areEnabled && gameManager != null)
        {
            gameManager.ResetToLevelStart();
            Checkpoint.ResetActiveCheckpoint();
        }

        RefreshCheckpointControls();
    }

    public void ToggleCheckpointsEnabled()
    {
        HandleCheckpointsToggleChanged(!Checkpoint.AreCheckpointsEnabled());
    }
}
