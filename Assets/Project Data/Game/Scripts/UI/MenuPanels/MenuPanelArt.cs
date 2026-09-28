using TMPro;
using UnityEngine;
using Watermelon.SkinStore;

namespace Watermelon
{
    /// <summary>
    /// Art for the main-menu panels (Story, Challenges, Customize, Shop, Collection,
    /// Achievements, Leaderboard), in Resources as "MenuPanelArt". Most images live in
    /// Images/MenuPanels; buttons and currency icons are shared with the rest of the game.
    /// Country arrays follow CountryCatalog order (China, Japan, India, South Korea, Thailand).
    /// </summary>
    [CreateAssetMenu(fileName = "MenuPanelArt", menuName = "Conveyor Chef/Menu Panel Art")]
    public sealed class MenuPanelArt : ScriptableObject
    {
        public const string ResourcePath = "MenuPanelArt";

        [Header("Frame")]
        public TMP_FontAsset font;
        public Sprite frame;
        public Sprite titleRibbon;
        public Sprite closeButton;

        [Header("Buttons (9-sliced)")]
        public Sprite buttonGreen;
        public Sprite buttonOrange;
        public Sprite buttonPurple;
        public Sprite buttonGray;

        [Header("Icons")]
        public Sprite coin;
        public Sprite diamond;
        public Sprite star;
        public Sprite check;
        public Sprite lockBadge;
        public Sprite gem;
        public Sprite chefAvatar;
        public Sprite chefFull;
        public Sprite iconShop;
        public Sprite iconCollection;
        public Sprite iconTrophy;
        public Sprite iconLeaderboard;

        [Header("Countries")]
        public Sprite[] flags = new Sprite[5];
        public Sprite[] landmarks = new Sprite[5];

        [Header("Power-ups (Undo, Hint, Shuffle)")]
        public PUSettings[] powerUps = new PUSettings[3];
        public Sprite[] powerUpIcons = new Sprite[3];

        [Header("Skins")]
        public SkinsDatabase skinsDatabase;

        public Sprite GetFlag(int country) => Get(flags, country);
        public Sprite GetLandmark(int country) => Get(landmarks, country);

        private static Sprite Get(Sprite[] sprites, int index)
        {
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }
    }
}
