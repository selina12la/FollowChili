using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameLoader : MonoBehaviour
{
    public int sceneIndex = 1; 

    public void StartGame()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}