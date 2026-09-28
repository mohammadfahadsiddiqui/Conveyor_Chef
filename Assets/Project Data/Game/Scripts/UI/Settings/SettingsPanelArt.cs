using TMPro;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Art for the shared Settings panel. Lives in a Resources folder as
    /// "SettingsPanelArt" so the panel can be opened from any scene.
    /// </summary>
    [CreateAssetMenu(fileName = "SettingsPanelArt", menuName = "Conveyor Chef/Settings Panel Art")]
    public sealed class SettingsPanelArt : ScriptableObject
    {
        public const string ResourcePath = "SettingsPanelArt";

        public Sprite panel;
        public Sprite title;
        public Sprite closeButton;
        public Sprite soundOn;
        public Sprite soundOff;
        public Sprite vibrationOn;
        public Sprite vibrationOff;
        public TMP_FontAsset font;
    }
}
