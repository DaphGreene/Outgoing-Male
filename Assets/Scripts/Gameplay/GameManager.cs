using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("backgroundMusic")]
    [SerializeField] private AudioSource runMusicSource;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private GameObject getReady;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private TMP_Text startPromptText;

    [Header("Start Screen UI Motion")]
    [SerializeField] private float startPromptBlinkSpeed = 2.3f;
    [SerializeField] private float startPromptMinAlpha = 0.35f;
    [SerializeField] private float startPromptMaxAlpha = 1f;

    [Header("Get Ready Motion")]
    [SerializeField] private float getReadyFloatAmplitude = 10f;
    [SerializeField] private float getReadyFloatSpeed = 1.4f;

    private int score;
    public int Score => score;

    private bool hasValidReferences = true;
    private CanvasGroup startPromptCanvasGroup;

    private void Awake()
    {
        hasValidReferences = ValidateRequiredReferences();
        if (!hasValidReferences)
        {
            enabled = false;
            return;
        }

        SetupStartScreenUi();

        Application.targetFrameRate = 60;
        SetReadyState();
    }

    private void Start()
    {
        if (!hasValidReferences) return;
        UpdateHighScoreText();
    }

    private void Update()
    {
        if (!hasValidReferences) return;

        // Start input is only valid from the Ready state.
        if (State != GameState.Ready) return;

        UpdateStartPromptBlink();

        // Ignore start input while another system (pause/game-over menu) has gameplay paused.
        if (Mathf.Approximately(Time.timeScale, 0f))
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || HasTapStartedThisFrame())
        {
            Play();
        }
    }

    private void SetReadyState()
    {
        if (!hasValidReferences) return;

        State = GameState.Ready;
        OnStateChanged?.Invoke(State);

        // UI
        ConfigureGetReadyPulse();
        if (getReady != null)
            getReady.SetActive(true);
        if (startPromptText != null)
        {
            startPromptText.gameObject.SetActive(true);
            UpdateStartPromptText();
        }
        gameOver.SetActive(false);

        // Gameplay
        if (player != null)
            player.SetFrozen(true);

        UpdateStartPromptBlink();

        // Keep menu/ready music flowing while waiting to start.
        if (MenuMusicPlayer.Instance != null)
        {
            MenuMusicPlayer.Instance.ResumeMenuMusic();
            if (runMusicSource != null && runMusicSource.isPlaying)
                runMusicSource.Stop();
        }
        else if (runMusicSource != null && !runMusicSource.isPlaying)
        {
            // Fallback when loading Game scene directly without MainMenu.
            runMusicSource.Play();
        }
    }

    public void Play()
    {
        if (!hasValidReferences) return;

        State = GameState.Playing;
        OnStateChanged?.Invoke(State);

        score = 0;
        scoreText.text = score.ToString();

        // UI
        if (getReady != null)
            getReady.SetActive(false);
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);
        gameOver.SetActive(false);

        // Gameplay
        if (player != null)
        {
            player.ResetState();
            player.SetFrozen(false);
        }
        Time.timeScale = 1f;

        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.PauseMenuMusic();

        if (runMusicSource != null)
        {
            runMusicSource.Stop();
            runMusicSource.Play();
        }

        ClearExistingObstacles();
    }

    public void ReturnToReady()
    {
        if (!hasValidReferences) return;

        score = 0;
        scoreText.text = score.ToString();
        Time.timeScale = 1f;

        if (player != null)
            player.ResetState();

        ClearExistingObstacles();
        SetReadyState();
    }

    public void GameOver()
    {
        if (!hasValidReferences) return;

        State = GameState.GameOver;
        OnStateChanged?.Invoke(State);

        // UI
        gameOver.SetActive(true);
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);
        if (getReady != null)
            getReady.SetActive(false);

        // Gameplay
        if (player != null)
            player.SetFrozen(true);

        if (runMusicSource != null && runMusicSource.isPlaying)
            runMusicSource.Stop();

        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.ResumeMenuMusic();
    }

    public void IncreaseScore(int amount = 1)
    {
        if (!hasValidReferences) return;
        if (amount <= 0) return;

        score += amount;
        scoreText.text = score.ToString();

        if (score > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
            UpdateHighScoreText();
        }
    }

    private void UpdateHighScoreText()
    {
        if (!hasValidReferences) return;
        highScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighScore", 0)}";
    }

    private bool ValidateRequiredReferences()
    {
        bool isValid = true;

        if (scoreText == null)
        {
            Debug.LogError("GameManager: 'scoreText' is not assigned.", this);
            isValid = false;
        }

        if (highScoreText == null)
        {
            Debug.LogError("GameManager: 'highScoreText' is not assigned.", this);
            isValid = false;
        }

        if (gameOver == null)
        {
            Debug.LogError("GameManager: 'gameOver' is not assigned.", this);
            isValid = false;
        }

        return isValid;
    }

    private void UpdateStartPromptText()
    {
        if (startPromptText == null)
            return;

        bool isMobile = Application.isMobilePlatform || Input.touchSupported;
        startPromptText.text = isMobile ? "Tap to Flap" : "Click/Space to Flap";
    }

    private void SetupStartScreenUi()
    {
        if (startPromptText != null)
        {
            startPromptText.raycastTarget = false;
            startPromptCanvasGroup = startPromptText.GetComponent<CanvasGroup>();
            if (startPromptCanvasGroup == null)
                startPromptCanvasGroup = startPromptText.gameObject.AddComponent<CanvasGroup>();
        }
        ConfigureGetReadyPulse();
    }

    private void ConfigureGetReadyPulse()
    {
        if (getReady == null)
            return;

        TMP_Text getReadyText = getReady.GetComponentInChildren<TMP_Text>(true);
        if (getReadyText != null)
            getReadyText.raycastTarget = false;

        GetReadyPulse pulse = getReady.GetComponent<GetReadyPulse>();
        if (pulse == null)
            pulse = getReady.AddComponent<GetReadyPulse>();

        pulse.SetMotion(getReadyFloatAmplitude, getReadyFloatSpeed);
    }

    private static bool HasTapStartedThisFrame()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private static void ClearExistingObstacles()
    {
        Obstacle[] obstacles = UnityEngine.Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
        for (int i = 0; i < obstacles.Length; i++)
            Destroy(obstacles[i].gameObject);

        StampPickup[] stampPickups = UnityEngine.Object.FindObjectsByType<StampPickup>(FindObjectsSortMode.None);
        for (int i = 0; i < stampPickups.Length; i++)
            Destroy(stampPickups[i].gameObject);
    }

    private void UpdateStartPromptBlink()
    {
        if (startPromptCanvasGroup == null)
            return;

        if (State != GameState.Ready || startPromptText == null || !startPromptText.gameObject.activeInHierarchy)
        {
            startPromptCanvasGroup.alpha = 1f;
            return;
        }

        float phase = (Mathf.Sin(Time.unscaledTime * startPromptBlinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        startPromptCanvasGroup.alpha = Mathf.Lerp(startPromptMinAlpha, startPromptMaxAlpha, phase);
    }
}

[RequireComponent(typeof(RectTransform))]
public class GetReadyPulse : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] private float bounceAmplitude = 10f;
    [SerializeField] private float bounceFrequency = 1.4f;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        baseAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        float bounceWave = Mathf.Sin(Time.unscaledTime * bounceFrequency * Mathf.PI * 2f);
        rectTransform.anchoredPosition = baseAnchoredPosition + Vector2.up * (bounceWave * bounceAmplitude);
    }

    public void SetMotion(float amplitude, float bounceSpeed)
    {
        bounceAmplitude = Mathf.Max(0f, amplitude);
        bounceFrequency = Mathf.Max(0f, bounceSpeed);
    }
}
