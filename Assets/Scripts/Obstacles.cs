using UnityEngine;

public class Obstacles : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private GameManager gameManager;

    // Called by the Spawner right after instantiation
    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    private void Update()
    {
        // Stop moving when not actively playing
        if (gameManager != null && !gameManager.IsPlaying)
            return;

        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
    }
}
