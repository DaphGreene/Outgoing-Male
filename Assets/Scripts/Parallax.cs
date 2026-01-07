using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationSpeed = 1f;

    [Header("Gameplay Gate (optional)")]
    [SerializeField] private GameManager gameManager;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        // Stop parallax when not actively playing
        if (gameManager != null && !gameManager.IsPlaying)
            return;

        meshRenderer.material.mainTextureOffset +=
            new Vector2(animationSpeed * Time.deltaTime, 0f);
    }
}
