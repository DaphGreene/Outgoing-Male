using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private const string SongProgressPersonalBestKey = "SongProgressPersonalBest";

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
    [SerializeField] private RectTransform songProgressPersonalBestMarker;
    [SerializeField] private TMP_Text personalBestToastText;
    [SerializeField] private CanvasGroup personalBestToastCanvasGroup;

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

    private int score;
    public int Score => score;
    public int CurrentHits => currentHits;

    private bool hasValidReferences = true;
    private CanvasGroup startPromptCanvasGroup;
    private int currentHits;
    private float runProgressNormalized;
    private float personalBestProgressNormalized;
    private int runStartingHighScore;
    private bool hasAnnouncedPersonalBestThisRun;
    private float personalBestToastShownAtUnscaledTime = -999f;

    private void Awake()
    {
        personalBestProgressNormalized = PlayerPrefs.GetFloat(SongProgressPersonalBestKey, 0f);
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
        RefreshSongProgressUi();
        RefreshSongProgressPersonalBestMarker();
        ConfigurePersonalBestToastMotion();
    }

    private void Update()
    {
        if (!hasValidReferences) return;

        if (State == GameState.Playing)
            UpdateRunProgressFromMusic();

        UpdatePersonalBestToast();

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
        ResetRunProgress();
        HidePersonalBestToastImmediate();

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
        scoreText.text = score.ToString();
        ResetRunProgress();

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
        if (State == GameState.GameOver) return;

        UpdateRunProgressFromMusic();

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

    public void ResetSongProgressPersonalBest()
    {
        personalBestProgressNormalized = 0f;
        PlayerPrefs.DeleteKey(SongProgressPersonalBestKey);
        PlayerPrefs.Save();
        RefreshSongProgressPersonalBestMarker();
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

    private void UpdateRunProgressFromMusic()
    {
        if (runMusicSource == null || runMusicSource.clip == null)
            return;

        float clipLength = runMusicSource.clip.length;
        if (clipLength <= 0f)
            return;

        SetRunProgressNormalized(runMusicSource.time / clipLength);
    }

    private void SetRunProgressNormalized(float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(runProgressNormalized, clampedValue))
            return;

        runProgressNormalized = clampedValue;
        RefreshSongProgressUi();
        UpdateSongProgressPersonalBest();
    }

    private void RefreshSongProgressUi()
    {
        if (songProgressFillRect == null)
            return;

        songProgressFillRect.anchorMin = new Vector2(0f, 0f);
        songProgressFillRect.anchorMax = new Vector2(runProgressNormalized, 1f);
        songProgressFillRect.anchoredPosition = Vector2.zero;
    }

    private void UpdateSongProgressPersonalBest()
    {
        if (runProgressNormalized <= personalBestProgressNormalized)
            return;

        personalBestProgressNormalized = runProgressNormalized;
        PlayerPrefs.SetFloat(SongProgressPersonalBestKey, personalBestProgressNormalized);
        PlayerPrefs.Save();
        RefreshSongProgressPersonalBestMarker();
    }

    private void RefreshSongProgressPersonalBestMarker()
    {
        if (songProgressPersonalBestMarker == null)
            return;

        bool hasPersonalBest = personalBestProgressNormalized > 0f;
        if (songProgressPersonalBestMarker.gameObject.activeSelf != hasPersonalBest)
            songProgressPersonalBestMarker.gameObject.SetActive(hasPersonalBest);

        if (!hasPersonalBest)
            return;

        float normalizedPosition = Mathf.Clamp01(personalBestProgressNormalized);
        songProgressPersonalBestMarker.anchorMin = new Vector2(normalizedPosition, 0f);
        songProgressPersonalBestMarker.anchorMax = new Vector2(normalizedPosition, 1f);
        songProgressPersonalBestMarker.anchoredPosition = Vector2.zero;
    }

    private void ShowPersonalBestToast()
    {
        if (personalBestToastText != null)
            personalBestToastText.text = "New Personal Best!";

        personalBestToastShownAtUnscaledTime = Time.unscaledTime;
        ConfigurePersonalBestToastMotion();

        if (personalBestToastText != null && !personalBestToastText.gameObject.activeSelf)
            personalBestToastText.gameObject.SetActive(true);
    }

    private void HidePersonalBestToastImmediate()
    {
        personalBestToastShownAtUnscaledTime = -999f;

        if (personalBestToastCanvasGroup != null)
            personalBestToastCanvasGroup.alpha = 0f;

        if (personalBestToastText != null && personalBestToastText.gameObject.activeSelf)
            personalBestToastText.gameObject.SetActive(false);
    }

    private void UpdatePersonalBestToast()
    {
        if (personalBestToastText == null || personalBestToastCanvasGroup == null)
            return;

        float elapsed = Time.unscaledTime - personalBestToastShownAtUnscaledTime;
        if (elapsed < 0f)
        {
            personalBestToastCanvasGroup.alpha = 0f;
            if (personalBestToastText.gameObject.activeSelf)
                personalBestToastText.gameObject.SetActive(false);
            return;
        }

        if (!personalBestToastText.gameObject.activeSelf)
            personalBestToastText.gameObject.SetActive(true);

        float fadeInEnd = personalBestToastFadeInDuration;
        float holdEnd = fadeInEnd + personalBestToastHoldDuration;
        float fadeOutEnd = holdEnd + personalBestToastFadeOutDuration;

        if (elapsed <= fadeInEnd && fadeInEnd > 0f)
        {
            personalBestToastCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInEnd);
            return;
        }

        if (elapsed <= holdEnd)
        {
            float flashPhase = (Mathf.Sin((elapsed - fadeInEnd) * personalBestToastFlashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            personalBestToastCanvasGroup.alpha = Mathf.Lerp(personalBestToastFlashMinAlpha, personalBestToastFlashMaxAlpha, flashPhase);
            return;
        }

        if (elapsed <= fadeOutEnd && personalBestToastFadeOutDuration > 0f)
        {
            float fadeOutElapsed = elapsed - holdEnd;
            personalBestToastCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeOutElapsed / personalBestToastFadeOutDuration);
            return;
        }

        HidePersonalBestToastImmediate();
    }

    private void ConfigurePersonalBestToastMotion()
    {
        if (personalBestToastText == null)
            return;

        TmpCharacterWaveMotion waveMotion = personalBestToastText.GetComponent<TmpCharacterWaveMotion>();
        if (waveMotion == null)
            waveMotion = personalBestToastText.gameObject.AddComponent<TmpCharacterWaveMotion>();

        waveMotion.SetMotion(
            personalBestToastWaveAmplitude,
            personalBestToastWaveSpeed,
            personalBestToastWaveOffsetPerCharacter);
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
