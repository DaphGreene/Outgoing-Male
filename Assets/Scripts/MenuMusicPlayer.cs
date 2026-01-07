using UnityEngine;

public class MenuMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;

    private void Start()
    {
        if (SoundMixerManager.Instance != null)
            SoundMixerManager.Instance.ApplySavedVolumes();

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
    }
}

