using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderSfxPreview : MonoBehaviour, IPointerUpHandler, IEndDragHandler
{
    [Header("Preview")]
    [SerializeField] private AudioSource previewSource;
    [SerializeField] private AudioClip previewClip;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PlayPreview();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PlayPreview();
    }

    private void PlayPreview()
    {
        if (previewSource == null || previewClip == null) return;

        // 🔇 Do not preview if SFX slider is effectively muted
        if (slider != null && slider.value <= 0.001f) return;

        // Match gameplay loudness baseline
        if (SoundFXManager.Instance != null)
            previewSource.volume = SoundFXManager.Instance.DefaultVolume;

        previewSource.PlayOneShot(previewClip);
    }
}

