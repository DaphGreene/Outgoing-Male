using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuCanvas;   // your PauseMenu object (Canvas)
    [SerializeField] private GameObject pauseRootPanel;    // contains Resume/Options/Restart/Quit
    [SerializeField] private GameObject optionsPanel;      // options submenu inside pause
    [SerializeField] private GameObject gameOverPanel;     // dedicated Game Over panel
    [SerializeField] private GameObject menuContainer;     // the thing you actually show/hide

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Behavior")]
    [SerializeField] private bool openOnGameOver = true;

    private bool isPaused;
    private bool gameOverShown;

    private void Start()
    {
        isPaused = false;
        menuContainer.SetActive(false);

        // make sure subpanels start in a known state
        ShowRoot();
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnStateChanged -= HandleGameStateChanged;
    }

    private void Update()
    {
        if (gameManager != null && gameManager.IsPlaying)
            gameOverShown = false;

        if (openOnGameOver && !gameOverShown && gameManager != null && gameManager.HasGameEnded)
        {
            gameOverShown = true;
            Pause();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
            Debug.Log("ESC detected");
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
            CloseMenuAndUnpause();
    }

    private void HandleEscape()
    {
        // If menu isn't open, open it
        if (!isPaused)
        {
            Pause();
            return;
        }

        // If in submenu, go back to root
        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            ShowRoot();
            return;
        }

        // Otherwise resume
        Resume();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        menuContainer.SetActive(true);
        ShowRoot();
    }

    public void Resume()
    {
        if (gameManager != null && gameManager.HasGameEnded)
            return;
        isPaused = false;
        menuContainer.SetActive(false);

        Time.timeScale = 1f;
    }

    public void OnResumePressed() => Resume();

    public void OnOptionsPressed()
    {
        pauseRootPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void OnBackFromOptionsPressed() => ShowRoot();

    public void OnRestartPressed()
    {
        CloseMenuAndUnpause(); // important: unpause first
        gameManager.Play(); // keep using Play() for now
    }

    public void OnTryAgainPressed()
    {
        CloseMenuAndUnpause(); // important: unpause first
        gameManager.Play(); // keep using Play() for now
    }

    public void OnQuitToMenuPressed()
    {
        CloseMenuAndUnpause(); // unpause first
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CloseMenuAndUnpause()
    {
        isPaused = false;
        menuContainer.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ShowRoot()
    {
        bool isGameOver = gameManager != null && gameManager.HasGameEnded;

        if (pauseRootPanel != null) pauseRootPanel.SetActive(!isGameOver);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(isGameOver);
    }
}
