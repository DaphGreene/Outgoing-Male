using UnityEngine;

public class MenuMusicPlayer : MonoBehaviour
{
    public static MenuMusicPlayer Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Ensure this object is a root object before persisting across scenes.
        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);

        if (SoundMixerManager.Instance != null)
            SoundMixerManager.Instance.ApplySavedVolumes();
    }

    private void Start()
    {
        ResumeMenuMusic();
    }

    public void ResumeMenuMusic()
    {
        if (musicSource == null)
            return;

        if (!musicSource.isPlaying)
            musicSource.UnPause();

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void PauseMenuMusic()
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        musicSource.Pause();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
