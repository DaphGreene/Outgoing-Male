using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";

    public int Score { get; private set; }
    public int HighScore { get; private set; }

    public delegate void ScoreChanged(int score, int highScore);
    public event ScoreChanged OnScoreChanged;

    private void Awake()
    {
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        Score = 0;
    }

    // TODO: Wire to UI after the UI scaling issue is resolved.
    public void ResetScore()
    {
        Score = 0;
        NotifyChanged();
    }

    public void AddPoints(int points)
    {
        if (points <= 0) return;

        Score += points;
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
        }

        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnScoreChanged?.Invoke(Score, HighScore);
    }
}
