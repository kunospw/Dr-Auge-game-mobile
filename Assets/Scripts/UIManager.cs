using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject retryPanel;
    public GameObject winPanel;        // Added win panel
    public GameObject pausePanel;
    public GameObject guiPanel; // 👈 Add this

    [Header("Buttons")]
    public Button startButton;
    public Button retryButton;
    public Button nextLevelButton;     // Added next level button
    public Button pauseButton;
    public Button continueButton;
    public Button mainMenuButton;

    [Header("Audio Controls")]
    public Scrollbar masterVolumeScrollbar;

    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 0f;
        mainMenuPanel.SetActive(true);
        retryPanel.SetActive(false);
        winPanel.SetActive(false);        // Hide win panel at start
        pausePanel.SetActive(false);
        guiPanel.SetActive(false); // 👈 Hide at start

        // Hook up buttons
        startButton.onClick.AddListener(StartGame);
        retryButton.onClick.AddListener(RestartGame);
        nextLevelButton.onClick.AddListener(NextLevel);  // Hook up next level button
        pauseButton.onClick.AddListener(TogglePause);
        continueButton.onClick.AddListener(TogglePause);
        mainMenuButton.onClick.AddListener(BackToMainMenu);
        
        // Hook up audio controls
        SetupAudioControls();
    }

    public void StartGame()
    {
        // Reset the winning flag when starting a new game
        FinishLine.IsWinning = false;
        Debug.Log("UIManager: Reset IsWinning flag to false for new game");
        
        Time.timeScale = 1f;
        mainMenuPanel.SetActive(false);
        guiPanel.SetActive(true); // 👈 Show GUI when game starts
        
        // Play gameplay music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeOutMusic("Main Menu", 1f);
            AudioManager.Instance.FadeInMusic("Gameplay", 1f);
        }
    }

    public void ShowRetry()
    {
        Debug.Log("UIManager: ShowRetry called");
        Time.timeScale = 0f;
        
        // Play game over sound and music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Game Over");
            AudioManager.Instance.StopSound("Gameplay");
        }
        
        if (retryPanel != null)
        {
            retryPanel.SetActive(true);
            Debug.Log("UIManager: Retry panel activated");
        }
        else
        {
            Debug.LogError("UIManager: Retry panel is null!");
        }
        
        if (guiPanel != null)
        {
            guiPanel.SetActive(false); // Hide GUI when showing retry panel
        }
    }

    public void ShowGameOver()
    {
        // Called when the player dies to show the retry panel
        Debug.Log("UIManager: ShowGameOver called");
        ShowRetry();
    }

    public void ShowWinPanel()
    {
        // Called when the player wins to show the win panel
        Debug.Log("UIManager: ShowWinPanel called");
        Time.timeScale = 0f;
        
        // Play game complete sound and stop gameplay music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Game Complete");
            AudioManager.Instance.StopSound("Gameplay");
        }
        
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("UIManager: Win panel activated");
        }
        else
        {
            Debug.LogError("UIManager: Win panel is null!");
        }
        
        if (guiPanel != null)
        {
            guiPanel.SetActive(false); // Hide GUI when showing win panel
        }
    }

    public void NextLevel()
    {
        // Reset the winning flag when going to next level
        FinishLine.IsWinning = false;
        Debug.Log("UIManager: Next Level clicked - reset IsWinning flag and returning to main menu");
        
        // For now, go back to main menu (you can change this to load next level later)
        BackToMainMenu();
    }

    public void RestartGame()
    {
        // Reset the winning flag when restarting
        FinishLine.IsWinning = false;
        Debug.Log("UIManager: Reset IsWinning flag to false for restart");
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
        
        // Pause/unpause audio
        if (AudioManager.Instance != null)
        {
            if (isPaused)
            {
                AudioManager.Instance.PauseAllAudio();
            }
            else
            {
                AudioManager.Instance.UnpauseAllAudio();
            }
        }
    }

    public void BackToMainMenu()
    {
        // Reset the winning flag when going back to main menu
        FinishLine.IsWinning = false;
        Debug.Log("UIManager: BackToMainMenu - reset IsWinning flag");
        
        // Restart the entire scene when going back to main menu
        Time.timeScale = 1f; // Reset time scale before reloading
        
        // Stop all audio and play main menu music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSound("Gameplay");
            AudioManager.Instance.PlayBackgroundMusic("Main Menu");
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    private void SetupAudioControls()
    {
        if (masterVolumeScrollbar != null && AudioManager.Instance != null)
        {
            // Set initial scrollbar value to current master volume
            masterVolumeScrollbar.value = AudioManager.Instance.masterVolume;
            
            // Hook up the scrollbar to volume control
            masterVolumeScrollbar.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            Debug.Log($"UIManager: Setup master volume scrollbar with initial value: {masterVolumeScrollbar.value}");
        }
        else
        {
            if (masterVolumeScrollbar == null)
                Debug.LogWarning("UIManager: Master volume scrollbar not assigned!");
            if (AudioManager.Instance == null)
                Debug.LogWarning("UIManager: AudioManager instance not found!");
        }
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
            Debug.Log($"UIManager: Master volume changed to: {value}");
        }
    }
}
