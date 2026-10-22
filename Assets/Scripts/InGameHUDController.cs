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
    [SerializeField] private PacStudentController player;
    [SerializeField] private PowerPelletController ghosts;
    [SerializeField] private int levelNumber = 1;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public bool RoundStarted { get; private set; }
    public double ElapsedTime { get; private set; }
    public bool IsGameOver { get; private set; }
    public int RemainingPellets { get; private set; }
    private double countdownElapsed;
    private float gameOverElapsed;

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
        if (IsGameOver)
        {
            gameOverElapsed += deltaTime;
            if (gameOverElapsed >= 3f) SceneManager.LoadScene("StartScene");
            return;
        }
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
        if (IsGameOver) return;
        Lives = Mathf.Max(0, Lives - 1);
        RefreshLives();
        if (Lives == 0) EndRound();
    }

    public void RegisterPellets(int count)
    {
        RemainingPellets = count;
    }

    public void PelletCollected()
    {
        if (IsGameOver || !RoundStarted || RemainingPellets <= 0) return;
        RemainingPellets--;
        if (RemainingPellets == 0) EndRound();
    }

    private void EndRound()
    {
        if (IsGameOver || !RoundStarted) return;
        IsGameOver = true;
        gameOverElapsed = 0f;
        controls.interactable = false;
        countdownText.text = "Game Over";
        countdownOverlay.SetActive(true);
        player.StopForGameOver();
        ghosts.SuspendForPlayerDeath();
        LevelRecords.SaveIfBetter(levelNumber, Score, ElapsedTime);
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
        if (points <= 0 || IsGameOver) return;
        Score += points;
        RefreshScore();
    }

    private void RefreshScore()
    {
        if (scoreText != null) scoreText.text = Score.ToString("D6");
    }

    public void ReturnToStartScene()
    {
        if (!RoundStarted || IsGameOver) return;
        SceneManager.LoadScene("StartScene");
    }
}
