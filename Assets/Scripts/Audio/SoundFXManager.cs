using UnityEngine;
using UnityEngine.Audio;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance { get; private set; }

    [Header("Mixer Routing")]
    [Tooltip("Route all SFX through this AudioMixerGroup so the SFX slider works.")]
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Prefab")]
    [Tooltip("AudioSource prefab used to play one-shot SFX. Should have Play On Awake OFF.")]
    [SerializeField] private AudioSource sfxSourcePrefab;

    [Header("Defaults")]
    [SerializeField, Range(0.0f, 1.0f)] private float defaultVolume = 0.3f;

    public float DefaultVolume => defaultVolume;

    private void Awake()
    {
        // Singleton + persistence so SFX works across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (sfxSourcePrefab == null)
        {
            Debug.LogError("SoundFXManager: SFX Source Prefab is not assigned.", this);
        }
    }

    public void PlaySoundFXClip(AudioClip clip, Transform spawnTransform, float volume = -1f)
    {
        if (clip == null) return;
        if (sfxSourcePrefab == null) return;

        AudioSource source = SpawnSource(spawnTransform);

        source.clip = clip;
        source.volume = (volume >= 0f) ? Mathf.Clamp01(volume) : defaultVolume;
        source.Play();

        Destroy(source.gameObject, clip.length);
    }

    public void PlayRandomSoundFXClip(AudioClip[] clips, Transform spawnTransform, float volume = -1f)
    {
        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        PlaySoundFXClip(clips[index], spawnTransform, volume);
    }

    private AudioSource SpawnSource(Transform spawnTransform)
    {
        Vector3 pos = (spawnTransform != null) ? spawnTransform.position : Vector3.zero;

        AudioSource source = Instantiate(sfxSourcePrefab, pos, Quaternion.identity);

        // Route to SFX mixer group so SFX slider affects it
        if (sfxGroup != null)
        {
            source.outputAudioMixerGroup = sfxGroup;
        }
        else
        {
            Debug.LogWarning("SoundFXManager: SFX Mixer Group is not assigned (SFX volume slider won't affect sounds).", this);
        }

        // Ensure 2D unless you explicitly want 3D
        source.spatialBlend = 0f;

        return source;
    }
}
