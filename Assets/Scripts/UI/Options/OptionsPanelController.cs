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
        SyncSlidersFromSaved();
        WireListenersOnce();
    }

    private void SyncSlidersFromSaved()
    {
        if (SoundMixerManager.Instance == null) return;

        // Set slider values without needing to click anything
        masterSlider.SetValueWithoutNotify(SoundMixerManager.Instance.GetMasterLinear());
        musicSlider.SetValueWithoutNotify(SoundMixerManager.Instance.GetMusicLinear());
        sfxSlider.SetValueWithoutNotify(SoundMixerManager.Instance.GetSfxLinear());
    }

    private void WireListenersOnce()
    {
        if (isWiringDone) return;
        isWiringDone = true;

        masterSlider.onValueChanged.AddListener(v => SoundMixerManager.Instance.SetMasterLinear(v));
        musicSlider.onValueChanged.AddListener(v => SoundMixerManager.Instance.SetMusicLinear(v));
        sfxSlider.onValueChanged.AddListener(v => SoundMixerManager.Instance.SetSfxLinear(v));
    }
}
