using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "ProfessionalMainMenuAssets", menuName = "Conveyor Chef/Professional Main Menu Assets")]
    public class ProfessionalMainMenuAssetCatalog : ScriptableObject
    {
        [Header("Hero")]
        public Sprite background;
        public Sprite logo;
        public Sprite chef;
        public Sprite avatar;

        [Header("Buttons")]
        public Sprite primaryButton;
        public Sprite secondaryButton;
        public Sprite darkPanel;
        public Sprite orangeButton;

        [Header("HUD")]
        public Sprite star;
        public Sprite coin;
        public Sprite plus;

        [Header("Navigation")]
        public Sprite shop;
        public Sprite customize;
        public Sprite settings;
    }
}
