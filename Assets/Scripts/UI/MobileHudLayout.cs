using TMPro;
using UnityEngine;

[ExecuteAlways]
public class MobileHudLayout : MonoBehaviour
{
    [System.Serializable]
    private struct RectTransformAdjustments
    {
        public RectTransform target;
        public Vector2 anchoredPositionOffset;
        public Vector2 sizeDeltaOffset;
        public Vector3 localScaleMultiplier;
    }

    [System.Serializable]
    private struct TextSizeAdjustments
    {
        public TMP_Text target;
        public float fontSizeOffset;
        public float fontSizeMinOffset;
        public float fontSizeMaxOffset;
    }

    [Header("Aspect Gate")]
    [SerializeField, Min(0.1f)] private float mobileAspectThreshold = 0.75f;
    [SerializeField] private bool previewInEditor;

    [Header("HUD Rects")]
    [SerializeField] private RectTransformAdjustments songProgressBar;
    [SerializeField] private RectTransformAdjustments lapCounter;
    [SerializeField] private RectTransformAdjustments songProgressPercent;
    [SerializeField] private RectTransformAdjustments getReady;
    [SerializeField] private RectTransformAdjustments startPrompt;

    [Header("Optional Text Size Tweaks")]
    [SerializeField] private TextSizeAdjustments getReadyText;
    [SerializeField] private TextSizeAdjustments startPromptText;

    private Vector2 songProgressBarBasePosition;
    private Vector2 songProgressBarBaseSize;
    private Vector3 songProgressBarBaseScale;
    private Vector2 lapCounterBasePosition;
    private Vector2 lapCounterBaseSize;
    private Vector3 lapCounterBaseScale;
    private Vector2 songProgressPercentBasePosition;
    private Vector2 songProgressPercentBaseSize;
    private Vector3 songProgressPercentBaseScale;
    private Vector2 getReadyBasePosition;
    private Vector2 getReadyBaseSize;
    private Vector3 getReadyBaseScale;
    private Vector2 startPromptBasePosition;
    private Vector2 startPromptBaseSize;
    private Vector3 startPromptBaseScale;
    private float getReadyTextBaseSize;
    private float getReadyTextBaseMinSize;
    private float getReadyTextBaseMaxSize;
    private float startPromptTextBaseSize;
    private float startPromptTextBaseMinSize;
    private float startPromptTextBaseMaxSize;
    private bool capturedDefaults;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void OnEnable()
    {
        if (!ShouldApplyLayout())
            return;

        CaptureDefaultsIfNeeded();
        ApplyCurrentLayout();
    }

    private void LateUpdate()
    {
        if (!ShouldApplyLayout())
            return;

        if (!capturedDefaults)
            CaptureDefaultsIfNeeded();

        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        ApplyCurrentLayout();
    }

    private void OnValidate()
    {
        mobileAspectThreshold = Mathf.Max(0.1f, mobileAspectThreshold);
        ValidateMultiplier(ref songProgressBar);
        ValidateMultiplier(ref lapCounter);
        ValidateMultiplier(ref songProgressPercent);
        ValidateMultiplier(ref getReady);
        ValidateMultiplier(ref startPrompt);

        if (!ShouldApplyLayout())
            return;

        CaptureDefaultsIfNeeded();
        ApplyCurrentLayout();
    }

    private void CaptureDefaultsIfNeeded()
    {
        if (capturedDefaults)
            return;

        CaptureRectDefaults(songProgressBar, ref songProgressBarBasePosition, ref songProgressBarBaseSize, ref songProgressBarBaseScale);
        CaptureRectDefaults(lapCounter, ref lapCounterBasePosition, ref lapCounterBaseSize, ref lapCounterBaseScale);
        CaptureRectDefaults(songProgressPercent, ref songProgressPercentBasePosition, ref songProgressPercentBaseSize, ref songProgressPercentBaseScale);
        CaptureRectDefaults(getReady, ref getReadyBasePosition, ref getReadyBaseSize, ref getReadyBaseScale);
        CaptureRectDefaults(startPrompt, ref startPromptBasePosition, ref startPromptBaseSize, ref startPromptBaseScale);

        CaptureTextDefaults(getReadyText, ref getReadyTextBaseSize, ref getReadyTextBaseMinSize, ref getReadyTextBaseMaxSize);
        CaptureTextDefaults(startPromptText, ref startPromptTextBaseSize, ref startPromptTextBaseMinSize, ref startPromptTextBaseMaxSize);

        capturedDefaults = true;
    }

