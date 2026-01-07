using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    public static SoundMixerManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam = "SFXVolume";

    private const string MasterKey = "Volume_Master";
    private const string MusicKey = "Volume_Music";
    private const string SfxKey   = "Volume_SFX";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySavedVolumes();
    }

    public float GetMasterLinear() => PlayerPrefs.GetFloat(MasterKey, 1f);
    public float GetMusicLinear()  => PlayerPrefs.GetFloat(MusicKey, 1f);
    public float GetSfxLinear()    => PlayerPrefs.GetFloat(SfxKey, 1f);

    public void SetMasterLinear(float value01)
    {
        value01 = Clamp01Safe(value01);
        PlayerPrefs.SetFloat(MasterKey, value01);
        SetMixerDb(masterParam, value01);
    }

    public void SetMusicLinear(float value01)
    {
        Debug.Log($"SetMusicLinear called with {value01}");
        value01 = Clamp01Safe(value01);
        PlayerPrefs.SetFloat(MusicKey, value01);
        SetMixerDb(musicParam, value01);
    }

    public void SetSfxLinear(float value01)
    {
        value01 = Clamp01Safe(value01);
        PlayerPrefs.SetFloat(SfxKey, value01);
        SetMixerDb(sfxParam, value01);
    }

    public void ApplySavedVolumes()
    {
        SetMixerDb(masterParam, GetMasterLinear());
        SetMixerDb(musicParam, GetMusicLinear());
        SetMixerDb(sfxParam, GetSfxLinear());
    }

    private void SetMixerDb(string exposedParam, float linear01)
    {
        if (audioMixer == null) return;

        // Convert 0..1 slider to decibels
        float db = Mathf.Log10(Mathf.Max(linear01, 0.0001f)) * 20f;
        audioMixer.SetFloat(exposedParam, db);
    }

    private float Clamp01Safe(float v) => Mathf.Clamp(v, 0.0001f, 1f);
}

