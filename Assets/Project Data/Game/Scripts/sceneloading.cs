using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneloading : MonoBehaviour
{
    public string gameSceneName = "Game";
    public void scenechange()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void quit()
    {
        Application.Quit();
    }
}
