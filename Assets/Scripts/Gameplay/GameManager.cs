using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private const string SongProgressBestKey = "SongProgressPersonalBest";
    private const string SongProgressBestLapKey = "SongProgressPersonalBestLap";
    private const string SongProgressBestLapProgressKey = "SongProgressPersonalBestLapProgress";

    private enum ToastKind
    {
        PersonalBest,
        NewStamp,
        LapComplete
    }

    private readonly struct ToastRequest
    {
        public ToastRequest(ToastKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }

        public ToastKind Kind { get; }
        public string Message { get; }
    }

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
    [SerializeField] private GameObject hitIndicatorRoot;
    [SerializeField] private Image hitIndicatorImage;
    [SerializeField] private Sprite hitAvailableSprite;
    [SerializeField] private Sprite hitSpentSprite;
    [SerializeField] private RectTransform songProgressFillRect;
    [SerializeField] private RectTransform songProgressBestGuideRect;
    [FormerlySerializedAs("songProgressPersonalBestMarker")]
    [SerializeField] private RectTransform songProgressBestMarker;
    [SerializeField] private float songProgressBestMarkerYOffset = 0f;
    [SerializeField] private TMP_Text lapCounterText;
    [SerializeField] private TMP_Text songProgressPercentText;
    [SerializeField] private TMP_Text personalBestToastText;
    [SerializeField] private CanvasGroup personalBestToastCanvasGroup;
    [SerializeField] private RectTransform sharedToastRect;

    [Header("Player Hits")]
    [SerializeField, Min(1)] private int startingHits = 1;

    [Header("Start Screen UI Motion")]
    [SerializeField] private float startPromptBlinkSpeed = 2.3f;
    [SerializeField] private float startPromptMinAlpha = 0.35f;
    [SerializeField] private float startPromptMaxAlpha = 1f;

    [Header("Get Ready Motion")]
    [SerializeField] private float getReadyFloatAmplitude = 10f;
    [SerializeField] private float getReadyFloatSpeed = 1.4f;

    [Header("Personal Best Toast")]
    [SerializeField] private float personalBestToastFadeInDuration = 0.12f;
    [SerializeField] private float personalBestToastHoldDuration = 4f;
    [SerializeField] private float personalBestToastFadeOutDuration = 0.5f;
    [SerializeField] private float personalBestToastFlashSpeed = 4f;
    [SerializeField] private float personalBestToastFlashMinAlpha = 0.45f;
    [SerializeField] private float personalBestToastFlashMaxAlpha = 1f;
    [SerializeField] private float personalBestToastWaveAmplitude = 6f;
    [SerializeField] private float personalBestToastWaveSpeed = 1.8f;
    [SerializeField] private float personalBestToastWaveOffsetPerCharacter = 0.45f;
    [SerializeField] private Color personalBestToastColor = new(1f, 0.9137255f, 0.2f, 1f);
    [SerializeField] private float personalBestToastFontSize = 42f;
    [SerializeField] private float personalBestToastFontSizeMax = 48f;
    [SerializeField] private Vector2 personalBestToastSize = new(480f, 64f);

    [Header("Stamp Toast")]
    [SerializeField] private float newStampToastFadeInDuration = 0.12f;
    [SerializeField] private float newStampToastHoldDuration = 3f;
    [SerializeField] private float newStampToastFadeOutDuration = 0.5f;
    [SerializeField] private float newStampToastFlashSpeed = 1f;
    [SerializeField] private float newStampToastFlashMinAlpha = 0.7f;
    [SerializeField] private float newStampToastFlashMaxAlpha = 1f;
    [SerializeField] private float newStampToastWaveAmplitude = 2.5f;
    [SerializeField] private float newStampToastWaveSpeed = 1.2f;
    [SerializeField] private float newStampToastWaveOffsetPerCharacter = 0.25f;
    [SerializeField] private Color newStampToastColor = new(0.6509804f, 1f, 0.8f, 1f);
    [SerializeField] private float newStampToastFontSize = 46f;
    [SerializeField] private float newStampToastFontSizeMax = 56f;
    [SerializeField] private Vector2 newStampToastSize = new(560f, 92f);

    [Header("Lap Toast")]
    [SerializeField] private float lapToastFadeInDuration = 0.12f;
    [SerializeField] private float lapToastHoldDuration = 2.2f;
    [SerializeField] private float lapToastFadeOutDuration = 0.45f;
    [SerializeField] private float lapToastFlashSpeed = 2f;
    [SerializeField] private float lapToastFlashMinAlpha = 0.55f;
    [SerializeField] private float lapToastFlashMaxAlpha = 1f;
    [SerializeField] private float lapToastWaveAmplitude = 4f;
    [SerializeField] private float lapToastWaveSpeed = 1.4f;
    [SerializeField] private float lapToastWaveOffsetPerCharacter = 0.3f;
    [SerializeField] private Color lapToastColor = new(0.99215686f, 0.4745098f, 0.8901961f, 1f);
    [SerializeField] private float lapToastFontSize = 44f;
    [SerializeField] private float lapToastFontSizeMax = 52f;
    [SerializeField] private Vector2 lapToastSize = new(520f, 76f);

    private int score;
    public int Score => score;
    public int CurrentHits => currentHits;

    private bool hasValidReferences = true;
    private CanvasGroup startPromptCanvasGroup;
    private int currentHits;
    private int currentLapCount;
    private float runProgressNormalized;
    private float previousRunMusicTimeSeconds = -1f;
    private int songProgressBestLapCount;
    private float songProgressBestNormalized;
    private bool hasPendingSongProgressBestSave;
    private bool suppressSongProgressBestUntilNextRun;
    private int runStartingHighScore;
    private bool hasAnnouncedPersonalBestThisRun;
    private float activeToastShownAtUnscaledTime = -999f;
    private readonly Queue<ToastRequest> queuedToastMessages = new();
    private ToastKind activeToastKind = ToastKind.PersonalBest;

    private void Awake()
    {
        LoadSongProgressBest();
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
        RefreshLapCounterUi();
        RefreshSongProgressUi();
        RefreshSongProgressBestMarker();
        ConfigureSharedToastMotion(ToastKind.PersonalBest);
    }

    private void Update()
    {
        if (!hasValidReferences) return;

        if (State == GameState.Playing)
        {
            UpdateRunProgressFromMusic();
            RefreshSongProgressBestMarker();
        }

        UpdateSharedToast();

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
        ResetHits();
        ResetLapState();
        ResetRunProgress();
        RefreshSongProgressBestMarker();
        HideSharedToastImmediate(true);

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
        runStartingHighScore = PlayerPrefs.GetInt("HighScore", 0);
        hasAnnouncedPersonalBestThisRun = false;
        suppressSongProgressBestUntilNextRun = false;
        ResetLapState();
        scoreText.text = score.ToString();
        ResetRunProgress();
        RefreshSongProgressBestMarker();

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

        SaveSongProgressBestIfDirty();

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
        if (State == GameState.GameOver) return;

        UpdateRunProgressFromMusic();
        SaveSongProgressBestIfDirty();

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

    public void HandlePauseMenuOpened()
    {
        if (!hasValidReferences) return;
        if (!IsPlaying) return;

        if (runMusicSource != null && runMusicSource.isPlaying)
            runMusicSource.Pause();

        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.ResumeMenuMusic();
    }

    public void HandlePauseMenuClosed()
    {
        if (!hasValidReferences) return;
        if (!IsPlaying) return;

        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.PauseMenuMusic();

        if (runMusicSource != null)
            runMusicSource.UnPause();
    }

    public void IncreaseScore(int amount = 1)
    {
        if (!hasValidReferences) return;
        if (amount <= 0) return;

        score += amount;
        scoreText.text = score.ToString();

        if (!hasAnnouncedPersonalBestThisRun && runStartingHighScore > 0 && score > runStartingHighScore)
        {
            hasAnnouncedPersonalBestThisRun = true;
            ShowPersonalBestToast();
        }

        if (score > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
            UpdateHighScoreText();
        }
    }

    public bool TakeHit()
    {
        if (!hasValidReferences) return false;
        if (State == GameState.GameOver) return true;
        if (currentHits <= 0) return true;

        currentHits = Mathf.Max(0, currentHits - 1);
        RefreshHitUi();

        if (currentHits > 0)
        {
            // Multi-hit behavior can be expanded when upgrades land.
            return false;
        }

        GameOver();
        return true;
    }

    private void UpdateHighScoreText()
    {
        if (!hasValidReferences) return;
        highScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighScore", 0)}";
    }

    public void RefreshHighScoreDisplayFromPrefs()
    {
        if (!hasValidReferences) return;
        UpdateHighScoreText();
    }

    public void DebugSetScore(int value)
    {
        if (!hasValidReferences) return;

        score = Mathf.Max(0, value);
        scoreText.text = score.ToString();
    }

    public void DebugSetPlaybackSpeed(float speed)
    {
        float clampedSpeed = Mathf.Max(0.1f, speed);
        Time.timeScale = clampedSpeed;

        if (runMusicSource != null)
            runMusicSource.pitch = clampedSpeed;
    }

    public void DebugSetLapAndProgress(int lapCount, float normalizedProgress)
    {
        if (!hasValidReferences) return;

        currentLapCount = Mathf.Max(0, lapCount);
        float targetMusicTimeSeconds = ResolveMusicTimeFromNormalizedProgress(normalizedProgress);
        previousRunMusicTimeSeconds = targetMusicTimeSeconds;

        if (runMusicSource != null && runMusicSource.clip != null)
            runMusicSource.time = Mathf.Clamp(targetMusicTimeSeconds, 0f, Mathf.Max(0f, runMusicSource.clip.length - 0.01f));

        RefreshLapCounterUi();
        SetRunProgressNormalized(normalizedProgress);
        RefreshSongProgressBestMarker();
    }

    public void DebugSetSongProgressBest(int lapCount, float normalizedProgress)
    {
        songProgressBestLapCount = Mathf.Max(0, lapCount);
        songProgressBestNormalized = Mathf.Clamp01(normalizedProgress);
        PlayerPrefs.SetInt(SongProgressBestLapKey, songProgressBestLapCount);
        PlayerPrefs.SetFloat(SongProgressBestKey, songProgressBestNormalized);
        PlayerPrefs.SetFloat(SongProgressBestLapProgressKey, songProgressBestNormalized);
        PlayerPrefs.Save();
        RefreshSongProgressBestMarker();
    }

    public int DebugGetCurrentLapCount() => currentLapCount;

    public float DebugGetRunProgressNormalized() => runProgressNormalized;

    public int DebugGetSongProgressBestLapCount() => songProgressBestLapCount;

    public float DebugGetSongProgressBestNormalized() => songProgressBestNormalized;

    public float DebugGetSongProgressMarkerX() => songProgressBestMarker != null ? songProgressBestMarker.localPosition.x : float.NaN;

    public void ResetSongProgressBest()
    {
        songProgressBestLapCount = 0;
        songProgressBestNormalized = 0f;
        hasPendingSongProgressBestSave = false;
        suppressSongProgressBestUntilNextRun = true;
        PlayerPrefs.DeleteKey(SongProgressBestKey);
        PlayerPrefs.DeleteKey(SongProgressBestLapKey);
        PlayerPrefs.DeleteKey(SongProgressBestLapProgressKey);
        PlayerPrefs.Save();
        RefreshSongProgressBestMarker();
    }

    public void ShowNewStampCollectedToast(string stampDisplayName)
    {
        string message = string.IsNullOrWhiteSpace(stampDisplayName)
            ? "New stamp collected!"
            : $"New stamp collected!\n{stampDisplayName}";
        EnqueueToast(ToastKind.NewStamp, message);
    }

    private void ShowLapCompleteToast()
    {
        EnqueueToast(ToastKind.LapComplete, $"Lap {currentLapCount} Complete!");
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

    private void LoadSongProgressBest()
    {
        if (PlayerPrefs.HasKey(SongProgressBestLapKey) || PlayerPrefs.HasKey(SongProgressBestLapProgressKey))
        {
            songProgressBestLapCount = Mathf.Max(0, PlayerPrefs.GetInt(SongProgressBestLapKey, 0));
            songProgressBestNormalized = Mathf.Clamp01(PlayerPrefs.GetFloat(SongProgressBestLapProgressKey, 0f));
            return;
        }

        songProgressBestLapCount = 0;
        songProgressBestNormalized = Mathf.Clamp01(PlayerPrefs.GetFloat(SongProgressBestKey, 0f));
    }

    private void ResetLapState()
    {
        currentLapCount = 0;
        previousRunMusicTimeSeconds = -1f;
        RefreshLapCounterUi();
    }

    private void RefreshLapCounterUi()
    {
        if (lapCounterText == null)
            return;

        lapCounterText.text = $"Lap {currentLapCount + 1}";
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
        {
            getReadyText.raycastTarget = false;

            TmpCharacterWaveMotion waveMotion = getReadyText.GetComponent<TmpCharacterWaveMotion>();
            if (waveMotion == null)
                waveMotion = getReadyText.gameObject.AddComponent<TmpCharacterWaveMotion>();

            waveMotion.SetMotion(getReadyFloatAmplitude, getReadyFloatSpeed, 0.45f);
        }

        GetReadyPulse pulse = getReady.GetComponent<GetReadyPulse>();
        if (pulse != null)
            pulse.enabled = false;
    }

    private void ResetHits()
    {
        currentHits = Mathf.Max(1, startingHits);
        RefreshHitUi();
    }

    private void RefreshHitUi()
    {
        if (hitIndicatorRoot != null && !hitIndicatorRoot.activeSelf)
            hitIndicatorRoot.SetActive(true);

        if (hitIndicatorImage == null)
            return;

        bool hasHitsRemaining = currentHits > 0;
        Sprite targetSprite = hasHitsRemaining ? hitAvailableSprite : hitSpentSprite;

        if (targetSprite != null)
            hitIndicatorImage.sprite = targetSprite;

        hitIndicatorImage.enabled = true;
    }

    private void ResetRunProgress()
    {
        runProgressNormalized = 0f;
        RefreshSongProgressUi();
    }

    private float ResolveMusicTimeFromNormalizedProgress(float normalizedProgress)
    {
        if (runMusicSource == null || runMusicSource.clip == null)
            return -1f;

        return Mathf.Clamp01(normalizedProgress) * runMusicSource.clip.length;
    }

    private void UpdateRunProgressFromMusic()
    {
        if (runMusicSource == null || runMusicSource.clip == null)
            return;

        float currentMusicTimeSeconds = runMusicSource.time;
        if (previousRunMusicTimeSeconds >= 0f && currentMusicTimeSeconds + 0.05f < previousRunMusicTimeSeconds)
            HandleLapCompleted();

        previousRunMusicTimeSeconds = currentMusicTimeSeconds;

        float clipLength = runMusicSource.clip.length;
        if (clipLength <= 0f)
            return;

        SetRunProgressNormalized(currentMusicTimeSeconds / clipLength);
    }

    private void SetRunProgressNormalized(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(runProgressNormalized, clampedValue))
            return;

        runProgressNormalized = clampedValue;
        RefreshSongProgressUi();
        UpdateSongProgressBest();
    }

    private void RefreshSongProgressUi()
    {
        if (songProgressFillRect == null)
            return;

        songProgressFillRect.anchorMin = new Vector2(0f, 0f);
        songProgressFillRect.anchorMax = new Vector2(runProgressNormalized, 1f);
        songProgressFillRect.anchoredPosition = Vector2.zero;

        if (songProgressPercentText != null)
            songProgressPercentText.text = $"{Mathf.RoundToInt(runProgressNormalized * 100f)}%";
    }

    private void UpdateSongProgressBest()
    {
        if (suppressSongProgressBestUntilNextRun)
            return;

        bool isNewBestLap = currentLapCount > songProgressBestLapCount;
        bool isFurtherInSameBestLap = currentLapCount == songProgressBestLapCount && runProgressNormalized > songProgressBestNormalized;
        if (!isNewBestLap && !isFurtherInSameBestLap)
            return;

        songProgressBestLapCount = currentLapCount;
        songProgressBestNormalized = runProgressNormalized;
        PlayerPrefs.SetInt(SongProgressBestLapKey, songProgressBestLapCount);
        PlayerPrefs.SetFloat(SongProgressBestKey, songProgressBestNormalized);
        PlayerPrefs.SetFloat(SongProgressBestLapProgressKey, songProgressBestNormalized);
        hasPendingSongProgressBestSave = true;
        RefreshLapCounterUi();
        RefreshSongProgressBestMarker();
    }

    private void SaveSongProgressBestIfDirty()
    {
        if (!hasPendingSongProgressBestSave)
            return;

        PlayerPrefs.Save();
        hasPendingSongProgressBestSave = false;
    }

    private void RefreshSongProgressBestMarker()
    {
        if (songProgressBestGuideRect == null || songProgressBestMarker == null)
            return;

        bool shouldShowMarker = currentLapCount >= songProgressBestLapCount;
        if (songProgressBestMarker.gameObject.activeSelf != shouldShowMarker)
            songProgressBestMarker.gameObject.SetActive(shouldShowMarker);

        if (!shouldShowMarker)
            return;

        float normalizedPosition = Mathf.Clamp01(songProgressBestNormalized);
        songProgressBestGuideRect.anchorMin = new Vector2(0f, 0f);
        songProgressBestGuideRect.anchorMax = new Vector2(normalizedPosition, 1f);
        songProgressBestGuideRect.anchoredPosition = Vector2.zero;
        songProgressBestGuideRect.sizeDelta = Vector2.zero;

        songProgressBestMarker.anchorMin = new Vector2(1f, 0.5f);
        songProgressBestMarker.anchorMax = new Vector2(1f, 0.5f);
        songProgressBestMarker.pivot = new Vector2(0.5f, 0.5f);
        songProgressBestMarker.anchoredPosition = new Vector2(0f, songProgressBestMarkerYOffset);
    }

    private void HandleLapCompleted()
    {
        SetRunProgressNormalized(1f);

        currentLapCount += 1;
        RefreshLapCounterUi();

        // Entering the next lap means the local progress marker starts fresh at 0 for this rotation.
        SetRunProgressNormalized(0f);
        ShowLapCompleteToast();
    }

    private void ShowPersonalBestToast()
    {
        EnqueueToast(ToastKind.PersonalBest, "New High Score!");
    }

    private void HideSharedToastImmediate(bool clearQueue)
    {
        activeToastShownAtUnscaledTime = -999f;

        if (clearQueue)
            queuedToastMessages.Clear();

        if (personalBestToastCanvasGroup != null)
            personalBestToastCanvasGroup.alpha = 0f;

        if (personalBestToastText != null && personalBestToastText.gameObject.activeSelf)
            personalBestToastText.gameObject.SetActive(false);
    }

    private void UpdateSharedToast()
    {
        if (personalBestToastText == null || personalBestToastCanvasGroup == null)
            return;

        if (activeToastShownAtUnscaledTime < 0f)
        {
            TryShowNextQueuedToast();
            return;
        }

        float elapsed = Time.unscaledTime - activeToastShownAtUnscaledTime;
        if (elapsed < 0f)
        {
            personalBestToastCanvasGroup.alpha = 0f;
            if (personalBestToastText.gameObject.activeSelf)
                personalBestToastText.gameObject.SetActive(false);
            return;
        }

        if (!personalBestToastText.gameObject.activeSelf)
            personalBestToastText.gameObject.SetActive(true);

        float fadeInEnd = GetActiveToastFadeInDuration();
        float holdEnd = fadeInEnd + GetActiveToastHoldDuration();
        float fadeOutEnd = holdEnd + GetActiveToastFadeOutDuration();

        if (elapsed <= fadeInEnd && fadeInEnd > 0f)
        {
            personalBestToastCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInEnd);
            return;
        }

        if (elapsed <= holdEnd)
        {
            float flashPhase = (Mathf.Sin((elapsed - fadeInEnd) * GetActiveToastFlashSpeed() * Mathf.PI * 2f) + 1f) * 0.5f;
            personalBestToastCanvasGroup.alpha = Mathf.Lerp(GetActiveToastFlashMinAlpha(), GetActiveToastFlashMaxAlpha(), flashPhase);
            return;
        }

        if (elapsed <= fadeOutEnd && GetActiveToastFadeOutDuration() > 0f)
        {
            float fadeOutElapsed = elapsed - holdEnd;
            personalBestToastCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeOutElapsed / GetActiveToastFadeOutDuration());
            return;
        }

        HideSharedToastImmediate(false);
        TryShowNextQueuedToast();
    }

    private void ConfigureSharedToastMotion(ToastKind toastKind)
    {
        if (personalBestToastText == null)
            return;

        TmpCharacterWaveMotion waveMotion = personalBestToastText.GetComponent<TmpCharacterWaveMotion>();
        if (waveMotion == null)
            waveMotion = personalBestToastText.gameObject.AddComponent<TmpCharacterWaveMotion>();

        waveMotion.SetMotion(GetToastWaveAmplitude(toastKind), GetToastWaveSpeed(toastKind), GetToastWaveOffsetPerCharacter(toastKind));
    }

    private void EnqueueToast(ToastKind toastKind, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        queuedToastMessages.Enqueue(new ToastRequest(toastKind, message));
        TryShowNextQueuedToast();
    }

    private void TryShowNextQueuedToast()
    {
        if (activeToastShownAtUnscaledTime >= 0f)
            return;

        if (personalBestToastText == null || personalBestToastCanvasGroup == null)
            return;

        if (queuedToastMessages.Count == 0)
            return;

        ToastRequest request = queuedToastMessages.Dequeue();
        activeToastKind = request.Kind;
        personalBestToastText.text = request.Message;
        activeToastShownAtUnscaledTime = Time.unscaledTime;
        ApplyToastStyle(request.Kind);
        ConfigureSharedToastMotion(request.Kind);

        if (!personalBestToastText.gameObject.activeSelf)
            personalBestToastText.gameObject.SetActive(true);
    }

    private void ApplyToastStyle(ToastKind toastKind)
    {
        if (personalBestToastText != null)
        {
            personalBestToastText.color = GetToastColor(toastKind);
            personalBestToastText.fontSize = GetToastFontSize(toastKind);
            personalBestToastText.fontSizeMax = GetToastFontSizeMax(toastKind);
        }

        if (sharedToastRect != null)
            sharedToastRect.sizeDelta = GetToastSize(toastKind);
    }

    private float GetActiveToastFadeInDuration() =>
        activeToastKind switch
        {
            ToastKind.PersonalBest => personalBestToastFadeInDuration,
            ToastKind.NewStamp => newStampToastFadeInDuration,
            _ => lapToastFadeInDuration
        };

    private float GetActiveToastHoldDuration() =>
        activeToastKind switch
        {
            ToastKind.PersonalBest => personalBestToastHoldDuration,
            ToastKind.NewStamp => newStampToastHoldDuration,
            _ => lapToastHoldDuration
        };

    private float GetActiveToastFadeOutDuration() =>
        activeToastKind switch
        {
            ToastKind.PersonalBest => personalBestToastFadeOutDuration,
            ToastKind.NewStamp => newStampToastFadeOutDuration,
            _ => lapToastFadeOutDuration
        };

    private float GetActiveToastFlashSpeed() =>
        activeToastKind switch
        {
            ToastKind.PersonalBest => personalBestToastFlashSpeed,
            ToastKind.NewStamp => newStampToastFlashSpeed,
            _ => lapToastFlashSpeed
        };

    private float GetActiveToastFlashMinAlpha() =>
        activeToastKind switch
        {
            ToastKind.PersonalBest => personalBestToastFlashMinAlpha,
            ToastKind.NewStamp => newStampToastFlashMinAlpha,
            _ => lapToastFlashMinAlpha
        };

    private float GetActiveToastFlashMaxAlpha() =>
        activeToastKind switch
        {
            ToastKind.PersonalBest => personalBestToastFlashMaxAlpha,
            ToastKind.NewStamp => newStampToastFlashMaxAlpha,
            _ => lapToastFlashMaxAlpha
        };

    private float GetToastWaveAmplitude(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastWaveAmplitude,
        ToastKind.NewStamp => newStampToastWaveAmplitude,
        _ => lapToastWaveAmplitude
    };

    private float GetToastWaveSpeed(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastWaveSpeed,
        ToastKind.NewStamp => newStampToastWaveSpeed,
        _ => lapToastWaveSpeed
    };

    private float GetToastWaveOffsetPerCharacter(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastWaveOffsetPerCharacter,
        ToastKind.NewStamp => newStampToastWaveOffsetPerCharacter,
        _ => lapToastWaveOffsetPerCharacter
    };

    private Color GetToastColor(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastColor,
        ToastKind.NewStamp => newStampToastColor,
        _ => lapToastColor
    };

    private float GetToastFontSize(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastFontSize,
        ToastKind.NewStamp => newStampToastFontSize,
        _ => lapToastFontSize
    };

    private float GetToastFontSizeMax(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastFontSizeMax,
        ToastKind.NewStamp => newStampToastFontSizeMax,
        _ => lapToastFontSizeMax
    };

    private Vector2 GetToastSize(ToastKind toastKind) => toastKind switch
    {
        ToastKind.PersonalBest => personalBestToastSize,
        ToastKind.NewStamp => newStampToastSize,
        _ => lapToastSize
    };

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
        Obstacle[] obstacles = UnityEngine.Object.FindObjectsByType<Obstacle>();
        for (int i = 0; i < obstacles.Length; i++)
            Destroy(obstacles[i].gameObject);

        StampPickup[] stampPickups = UnityEngine.Object.FindObjectsByType<StampPickup>();
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

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveSongProgressBestIfDirty();
    }

    private void OnApplicationQuit()
    {
        SaveSongProgressBestIfDirty();
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

[RequireComponent(typeof(TMP_Text))]
public class TmpCharacterWaveMotion : MonoBehaviour
{
    [SerializeField] private float waveAmplitude = 6f;
    [SerializeField] private float waveFrequency = 1.8f;
    [SerializeField] private float phaseOffsetPerCharacter = 0.45f;

    private TMP_Text textComponent;
    private TMP_MeshInfo[] cachedMeshInfo;
    private string cachedText;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        CacheMeshData();
    }

    private void OnEnable()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        CacheMeshData();
    }

    private void LateUpdate()
    {
        if (textComponent == null)
            return;

        if (cachedMeshInfo == null || cachedMeshInfo.Length == 0 || cachedText != textComponent.text)
            CacheMeshData();

        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        if (textInfo.characterCount == 0 || cachedMeshInfo == null || cachedMeshInfo.Length != textInfo.meshInfo.Length)
            return;

        for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
        {
            var sourceVertices = cachedMeshInfo[meshIndex].vertices;
            var destinationVertices = textInfo.meshInfo[meshIndex].vertices;
            Array.Copy(sourceVertices, destinationVertices, sourceVertices.Length);
        }

        float baseTime = Time.unscaledTime * waveFrequency * Mathf.PI * 2f;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible)
                continue;

            int materialIndex = characterInfo.materialReferenceIndex;
            int vertexIndex = characterInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 midpoint = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;
            float waveOffset = Mathf.Sin(baseTime + (i * phaseOffsetPerCharacter)) * waveAmplitude;
            Vector3 offset = new(0f, waveOffset, 0f);

            vertices[vertexIndex] = cachedMeshInfo[materialIndex].vertices[vertexIndex] + offset;
            vertices[vertexIndex + 1] = cachedMeshInfo[materialIndex].vertices[vertexIndex + 1] + offset;
            vertices[vertexIndex + 2] = cachedMeshInfo[materialIndex].vertices[vertexIndex + 2] + offset;
            vertices[vertexIndex + 3] = cachedMeshInfo[materialIndex].vertices[vertexIndex + 3] + offset;
        }

        for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
        {
            textInfo.meshInfo[meshIndex].mesh.vertices = textInfo.meshInfo[meshIndex].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[meshIndex].mesh, meshIndex);
        }
    }

    public void SetMotion(float amplitude, float speed, float characterPhaseOffset)
    {
        waveAmplitude = Mathf.Max(0f, amplitude);
        waveFrequency = Mathf.Max(0f, speed);
        phaseOffsetPerCharacter = Mathf.Max(0f, characterPhaseOffset);
        CacheMeshData();
    }

    private void CacheMeshData()
    {
        if (textComponent == null)
            return;

        textComponent.ForceMeshUpdate();
        cachedText = textComponent.text;
        cachedMeshInfo = textComponent.textInfo.CopyMeshInfoVertexData();
    }
}
