using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int corn = 0;
    public TMP_Text cornText; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddCorn()
    {
        corn++;
        Debug.Log("Corn: " + corn);

        if (cornText != null)
        {
            cornText.text = "Corn: " + corn;
        }
    }
}