using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private KeyCode toggleOverlayKey = KeyCode.F8;

    [Header("UI")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private OverlayCorner overlayCorner = OverlayCorner.LowerLeft;
    [SerializeField] private Vector2 overlayMargin = new(10f, 10f);
    [SerializeField] private Vector2 overlaySize = new(320f, 136f);
    [SerializeField] private int overlayFontSize = 12;
    [SerializeField] private float overlayLineHeight = 15f;

    private Player player;
    private GameManager gameManager;
    private GUIStyle overlayTextStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Object.FindFirstObjectByType<GameplayDebugTools>() != null)
            return;

        GameObject toolObject = new("GameplayDebugTools");
        Object.DontDestroyOnLoad(toolObject);
        toolObject.AddComponent<GameplayDebugTools>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
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

        if (Input.GetKeyDown(toggleOverlayKey))
            showOverlay = !showOverlay;
    }

    private void OnGUI()
    {
        if (!showOverlay)
            return;

        string invincible = player != null && player.IsDebugInvincible ? "ON" : "OFF";
        string autoFlap = player != null && player.IsDebugAutoFlap ? "ON" : "OFF";
        string hover = player != null && player.IsDebugHoverEnabled ? "ON" : "OFF";

        EnsureOverlayStyle();

        Rect panelRect = BuildOverlayRect();
        GUI.Box(panelRect, GUIContent.none);

        float x = panelRect.x + 10f;
        float y = panelRect.y + 8f;
        float lineHeight = overlayLineHeight;

        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), "Debug Tools", overlayTextStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), $"{toggleInvincibleKey}: Invincible [{invincible}]", overlayTextStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), $"{toggleAutoFlapKey}: Auto-Flap [{autoFlap}]", overlayTextStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), $"{toggleHoverKey}: Hover [{hover}]", overlayTextStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), $"{resetProgressKey}: Reset HighScore + Stamps", overlayTextStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), $"{startRunKey}: Start Run   {toggleOverlayKey}: Toggle Panel", overlayTextStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, panelRect.width - 20f, lineHeight), "Note: F10 is reserved for Unity Recorder.", overlayTextStyle);
    }

    private void ToggleInvincible()
    {
        if (player == null)
            return;

        player.SetDebugInvincible(!player.IsDebugInvincible);
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

        Debug.Log($"DebugTools: Hover {(enabled ? "ENABLED" : "DISABLED")} at y={targetY:0.00}");
    }

    private void ResetProgression()
    {
        PlayerPrefs.SetInt("HighScore", 0);
        PlayerPrefs.Save();
        StampBank.SetCount(0);

        var highScoreHuds = Object.FindObjectsByType<HighScoreHud>(FindObjectsSortMode.None);
        for (int i = 0; i < highScoreHuds.Length; i++)
            highScoreHuds[i].Refresh();

        if (gameManager != null)
            gameManager.RefreshHighScoreDisplayFromPrefs();

        Debug.Log("DebugTools: Reset HighScore and Stamp_Count to 0.");
    }

    private void StartRunIfReady()
    {
        if (gameManager == null || gameManager.State != GameManager.GameState.Ready)
            return;

        gameManager.Play();
        Debug.Log("DebugTools: Forced run start from Ready state.");
    }

    private void EnsureReferences()
    {
        if (player == null)
            player = Object.FindFirstObjectByType<Player>();

        if (gameManager == null)
            gameManager = Object.FindFirstObjectByType<GameManager>();
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
                wordWrap = false,
                clipping = TextClipping.Clip
            };
        }

        overlayTextStyle.fontSize = overlayFontSize;
    }

    private void OnValidate()
    {
        if (overlaySize.x < 100f) overlaySize.x = 100f;
        if (overlaySize.y < 80f) overlaySize.y = 80f;
        if (overlayFontSize < 10) overlayFontSize = 10;
        if (overlayLineHeight < 12f) overlayLineHeight = 12f;
    }
}
#endif
