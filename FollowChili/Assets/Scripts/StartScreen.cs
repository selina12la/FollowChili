using UnityEngine;

public class StartScreen : MonoBehaviour
{
    public GameObject startScreenUI;

    public void StartGame()
    {
        startScreenUI.SetActive(false);
        Time.timeScale = 1f; 
    }
}
