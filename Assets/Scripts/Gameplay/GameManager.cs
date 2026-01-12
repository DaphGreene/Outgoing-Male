using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState { Ready, Playing, GameOver }
    public GameState State { get; private set; } = GameState.Ready;

    public bool IsPlaying => State == GameState.Playing;
    public bool HasGameEnded => State == GameState.GameOver;
    public bool CanPause => IsPlaying;
    public event Action<GameState> OnStateChanged;

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private AudioSource backgroundMusic;

    [Header("UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject getReady;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private Text startPromptText;

    private int score;
    public int Score => score;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        SetReadyState();
    }

    private void Start()
    {
        UpdateHighScoreText();
    }

    private void Update()
    {
        // Only allow "start run" input when waiting to start.
        if (State == GameState.Playing) return;

        // Don't treat UI clicks as gameplay input
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (!pointerOverUI && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            Play();
        }
    }

    private void SetReadyState()
    {
        State = GameState.Ready;
        OnStateChanged?.Invoke(State);

        // UI
        if (getReady != null)
            getReady.SetActive(true);
        if (startPromptText != null)
        {
            startPromptText.gameObject.SetActive(true);
            UpdateStartPromptText();
        }
        if (playButton != null)
            playButton.SetActive(true);
        gameOver.SetActive(false);

        // Gameplay
        player.enabled = false;

        // Music: your call. For now, leave it playing or stop it.
        // backgroundMusic.Stop();
    }

    public void Play()
    {
        State = GameState.Playing;
        OnStateChanged?.Invoke(State);

        score = 0;
        scoreText.text = score.ToString();

        // UI
        if (getReady != null)
            getReady.SetActive(false);
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);
        if (playButton != null)
            playButton.SetActive(false);
        gameOver.SetActive(false);

        // Gameplay
        if (player != null)
        {
            player.ResetState();
            player.enabled = true;
        }
        Time.timeScale = 1f;

        if (backgroundMusic != null)
        {
            backgroundMusic.Stop();
            backgroundMusic.Play();
        }

        // Cleanup old obstacles
        Obstacle[] obstacles = UnityEngine.Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);

        for (int i = 0; i < obstacles.Length; i++)
        {
            Destroy(obstacles[i].gameObject);
        }
    }

    public void GameOver()
    {
        State = GameState.GameOver;
        OnStateChanged?.Invoke(State);

        // UI
        gameOver.SetActive(true);
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);
        if (playButton != null)
            playButton.SetActive(true);
        if (getReady != null)
            getReady.SetActive(false);

        // Gameplay
        player.enabled = false;

        if (backgroundMusic != null)
            backgroundMusic.Stop();
    }

    public void IncreaseScore()
    {
        score++;
        scoreText.text = score.ToString();

        if (score > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", score);
            UpdateHighScoreText();
        }
    }

    private void UpdateHighScoreText()
    {
        highScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighScore", 0)}";
    }

    private void UpdateStartPromptText()
    {
        if (startPromptText == null)
            return;

        bool isMobile = Application.isMobilePlatform || Input.touchSupported;
        startPromptText.text = isMobile ? "Tap to Flap" : "Click or Space to Flap";
    }
}
