using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject characterSelectPanel;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        ShowMainMenu();
    }

    public void OnStartPressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnOptionsPressed()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        characterSelectPanel.SetActive(false);
    }

    public void OnCharacterSelectPressed()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        characterSelectPanel.SetActive(true);
    }

    public void OnBackToMainMenuPressed()
    {
        ShowMainMenu();
    }

    public void OnQuitPressed()
    {
        Debug.Log("Quit pressed");
        Application.Quit();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
        characterSelectPanel.SetActive(false);
    }
}