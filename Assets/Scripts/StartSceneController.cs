using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

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
    private Vector3 lastMousePosition;

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
        for (int i = 0; i < highScoreTexts.Length; i = i + 1)
        {
            highScoreTexts[i].text = LevelRecords.LoadScore(i + 1).ToString();
        }

        for (int i = 0; i < bestTimeTexts.Length; i = i + 1)
        {
            string timeText = InGameHUDController.FormatTime(
                LevelRecords.LoadTime(i + 1)
            );

            bestTimeTexts[i].text = timeText;
        }
    }

    private void Update()
    {
        EventSystem events = EventSystem.current;
        if (events == null || loadingLevel) return;

        if (Input.mousePosition != lastMousePosition)
        {
            events.SetSelectedGameObject(null);
        }
        else if (events.currentSelectedGameObject == null &&
            (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f || Input.GetButtonDown("Submit")))
        {
            events.SetSelectedGameObject(events.firstSelectedGameObject);
        }

        lastMousePosition = Input.mousePosition;
    }

    private void LateUpdate()
    {
        if (borderBounds == null || borderSnacks == null)
        {
            return;
        }

        if (borderSnacks.Length == 0)
        {
            return;
        }

        float width = borderBounds.rect.width;
        float height = borderBounds.rect.height;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        float perimeter = 2f * (width + height);

        borderDistance = borderDistance + borderSpeed * Time.unscaledDeltaTime;
        borderDistance = Mathf.Repeat(borderDistance, perimeter);

        float spacing = perimeter / borderSnacks.Length;

        for (int i = 0; i < borderSnacks.Length; i = i + 1)
        {
            if (borderSnacks[i] == null)
            {
                continue;
            }

            float distance = borderDistance + spacing * i;
            distance = Mathf.Repeat(distance, perimeter);

            Vector2 anchor;

            if (distance < width)
            {
                float x = distance / width;
                anchor = new Vector2(x, 1);
            }
            else if (distance < width + height)
            {
                float down = distance - width;
                float y = 1 - down / height;

                anchor = new Vector2(1, y);
            }
            else if (distance < width * 2 + height)
            {
                float left = distance - width - height;
                float x = 1 - left / width;

                anchor = new Vector2(x, 0);
            }
            else
            {
                float up = distance - width * 2 - height;
                float y = up / height;

                anchor = new Vector2(0, y);
            }

            borderSnacks[i].anchorMin = anchor;
            borderSnacks[i].anchorMax = anchor;
            borderSnacks[i].anchoredPosition = new Vector2(0, 0);
        }
    }

    public void LoadLevelOne()
    {
        if (loadingLevel)
        {
            return;
        }

        loadingLevel = true;
        SceneManager.LoadScene(levelOneScene);
    }

    public void LoadLevelTwo()
    {
        if (loadingLevel)
        {
            return;
        }

        loadingLevel = true;
        SceneManager.LoadScene("InnovationScene");
    }
}
