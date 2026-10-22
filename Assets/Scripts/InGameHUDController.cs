using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameHUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    public int Score { get; private set; }

    private void Awake()
    {
        Score = 0;
        RefreshScore();
    }

    public void AddScore(int points)
    {
        if (points <= 0) return;
        Score += points;
        RefreshScore();
    }

    private void RefreshScore()
    {
        if (scoreText != null) scoreText.text = Score.ToString("D6");
    }

    public void ReturnToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}
