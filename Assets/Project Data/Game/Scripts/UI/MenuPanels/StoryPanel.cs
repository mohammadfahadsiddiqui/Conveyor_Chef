using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// STORY: the chef's journey through every country, one tab per continent. Each chapter
    /// shows its landmark, flag, level and star progress, and PLAY (opens that country's Level
    /// Selection), REPLAY for finished chapters, or a lock until the previous country is complete.
    /// </summary>
    public sealed class StoryPanel : MenuPanel
    {
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const string SelectedCountryLevelStartKey = "CC_CountryMap_SelectedLevelStart";
        private const string FromCountryMapKey = "CC_LevelSelection_FromCountryMap";

        // WorldCatalog continent order.
        private static readonly string[] ContinentTabs = { "ASIA", "N.AMER", "S.AMER", "EUROPE", "AFRICA", "OCEANIA", "POLAR" };

        protected override string Title => "STORY";
        protected override string[] Tabs => ContinentTabs;

        // Opens on the continent of the chapter being played.
        protected override int OpeningTab => Mathf.Max(0, WorldCatalog.ContinentOfCountry(CurrentChapter));

        private static int CurrentChapter
        {
            get
            {
                for (int country = 0; country < CountryCatalog.CountryCount; country++)
                {
                    if (!PlayerStats.IsCountryComplete(country))
                        return country;
                }
                return CountryCatalog.CountryCount - 1;
            }
        }

        protected override void BuildContent()
        {
            BuildIntro();

            int first = SelectedTab * WorldCatalog.CountriesPerContinent;
            for (int i = 0; i < WorldCatalog.CountriesPerContinent; i++)
                BuildChapter(first + i);
        }

        private static readonly Color32 Heading = new Color32(160, 56, 18, 255);

        private void BuildIntro()
        {
            RectTransform card = MenuUI.Card("Intro", Content, 290f, true);

            Image chef = MenuUI.Image("Chef", card, Art.chefFull);
            MenuUI.Anchor(chef.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(130f, 260f));

            TextMeshProUGUI heading = MenuUI.Text("Heading", card, "THE CHEF'S JOURNEY", 44f, Heading, TextAlignmentOptions.Left);
            MenuUI.Anchor(heading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(176f, -26f), new Vector2(580f, 56f));

            TextMeshProUGUI body = MenuUI.Text("Body", card,
                "Cook your way around the world and master every country's street food.",
                30f, MenuUI.TextDark, TextAlignmentOptions.TopLeft, true);
            MenuUI.Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(176f, -90f), new Vector2(580f, 84f));

            PlayerStats.GetProgress(out int completed, out int stars, out _);
            RectTransform stats = MenuUI.Rect("Stats", card);
            MenuUI.Anchor(stats, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(176f, 26f), new Vector2(580f, 56f));
            HorizontalLayoutGroup row = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleLeft;
            row.spacing = 26f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            MenuUI.Pill("Countries", stats, Art.GetFlag(0), PlayerStats.CountriesCompleted + "/" + CountryCatalog.CountryCount, 32f, MenuUI.TextDark);
            MenuUI.Pill("Levels", stats, Art.iconTrophy, completed + "/" + PlayerStats.TotalLevels, 32f, MenuUI.TextDark);
            MenuUI.Pill("Stars", stats, Art.star, stars + "/" + PlayerStats.TotalStars, 32f, MenuUI.TextDark);
        }

        private void BuildChapter(int country)
        {
            bool unlocked = PlayerStats.IsCountryUnlocked(country);
            bool complete = PlayerStats.IsCountryComplete(country);
            int levels = PlayerStats.GetCountryLevelsCompleted(country);
            int stars = PlayerStats.GetCountryStars(country);
            bool current = unlocked && !complete;

            RectTransform card = MenuUI.Card("Chapter " + (country + 1), Content, 590f, current);

            // Landmark banner across the top of the card.
            Image banner = MenuUI.Image("Banner", card, MenuUI.Rounded(22, 0, new Color32(186, 226, 246, 255), Color.clear), null, false);
            MenuUI.Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(750f, 240f));
            Sprite landmarkSprite = GetCountryArt(country, WorldArt.Landmark, Art.GetLandmark(country));
            if (landmarkSprite != null)
            {
                Image landmark = MenuUI.Image("Landmark", banner.transform, landmarkSprite);
                MenuUI.Stretch(landmark.rectTransform, 20f, 8f, 20f, 8f);
                if (!unlocked)
                    landmark.color = new Color(0.45f, 0.45f, 0.5f, 1f);
            }
            else
            {
                // No landmark art yet: the country name across the banner.
                TextMeshProUGUI placeholder = MenuUI.OutlinedText("Landmark Name", banner.transform,
                    CountryCatalog.GetName(country).ToUpperInvariant(), 80f, new Color32(40, 90, 140, 255));
                MenuUI.Stretch(placeholder.rectTransform, 20f, 8f, 20f, 8f);
            }
            if (!unlocked)
                banner.color = new Color(0.62f, 0.64f, 0.68f, 1f);
            if (complete)
            {
                Image check = MenuUI.Image("Done", banner.transform, Art.check);
                MenuUI.Anchor(check.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(84f, 74f));
            }

            // Flag badge, chapter and country name.
            Sprite flagSprite = GetCountryArt(country, WorldArt.FlagRound, Art.GetFlag(country));
            if (flagSprite != null)
            {
                Image flag = MenuUI.Image("Flag", card, flagSprite);
                MenuUI.Anchor(flag.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(84f, -300f), new Vector2(116f, 116f));
            }

            TextMeshProUGUI chapter = MenuUI.Text("Chapter", card, "CHAPTER " + (country + 1), 30f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(chapter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -262f), new Vector2(380f, 38f));

            TextMeshProUGUI name = MenuUI.Text("Country", card, CountryCatalog.GetName(country).ToUpperInvariant(), 52f, Heading, TextAlignmentOptions.Left);
            MenuUI.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -298f), new Vector2(400f, 62f));

            RectTransform starPill = MenuUI.Pill("Stars", card, Art.star, stars + "/" + (CountryCatalog.LevelsPerCountry * 3), 38f, MenuUI.TextDark);
            MenuUI.Anchor(starPill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -272f), new Vector2(190f, 60f));

            // Two-line description.
            WorldCatalog.Country info = WorldCatalog.GetCountry(country);
            TextMeshProUGUI story = MenuUI.Text("Story", card, info != null ? info.Story : string.Empty, 30f, MenuUI.TextDark, TextAlignmentOptions.TopLeft, true);
            MenuUI.Anchor(story.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -378f), new Vector2(726f, 84f));

            // Bottom row: level progress and the action.
            RectTransform bar = MenuUI.ProgressBar(card, (float)levels / CountryCatalog.LevelsPerCountry, complete ? MenuUI.Gold : MenuUI.Green,
                "LEVELS " + levels + "/" + CountryCatalog.LevelsPerCountry);
            MenuUI.Anchor(bar, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(28f, 72f), new Vector2(460f, 52f));

            if (!unlocked)
            {
                Image lockBadge = MenuUI.Image("Lock", card, Art.lockBadge);
                MenuUI.Anchor(lockBadge.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-134f, 84f), new Vector2(112f, 90f));

                TextMeshProUGUI hint = MenuUI.Text("Locked", card, "Finish " + CountryCatalog.GetName(country - 1), 26f, MenuUI.TextSoft);
                MenuUI.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-134f, 26f), new Vector2(240f, 34f));
            }
            else
            {
                int chosen = country;
                Button button = MenuUI.LabelButton("Play", card, complete ? Art.buttonOrange : Art.buttonGreen,
                    complete ? "REPLAY" : "PLAY", 40f, () => PlayCountry(chosen), null,
                    complete ? new Color32(140, 70, 10, 255) : new Color32(30, 90, 20, 255));
                MenuUI.Anchor((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-24f, 72f), new Vector2(230f, 100f));
            }
        }

        // The country's own art (Resources/World/...), else the panel art set for the first five.
        private static Sprite GetCountryArt(int country, string key, Sprite panelArt)
        {
            Sprite own = WorldArt.ForCountry(country, key, false);
            return own != null ? own : panelArt;
        }

        // Same hand-off the Country Map uses when a country is picked.
        private void PlayCountry(int country)
        {
            PlayerPrefs.SetInt(SelectedCountryKey, country);
            PlayerPrefs.SetInt(SelectedCountryLevelStartKey, country * CountryCatalog.LevelsPerCountry);
            PlayerPrefs.SetInt(FromCountryMapKey, 1);
            PlayerPrefs.Save();

            Close();
            EnhancedLoadingScreen.LoadViaLoadingScreen("LevelSelection");
        }
    }
}
