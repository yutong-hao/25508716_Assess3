using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameHUDController : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject ghostTimerPanel;
    [SerializeField] private TMP_Text ghostTimerText;
    [SerializeField] private GameObject[] lifeIndicators;
    [SerializeField] private GameObject countdownOverlay;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text gameTimerText;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private AudioManager audioManager;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public bool RoundStarted { get; private set; }
    public double ElapsedTime { get; private set; }
    private double countdownElapsed;

    private void Awake()
    {
        Score = 0;
        Lives = 3;
        RefreshLives();
        RefreshScore();
        SetGhostTimer(0f);
        RoundStarted = false;
        ElapsedTime = 0d;
        gameTimerText.text = "00:00:00";
        controls.interactable = false;
        countdownText.text = "3";
        countdownOverlay.SetActive(true);
    }

    private void Start()
    {
        audioManager.BeginRoundCountdown();
    }

    private void Update()
    {
        AdvanceRound(Time.deltaTime);
    }

    private void AdvanceRound(float deltaTime)
    {
        if (!RoundStarted)
        {
            countdownElapsed += deltaTime;
            if (countdownElapsed < 4d)
            {
                int step = (int)countdownElapsed;
                countdownText.text = step < 3 ? (3 - step).ToString() : "GO!";
                return;
            }
            RoundStarted = true;
            countdownOverlay.SetActive(false);
            controls.interactable = true;
            audioManager.CompleteRoundCountdown();
            ElapsedTime = countdownElapsed - 4d;
        }
        else if (Lives > 0) ElapsedTime += deltaTime;
        gameTimerText.text = FormatTime(ElapsedTime);
    }

    public static string FormatTime(double seconds)
    {
        long hundredths = (long)System.Math.Floor(System.Math.Max(0d, seconds) * 100d);
        return $"{hundredths / 6000:D2}:{hundredths / 100 % 60:D2}:{hundredths % 100:D2}";
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
        if (!RoundStarted) return;
        SceneManager.LoadScene("StartScene");
    }
}
