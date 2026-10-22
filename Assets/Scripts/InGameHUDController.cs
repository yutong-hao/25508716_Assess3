using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameHUDController : MonoBehaviour
{
    public void ReturnToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}
