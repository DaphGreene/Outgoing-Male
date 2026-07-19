using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TMP_Text))]
public class StampCountHud : MonoBehaviour
{
    [Header("Format")]
    [SerializeField] private bool useInlineSpriteIcon = false;
    [SerializeField] private string iconMarkup = "<sprite name=\"HUD_Stamp\">";
    [SerializeField] private string prefix = ": ";

    private TMP_Text label;
    private static StampCountHud fallbackHud;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        StampBank.OnStampCountChanged += HandleStampCountChanged;
        HandleStampCountChanged(StampBank.Count);
    }

    private void OnDisable()
    {
        StampBank.OnStampCountChanged -= HandleStampCountChanged;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureHudInLoadedScene()
    {
        if (fallbackHud != null)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        var texts = Object.FindObjectsByType<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null)
                continue;
            if (text.gameObject.scene != activeScene)
                continue;
            if (text.name != "StampCount (TMP)")
                continue;

            fallbackHud = text.GetComponent<StampCountHud>();
            if (fallbackHud == null)
                fallbackHud = text.gameObject.AddComponent<StampCountHud>();

            fallbackHud.HandleStampCountChanged(StampBank.Count);
            break;
        }
    }

    private void HandleStampCountChanged(int count)
    {
        if (label == null)
            return;

        if (!useInlineSpriteIcon || string.IsNullOrEmpty(iconMarkup))
        {            
            label.text = $"{prefix}{count}";
            return;
        }

        label.text = $"{iconMarkup}{prefix}{count}";
    }
}
