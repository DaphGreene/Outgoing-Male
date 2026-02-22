using UnityEngine;

[CreateAssetMenu(fileName = "StampDefinition", menuName = "Data/Stamps/Stamp Definition")]
public class StampDefinition : ScriptableObject
{
    public enum FloatPatternType
    {
        SineSlow,
        SineFast,
        Triangle,
        Saw,
        Loop
    }

    [Header("Identity")]
    [SerializeField] private string stampId;
    [SerializeField] private string displayName;

    [Header("Visual")]
    [SerializeField] private Sprite sprite;

    [Header("Gameplay")]
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private float spawnWeight = 1f;
    [SerializeField] private bool enabledForSpawning = true;
    [SerializeField] private float spawnMinHeight = 0.5f;
    [SerializeField] private float spawnMaxHeight = 0.5f;

    [Header("Movement Pattern")]
    [SerializeField] private FloatPatternType floatPattern = FloatPatternType.SineSlow;
    [SerializeField] private float floatAmplitude = 0.04f;
    [SerializeField] private float floatFrequency = 0.9f;
    [SerializeField] private float horizontalSpeedMultiplier = 0.82f;
    [SerializeField] private float floatPhaseOffset = 0f;
    [SerializeField] private float loopRadiusX = 0.15f;
    [SerializeField] private float loopRadiusY = 0.08f;

    public string StampId => stampId;
    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public int ScoreValue => scoreValue;
    public float SpawnWeight => spawnWeight;
    public bool EnabledForSpawning => enabledForSpawning;
    public float SpawnMinHeight => spawnMinHeight;
    public float SpawnMaxHeight => spawnMaxHeight;
    public FloatPatternType FloatPattern => floatPattern;
    public float FloatAmplitude => floatAmplitude;
    public float FloatFrequency => floatFrequency;
    public float HorizontalSpeedMultiplier => horizontalSpeedMultiplier;
    public float FloatPhaseOffset => floatPhaseOffset;
    public float LoopRadiusX => loopRadiusX;
    public float LoopRadiusY => loopRadiusY;

    private void OnValidate()
    {
        if (scoreValue < 0) scoreValue = 0;
        if (spawnWeight < 0f) spawnWeight = 0f;
        if (spawnMaxHeight < spawnMinHeight) spawnMaxHeight = spawnMinHeight;
        if (floatAmplitude < 0f) floatAmplitude = 0f;
        if (floatFrequency < 0f) floatFrequency = 0f;
        if (horizontalSpeedMultiplier < 0.1f) horizontalSpeedMultiplier = 0.1f;
        if (loopRadiusX < 0f) loopRadiusX = 0f;
        if (loopRadiusY < 0f) loopRadiusY = 0f;
    }
}
