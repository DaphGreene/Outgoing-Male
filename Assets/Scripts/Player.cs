using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public Sprite[] sprites;
    private int spriteIndex;
    private Vector3 direction;
    public float gravity = -20f;
    public float strength = 4f;
    [SerializeField] private AudioClip[] flapSoundClips;
    [SerializeField] private AudioClip deathSoundClip;
    [SerializeField] private AudioClip scoreSoundClip;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
    }

    private void OnEnable()
    {
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
    }

    private void Update()
    {
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetKeyDown(KeyCode.Space) || (!pointerOverUI && Input.GetMouseButtonDown(0)))
        {
            direction += Vector3.up * strength;

            if (flapSoundClips != null && flapSoundClips.Length > 0 && SoundFXManager.Instance != null)
            {
                AudioClip randomClip = flapSoundClips[Random.Range(0, flapSoundClips.Length)];
                if (randomClip != null)
                    SoundFXManager.Instance.PlaySoundFXClip(randomClip, transform);
            }
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Note: touch UI detection is trickier; we can add it later.
            if (touch.phase == TouchPhase.Began)
            {
                direction += Vector3.up * strength;
            }
        }

        direction.y += gravity * Time.deltaTime;
        direction.y = Mathf.Clamp(direction.y, -20f, 8f);
        transform.position += direction * Time.deltaTime;
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
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null) return;

        // 🚫 Prevent repeated triggers after GameOver
        if (gameManager.HasGameEnded)
            return;

        if (other.CompareTag("Obstacle"))
        {
            gameManager.GameOver();

            // Play death sound ONCE
            if (deathSoundClip != null && SoundFXManager.Instance != null)
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
}
