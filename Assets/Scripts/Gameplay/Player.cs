using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    private int spriteIndex;
    private Vector3 direction;
    private Vector3 startPosition;
    private bool isFrozen;
    public float gravity = -20f;
    public float strength = 4f;
    [SerializeField] private AudioClip[] flapSoundClips;
    [SerializeField] private AudioClip deathSoundClip;
    [SerializeField] private AudioClip scoreSoundClip;
    [SerializeField] private GameManager gameManager;

    [Header("Debug")]
    [SerializeField] private bool debugInvincible;
    [SerializeField] private bool debugAutoFlap;
    [SerializeField] private float debugAutoFlapInterval = 0.2f;
    [SerializeField] private bool debugHoverEnabled;
    [SerializeField] private float debugHoverTargetY = 0.5f;
    [SerializeField] private float debugHoverLerpSpeed = 8f;

    private float debugAutoFlapTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        if (gameManager == null)
            gameManager = Object.FindAnyObjectByType<GameManager>();

        if (gameManager == null)
            Debug.LogError("Player: GameManager reference is missing.", this);
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
    }

    private void OnEnable()
    {
        ResetState();
    }

    public void ResetState()
    {
        transform.position = startPosition;
        direction = Vector3.zero;
        debugAutoFlapTimer = 0f;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (isFrozen)
            direction = Vector3.zero;
    }

    private void Update()
    {
        if (isFrozen)
            return;

        if (debugHoverEnabled)
        {
            direction = Vector3.zero;
            Vector3 hoverPosition = transform.position;
            hoverPosition.y = Mathf.Lerp(hoverPosition.y, debugHoverTargetY, debugHoverLerpSpeed * Time.deltaTime);
            transform.position = hoverPosition;
            return;
        }

        if (debugAutoFlap)
        {
            debugAutoFlapTimer -= Time.deltaTime;
            if (debugAutoFlapTimer <= 0f)
            {
                ApplyFlap();
                debugAutoFlapTimer = debugAutoFlapInterval;
            }
        }

        if (ShouldFlapThisFrame())
            ApplyFlap();

        direction.y += gravity * Time.deltaTime;
        direction.y = Mathf.Clamp(direction.y, -20f, 8f);
        transform.position += direction * Time.deltaTime;
    }

    private bool ShouldFlapThisFrame()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            return true;

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!pointerOverUI && Input.GetMouseButtonDown(0))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began)
                continue;

            bool touchOverUI = EventSystem.current != null &&
                               EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            if (!touchOverUI)
                return true;
        }

        return false;
    }

    private void ApplyFlap()
    {
        direction += Vector3.up * strength;

        if (flapSoundClips != null && flapSoundClips.Length > 0 && SoundFXManager.Instance != null)
        {
            AudioClip randomClip = flapSoundClips[Random.Range(0, flapSoundClips.Length)];
            if (randomClip != null)
                SoundFXManager.Instance.PlaySoundFXClip(randomClip, transform);
        }
    }

    private void AnimateSprite()
    {
        spriteIndex++;

        if (sprites == null || sprites.Length == 0) return;

        if (spriteIndex >= sprites.Length) {
            spriteIndex = 0;
        }

        spriteRenderer.sprite = sprites[spriteIndex];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager == null) return;

        // 🚫 Prevent repeated triggers after GameOver
        if (gameManager.HasGameEnded)
            return;

        if (other.CompareTag("Obstacle"))
        {
            if (debugInvincible)
                return;

            bool wasFatalHit = gameManager.TakeHit();

            // Keep the existing death SFX behavior for the current one-hit setup.
            if (wasFatalHit && deathSoundClip != null && SoundFXManager.Instance != null)
                SoundFXManager.Instance.PlaySoundFXClip(deathSoundClip, transform);
        }
        else if (other.CompareTag("Scoring"))
        {
            gameManager.IncreaseScore();

            if (gameManager.Score == 10 || gameManager.Score % 50 == 0)
            {
                if (scoreSoundClip != null && SoundFXManager.Instance != null)
                    SoundFXManager.Instance.PlaySoundFXClip(scoreSoundClip, transform);
            }
        }
    }

    public void SetDebugInvincible(bool enabled)
    {
        debugInvincible = enabled;
    }

    public void SetDebugAutoFlap(bool enabled)
    {
        debugAutoFlap = enabled;
        debugAutoFlapTimer = 0f;
    }

    public void SetDebugHover(bool enabled, float targetY)
    {
        debugHoverEnabled = enabled;
        debugHoverTargetY = targetY;
        if (!enabled)
            direction = Vector3.zero;
    }

    public bool IsDebugInvincible => debugInvincible;
    public bool IsDebugAutoFlap => debugAutoFlap;
    public bool IsDebugHoverEnabled => debugHoverEnabled;

    private void OnValidate()
    {
        if (debugAutoFlapInterval < 0.05f) debugAutoFlapInterval = 0.05f;
        if (debugHoverLerpSpeed < 0f) debugHoverLerpSpeed = 0f;
    }
}
