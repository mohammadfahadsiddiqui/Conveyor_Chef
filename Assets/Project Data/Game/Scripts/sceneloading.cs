using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
                SanitiseMenuRuntime();
            }
        }

        private void SanitiseMenuRuntime()
        {
            // The menu scene already owns an EventSystem. The redesign must never leave
            // more than one enabled EventSystem because Unity logs that warning every frame.
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            EventSystem keeper = EventSystem.current;
            if (keeper == null || !keeper.enabled || !keeper.gameObject.activeInHierarchy)
            {
                keeper = null;

                for (int i = 0; i < eventSystems.Length; i++)
                {
                    EventSystem candidate = eventSystems[i];
                    if (candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
                    {
                        keeper = candidate;
                        break;
                    }
                }
            }

            if (keeper == null && eventSystems.Length > 0)
            {
                keeper = eventSystems[0];
                keeper.enabled = true;
            }

            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem eventSystem = eventSystems[i];
                if (eventSystem == null || eventSystem == keeper)
                    continue;

                eventSystem.enabled = false;

                BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();
                for (int j = 0; j < inputModules.Length; j++)
                    inputModules[j].enabled = false;
            }

            // FredokaOne does not contain the triangular play glyph used by the redesign.
            // Replace it before the first normal menu render so TMP does not spam warnings.
            TextMeshProUGUI[] labels = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].name == "PlayLabel")
                {
                    labels[i].text = "PLAY";
                    break;
                }
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
