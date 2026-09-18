using UnityEngine;
using UnityEngine.SceneManagement;
using Watermelon.BusStop;

namespace Watermelon.BusStop
{
    public class sceneloading : MonoBehaviour
    {
        public string gameSceneName = "Game";
        public void scenechange()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            Watermelon.EnhancedLoadingScreen.LoadViaLoadingScreen(gameSceneName);
        }

        public void quit()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            Application.Quit();
        }

        public void buttonsound()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
        }
    }
}
