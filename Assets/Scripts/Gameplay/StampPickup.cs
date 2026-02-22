using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class StampPickup : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField, Range(0.1f, 1.5f)] private float horizontalSpeedMultiplier = 0.82f;
    [FormerlySerializedAs("fallbackScoreValue")]
    [SerializeField] private int fallbackStampValue = 1;
    [SerializeField] private float destroyX = -16f;

    [Header("Visual")]
    [SerializeField] private bool normalizeSpriteSize = true;
    [SerializeField] private Vector2 maxWorldSize = new(0.5f, 0.5f);
    [SerializeField] private bool autoFitColliderToSprite = true;

    [Header("Float Motion")]
    [SerializeField] private bool enableFloatMotion = true;
    [SerializeField] private StampDefinition.FloatPatternType floatPattern = StampDefinition.FloatPatternType.SineSlow;
    [SerializeField] private float floatAmplitude = 0.04f;
    [SerializeField] private float floatFrequency = 0.9f;
    [SerializeField] private float floatPhaseOffset = 0f;
    [SerializeField] private float loopRadiusX = 0.15f;
    [SerializeField] private float loopRadiusY = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool debugDrawColliderWhenSelected = true;
    [SerializeField] private bool debugDrawColliderAlways = false;
    [SerializeField] private Color debugColliderColor = new(0.2f, 1f, 0.4f, 0.9f);
    [SerializeField] private Color debugColliderMissingColor = new(1f, 0.3f, 0.3f, 0.9f);

    [Header("Audio")]
    [SerializeField] private AudioClip collectSoundClip;

    private StampDefinition stampDefinition;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCollider;
    private bool hasBeenCollected;
    private Vector3 baseLocalScale;
    private float driftBaseX;
    private float floatBaseY;
    private float floatElapsed;
    private float activeHorizontalSpeedMultiplier;
    private StampDefinition.FloatPatternType activeFloatPattern;
    private float activeFloatAmplitude;
    private float activeFloatFrequency;
    private float activeFloatPhaseOffset;
    private float activeLoopRadiusX;
    private float activeLoopRadiusY;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        baseLocalScale = transform.localScale;

        triggerCollider.isTrigger = true;
        RefreshMovementProfile();
        ApplyVisual();
    }

    private void OnEnable()
    {
        driftBaseX = transform.position.x;
        floatBaseY = transform.position.y;
        floatElapsed = 0f;
    }

    public void Configure(StampDefinition definition, GameManager gm)
    {
        stampDefinition = definition;
        gameManager = gm;
        driftBaseX = transform.position.x;
        floatBaseY = transform.position.y;
        floatElapsed = 0f;
        RefreshMovementProfile();
        ApplyVisual();
    }

    private void Update()
    {
        if (gameManager != null && !gameManager.IsPlaying)
            return;

        float horizontalSpeed = moveSpeed * activeHorizontalSpeedMultiplier;
        driftBaseX -= horizontalSpeed * Time.deltaTime;

        Vector3 position = transform.position;
        position.x = driftBaseX;
        position.y = floatBaseY;

        if (enableFloatMotion)
        {
            floatElapsed += Time.deltaTime;
            Vector2 offset = EvaluateFloatOffset(
                floatElapsed + activeFloatPhaseOffset,
                activeFloatPattern,
                activeFloatFrequency,
                activeFloatAmplitude,
                activeLoopRadiusX,
                activeLoopRadiusY);
            position.x += offset.x;
            position.y += offset.y;
        }

        transform.position = position;

        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenCollected)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (gameManager != null && !gameManager.IsPlaying)
            return;

        hasBeenCollected = true;
        triggerCollider.enabled = false;
        spriteRenderer.enabled = false;

        if (fallbackStampValue > 0)
            StampBank.AddStamps(fallbackStampValue);

        if (collectSoundClip != null && SoundFXManager.Instance != null)
            SoundFXManager.Instance.PlaySoundFXClip(collectSoundClip, transform);

        Destroy(gameObject);
    }

    private void ApplyVisual()
    {
        if (spriteRenderer == null)
            return;

        if (stampDefinition != null && stampDefinition.Sprite != null)
            spriteRenderer.sprite = stampDefinition.Sprite;

        ApplyScaleToCurrentSprite();
    }

    private void RefreshMovementProfile()
    {
        activeHorizontalSpeedMultiplier = horizontalSpeedMultiplier;
        activeFloatPattern = floatPattern;
        activeFloatAmplitude = floatAmplitude;
        activeFloatFrequency = floatFrequency;
        activeFloatPhaseOffset = floatPhaseOffset;
        activeLoopRadiusX = loopRadiusX;
        activeLoopRadiusY = loopRadiusY;

        if (stampDefinition == null)
            return;

        activeHorizontalSpeedMultiplier = stampDefinition.HorizontalSpeedMultiplier;
        activeFloatPattern = stampDefinition.FloatPattern;
        activeFloatAmplitude = stampDefinition.FloatAmplitude;
        activeFloatFrequency = stampDefinition.FloatFrequency;
        activeFloatPhaseOffset = stampDefinition.FloatPhaseOffset;
        activeLoopRadiusX = stampDefinition.LoopRadiusX;
        activeLoopRadiusY = stampDefinition.LoopRadiusY;
    }

    private static Vector2 EvaluateFloatOffset(
        float t,
        StampDefinition.FloatPatternType pattern,
        float frequency,
        float amplitude,
        float loopX,
        float loopY)
    {
        switch (pattern)
        {
            case StampDefinition.FloatPatternType.SineFast:
                return new Vector2(0f, Mathf.Sin(t * frequency * 1.8f) * amplitude);

            case StampDefinition.FloatPatternType.Triangle:
                return new Vector2(0f, (Mathf.PingPong(t * frequency, 1f) * 2f - 1f) * amplitude);

            case StampDefinition.FloatPatternType.Saw:
                return new Vector2(0f, (Mathf.Repeat(t * frequency, 1f) * 2f - 1f) * amplitude);

            case StampDefinition.FloatPatternType.Loop:
                float angle = t * frequency * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(angle) * loopX, Mathf.Sin(angle) * loopY);

            case StampDefinition.FloatPatternType.SineSlow:
            default:
                return new Vector2(0f, Mathf.Sin(t * frequency) * amplitude);
        }
    }

    private void ApplyScaleToCurrentSprite()
    {
        transform.localScale = baseLocalScale;

        if (spriteRenderer != null && spriteRenderer.sprite != null && normalizeSpriteSize)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                float scaleX = maxWorldSize.x / spriteSize.x;
                float scaleY = maxWorldSize.y / spriteSize.y;
                float scaleMultiplier = Mathf.Min(scaleX, scaleY);

                if (scaleMultiplier > 0f)
                    transform.localScale = baseLocalScale * scaleMultiplier;
            }
        }

        FitColliderToSprite();
    }

    private void FitColliderToSprite()
    {
        if (!autoFitColliderToSprite || triggerCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        if (triggerCollider is not BoxCollider2D boxCollider)
            return;

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        boxCollider.size = spriteBounds.size;
        boxCollider.offset = spriteBounds.center;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawColliderAlways)
            return;

        DrawColliderGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawColliderWhenSelected)
            return;

        DrawColliderGizmo();
    }

    private void DrawColliderGizmo()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Gizmos.color = debugColliderMissingColor;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            return;
        }

        Gizmos.color = boxCollider.enabled ? debugColliderColor : debugColliderMissingColor;

        Vector2 halfSize = boxCollider.size * 0.5f;
        Vector2 offset = boxCollider.offset;

        Vector3 localA = new(offset.x - halfSize.x, offset.y - halfSize.y, 0f);
        Vector3 localB = new(offset.x - halfSize.x, offset.y + halfSize.y, 0f);
        Vector3 localC = new(offset.x + halfSize.x, offset.y + halfSize.y, 0f);
        Vector3 localD = new(offset.x + halfSize.x, offset.y - halfSize.y, 0f);

        Vector3 worldA = transform.TransformPoint(localA);
        Vector3 worldB = transform.TransformPoint(localB);
        Vector3 worldC = transform.TransformPoint(localC);
        Vector3 worldD = transform.TransformPoint(localD);

        Gizmos.DrawLine(worldA, worldB);
        Gizmos.DrawLine(worldB, worldC);
        Gizmos.DrawLine(worldC, worldD);
        Gizmos.DrawLine(worldD, worldA);
    }

    private void OnValidate()
    {
        if (moveSpeed < 0f) moveSpeed = 0f;
        if (horizontalSpeedMultiplier < 0.1f) horizontalSpeedMultiplier = 0.1f;
        if (fallbackStampValue < 0) fallbackStampValue = 0;
        if (maxWorldSize.x < 0.01f) maxWorldSize.x = 0.01f;
        if (maxWorldSize.y < 0.01f) maxWorldSize.y = 0.01f;
        if (floatAmplitude < 0f) floatAmplitude = 0f;
        if (floatFrequency < 0f) floatFrequency = 0f;
        if (loopRadiusX < 0f) loopRadiusX = 0f;
        if (loopRadiusY < 0f) loopRadiusY = 0f;
    }
}
