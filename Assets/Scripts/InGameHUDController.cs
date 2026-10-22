using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameHUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject ghostTimerPanel;
    [SerializeField] private TMP_Text ghostTimerText;
    [SerializeField] private GameObject[] lifeIndicators;

    public int Score { get; private set; }
    public int Lives { get; private set; }

    private void Awake()
    {
        Score = 0;
        Lives = 3;
        RefreshLives();
        RefreshScore();
        SetGhostTimer(0f);
    }

    public void LoseLife()
    {
        Lives = Mathf.Max(0, Lives - 1);
        RefreshLives();
    }

    private void RefreshLives()
    {
        if (lifeIndicators == null) return;
        for (int index = 0; index < lifeIndicators.Length; index++)
            if (lifeIndicators[index] != null) lifeIndicators[index].SetActive(index < Lives);
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
