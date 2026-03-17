using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public class MainMenu : MonoBehaviour
{
    // Reference to the main menu panel containing Play / Options / Exit
    public GameObject mainMenuPanel;

    // Reference to the options menu panel
    public GameObject optionsPanel;

    // Slider used to control master game volume
    public Slider volumeSlider;


    // Toggle used to enable or disable fullscreen mode
    public Toggle fullscreenToggle;

    // Toggle used to mute all game audio
    public Toggle muteToggle;

    // Slider used to control graphics quality
    public Slider qualitySlider;

    // Dropdown used to choose screen resolution
    public TMP_Dropdown resolutionDropdown;

    // Array storing all supported screen resolutions
    private Resolution[] resolutions;

    private void ApplyDisplaySettings(int resolutionIndex, bool useFullscreen)
    {
        Resolution selectedResolution = resolutions[resolutionIndex];
        FullScreenMode fullscreenMode = useFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(selectedResolution.width, selectedResolution.height, fullscreenMode);
    }


    // Start runs when the menu scene first loads
    private void Start()
    {
        // Ensure the main menu is visible and options menu is hidden
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);

        // Fill the resolution dropdown with supported screen resolutions
        PopulateResolutions();

        // Load previously saved settings from PlayerPrefs
        LoadSettings();
    }


    // Called when the Play button is pressed
    public void PlayGame()
    {
        // Load the main gameplay scene
        SceneManager.LoadScene("Level_01");
    }


    // Opens the Options menu
    public void OpenOptions()
    {
        // Hide the main menu panel
        mainMenuPanel.SetActive(false);

        // Show the options menu panel
        optionsPanel.SetActive(true);
    }


    // Returns from the Options menu back to the main menu
    public void BackToMainMenu()
    {
        // Hide options menu
        optionsPanel.SetActive(false);

        // Show main menu
        mainMenuPanel.SetActive(true);
    }

    // Fills the resolution dropdown with all supported screen resolutions
    private void PopulateResolutions()
    {
        // Get all resolutions supported by the monitor
        resolutions = Screen.resolutions;

        // Remove any existing dropdown options
        resolutionDropdown.ClearOptions();

        // Create a list to hold resolution text options
        List<string> options = new List<string>();

        // Variable to remember which resolution matches the current screen size
        int currentResolutionIndex = 0;

        // Loop through all supported resolutions
        for (int i = 0; i < resolutions.Length; i++)
        {
            // Create text like "1920 x 1080"
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Check if this resolution matches the current screen resolution
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        // Add all resolution options to the dropdown
        resolutionDropdown.AddOptions(options);

        // Set dropdown to current resolution by default
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // Applies the settings chosen in the options menu
    public void ApplySettings()
    {
        // If mute is enabled, force volume to 0
        if (muteToggle.isOn)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            // Otherwise set volume based on slider value
            AudioListener.volume = volumeSlider.value;
        }

        bool useFullscreen = fullscreenToggle.isOn;

        // Apply graphics quality based on the quality slider
        QualitySettings.SetQualityLevel((int)qualitySlider.value);

        // Apply the selected screen resolution and window mode explicitly
        ApplyDisplaySettings(resolutionDropdown.value, useFullscreen);

        // Save all settings so they persist between game launches
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.SetInt("Fullscreen", useFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Muted", muteToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("QualityLevel", (int)qualitySlider.value);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);

        // Save PlayerPrefs to disk
        PlayerPrefs.Save();

        Debug.Log("Settings applied");
    }

    // Loads previously saved settings
    private void LoadSettings()
    {
        // Retrieve saved settings or use defaults if none exist
        float savedVolume = PlayerPrefs.GetFloat("Volume", 1f);
        int savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1);
        int savedMuted = PlayerPrefs.GetInt("Muted", 0);
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", 2);
        int savedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);

        bool useFullscreen = savedFullscreen == 1;

        // Update UI sliders and toggles with saved values
        volumeSlider.value = savedVolume;
        fullscreenToggle.isOn = useFullscreen;
        muteToggle.isOn = savedMuted == 1;
        qualitySlider.value = savedQuality;

        // Make sure the saved resolution index is valid before using it
        if (savedResolutionIndex >= 0 && savedResolutionIndex < resolutions.Length)
        {
            resolutionDropdown.value = savedResolutionIndex;
        }

        // Apply audio settings
        if (muteToggle.isOn)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = savedVolume;
        }

        // Apply graphics quality
        QualitySettings.SetQualityLevel(savedQuality);

        // Apply saved resolution and window mode explicitly
        ApplyDisplaySettings(resolutionDropdown.value, useFullscreen);

        // Refresh dropdown visual
        resolutionDropdown.RefreshShownValue();
    }


    // Called when Exit button is pressed
    public void QuitGame()
    {
        Debug.Log("Quit button clicked");

        // Close the application (works in built game)
        Application.Quit();
    }
}
