using UnityEngine;

public class HP : MonoBehaviour
{
    public int health = 8;

    private bool isDead = false; 

    public void TakeDamage()
    {
        if (isDead)
        {
            return;
        }

        health--;

        Debug.Log("Health: " + health);

        if (health <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isDead = true;

        Debug.Log("GAME OVER");
    }
}