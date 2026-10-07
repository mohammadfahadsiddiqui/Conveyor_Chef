using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// In-game pause menu, opened by the HUD pause button and the grid button on the Game
    /// toolbar. Gameplay is frozen (Time.timeScale = 0) while it is open.
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
        [Tooltip("The HUD pause button (opens this menu through UIGame).")]
        [SerializeField] Button pauseButton;

        [Header("Toggle Art")]
        [SerializeField] Sprite soundOnSprite;
        [SerializeField] Sprite soundOffSprite;
        [SerializeField] Sprite vibrationOnSprite;
        [SerializeField] Sprite vibrationOffSprite;

        public bool IsOpen => root != null && root.activeSelf;

        private bool paused;
        private float timeScaleBeforePause = 1f;

        private void Awake()
        {
            Wire(openButton, Open);
            Wire(pauseButton, Open);      // the HUD pause button pauses with this menu
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
            if (root == null || root.activeSelf)
                return;

            PlayClick();
            RefreshLabels();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            RaycastController.Disable();
            Pause();

            Debug.Log("[GameMenu] Opened");
        }

        public void Close()
        {
            if (root == null || !root.activeSelf)
                return;

            PlayClick();
            root.SetActive(false);
            Resume();

            if (GameController.IsGameActive)
                RaycastController.Enable();
        }

        private void Pause()
        {
            if (paused)
                return;
            paused = true;
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        private void Resume()
        {
            if (!paused)
                return;
            paused = false;
            Time.timeScale = timeScaleBeforePause;
        }

        private void OnDisable()
        {
            Resume();
        }

        private void OnDestroy()
        {
            Resume();
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

            // "Restart?" confirmation, then the level is replayed.
            UIGame game = GetComponentInParent<UIGame>();
            if (game == null)
                game = UIController.GetPage<UIGame>();
            if (game != null)
                game.ShowReplayConfirmation();
        }

        private void Home()
        {
            root.SetActive(false);
            Resume();
            PlayClick();
            GameController.ReturnToLevelSelection();
        }

        // The toggle art shows the current state (SOUND ON / SOUND OFF, ...); plain
        // labels are only used when no art is assigned.
        private void RefreshLabels()
        {
            bool soundOn = AudioController.GetVolume() > 0.001f;
            bool vibrationOn = AudioController.IsVibrationEnabled();

            SetState(soundButton, soundLabel, soundOn, soundOnSprite, soundOffSprite, "SOUND");
            SetState(vibrationButton, vibrationLabel, vibrationOn, vibrationOnSprite, vibrationOffSprite, "VIBRATION");
        }

        private static void SetState(Button button, TMP_Text label, bool on, Sprite onSprite, Sprite offSprite, string name)
        {
            Sprite sprite = on ? onSprite : offSprite;
            Image image = button != null ? button.targetGraphic as Image : null;
            if (image != null && sprite != null)
                image.sprite = sprite;

            if (label != null)
            {
                label.gameObject.SetActive(sprite == null);
                label.text = name + (on ? ": ON" : ": OFF");
            }
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
            Button restart, Button home, Button pause,
            Sprite soundOn, Sprite soundOff, Sprite vibrationOn, Sprite vibrationOff)
        {
            soundOnSprite = soundOn;
            soundOffSprite = soundOff;
            vibrationOnSprite = vibrationOn;
            vibrationOffSprite = vibrationOff;
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
