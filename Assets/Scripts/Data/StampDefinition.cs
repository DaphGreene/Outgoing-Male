using UnityEngine;

[CreateAssetMenu(fileName = "StampDefinition", menuName = "Data/Stamps/Stamp Definition")]
public class StampDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string stampId;
    [SerializeField] private string displayName;

    [Header("Visual")]
    [SerializeField] private Sprite sprite;

    [Header("Gameplay")]
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private float spawnWeight = 1f;

    public string StampId => stampId;
    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public int ScoreValue => scoreValue;
    public float SpawnWeight => spawnWeight;

    private void OnValidate()
    {
        if (scoreValue < 0) scoreValue = 0;
        if (spawnWeight < 0f) spawnWeight = 0f;
    }
}
