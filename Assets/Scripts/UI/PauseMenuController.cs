using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuCanvas;   // your PauseMenu object (Canvas)
    [SerializeField] private GameObject pauseRootPanel;    // contains Resume/Options/Restart/Quit
    [SerializeField] private GameObject optionsPanel;      // options submenu inside pause
    [SerializeField] private GameObject menuContainer;     // the thing you actually show/hide

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    private bool isPaused;

    private void Start()
    {
        isPaused = false;
        menuContainer.SetActive(false);

        // make sure subpanels start in a known state
        ShowRoot();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
            Debug.Log("ESC detected");
        }
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
        isPaused = false;
        menuContainer.SetActive(false);

        if (gameManager != null && gameManager.CanPause)
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
        Resume(); // important: unpause first
        gameManager.Play(); // keep using Play() for now
    }

    public void OnQuitToMenuPressed()
    {
        Resume(); // unpause first
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowRoot()
    {
        if (pauseRootPanel != null) pauseRootPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
}
