using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class AdaptiveOrthographicCamera : MonoBehaviour
{
    [Header("Framing")]
    [SerializeField, Min(0.1f)] private float desktopOrthographicSize = 4f;
    [SerializeField, Min(0.1f)] private float mobileOrthographicSize = 5.25f;

    [Header("Aspect Blend")]
    [SerializeField, Min(0.1f)] private float desktopAspectRatio = 16f / 9f;
    [SerializeField, Min(0.1f)] private float mobileAspectRatio = 9f / 16f;

    private Camera targetCamera;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void OnEnable()
    {
        CacheReferences();
        ApplyFraming();
    }

    private void LateUpdate()
    {
        CacheReferences();

        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            return;

        ApplyFraming();
    }

    private void OnValidate()
    {
        desktopOrthographicSize = Mathf.Max(0.1f, desktopOrthographicSize);
        mobileOrthographicSize = Mathf.Max(0.1f, mobileOrthographicSize);
        desktopAspectRatio = Mathf.Max(0.1f, desktopAspectRatio);
        mobileAspectRatio = Mathf.Max(0.1f, mobileAspectRatio);

        if (mobileAspectRatio > desktopAspectRatio)
            mobileAspectRatio = desktopAspectRatio;

        CacheReferences();
        ApplyFraming();
    }

    private void CacheReferences()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void ApplyFraming()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float currentAspect = Mathf.Max(0.01f, (float)Screen.width / Mathf.Max(1f, Screen.height));
        float blend = Mathf.InverseLerp(desktopAspectRatio, mobileAspectRatio, currentAspect);
        float targetSize = Mathf.Lerp(desktopOrthographicSize, mobileOrthographicSize, blend);

        targetCamera.orthographicSize = targetSize;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }
}
