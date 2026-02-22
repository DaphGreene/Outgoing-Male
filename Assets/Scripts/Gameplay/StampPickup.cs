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
    [SerializeField] private float floatAmplitude = 0.04f;
    [SerializeField] private float floatFrequency = 0.9f;

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
    private float floatBaseY;
    private float floatElapsed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        baseLocalScale = transform.localScale;

        triggerCollider.isTrigger = true;
        ApplyVisual();
    }

    private void OnEnable()
    {
        floatBaseY = transform.position.y;
        floatElapsed = 0f;
    }

    public void Configure(StampDefinition definition, GameManager gm)
    {
        stampDefinition = definition;
        gameManager = gm;
        floatBaseY = transform.position.y;
        floatElapsed = 0f;
        ApplyVisual();
    }

    private void Update()
    {
        if (gameManager != null && !gameManager.IsPlaying)
            return;

        Vector3 position = transform.position;
        float horizontalSpeed = moveSpeed * horizontalSpeedMultiplier;
        position += Vector3.left * horizontalSpeed * Time.deltaTime;

        if (enableFloatMotion)
        {
            floatElapsed += Time.deltaTime;
            float yOffset = Mathf.Sin(floatElapsed * floatFrequency) * floatAmplitude;
            position.y = floatBaseY + yOffset;
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
        if (fallbackStampValue < 0) fallbackStampValue = 0;
        if (maxWorldSize.x < 0.01f) maxWorldSize.x = 0.01f;
        if (maxWorldSize.y < 0.01f) maxWorldSize.y = 0.01f;
        if (floatAmplitude < 0f) floatAmplitude = 0f;
        if (floatFrequency < 0f) floatFrequency = 0f;
    }
}
