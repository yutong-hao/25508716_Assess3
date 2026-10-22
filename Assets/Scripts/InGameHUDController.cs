using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameHUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject ghostTimerPanel;
    [SerializeField] private TMP_Text ghostTimerText;

    public int Score { get; private set; }

    private void Awake()
    {
        Score = 0;
        RefreshScore();
        SetGhostTimer(0f);
    }

    public void SetGhostTimer(float seconds)
    {
        if (ghostTimerPanel != null) ghostTimerPanel.SetActive(seconds > 0f);
        if (ghostTimerText != null) ghostTimerText.text = Mathf.CeilToInt(Mathf.Max(0f, seconds)).ToString();
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
