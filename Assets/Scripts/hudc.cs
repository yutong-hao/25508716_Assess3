using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class hudc : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreTxt;
    [SerializeField] private GameObject ghPanel;
    [SerializeField] private TMP_Text ghTimerTxt;
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private GameObject cdOverlay;
    [SerializeField] private TMP_Text cdText;
    [SerializeField] private TMP_Text timerTxt;
    [SerializeField] private CanvasGroup ctrls;
    [SerializeField] private am audMgr;
    [SerializeField] private PacStudentController pc;
    [SerializeField] private pp gh;
    [SerializeField] private int lvlNum = 1;
    [SerializeField] private bool deliveryObj;
    [SerializeField] private Sprite victoryCat;
    [SerializeField] private Sprite loseCat;
    [SerializeField] private Sprite resultCorn;
    [SerializeField] private Sprite resultPel;

    public int Score { get; private set; }
    public int Lives { get; private set; }
    public bool Started { get; private set; }
    public double Elapsed { get; private set; }
    public bool GameOver { get; private set; }
    public int PelletsLeft { get; private set; }

    private double cdElapsed;
    private float overT;

    private void Awake()
    {
        Score = 0;
        Lives = 3;

        RefreshLives();
        RefreshScore();
        SetGhostTimer(0f);

        Started = false;
        Elapsed = 0d;

        timerTxt.text = "00:00:00";
        ctrls.interactable = false;
        cdText.text = "3";
        cdOverlay.SetActive(true);
    }

    private void Start()
    {
        audMgr.BeginRoundCountdown();
    }

    private void Update()
    {
        AdvanceRound(Time.deltaTime);
    }

    private void AdvanceRound(float dt)
    {
        if (GameOver)
        {
            overT = overT + dt;

            if (overT >= 3f)
            {
                SceneManager.LoadScene("StartScene");
            }

            return;
        }

        if (!Started)
        {
            cdElapsed = cdElapsed + dt;

            if (cdElapsed < 4d)
            {
                int step = (int)cdElapsed;

                if (step < 3)
                {
                    cdText.text = (3 - step).ToString();
                }
                else
                {
                    cdText.text = "GO!";
                }

                return;
            }

            Started = true;
            cdOverlay.SetActive(false);
            ctrls.interactable = true;
            audMgr.CompleteRoundCountdown();

            Elapsed = cdElapsed - 4d;
        }
        else if (Lives > 0)
        {
            Elapsed = Elapsed + dt;
        }

        timerTxt.text = FormatTime(Elapsed);
    }

    public static string FormatTime(double secs)
    {
        double safeSecs = System.Math.Max(0d, secs);
        long centis = (long)System.Math.Floor(safeSecs * 100d);

        long mins = centis / 6000;
        long leftSecs = centis / 100 % 60;
        long leftCentis = centis % 100;

        string minTxt = mins.ToString("D2");
        string secTxt = leftSecs.ToString("D2");
        string centiTxt = leftCentis.ToString("D2");

        return minTxt + ":" + secTxt + ":" + centiTxt;
    }

    public void LoseLife()
    {
        if (GameOver)
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
        PelletsLeft = count;
    }

    public void PelletCollected()
    {
        if (GameOver || !Started || PelletsLeft <= 0)
        {
            return;
        }

        PelletsLeft = PelletsLeft - 1;

        if (PelletsLeft == 0 && !deliveryObj)
        {
            EndRound("Game Over");
        }
    }

    public void CompleteDelivery()
    {
        if (deliveryObj)
        {
            EndRound("Delivery Complete!");
        }
    }

    private void EndRound(string message = "Game Over")
    {
        if (GameOver || !Started)
        {
            return;
        }

        GameOver = true;
        overT = 0f;
        ctrls.interactable = false;

        cdText.text = message;
        cdOverlay.SetActive(true);

        pc.StopForGameOver();
        gh.SuspendForPlayerDeath();

        bool newHS = Score > lr.LoadScore(lvlNum);
        lr.SaveIfBetter(lvlNum, Score, Elapsed);

        if (lvlNum == 1 && !deliveryObj)
        {
            ShowResult(Lives > 0 && PelletsLeft == 0, newHS);
        }
        else if (lvlNum == 2 && deliveryObj)
        {
            ShowResult(Lives > 0, newHS);
        }
    }

    private void ShowResult(bool victory, bool newHS)
    {
        Color cream = new Color32(255, 242, 205, 255);
        Color gold = new Color32(255, 191, 70, 255);
        Color border = victory ? gold : new Color32(163, 149, 115, 255);
        Color navy = new Color32(73, 54, 47, 255);
        Color muted = new Color32(178, 194, 213, 255);

        cdText.gameObject.SetActive(false);
        cdOverlay.transform.SetAsLastSibling();
        cdOverlay.GetComponent<Image>().color = new Color32(12, 24, 44, 220);

        ResultBox("ResultShadow", cdOverlay.transform,
            new Vector2(6f, -8f), new Vector2(566f, 506f), new Color32(5, 14, 28, 170));
        RectTransform card = ResultBox(lvlNum == 1 ? "LevelOneResult" : "LevelTwoResult", cdOverlay.transform,
            Vector2.zero, new Vector2(560f, 500f), border);
        ResultBox("CardBackground", card, Vector2.zero, new Vector2(556f, 496f), navy);
        ResultBox("TopAccent", card, new Vector2(0f, 248f), new Vector2(96f, 4f), border);

        string lvlLabel = lvlNum == 1 ? "LEVEL 1  /  MIDNIGHT SNACK CHASE" : "LEVEL 2  /  POPCORN DELIVERY";
        ResultText("LevelLabel", card, lvlLabel,
            new Vector2(0f, 209f), new Vector2(480f, 24f), 14f, muted);
        ResultText("ResultTitle", card, lvlNum == 2 && victory ? "VICTORY!" : "GAME OVER",
            new Vector2(0f, 160f), new Vector2(480f, 65f), 48f, victory ? gold : cream);
        ResultSprite("ResultCat", card, victory ? victoryCat : loseCat,
            new Vector2(0f, 57f), new Vector2(144f, 112f));

        if (victory)
        {
            ResultSprite("PopcornLeft", card, resultCorn, new Vector2(-143f, 48f), new Vector2(90f, 90f));
            ResultSprite("PopcornRight", card, resultCorn, new Vector2(143f, 48f), new Vector2(90f, 90f));
            ResultSprite("PelletLeft", card, resultPel, new Vector2(-206f, 57f), new Vector2(14f, 19f));
            ResultSprite("PelletRight", card, resultPel, new Vector2(206f, 57f), new Vector2(14f, 19f));
        }

        if (newHS)
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
        ResultText("FinalTime", card, FormatTime(Elapsed), new Vector2(90f, -130f), new Vector2(220f, 35f), 28f, cream,
            TextAlignmentOptions.MidlineRight);
        ResultText("ReturnLabel", card, "Returning to menu...",
            new Vector2(0f, -208f), new Vector2(440f, 30f), 16f, muted);
    }

    private RectTransform ResultBox(string objName, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject element = new GameObject(objName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)element.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        Image image = element.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void ResultSprite(string objName, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        RectTransform rect = ResultBox(objName, parent, pos, size, Color.white);
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
    }

    private void ResultText(string objName, Transform parent, string value, Vector2 pos, Vector2 size,
        float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI label = Instantiate((TextMeshProUGUI)cdText, parent);
        label.name = objName;
        label.gameObject.SetActive(true);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = size;
        label.rectTransform.anchoredPosition = pos;
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
        if (lifeIcons == null)
        {
            return;
        }

        for (int i = 0; i < lifeIcons.Length; i = i + 1)
        {
            if (lifeIcons[i] != null)
            {
                if (i < Lives)
                {
                    lifeIcons[i].SetActive(true);
                }
                else
                {
                    lifeIcons[i].SetActive(false);
                }
            }
        }
    }

    public void SetGhostTimer(float secs)
    {
        if (ghPanel != null)
        {
            if (secs > 0f)
            {
                ghPanel.SetActive(true);
            }
            else
            {
                ghPanel.SetActive(false);
            }
        }

        if (ghTimerTxt != null)
        {
            float safeSecs = Mathf.Max(0f, secs);
            int displaySec = Mathf.CeilToInt(safeSecs);

            ghTimerTxt.text = displaySec.ToString();
        }
    }

    public void AddScore(int points)
    {
        if (points <= 0 || GameOver)
        {
            return;
        }

        Score = Score + points;
        RefreshScore();
    }

    private void RefreshScore()
    {
        if (scoreTxt != null)
        {
            scoreTxt.text = Score.ToString("D6");
        }
    }

    public void ReturnToStartScene()
    {
        if (!Started || GameOver)
        {
            return;
        }

        SceneManager.LoadScene("StartScene");
    }
}
