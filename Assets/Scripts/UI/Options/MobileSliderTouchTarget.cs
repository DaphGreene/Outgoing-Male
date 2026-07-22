using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class MobileSliderTouchTarget : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float mobileAspectThreshold = 0.75f;
    [SerializeField, Min(0f)] private float extraHeight = 16f;
    [SerializeField] private bool previewInEditor;

    private RectTransform rectTransform;
    private Vector2 baseSizeDelta;
    private bool capturedDefaults;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void OnEnable()
    {
        if (!ShouldApplyLayout())
            return;

        CacheReferences();
        CaptureDefaultsIfNeeded();
        ApplyCurrentLayout();
    }

    private void LateUpdate()
    {
        if (!ShouldApplyLayout())
            return;

        CacheReferences();

        if (!capturedDefaults)
            CaptureDefaultsIfNeeded();

        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        ApplyCurrentLayout();
    }

    private void OnValidate()
    {
        mobileAspectThreshold = Mathf.Max(0.1f, mobileAspectThreshold);
        extraHeight = Mathf.Max(0f, extraHeight);

        if (!ShouldApplyLayout())
            return;

        CacheReferences();
        CaptureDefaultsIfNeeded();
        ApplyCurrentLayout();
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    private void CaptureDefaultsIfNeeded()
    {
        if (capturedDefaults || rectTransform == null)
            return;

        baseSizeDelta = rectTransform.sizeDelta;
        capturedDefaults = true;
    }

    private void ApplyCurrentLayout()
    {
        if (rectTransform == null)
            return;

        Vector2 targetSize = baseSizeDelta;
        if (IsMobileAspect())
            targetSize.y += extraHeight;

        rectTransform.sizeDelta = targetSize;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private bool IsMobileAspect()
    {
        float currentAspect = Mathf.Max(0.01f, (float)Screen.width / Mathf.Max(1f, Screen.height));
        return currentAspect <= mobileAspectThreshold;
    }

    private bool ShouldApplyLayout()
    {
        return Application.isPlaying || previewInEditor;
    }
}
