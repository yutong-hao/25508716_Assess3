using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class startc : MonoBehaviour
{
    [SerializeField] private RectTransform bdBounds;
    [SerializeField] private RectTransform[] bdSnacks;
    [SerializeField] private float bdSpd = 75f;
    [SerializeField] private string lvlOneScene = "SampleScene";
    [SerializeField] private TMP_Text[] hsTexts;
    [SerializeField] private TMP_Text[] btTexts;

    private float bdDist;
    private bool loading;
    private Vector3 lastMousePos;

    private void Start()
    {
        lastMousePos = Input.mousePosition;
        for (int i = 0; i < hsTexts.Length; i = i + 1)
        {
            hsTexts[i].text = lr.LoadScore(i + 1).ToString();
        }

        for (int i = 0; i < btTexts.Length; i = i + 1)
        {
            string timeTxt = hudc.FormatTime(
                lr.LoadTime(i + 1)
            );

            btTexts[i].text = timeTxt;
        }
    }

    private void Update()
    {
        EventSystem events = EventSystem.current;
        if (events == null || loading) return;

        if (Input.mousePosition != lastMousePos)
        {
            events.SetSelectedGameObject(null);
        }
        else if (events.currentSelectedGameObject == null &&
            (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f || Input.GetButtonDown("Submit")))
        {
            events.SetSelectedGameObject(events.firstSelectedGameObject);
        }

        lastMousePos = Input.mousePosition;
    }

    private void LateUpdate()
    {
        if (bdBounds == null || bdSnacks == null)
        {
            return;
        }

        if (bdSnacks.Length == 0)
        {
            return;
        }

        float width = bdBounds.rect.width;
        float height = bdBounds.rect.height;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        float perim = 2f * (width + height);

        bdDist = bdDist + bdSpd * Time.unscaledDeltaTime;
        bdDist = Mathf.Repeat(bdDist, perim);

        float gap = perim / bdSnacks.Length;

        for (int i = 0; i < bdSnacks.Length; i = i + 1)
        {
            if (bdSnacks[i] == null)
            {
                continue;
            }

            float distance = bdDist + gap * i;
            distance = Mathf.Repeat(distance, perim);

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

            bdSnacks[i].anchorMin = anchor;
            bdSnacks[i].anchorMax = anchor;
            bdSnacks[i].anchoredPosition = new Vector2(0, 0);
        }
    }

    public void LoadLevelOne()
    {
        if (loading)
        {
            return;
        }

        loading = true;
        SceneManager.LoadScene(lvlOneScene);
    }

    public void LoadLevelTwo()
    {
        if (loading)
        {
            return;
        }

        loading = true;
        SceneManager.LoadScene("InnovationScene");
    }
}
