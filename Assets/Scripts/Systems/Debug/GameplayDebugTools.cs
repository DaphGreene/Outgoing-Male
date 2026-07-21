using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class GameplayDebugTools : MonoBehaviour
{
    private enum OverlayCorner
    {
        UpperLeft,
        LowerLeft
    }

    [Header("Hotkeys")]
    [SerializeField] private KeyCode toggleInvincibleKey = KeyCode.F1;
    [SerializeField] private KeyCode toggleAutoFlapKey = KeyCode.F2;
    [SerializeField] private KeyCode toggleHoverKey = KeyCode.F3;
    [SerializeField] private KeyCode resetProgressKey = KeyCode.F4;
    [SerializeField] private KeyCode startRunKey = KeyCode.F5;
    [SerializeField] private KeyCode applySpeedKey = KeyCode.F6;
    [SerializeField] private KeyCode applyProgressKey = KeyCode.F7;
    [SerializeField] private KeyCode toggleOverlayKey = KeyCode.F8;
    [SerializeField] private KeyCode applyScoreKey = KeyCode.F9;

    [Header("UI")]
    [SerializeField] private bool showOverlay = false;
    [SerializeField] private OverlayCorner overlayCorner = OverlayCorner.LowerLeft;
    [SerializeField] private Vector2 overlayMargin = new(10f, 10f);
    [SerializeField] private Vector2 overlaySize = new(460f, 520f);
    [SerializeField] private int overlayFontSize = 24;
    [SerializeField] private float overlayLineHeight = 32f;
    [SerializeField] private Vector2 debugToastAnchoredPosition = new(0f, -120f);
    [SerializeField] private Vector2 debugToastSize = new(560f, 56f);
    [SerializeField, Min(0.1f)] private float debugToastDuration = 1.6f;
    [SerializeField, Min(0.1f)] private float debugToastFlashSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float debugToastMinAlpha = 0.3f;
    [SerializeField, Min(12f)] private float debugToastFontSize = 36f;

    [Header("Testing")]
    [SerializeField, Min(0.1f)] private float debugGameSpeed = 1f;
    [SerializeField, Min(0)] private int debugScore = 0;
    [SerializeField, Min(0)] private int debugLap = 0;
    [SerializeField, Range(0f, 1f)] private float debugProgress = 0f;
    [SerializeField] private bool syncSongProgressMarkerToDebugProgress = false;

    private Player player;
    private GameManager gameManager;
    private GUIStyle overlayTextStyle;
    private TMP_Text debugToastText;
    private CanvasGroup debugToastCanvasGroup;
    private Coroutine debugToastRoutine;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        EnsureReferences();

        if (Input.GetKeyDown(toggleInvincibleKey))
            ToggleInvincible();

        if (Input.GetKeyDown(toggleAutoFlapKey))
            ToggleAutoFlap();

        if (Input.GetKeyDown(toggleHoverKey))
            ToggleHover();

        if (Input.GetKeyDown(resetProgressKey))
            ResetProgression();

        if (Input.GetKeyDown(startRunKey))
            StartRunIfReady();

        if (Input.GetKeyDown(applySpeedKey))
            ApplyGameSpeed();

        if (Input.GetKeyDown(applyProgressKey))
            ApplyLapAndProgress();

        if (Input.GetKeyDown(toggleOverlayKey))
            showOverlay = !showOverlay;

        if (Input.GetKeyDown(applyScoreKey))
            ApplyScore();
    }

    private void OnGUI()
    {
        if (!showOverlay)
            return;

        string invincible = player != null && player.IsDebugInvincible ? "ON" : "OFF";
        string autoFlap = player != null && player.IsDebugAutoFlap ? "ON" : "OFF";
        string hover = player != null && player.IsDebugHoverEnabled ? "ON" : "OFF";
        string runProgressSummary = gameManager != null
            ? $"Run: Lap {gameManager.DebugGetCurrentLapCount() + 1} @ {gameManager.DebugGetRunProgressNormalized():P0}"
            : "Run: n/a";
        string songProgressBestSummary = gameManager != null
            ? $"Song Best: Lap {gameManager.DebugGetSongProgressBestLapCount() + 1} @ {gameManager.DebugGetSongProgressBestNormalized():P0}"
            : "Song Best: n/a";
        string songProgressMarkerSummary = gameManager != null
            ? $"Song Marker X: {gameManager.DebugGetSongProgressMarkerX():0.0}"
            : "Song Marker X: n/a";

        EnsureOverlayStyle();

        Rect panelRect = BuildOverlayRect();
        GUI.Box(panelRect, GUIContent.none);

        float x = panelRect.x + 10f;
        float y = panelRect.y + 8f;
        float contentWidth = panelRect.width - 20f;

        y = DrawOverlayLabel(x, y, contentWidth, "Debug Tools");
        y = DrawOverlayLabel(x, y, contentWidth, $"{toggleInvincibleKey}: Invincible [{invincible}]");
        y = DrawOverlayLabel(x, y, contentWidth, $"{toggleAutoFlapKey}: Auto-Flap [{autoFlap}]");
        y = DrawOverlayLabel(x, y, contentWidth, $"{toggleHoverKey}: Hover [{hover}]");
        y = DrawOverlayLabel(x, y, contentWidth, $"{resetProgressKey}: Reset HighScore + Stamps + PB + Found");
        y = DrawOverlayLabel(x, y, contentWidth, $"{startRunKey}: Start Run   {applySpeedKey}: Apply Speed ({debugGameSpeed:0.##}x)");
        y = DrawOverlayLabel(x, y, contentWidth, $"{applyProgressKey}: Set Lap/Bar [{debugLap + 1}, {debugProgress:P0}]");
        y = DrawOverlayLabel(x, y, contentWidth, $"{applyScoreKey}: Set Score [{debugScore}]   {toggleOverlayKey}: Toggle Panel");
        string markerMode = syncSongProgressMarkerToDebugProgress ? "Sync Song Marker [ON]" : "Sync Song Marker [OFF]";
        y = DrawOverlayLabel(x, y, contentWidth, markerMode);
        y = DrawOverlayLabel(x, y, contentWidth, runProgressSummary);
        y = DrawOverlayLabel(x, y, contentWidth, songProgressBestSummary);
        y = DrawOverlayLabel(x, y, contentWidth, songProgressMarkerSummary);
        y = DrawOverlayLabel(x, y, contentWidth, "Edit test values on GameplayDebugTools in Inspector.");
        DrawOverlayLabel(x, y, contentWidth, "Note: F10 is reserved for Unity Recorder.");
    }

    private void ToggleInvincible()
    {
        if (player == null)
            return;

        player.SetDebugInvincible(!player.IsDebugInvincible);
        ShowDebugToast($"Invincibility: {player.IsDebugInvincible}");
        Debug.Log($"DebugTools: Invincibility {(player.IsDebugInvincible ? "ENABLED" : "DISABLED")}");
    }

    private void ToggleAutoFlap()
    {
        if (player == null)
            return;

        bool enabled = !player.IsDebugAutoFlap;
        player.SetDebugAutoFlap(enabled);

        if (enabled && player.IsDebugHoverEnabled)
            player.SetDebugHover(false, 0f);

        ShowDebugToast($"Auto-Flap: {enabled}");
        Debug.Log($"DebugTools: Auto-flap {(enabled ? "ENABLED" : "DISABLED")}");
    }

    private void ToggleHover()
    {
        if (player == null)
            return;

        bool enabled = !player.IsDebugHoverEnabled;
        float targetY = player.transform.position.y;
        player.SetDebugHover(enabled, targetY);

        if (enabled && player.IsDebugAutoFlap)
            player.SetDebugAutoFlap(false);

        ShowDebugToast($"Hover: {enabled}");
        Debug.Log($"DebugTools: Hover {(enabled ? "ENABLED" : "DISABLED")} at y={targetY:0.00}");
    }

    private void ResetProgression()
    {
        PlayerPrefs.SetInt("HighScore", 0);
        PlayerPrefs.Save();
        StampBank.SetCount(0);
        StampBank.ClearDiscoveredStamps();

        var highScoreHuds = Object.FindObjectsByType<HighScoreHud>();
        for (int i = 0; i < highScoreHuds.Length; i++)
            highScoreHuds[i].Refresh();

        if (gameManager != null)
        {
            gameManager.RefreshHighScoreDisplayFromPrefs();
            gameManager.ResetSongProgressBest();
        }

        ShowDebugToast("Reset Progression");
        Debug.Log("DebugTools: Reset HighScore, Stamp_Count, song progress best, and discovered stamps.");
    }

    private void StartRunIfReady()
    {
        if (gameManager == null || gameManager.State != GameManager.GameState.Ready)
            return;

        gameManager.Play();
        ShowDebugToast("Run Started");
        Debug.Log("DebugTools: Forced run start from Ready state.");
    }

    private void ApplyGameSpeed()
    {
        if (gameManager != null)
            gameManager.DebugSetPlaybackSpeed(debugGameSpeed);
        else
            Time.timeScale = Mathf.Max(0.1f, debugGameSpeed);

        ShowDebugToast($"Game Speed: {Mathf.Max(0.1f, debugGameSpeed):0.##}x");
        Debug.Log($"DebugTools: Time scale set to {Mathf.Max(0.1f, debugGameSpeed):0.##}x and run music pitch matched.");
    }

    private void ApplyLapAndProgress()
    {
        if (gameManager == null)
            return;

        gameManager.DebugSetLapAndProgress(debugLap, debugProgress);

        if (syncSongProgressMarkerToDebugProgress)
            gameManager.DebugSetSongProgressBest(debugLap, debugProgress);

        ShowDebugToast($"Lap {debugLap + 1} @ {debugProgress:P0}");
        Debug.Log($"DebugTools: Set lap to {debugLap + 1} and progress to {debugProgress:P0}{(syncSongProgressMarkerToDebugProgress ? " with song marker sync" : string.Empty)}.");
    }

    private void ApplyScore()
    {
        if (gameManager == null)
            return;

        gameManager.DebugSetScore(debugScore);
        ShowDebugToast($"Score: {debugScore}");
        Debug.Log($"DebugTools: Set score to {debugScore}.");
    }

    private void EnsureReferences()
    {
        if (player == null)
            player = Object.FindAnyObjectByType<Player>();

        if (gameManager == null)
            gameManager = Object.FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            debugScore = Mathf.Max(0, debugScore);
            debugLap = Mathf.Max(0, debugLap);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        debugToastText = null;
        debugToastCanvasGroup = null;
    }

    private void ShowDebugToast(string message)
    {
        if (!EnsureDebugToast())
            return;

        debugToastText.text = message;

        if (debugToastRoutine != null)
            StopCoroutine(debugToastRoutine);

        debugToastRoutine = StartCoroutine(PlayDebugToast());
    }

    private bool EnsureDebugToast()
    {
        if (debugToastText != null && debugToastCanvasGroup != null)
            return true;

        Canvas targetCanvas = FindDebugToastCanvas();
        if (targetCanvas == null)
            return false;

        GameObject toastRoot = new("DebugActionToast");
        toastRoot.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRect = toastRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = debugToastAnchoredPosition;
        rootRect.sizeDelta = debugToastSize;

        debugToastCanvasGroup = toastRoot.AddComponent<CanvasGroup>();
        debugToastCanvasGroup.alpha = 0f;
        debugToastCanvasGroup.interactable = false;
        debugToastCanvasGroup.blocksRaycasts = false;

        GameObject textObject = new("DebugActionToastText");
        textObject.transform.SetParent(toastRoot.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        debugToastText = textObject.AddComponent<TextMeshProUGUI>();
        debugToastText.raycastTarget = false;
        debugToastText.alignment = TextAlignmentOptions.Center;
        debugToastText.fontSize = debugToastFontSize;
        debugToastText.fontSizeMax = debugToastFontSize;
        debugToastText.enableAutoSizing = false;
        debugToastText.text = string.Empty;
        debugToastText.color = Color.white;

        ApplyStampCountStyle(debugToastText);
        return true;
    }

    private Canvas FindDebugToastCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i].isActiveAndEnabled)
                continue;

            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                return canvases[i];
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private void ApplyStampCountStyle(TMP_Text targetText)
    {
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text sourceText = texts[i];
            if (sourceText == null || sourceText.name != "StampCount (TMP)")
                continue;

            targetText.font = sourceText.font;
            targetText.fontSharedMaterial = sourceText.fontSharedMaterial;
            targetText.color = sourceText.color;
            return;
        }
    }

    private System.Collections.IEnumerator PlayDebugToast()
    {
        float elapsed = 0f;

        while (elapsed < debugToastDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float flash = (Mathf.Sin(elapsed * debugToastFlashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            debugToastCanvasGroup.alpha = Mathf.Lerp(debugToastMinAlpha, 1f, flash);
            yield return null;
        }

        debugToastCanvasGroup.alpha = 0f;
        debugToastRoutine = null;
    }

    private Rect BuildOverlayRect()
    {
        float x = overlayMargin.x;
        float y = overlayMargin.y;

        if (overlayCorner == OverlayCorner.LowerLeft)
            y = Screen.height - overlayMargin.y - overlaySize.y;

        return new Rect(x, y, overlaySize.x, overlaySize.y);
    }

    private void EnsureOverlayStyle()
    {
        if (overlayTextStyle == null)
        {
            overlayTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                clipping = TextClipping.Overflow
            };
        }

        overlayTextStyle.fontSize = overlayFontSize;
    }

    private float DrawOverlayLabel(float x, float y, float width, string text)
    {
        GUIContent content = new(text);
        float height = Mathf.Max(overlayLineHeight, overlayTextStyle.CalcHeight(content, width));
        GUI.Label(new Rect(x, y, width, height), content, overlayTextStyle);
        return y + height;
    }

    private void OnValidate()
    {
        if (overlaySize.x < 100f) overlaySize.x = 100f;
        if (overlaySize.y < 80f) overlaySize.y = 80f;
        if (overlayFontSize < 10) overlayFontSize = 10;
        if (overlayLineHeight < 12f) overlayLineHeight = 12f;
        if (debugToastDuration < 0.1f) debugToastDuration = 0.1f;
        if (debugToastFlashSpeed < 0.1f) debugToastFlashSpeed = 0.1f;
        if (debugToastFontSize < 12f) debugToastFontSize = 12f;
        if (debugGameSpeed < 0.1f) debugGameSpeed = 0.1f;
        if (debugScore < 0) debugScore = 0;
        if (debugLap < 0) debugLap = 0;
    }
}
#endif
