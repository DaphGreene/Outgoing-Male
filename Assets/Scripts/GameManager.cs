using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public Player player;
    public Text scoreText;
    public Text highScoreText;
    public GameObject playButton;
    public GameObject getReady;
    public GameObject gameOver;
    private int score;
    private int highScore;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        gameOver.SetActive(false);
        
        Pause();
    }

    private void Start() 
    {
        UpdateHighScoreText();
    }

    public void Play()
    {
        score = 0;
        scoreText.text = score.ToString();

        getReady.SetActive(false);
        playButton.SetActive(false);
        gameOver.SetActive(false);

        Time.timeScale = 1f;
        player.enabled = true;

        Cables[] cables = FindObjectsOfType<Cables>();

        for (int i = 0; i < cables.Length; i++) {
            Destroy(cables[i].gameObject);
        }
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
    }

    public void GameOver()
    {
        gameOver.SetActive(true);
        playButton.SetActive(true);

        Pause();
    }

    public void IncreaseScore()
    {
        score++;
        scoreText.text = score.ToString();
        CheckHighScore();

        // Local HighScore
        // PlayerPrefs.SetInt("HighScore", score);
        // PlayerPrefs.GetInt("HighScore");
    }

    public void CheckHighScore()
    {
        if(score > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", score);
            // Dynamically updates HighScore
            UpdateHighScoreText();
        }
    }

    public void UpdateHighScoreText()
    {
        highScoreText.text = $"High Score: {PlayerPrefs.GetInt("HighScore", 0)}";
    }
}
