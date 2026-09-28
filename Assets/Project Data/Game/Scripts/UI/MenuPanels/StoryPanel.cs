using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// STORY: the chef's journey through the five countries. Each chapter shows its landmark,
    /// flag, dishes, level and star progress, and PLAY (opens that country's Level Selection),
    /// REPLAY for finished chapters, or a lock until the previous country is complete.
    /// </summary>
    public sealed class StoryPanel : MenuPanel
    {
        private const string SelectedCountryKey = "CC_CountryMap_SelectedCountry";
        private const string SelectedCountryLevelStartKey = "CC_CountryMap_SelectedLevelStart";
        private const string FromCountryMapKey = "CC_LevelSelection_FromCountryMap";

        protected override string Title => "STORY";

        protected override void BuildContent()
        {
            BuildIntro();

            for (int country = 0; country < CountryCatalog.CountryCount; country++)
                BuildChapter(country);
        }

        private void BuildIntro()
        {
            RectTransform card = MenuUI.Card("Intro", Content, 300f, true);

            Image chef = MenuUI.Image("Chef", card, Art.chefFull);
            MenuUI.Anchor(chef.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(150f, 280f));

            TextMeshProUGUI heading = MenuUI.Text("Heading", card, "THE CHEF'S JOURNEY", 44f, new Color32(170, 60, 20, 255), TextAlignmentOptions.Left);
            MenuUI.Anchor(heading.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(185f, -24f), new Vector2(560f, 58f));

            PlayerStats.GetProgress(out int completed, out int stars, out _);
            TextMeshProUGUI body = MenuUI.Text("Body", card,
                "Cook your way across Asia and learn every country's favourite street food.\n" +
                $"<color=#A0521E>{PlayerStats.CountriesCompleted}/{CountryCatalog.CountryCount} countries  -  {completed}/{PlayerStats.TotalLevels} levels  -  {stars}/{PlayerStats.TotalStars} stars</color>",
                30f, MenuUI.TextDark, TextAlignmentOptions.TopLeft, true);
            MenuUI.Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(185f, -90f), new Vector2(560f, 190f));
        }

        private void BuildChapter(int country)
        {
            bool unlocked = PlayerStats.IsCountryUnlocked(country);
            bool complete = PlayerStats.IsCountryComplete(country);
            int levels = PlayerStats.GetCountryLevelsCompleted(country);
            int stars = PlayerStats.GetCountryStars(country);
            bool current = unlocked && !complete;

            RectTransform card = MenuUI.Card("Chapter " + (country + 1), Content, 470f, current);

            // Landmark banner across the top of the card.
            Image banner = MenuUI.Image("Banner", card, MenuUI.Rounded(22, 0, new Color32(186, 226, 246, 255), Color.clear), null, false);
            MenuUI.Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(700f, 230f));
            Image landmark = MenuUI.Image("Landmark", banner.transform, Art.GetLandmark(country));
            MenuUI.Stretch(landmark.rectTransform, 20f, 6f, 20f, 6f);
            if (!unlocked)
            {
                landmark.color = new Color(0.45f, 0.45f, 0.5f, 1f);
                banner.color = new Color(0.6f, 0.62f, 0.66f, 1f);
            }

            Image flag = MenuUI.Image("Flag", card, Art.GetFlag(country));
            MenuUI.Anchor(flag.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(76f, -250f), new Vector2(104f, 104f));

            TextMeshProUGUI chapter = MenuUI.Text("Chapter", card, "CHAPTER " + (country + 1), 28f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(chapter.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -250f), new Vector2(360f, 38f));

            TextMeshProUGUI name = MenuUI.OutlinedText("Country", card, CountryCatalog.GetName(country).ToUpperInvariant(), 46f,
                new Color32(120, 50, 10, 255), TextAlignmentOptions.Left);
            MenuUI.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -284f), new Vector2(420f, 58f));

            TextMeshProUGUI story = MenuUI.Text("Story", card, DishCatalog.CountryStories[country], 26f, MenuUI.TextDark, TextAlignmentOptions.TopLeft, true);
            MenuUI.Anchor(story.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -350f), new Vector2(470f, 100f));

            // Stars and levels.
            RectTransform starRow = MenuUI.Pill("Stars", card, Art.star, stars + "/" + (CountryCatalog.LevelsPerCountry * 3), 32f, MenuUI.TextDark);
            MenuUI.Anchor(starRow, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-28f, -262f), new Vector2(200f, 50f));
            TextMeshProUGUI levelText = MenuUI.Text("Levels", card, "LEVELS " + levels + "/" + CountryCatalog.LevelsPerCountry, 28f, MenuUI.TextSoft, TextAlignmentOptions.Right);
            MenuUI.Anchor(levelText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-28f, -310f), new Vector2(220f, 40f));

            // Action.
            Vector2 actionAnchor = new Vector2(1f, 0f);
            if (!unlocked)
            {
                Image lockBadge = MenuUI.Image("Lock", card, Art.lockBadge);
                MenuUI.Anchor(lockBadge.rectTransform, actionAnchor, new Vector2(1f, 0f), new Vector2(-40f, 26f), new Vector2(130f, 105f));

                TextMeshProUGUI hint = MenuUI.Text("Locked", card, "Finish " + CountryCatalog.GetName(country - 1), 24f, MenuUI.TextSoft, TextAlignmentOptions.Center);
                MenuUI.Anchor(hint.rectTransform, actionAnchor, new Vector2(1f, 0f), new Vector2(-10f, 2f), new Vector2(200f, 30f));
            }
            else
            {
                int chosen = country;
                Button button = MenuUI.LabelButton("Play", card, complete ? Art.buttonOrange : Art.buttonGreen,
                    complete ? "REPLAY" : "PLAY", 40f, () => PlayCountry(chosen), null,
                    complete ? new Color32(140, 70, 10, 255) : new Color32(30, 90, 20, 255));
                MenuUI.Anchor((RectTransform)button.transform, actionAnchor, new Vector2(1f, 0f), new Vector2(-24f, 22f), new Vector2(210f, 100f));

                if (complete)
                {
                    Image check = MenuUI.Image("Done", card, Art.check);
                    MenuUI.Anchor(check.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -30f), new Vector2(82f, 72f));
                }
            }
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
