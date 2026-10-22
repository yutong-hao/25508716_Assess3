using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

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
    [SerializeField] private bool deliveryObjective;
    [SerializeField] private Sprite victoryCat;
    [SerializeField] private Sprite defeatedCat;
    [SerializeField] private Sprite resultPopcorn;
    [SerializeField] private Sprite resultPellet;

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
            gameOverElapsed = gameOverElapsed + deltaTime;

            if (gameOverElapsed >= 3f)
            {
                SceneManager.LoadScene("StartScene");
            }

            return;
        }

        if (!RoundStarted)
        {
            countdownElapsed = countdownElapsed + deltaTime;

            if (countdownElapsed < 4d)
            {
                int step = (int)countdownElapsed;

                if (step < 3)
                {
                    countdownText.text = (3 - step).ToString();
                }
                else
                {
                    countdownText.text = "GO!";
                }

                return;
            }

            RoundStarted = true;
            countdownOverlay.SetActive(false);
            controls.interactable = true;
            audioManager.CompleteRoundCountdown();

            ElapsedTime = countdownElapsed - 4d;
        }
        else if (Lives > 0)
        {
            ElapsedTime = ElapsedTime + deltaTime;
        }

        gameTimerText.text = FormatTime(ElapsedTime);
    }

    public static string FormatTime(double seconds)
    {
        double safeSeconds = System.Math.Max(0d, seconds);
        long hundredths = (long)System.Math.Floor(safeSeconds * 100d);

        long minutes = hundredths / 6000;
        long remainingSeconds = hundredths / 100 % 60;
        long remainingHundredths = hundredths % 100;

        string minutesText = minutes.ToString("D2");
        string secondsText = remainingSeconds.ToString("D2");
        string hundredthsText = remainingHundredths.ToString("D2");

        return minutesText + ":" + secondsText + ":" + hundredthsText;
    }

    public void LoseLife()
    {
        if (IsGameOver)
        {
            return;
        }
        
        Lives = Mathf.Max(0, Lives - 1);
        RefreshLives();

        if (Lives == 0)
        {
            EndRound();
        }
    }

    public void RegisterPellets(int count)
    {
        RemainingPellets = count;
    }

    public void PelletCollected()
    {
        if (IsGameOver || !RoundStarted || RemainingPellets <= 0)
        {
            return;
        }

        RemainingPellets = RemainingPellets - 1;

        if (RemainingPellets == 0 && !deliveryObjective)
        {
            EndRound("Game Over");
        }
    }

    public void CompleteDelivery()
    {
        if (deliveryObjective)
        {
            EndRound("Delivery Complete!");
        }
    }

    private void EndRound(string message = "Game Over")
    {
        if (IsGameOver || !RoundStarted)
        {
            return;
        }

        IsGameOver = true;
        gameOverElapsed = 0f;
        controls.interactable = false;

        countdownText.text = message;
        countdownOverlay.SetActive(true);

        player.StopForGameOver();
        ghosts.SuspendForPlayerDeath();

        bool newHighScore = Score > LevelRecords.LoadScore(levelNumber);
        LevelRecords.SaveIfBetter(levelNumber, Score, ElapsedTime);

        if (levelNumber == 1 && !deliveryObjective)
        {
            ShowResult(Lives > 0 && RemainingPellets == 0, newHighScore);
        }
        else if (levelNumber == 2 && deliveryObjective)
        {
            ShowResult(Lives > 0, newHighScore);
        }
    }

    private void ShowResult(bool victory, bool newHighScore)
    {
        Color cream = new Color32(255, 242, 205, 255);
        Color gold = new Color32(255, 191, 70, 255);
        Color border = victory ? gold : new Color32(163, 149, 115, 255);
        Color navy = new Color32(73, 54, 47, 255);
        Color muted = new Color32(178, 194, 213, 255);

        countdownText.gameObject.SetActive(false);
        countdownOverlay.transform.SetAsLastSibling();
        countdownOverlay.GetComponent<Image>().color = new Color32(12, 24, 44, 220);

        ResultBox("ResultShadow", countdownOverlay.transform,
            new Vector2(6f, -8f), new Vector2(566f, 506f), new Color32(5, 14, 28, 170));
        RectTransform card = ResultBox(levelNumber == 1 ? "LevelOneResult" : "LevelTwoResult", countdownOverlay.transform,
            Vector2.zero, new Vector2(560f, 500f), border);
        ResultBox("CardBackground", card, Vector2.zero, new Vector2(556f, 496f), navy);
        ResultBox("TopAccent", card, new Vector2(0f, 248f), new Vector2(96f, 4f), border);

        string levelLabel = levelNumber == 1 ? "LEVEL 1  /  MIDNIGHT SNACK CHASE" : "LEVEL 2  /  POPCORN DELIVERY";
        ResultText("LevelLabel", card, levelLabel,
            new Vector2(0f, 209f), new Vector2(480f, 24f), 14f, muted);
        ResultText("ResultTitle", card, levelNumber == 2 && victory ? "VICTORY!" : "GAME OVER",
            new Vector2(0f, 160f), new Vector2(480f, 65f), 48f, victory ? gold : cream);
        ResultSprite("ResultCat", card, victory ? victoryCat : defeatedCat,
            new Vector2(0f, 57f), new Vector2(144f, 112f));

        if (victory)
        {
            ResultSprite("PopcornLeft", card, resultPopcorn, new Vector2(-143f, 48f), new Vector2(90f, 90f));
            ResultSprite("PopcornRight", card, resultPopcorn, new Vector2(143f, 48f), new Vector2(90f, 90f));
            ResultSprite("PelletLeft", card, resultPellet, new Vector2(-206f, 57f), new Vector2(14f, 19f));
            ResultSprite("PelletRight", card, resultPellet, new Vector2(206f, 57f), new Vector2(14f, 19f));
        }

        if (newHighScore)
        {
            ResultText("RecordLabel", card, "NEW HIGH SCORE!",
                new Vector2(0f, -19f), new Vector2(440f, 25f), 16f, gold);
        }

        ResultBox("StatsBackground", card, new Vector2(0f, -104f), new Vector2(456f, 112f),
            new Color32(48, 35, 30, 255));
        ResultText("ScoreLabel", card, "SCORE", new Vector2(-130f, -79f), new Vector2(140f, 30f), 18f, cream,
            TextAlignmentOptions.MidlineLeft);
        ResultText("FinalScore", card, Score.ToString("D6"), new Vector2(90f, -79f), new Vector2(220f, 35f), 28f, gold,
            TextAlignmentOptions.MidlineRight);
        ResultText("TimeLabel", card, "TIME", new Vector2(-130f, -130f), new Vector2(140f, 30f), 18f, cream,
            TextAlignmentOptions.MidlineLeft);
        ResultText("FinalTime", card, FormatTime(ElapsedTime), new Vector2(90f, -130f), new Vector2(220f, 35f), 28f, cream,
            TextAlignmentOptions.MidlineRight);
        ResultText("ReturnLabel", card, "Returning to menu...",
            new Vector2(0f, -208f), new Vector2(440f, 30f), 16f, muted);
    }

    private RectTransform ResultBox(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject element = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)element.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = element.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void ResultSprite(string objectName, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        RectTransform rect = ResultBox(objectName, parent, position, size, Color.white);
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
    }

    private void ResultText(string objectName, Transform parent, string value, Vector2 position, Vector2 size,
        float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI label = Instantiate((TextMeshProUGUI)countdownText, parent);
        label.name = objectName;
        label.gameObject.SetActive(true);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = size;
        label.rectTransform.anchoredPosition = position;
        label.text = value;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * 0.65f;
        label.fontSizeMax = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
    }

    private void RefreshLives()
    {
        if (lifeIndicators == null)
        {
            return;
        }

        for (int i = 0; i < lifeIndicators.Length; i = i + 1)
        {
            if (lifeIndicators[i] != null)
            {
                if (i < Lives)
                {
                    lifeIndicators[i].SetActive(true);
                }
                else
                {
                    lifeIndicators[i].SetActive(false);
                }
            }
        }
    }

    public void SetGhostTimer(float seconds)
    {
        if (ghostTimerPanel != null)
        {
            if (seconds > 0f)
            {
                ghostTimerPanel.SetActive(true);
            }
            else
            {
                ghostTimerPanel.SetActive(false);
            }
        }

        if (ghostTimerText != null)
        {
            float safeSeconds = Mathf.Max(0f, seconds);
            int displaySeconds = Mathf.CeilToInt(safeSeconds);

            ghostTimerText.text = displaySeconds.ToString();
        }
    }

    public void AddScore(int points)
    {
        if (points <= 0 || IsGameOver)
        {
            return;
        }

        Score = Score + points;
        RefreshScore();
    }

    private void RefreshScore()
    {
        if (scoreText != null)
        {
            scoreText.text = Score.ToString("D6");
        }
    }

    public void ReturnToStartScene()
    {
        if (!RoundStarted || IsGameOver)
        {
            return;
        }

        SceneManager.LoadScene("StartScene");
    }
}
