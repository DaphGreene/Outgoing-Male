using UnityEngine;

[ExecuteAlways]
public class MobileGameplayLayout : MonoBehaviour
{
    [System.Serializable]
    private struct TransformAdjustments
    {
        public Transform target;
        public Vector3 localPositionOffset;
        public Vector3 localScaleMultiplier;
    }

    [Header("Aspect Gate")]
    [SerializeField, Min(0.1f)] private float mobileAspectThreshold = 0.75f;
    [SerializeField] private bool previewInEditor;

    [Header("Gameplay Pieces")]
    [SerializeField] private TransformAdjustments player;
    [SerializeField] private TransformAdjustments backgroundGroup;
    [SerializeField] private TransformAdjustments frontCloudGroup;
    [SerializeField] private TransformAdjustments groundGroup;

    private Vector3 playerBasePosition;
    private Vector3 playerBaseScale;
    private Vector3 backgroundBasePosition;
    private Vector3 backgroundBaseScale;
    private Vector3 frontCloudBasePosition;
    private Vector3 frontCloudBaseScale;
    private Vector3 groundBasePosition;
    private Vector3 groundBaseScale;
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
        ValidateMultiplier(ref player);
        ValidateMultiplier(ref backgroundGroup);
        ValidateMultiplier(ref frontCloudGroup);
        ValidateMultiplier(ref groundGroup);

        if (!ShouldApplyLayout())
            return;

        CaptureDefaultsIfNeeded();
        ApplyCurrentLayout();
    }

    private void CaptureDefaultsIfNeeded()
    {
        if (capturedDefaults)
            return;

        if (player.target != null)
        {
            playerBasePosition = player.target.localPosition;
            playerBaseScale = player.target.localScale;
        }

        if (backgroundGroup.target != null)
        {
            backgroundBasePosition = backgroundGroup.target.localPosition;
            backgroundBaseScale = backgroundGroup.target.localScale;
        }

        if (frontCloudGroup.target != null)
        {
            frontCloudBasePosition = frontCloudGroup.target.localPosition;
            frontCloudBaseScale = frontCloudGroup.target.localScale;
        }

        if (groundGroup.target != null)
        {
            groundBasePosition = groundGroup.target.localPosition;
            groundBaseScale = groundGroup.target.localScale;
        }

        capturedDefaults = true;
    }

    private void ApplyCurrentLayout()
    {
        bool useMobileLayout = IsMobileAspect();

        ApplyAdjustments(player, playerBasePosition, playerBaseScale, useMobileLayout);
        ApplyAdjustments(backgroundGroup, backgroundBasePosition, backgroundBaseScale, useMobileLayout);
        ApplyAdjustments(frontCloudGroup, frontCloudBasePosition, frontCloudBaseScale, useMobileLayout);
        ApplyAdjustments(groundGroup, groundBasePosition, groundBaseScale, useMobileLayout);

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

    private static void ApplyAdjustments(TransformAdjustments adjustments, Vector3 basePosition, Vector3 baseScale, bool useMobileLayout)
    {
        if (adjustments.target == null)
            return;

        if (!useMobileLayout)
        {
            adjustments.target.localPosition = basePosition;
            adjustments.target.localScale = baseScale;
            return;
        }

        adjustments.target.localPosition = basePosition + adjustments.localPositionOffset;
        adjustments.target.localScale = Vector3.Scale(baseScale, adjustments.localScaleMultiplier);
    }

    private static void ValidateMultiplier(ref TransformAdjustments adjustments)
    {
        if (adjustments.localScaleMultiplier == Vector3.zero)
            adjustments.localScaleMultiplier = Vector3.one;

        adjustments.localScaleMultiplier.x = Mathf.Max(0.01f, adjustments.localScaleMultiplier.x);
        adjustments.localScaleMultiplier.y = Mathf.Max(0.01f, adjustments.localScaleMultiplier.y);
        adjustments.localScaleMultiplier.z = Mathf.Max(0.01f, adjustments.localScaleMultiplier.z);
    }
}
