using UnityEngine;

public class ParallaxScroller : MonoBehaviour
{
    [Header("Sprite Pieces (left-to-right order)")]
    [SerializeField] private Transform[] pieces;

    [Header("Scroll")]
    [Tooltip("Units per second to move left.")]
    [SerializeField] private float speed = 0.5f;

    [Tooltip("Extra distance offscreen before recycling.")]
    [SerializeField] private float offscreenPadding = 0.5f;

    [Tooltip("Optional: only scroll during gameplay.")]
    [SerializeField] private GameManager gameManager;

    private Camera mainCamera;
    private float[] halfWidths;

    private void Awake()
    {
        if (pieces == null || pieces.Length < 2)
        {
            Debug.LogError("ParallaxScroller: Assign at least 2 pieces.", this);
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ParallaxScroller: No Main Camera found.", this);
            enabled = false;
            return;
        }

        halfWidths = new float[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            var sr = pieces[i].GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError("ParallaxScroller: Each piece needs a SpriteRenderer.", this);
                enabled = false;
                return;
            }

            halfWidths[i] = sr.bounds.extents.x;
        }
    }

    private void Update()
    {
        if (gameManager != null && !gameManager.IsPlaying)
            return;

        float move = speed * Time.deltaTime;

        // Move all pieces left
        for (int i = 0; i < pieces.Length; i++)
            pieces[i].position += Vector3.left * move;

        // Find rightmost edge X
        float rightmostEdge = pieces[0].position.x + halfWidths[0];
        for (int i = 1; i < pieces.Length; i++)
        {
            float edge = pieces[i].position.x + halfWidths[i];
            if (edge > rightmostEdge)
                rightmostEdge = edge;
        }

        float leftEdge;
        if (mainCamera.orthographic)
        {
            float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
            leftEdge = mainCamera.transform.position.x - halfWidth;
        }
        else
        {
            float zDistance = Mathf.Abs(mainCamera.transform.position.z - pieces[0].position.z);
            leftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, zDistance)).x;
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            float rightSide = pieces[i].position.x + halfWidths[i];
            if (rightSide < leftEdge - offscreenPadding)
            {
                float newX = rightmostEdge + halfWidths[i];
                pieces[i].position = new Vector3(newX, pieces[i].position.y, pieces[i].position.z);
                rightmostEdge = newX + halfWidths[i];
            }
        }
    }
}
