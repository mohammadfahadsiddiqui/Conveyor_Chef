using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon.BusStop
{
    public class sceneloading : MonoBehaviour
    {
        public string gameSceneName = "Game";

        private void Start()
        {
            // The existing menu scene uses this component to open LevelSelection.
            // Only bootstrap the redesigned presentation when that menu flow is active;
            // other scenes using sceneloading keep their original behaviour.
            if (SceneManager.GetActiveScene().name == "menu")
            {
                MainMenuRedesignController redesign = GetComponent<MainMenuRedesignController>();
                if (redesign == null)
                    redesign = gameObject.AddComponent<MainMenuRedesignController>();

                redesign.Initialise(this);
            }
        }

        public void scenechange()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            SceneManager.LoadScene(gameSceneName);
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
