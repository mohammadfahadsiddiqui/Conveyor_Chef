using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// In-game menu opened by the grid button on the Game toolbar.
    ///
    /// In the original Game UI the grid button opened the settings panel of UI Main Menu
    /// (sound / vibration) together with the replay button. That panel lives on a page
    /// that is hidden during gameplay, so this popup offers the same options on the Game
    /// Main Canvas: Resume, Sound, Vibration, Restart (the existing quit/replay
    /// confirmation) and Home (Level Selection).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameMenuPopup : MonoBehaviour
    {
        [SerializeField] Button openButton;
        [SerializeField] GameObject root;
        [SerializeField] Button backgroundButton;
        [SerializeField] Button resumeButton;
        [SerializeField] Button soundButton;
        [SerializeField] TMP_Text soundLabel;
        [SerializeField] Button vibrationButton;
        [SerializeField] TMP_Text vibrationLabel;
        [SerializeField] Button restartButton;
        [SerializeField] Button homeButton;
        [Tooltip("The HUD pause button; Restart reuses its quit / replay confirmation popup.")]
        [SerializeField] Button pauseButton;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            Wire(openButton, Open);
            Wire(backgroundButton, Close);
            Wire(resumeButton, Close);
            Wire(soundButton, ToggleSound);
            Wire(vibrationButton, ToggleVibration);
            Wire(restartButton, Restart);
            Wire(homeButton, Home);

            if (root != null)
                root.SetActive(false);
        }

        public void Open()
        {
            if (root == null)
                return;

            PlayClick();
            RefreshLabels();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            RaycastController.Disable();

            Debug.Log("[GameMenu] Opened");
        }

        public void Close()
        {
            if (root == null || !root.activeSelf)
                return;

            PlayClick();
            root.SetActive(false);

            if (GameController.IsGameActive)
                RaycastController.Enable();
        }

        private void ToggleSound()
        {
            bool enabled = AudioController.GetVolume() > 0.001f;
            AudioController.SetVolume(enabled ? 0f : 1f);
            PlayClick();
            RefreshLabels();
        }

        private void ToggleVibration()
        {
            AudioController.SetVibrationState(!AudioController.IsVibrationEnabled());
            PlayClick();
            RefreshLabels();
        }

        private void Restart()
        {
            Close();

            // Same flow as the pause button: confirmation popup, then replay.
            if (pauseButton != null)
                pauseButton.onClick.Invoke();
        }

        private void Home()
        {
            root.SetActive(false);
            PlayClick();
            GameController.ReturnToLevelSelection();
        }

        private void RefreshLabels()
        {
            if (soundLabel != null)
                soundLabel.text = AudioController.GetVolume() > 0.001f ? "SOUND: ON" : "SOUND: OFF";

            if (vibrationLabel != null)
                vibrationLabel.text = AudioController.IsVibrationEnabled() ? "VIBRATION: ON" : "VIBRATION: OFF";
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void PlayClick()
        {
            AudioController.PlaySound(AudioController.Sounds.buttonSound);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            Button open, GameObject menuRoot, Button background, Button resume,
            Button sound, TMP_Text soundText, Button vibration, TMP_Text vibrationText,
            Button restart, Button home, Button pause)
        {
            openButton = open;
            root = menuRoot;
            backgroundButton = background;
            resumeButton = resume;
            soundButton = sound;
            soundLabel = soundText;
            vibrationButton = vibration;
            vibrationLabel = vibrationText;
            restartButton = restart;
            homeButton = home;
            pauseButton = pause;
        }
#endif
    }
}