    private void ApplyCurrentLayout()
    {
        bool useMobileLayout = IsMobileAspect();

        ApplyRectAdjustments(songProgressBar, songProgressBarBasePosition, songProgressBarBaseSize, songProgressBarBaseScale, useMobileLayout);
        ApplyRectAdjustments(lapCounter, lapCounterBasePosition, lapCounterBaseSize, lapCounterBaseScale, useMobileLayout);
        ApplyRectAdjustments(songProgressPercent, songProgressPercentBasePosition, songProgressPercentBaseSize, songProgressPercentBaseScale, useMobileLayout);
        ApplyRectAdjustments(getReady, getReadyBasePosition, getReadyBaseSize, getReadyBaseScale, useMobileLayout);
        ApplyRectAdjustments(startPrompt, startPromptBasePosition, startPromptBaseSize, startPromptBaseScale, useMobileLayout);

        ApplyTextAdjustments(getReadyText, getReadyTextBaseSize, getReadyTextBaseMinSize, getReadyTextBaseMaxSize, useMobileLayout);
        ApplyTextAdjustments(startPromptText, startPromptTextBaseSize, startPromptTextBaseMinSize, startPromptTextBaseMaxSize, useMobileLayout);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private bool ShouldApplyLayout()
    {
        return Application.isPlaying || previewInEditor;
    }

    private bool IsMobileAspect()
    {
        float currentAspect = Mathf.Max(0.01f, (float)Screen.width / Mathf.Max(1f, Screen.height));
        return currentAspect <= mobileAspectThreshold;
    }

    private static void CaptureRectDefaults(RectTransformAdjustments adjustments, ref Vector2 basePosition, ref Vector2 baseSize, ref Vector3 baseScale)
    {
        if (adjustments.target == null)
            return;

        basePosition = adjustments.target.anchoredPosition;
        baseSize = adjustments.target.sizeDelta;
        baseScale = adjustments.target.localScale;
    }

    private static void CaptureTextDefaults(TextSizeAdjustments adjustments, ref float baseSize, ref float baseMinSize, ref float baseMaxSize)
    {
        if (adjustments.target == null)
            return;

        baseSize = adjustments.target.fontSize;
        baseMinSize = adjustments.target.fontSizeMin;
        baseMaxSize = adjustments.target.fontSizeMax;
    }

    private static void ApplyRectAdjustments(
        RectTransformAdjustments adjustments,
        Vector2 basePosition,
        Vector2 baseSize,
        Vector3 baseScale,
        bool useMobileLayout)
    {
        if (adjustments.target == null)
            return;

        if (!useMobileLayout)
        {
            adjustments.target.anchoredPosition = basePosition;
            adjustments.target.sizeDelta = baseSize;
            adjustments.target.localScale = baseScale;
            return;
        }

        adjustments.target.anchoredPosition = basePosition + adjustments.anchoredPositionOffset;
        adjustments.target.sizeDelta = baseSize + adjustments.sizeDeltaOffset;
        adjustments.target.localScale = Vector3.Scale(baseScale, adjustments.localScaleMultiplier);
    }

    private static void ApplyTextAdjustments(
        TextSizeAdjustments adjustments,
        float baseSize,
        float baseMinSize,
        float baseMaxSize,
        bool useMobileLayout)
    {
        if (adjustments.target == null)
            return;

        if (!useMobileLayout)
        {
            adjustments.target.fontSize = baseSize;
            adjustments.target.fontSizeMin = baseMinSize;
            adjustments.target.fontSizeMax = baseMaxSize;
            return;
        }

        adjustments.target.fontSize = Mathf.Max(1f, baseSize + adjustments.fontSizeOffset);
        adjustments.target.fontSizeMin = Mathf.Max(1f, baseMinSize + adjustments.fontSizeMinOffset);
        adjustments.target.fontSizeMax = Mathf.Max(adjustments.target.fontSizeMin, baseMaxSize + adjustments.fontSizeMaxOffset);
    }

    private static void ValidateMultiplier(ref RectTransformAdjustments adjustments)
    {
        if (adjustments.localScaleMultiplier == Vector3.zero)
            adjustments.localScaleMultiplier = Vector3.one;

        adjustments.localScaleMultiplier.x = Mathf.Max(0.01f, adjustments.localScaleMultiplier.x);
        adjustments.localScaleMultiplier.y = Mathf.Max(0.01f, adjustments.localScaleMultiplier.y);
        adjustments.localScaleMultiplier.z = Mathf.Max(0.01f, adjustments.localScaleMultiplier.z);
    }
}
