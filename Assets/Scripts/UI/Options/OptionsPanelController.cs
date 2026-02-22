using UnityEngine;
using UnityEngine.UI;

public class OptionsPanelController : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isWiringDone;

    private void OnEnable()
    {
        EnsureMixerApplied();
        SyncSlidersFromSaved();
        WireListenersOnce();
    }

    private void SyncSlidersFromSaved()
    {
        SoundMixerManager mixerManager = GetMixerManager();
        if (mixerManager == null) return;

        // Set slider values without needing to click anything
        masterSlider.SetValueWithoutNotify(mixerManager.GetMasterLinear());
        musicSlider.SetValueWithoutNotify(mixerManager.GetMusicLinear());
        sfxSlider.SetValueWithoutNotify(mixerManager.GetSfxLinear());
    }

    private void WireListenersOnce()
    {
        if (isWiringDone) return;
        isWiringDone = true;

        masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }

    private void OnMasterSliderChanged(float value)
    {
        SoundMixerManager mixerManager = GetMixerManager();
        if (mixerManager == null) return;
        mixerManager.SetMasterLinear(value);
    }

    private void OnMusicSliderChanged(float value)
    {
        SoundMixerManager mixerManager = GetMixerManager();
        if (mixerManager == null) return;
        mixerManager.SetMusicLinear(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        SoundMixerManager mixerManager = GetMixerManager();
        if (mixerManager == null) return;
        mixerManager.SetSfxLinear(value);
    }

    private void EnsureMixerApplied()
    {
        SoundMixerManager mixerManager = GetMixerManager();
        if (mixerManager == null) return;
        mixerManager.ApplySavedVolumes();
    }

    private static SoundMixerManager GetMixerManager()
    {
        if (SoundMixerManager.Instance != null)
            return SoundMixerManager.Instance;

        return Object.FindFirstObjectByType<SoundMixerManager>();
    }
}
