using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartSceneController : MonoBehaviour
{
    [SerializeField] private RectTransform borderBounds;
    [SerializeField] private RectTransform[] borderSnacks;
    [SerializeField] private float borderSpeed = 75f;
    [SerializeField] private string levelOneScene = "SampleScene";
    [SerializeField] private TMP_Text[] highScoreTexts;
    [SerializeField] private TMP_Text[] bestTimeTexts;

    private float borderDistance;
    private bool loadingLevel;

    private void Start()
    {
        for (int index = 0; index < highScoreTexts.Length; index++)
            highScoreTexts[index].text = LevelRecords.LoadScore(index + 1).ToString();
        for (int index = 0; index < bestTimeTexts.Length; index++)
            bestTimeTexts[index].text = InGameHUDController.FormatTime(LevelRecords.LoadTime(index + 1));
    }

    void LateUpdate()
    {
        if (borderBounds == null || borderSnacks == null || borderSnacks.Length == 0) return;

        Rect bounds = borderBounds.rect;
        float perimeter = 2f * (bounds.width + bounds.height);
        if (bounds.width <= 0f || bounds.height <= 0f) return;

        borderDistance = Mathf.Repeat(borderDistance + borderSpeed * Time.unscaledDeltaTime, perimeter);
        for (int index = 0; index < borderSnacks.Length; index++)
        {
            if (borderSnacks[index] == null) continue;

            float distance = Mathf.Repeat(borderDistance + perimeter * index / borderSnacks.Length, perimeter);
            Vector2 position;
            if (distance < bounds.width)
                position = new Vector2(bounds.xMin + distance, bounds.yMax);
            else if (distance < bounds.width + bounds.height)
                position = new Vector2(bounds.xMax, bounds.yMax - (distance - bounds.width));
            else if (distance < 2f * bounds.width + bounds.height)
                position = new Vector2(bounds.xMax - (distance - bounds.width - bounds.height), bounds.yMin);
            else
                position = new Vector2(bounds.xMin, bounds.yMin + (distance - 2f * bounds.width - bounds.height));

            Vector2 anchor = new Vector2((position.x - bounds.xMin) / bounds.width, (position.y - bounds.yMin) / bounds.height);
            borderSnacks[index].anchorMin = anchor;
            borderSnacks[index].anchorMax = anchor;
            borderSnacks[index].anchoredPosition = Vector2.zero;
        }
    }

    public void LoadLevelOne()
    {
        if (loadingLevel) return;
        loadingLevel = true;
        SceneManager.LoadScene(levelOneScene);
    }
}
