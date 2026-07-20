using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    public static bool IsBootSequenceActive { get; private set; }

    [Header("Scene Flow")]
    [Tooltip("Scene loaded after the boot splash finishes.")]
    [SerializeField] private string firstScene = "MainMenu";

    [Header("Splash Logo")]
    [Tooltip("Logo shown over the black boot screen.")]
    [SerializeField] private Sprite splashLogoSprite;
    [Tooltip("Centered size of the splash logo in canvas pixels.")]
    [SerializeField] private Vector2 logoSize = new(320f, 320f);

    [Header("Splash Timing")]
    [Tooltip("Seconds for the logo and music to fade in.")]
    [Min(0f)]
    [SerializeField] private float logoFadeInDuration = 1f;
    [Tooltip("Seconds to hold the logo fully visible before fading it out.")]
    [Min(0f)]
    [SerializeField] private float logoHoldDuration = 1.25f;
    [Tooltip("Seconds for the logo itself to fade out before MainMenu loads.")]
    [Min(0f)]
    [SerializeField] private float logoFadeOutDuration = 0.35f;
    [Tooltip("Seconds for the black overlay to fade away after MainMenu loads.")]
    [Min(0f)]
    [SerializeField] private float sceneFadeOutDuration = 0.75f;

    private static bool hasBootstrapped;
    private GameObject bootstrapRoot;
    private Camera bootCamera;
    private AudioListener bootAudioListener;
    private CanvasGroup splashCanvasGroup;
    private CanvasGroup logoCanvasGroup;

    private void Awake()
    {
        bootstrapRoot = transform.root.gameObject;

        if (hasBootstrapped)
        {
            Destroy(bootstrapRoot);
            return;
        }

        hasBootstrapped = true;
        IsBootSequenceActive = true;
        DontDestroyOnLoad(bootstrapRoot);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureBootPresentationCamera();
        CreateSplashOverlay();
    }

    private void Start()
    {
        StartCoroutine(BootSequence());
    }

    private void OnDestroy()
    {
        if (bootstrapRoot == gameObject || bootstrapRoot == transform.root.gameObject)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedAudioSettings();
    }

    private static void ApplySavedAudioSettings()
    {
        if (SoundMixerManager.Instance != null)
            SoundMixerManager.Instance.ApplySavedVolumes();
    }

    private IEnumerator BootSequence()
    {
        ApplySavedAudioSettings();

        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.PlayFromStart(0f);

        yield return FadeLogoAndMusicIn();

        if (logoHoldDuration > 0f)
            yield return new WaitForSeconds(logoHoldDuration);

        if (logoCanvasGroup != null)
            yield return FadeCanvasGroup(logoCanvasGroup, logoCanvasGroup.alpha, 0f, logoFadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(firstScene);
        while (loadOperation != null && !loadOperation.isDone)
            yield return null;

        ApplySavedAudioSettings();
        RemoveBootPresentationCamera();

        if (splashCanvasGroup != null)
            yield return FadeCanvasGroup(splashCanvasGroup, splashCanvasGroup.alpha, 0f, sceneFadeOutDuration);

        IsBootSequenceActive = false;

        if (splashCanvasGroup != null)
            Destroy(splashCanvasGroup.gameObject);
    }

    private IEnumerator FadeLogoAndMusicIn()
    {
        if (logoCanvasGroup == null && MenuMusicPlayer.Instance == null)
            yield break;

        float duration = Mathf.Max(logoFadeInDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (logoCanvasGroup != null)
                logoCanvasGroup.alpha = t;

            if (MenuMusicPlayer.Instance != null)
                MenuMusicPlayer.Instance.SetVolume(t);

            yield return null;
        }

        if (logoCanvasGroup != null)
            logoCanvasGroup.alpha = 1f;

        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.SetVolume(1f);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void EnsureBootPresentationCamera()
    {
        GameObject cameraObject = new("BootCamera");
        cameraObject.transform.SetParent(bootstrapRoot.transform, false);

        bootCamera = cameraObject.AddComponent<Camera>();
        bootCamera.clearFlags = CameraClearFlags.SolidColor;
        bootCamera.backgroundColor = Color.black;
        bootCamera.cullingMask = 0;
        bootCamera.orthographic = true;
        bootCamera.nearClipPlane = 0.3f;
        bootCamera.farClipPlane = 10f;
        bootCamera.depth = -100f;

        bootAudioListener = cameraObject.AddComponent<AudioListener>();
    }

    private void RemoveBootPresentationCamera()
    {
        if (bootCamera == null && bootAudioListener == null)
            return;

        GameObject cameraObject = bootCamera != null ? bootCamera.gameObject : bootAudioListener.gameObject;
        Destroy(cameraObject);
        bootCamera = null;
        bootAudioListener = null;
    }

    private void CreateSplashOverlay()
    {
        GameObject canvasObject = new("BootSplashCanvas");
        canvasObject.transform.SetParent(bootstrapRoot.transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        splashCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        splashCanvasGroup.alpha = 1f;

        GameObject backgroundObject = new("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = Color.black;

        GameObject logoObject = new("MemoryLeakLogo");
        logoObject.transform.SetParent(canvasObject.transform, false);

        RectTransform logoRect = logoObject.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.anchoredPosition = Vector2.zero;
        logoRect.sizeDelta = logoSize;

        Image logoImage = logoObject.AddComponent<Image>();
        logoImage.preserveAspect = true;
        logoImage.color = Color.white;
        logoImage.sprite = splashLogoSprite;

        logoCanvasGroup = logoObject.AddComponent<CanvasGroup>();
        logoCanvasGroup.alpha = 0f;
    }
}
