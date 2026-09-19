using UnityEngine;

public class CornMaker : MonoBehaviour
{
    public int score = 0;

    public void AddScore()
    {
        score++;

        Debug.Log("Score: " + score);
    }
}