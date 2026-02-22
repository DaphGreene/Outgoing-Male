using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class HighScoreHud : MonoBehaviour
{
    [SerializeField] private string labelPrefix = "High Score: ";
    [SerializeField] private string playerPrefsKey = "HighScore";

    private TMP_Text textLabel;

    private void Awake()
    {
        textLabel = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (textLabel == null)
            return;

        int highScore = PlayerPrefs.GetInt(playerPrefsKey, 0);
        textLabel.text = $"{labelPrefix}{highScore}";
    }
}
