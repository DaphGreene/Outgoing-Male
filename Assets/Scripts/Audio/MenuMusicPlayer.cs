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
        if (Bootstrapper.IsBootSequenceActive)
            return;

        ResumeMenuMusic();
    }

    public void PlayFromStart(float volume = 1f)
    {
        if (musicSource == null)
            return;

        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
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

    public void SetVolume(float volume)
    {
        if (musicSource == null)
            return;

        musicSource.volume = Mathf.Clamp01(volume);
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
