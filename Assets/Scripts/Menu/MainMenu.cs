using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Slide Settings")]
    public RectTransform menuContainer;
    public float slideSpeed = 15f;

    // 0 = Main Menu, -1 = Options (shifts the container left by one screen width)
    private static float targetScreenMultiplier = 0f;

    [Header("Navigation Focus")]
    public GameObject firstMainMenuButton; // Start Button
    public GameObject firstOptionsButton; // Music Slider
    public GameObject optionsButtonOnMain; // The button that opens options (to return focus)

    [Header("UI & Scene")]
    [SerializeField] private GameObject optionsPanel;
    public SceneFader fader;

    [Header("Audio Settings")]
    public AudioMixer mainMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Audio Muffle")]
    public float normalFreq = 22000f;
    public float muffledFreq = 800f;
    public float muffleSmoothSpeed = 10f;
    private float currentFreq = 22000f;

    private void Start()
    {
        // 1. Load Audio Prefs PLEASE REVIEW THIS!!!
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        if (musicSlider != null) musicSlider.value = savedMusic;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        // 2. Setup Initial Selection for Controller/Keyboard support
        if (firstMainMenuButton != null)
            EventSystem.current.SetSelectedGameObject(firstMainMenuButton);

    }

    private void Update()
    {
        if (menuContainer != null)
        {
            // 1. Get the width from the Canvas directly
            Canvas canvas = menuContainer.GetComponentInParent<Canvas>();
            float width = 1920f; // Default fallback

            if (canvas != null)
            {
                // This gets the true 'Canvas' width regardless of screen size
                width = canvas.GetComponent<RectTransform>().rect.width;
            }

            // 2. Calculate the target
            float targetX = targetScreenMultiplier * width;

            // 3. Move it
            Vector2 currentPos = menuContainer.anchoredPosition;

            if (Mathf.Abs(currentPos.x - targetX) > 0.1f)
            {
                float newX = Mathf.Lerp(currentPos.x, targetX, Time.unscaledDeltaTime * slideSpeed);
                menuContainer.anchoredPosition = new Vector2(newX, 0f);
            }
            else
            {
                menuContainer.anchoredPosition = new Vector2(targetX, 0f);
            }

            // If multiplier is -1 (Options), we want muffled. If 0 (Main), we want normal.
            float targetFreq = (targetScreenMultiplier < -0.5f) ? muffledFreq : normalFreq;

            currentFreq = Mathf.Lerp(currentFreq, targetFreq, Time.unscaledDeltaTime * muffleSmoothSpeed);

            if (mainMixer != null)
            {
                mainMixer.SetFloat("MusicMuffle", currentFreq);
            }
        }
    }

    // --- Transition Methods ---

    /*public void OpenOptions()
    {
        Debug.Log("OpenOptions Called!");
        targetScreenMultiplier = -1f; // Slide to the left

        if (firstOptionsButton != null)
            EventSystem.current.SetSelectedGameObject(firstOptionsButton);
    }*/

    public void ToggleOptions(bool isOpen)
    {
        Debug.Log("ToggleOptions called. isOpen: " + isOpen);

        if (isOpen)
        {
            targetScreenMultiplier = -1f; // Slide to Options
            if (firstOptionsButton != null)
                EventSystem.current.SetSelectedGameObject(firstOptionsButton);
        }
        else
        {
            targetScreenMultiplier = 0f; // Slide to Main
            if (optionsButtonOnMain != null)
                EventSystem.current.SetSelectedGameObject(optionsButtonOnMain);
        }
    }

    // --- Game Logic ---

    public void StartGame()
    {
        if (fader != null) fader.FadeTo("Level");
        else Debug.LogError("SceneFader is missing from MainMenu script!");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Button Pressed!");
        PlayerPrefs.Save();
        Application.Quit();
    }

    // --- Audio Logic ---

    public void SetMusicVolume(float volume)
    {
        if (mainMixer != null)
        {
            // 1. Clamp to a tiny floor so the math never breaks
            float val = Mathf.Max(volume, 0.0001f);

            // 2. Convert to Decibels
            float db = Mathf.Log10(val) * 20f;

            // 3. Set Mixer
            mainMixer.SetFloat("MusicVol", db);

            // 4. Save the RAW 0-1 value in PlayerPrefs
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (mainMixer != null)
        {
            // 1. Prevent the "Zero Crash" (Log10 of 0 is infinity)
            float val = Mathf.Max(volume, 0.0001f);

            // 2. Convert to Decibels (-80dB to 0dB)
            float db = Mathf.Log10(val) * 20f;

            // 3. Apply to the Mixer (Make sure the parameter is named "SFXVol")
            mainMixer.SetFloat("SFXVol", db);

            // 4. Save to PlayerPrefs
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }
    }
}