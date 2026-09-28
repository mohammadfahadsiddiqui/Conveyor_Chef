using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// ACHIEVEMENTS: long-term goals (levels, stars, dishes served, countries, collection,
    /// power-ups) with a progress bar and a coin or diamond reward. Rewards are claimed once;
    /// claimable ones are listed first, claimed ones last.
    /// </summary>
    public sealed class AchievementsPanel : MenuPanel
    {
        private sealed class Achievement
        {
            public string Id;
            public string Title;
            public string Description;
            public Func<int> Progress;
            public int Target;
            public CurrencyType Currency;
            public int Reward;
            public Func<MenuPanelArt, Sprite> Icon;

            public int Current => Mathf.Min(Progress(), Target);
            public bool Done => Progress() >= Target;
            public bool Claimed => PlayerStats.GetFlag("achievement_" + Id);
        }

        protected override string Title => "ACHIEVEMENTS";
        protected override bool ShowCurrencies => true;

        private static readonly Achievement[] All =
        {
            Make("first_win", "First Service", "Complete your first level", () => PlayerStats.LevelsCompleted, 1, CurrencyType.Coins, 100, a => a.iconTrophy),
            Make("levels_5", "Line Cook", "Complete 5 levels", () => PlayerStats.LevelsCompleted, 5, CurrencyType.Coins, 200, a => a.iconTrophy),
            Make("levels_all", "Head Chef", "Complete all " + PlayerStats.TotalLevels + " levels", () => PlayerStats.LevelsCompleted, PlayerStats.TotalLevels, CurrencyType.Diamonds, 15, a => a.iconTrophy),
            Make("stars_10", "Rising Star", "Collect 10 stars", () => PlayerStats.StarsEarned, 10, CurrencyType.Coins, 150, a => a.star),
            Make("stars_30", "Star Chef", "Collect 30 stars", () => PlayerStats.StarsEarned, 30, CurrencyType.Coins, 300, a => a.star),
            Make("stars_all", "Master of Stars", "Collect all " + PlayerStats.TotalStars + " stars", () => PlayerStats.StarsEarned, PlayerStats.TotalStars, CurrencyType.Diamonds, 20, a => a.star),
            Make("serve_100", "Busy Kitchen", "Serve 100 dishes", () => PlayerStats.DishesServed, 100, CurrencyType.Coins, 150, a => a.chefAvatar),
            Make("serve_500", "Food Rush", "Serve 500 dishes", () => PlayerStats.DishesServed, 500, CurrencyType.Coins, 300, a => a.chefAvatar),
            Make("serve_2000", "Conveyor Legend", "Serve 2,000 dishes", () => PlayerStats.DishesServed, 2000, CurrencyType.Diamonds, 15, a => a.chefAvatar),
            Make("country_1", "Passport Stamp", "Complete a whole country", () => PlayerStats.CountriesCompleted, 1, CurrencyType.Coins, 200, a => a.GetFlag(0)),
            Make("country_all", "World Tour", "Complete all 5 countries", () => PlayerStats.CountriesCompleted, 5, CurrencyType.Diamonds, 30, a => a.GetFlag(4)),
            Make("collect_all", "Gourmet Collector", "Discover all " + PlayerStats.TotalDishes + " dishes", () => PlayerStats.DishesDiscovered, PlayerStats.TotalDishes, CurrencyType.Diamonds, 20, a => a.iconCollection),
            Make("power_10", "Helping Hand", "Use 10 power-ups", () => PlayerStats.PowerUpsUsed, 10, CurrencyType.Coins, 100,
                a => a.powerUpIcons != null && a.powerUpIcons.Length > 1 ? a.powerUpIcons[1] : a.iconShop),
        };

        protected override void BuildContent()
        {
            int unlocked = 0;
            List<Achievement> ordered = new List<Achievement>(All);
            foreach (Achievement a in All)
            {
                if (a.Done)
                    unlocked++;
            }

            ordered.Sort((x, y) => Rank(x).CompareTo(Rank(y)));

            RectTransform header = MenuUI.Card("Header", Content, 170f, true);
            Image trophy = MenuUI.Image("Trophy", header, Art.iconTrophy);
            MenuUI.Anchor(trophy.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(150f, 136f));
            TextMeshProUGUI title = MenuUI.Text("Title", header, $"UNLOCKED  {unlocked}/{All.Length}", 38f, MenuUI.TextDark, TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(196f, -28f), new Vector2(540f, 50f));
            RectTransform bar = MenuUI.ProgressBar(header, (float)unlocked / All.Length, MenuUI.Gold);
            MenuUI.Anchor(bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(196f, 34f), new Vector2(540f, 42f));

            foreach (Achievement a in ordered)
                BuildRow(a);
        }

        // Claimable first, then in progress, then already claimed.
        private static int Rank(Achievement a) => a.Claimed ? 2 : a.Done ? 0 : 1;

        private void BuildRow(Achievement a)
        {
            bool claimed = a.Claimed;
            bool done = a.Done;

            RectTransform card = MenuUI.Card(a.Id, Content, 200f, done && !claimed);

            Image iconBack = MenuUI.Image("Icon Back", card, MenuUI.Rounded(24, 3, new Color32(255, 236, 190, 255), MenuUI.CardBorder), null, false);
            MenuUI.Anchor(iconBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(146f, 146f));
            Image icon = MenuUI.Image("Icon", iconBack.transform, a.Icon(Art));
            MenuUI.Stretch(icon.rectTransform, 14f, 14f, 14f, 14f);
            if (!done)
                icon.color = new Color(1f, 1f, 1f, 0.55f);

            TextMeshProUGUI title = MenuUI.Text("Title", card, a.Title.ToUpperInvariant(), 34f, MenuUI.TextDark, TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(186f, -20f), new Vector2(360f, 46f));
            TextMeshProUGUI description = MenuUI.Text("Description", card, a.Description, 26f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(186f, -66f), new Vector2(360f, 36f));

            RectTransform bar = MenuUI.ProgressBar(card, (float)a.Current / a.Target, done ? MenuUI.Gold : MenuUI.Green,
                a.Current.ToString("N0") + "/" + a.Target.ToString("N0"));
            MenuUI.Anchor(bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(186f, 22f), new Vector2(340f, 48f));

            Vector2 anchor = new Vector2(1f, 0.5f);
            if (claimed)
            {
                Image check = MenuUI.Image("Claimed", card, Art.check);
                MenuUI.Anchor(check.rectTransform, anchor, new Vector2(1f, 0.5f), new Vector2(-54f, 14f), new Vector2(104f, 90f));
                TextMeshProUGUI label = MenuUI.Text("Label", card, "CLAIMED", 26f, MenuUI.Green);
                MenuUI.Anchor(label.rectTransform, anchor, new Vector2(1f, 0.5f), new Vector2(-34f, -54f), new Vector2(150f, 34f));
            }
            else
            {
                RectTransform reward = MenuUI.Pill("Reward", card, CurrencyIcon(a.Currency), a.Reward.ToString("N0"), 30f, MenuUI.TextDark);
                MenuUI.Anchor(reward, anchor, new Vector2(1f, 0.5f), new Vector2(-30f, 52f), new Vector2(170f, 44f));

                Button claim = MenuUI.LabelButton("Claim", card, done ? Art.buttonGreen : Art.buttonGray, "CLAIM", 36f, () =>
                {
                    if (!a.Done || a.Claimed)
                        return;
                    PlayerStats.SetFlag("achievement_" + a.Id);
                    Grant(a.Currency, a.Reward);
                    Refresh();
                }, null, done ? new Color32(30, 90, 20, 255) : new Color32(70, 70, 70, 255));
                claim.interactable = done;
                MenuUI.Anchor((RectTransform)claim.transform, anchor, new Vector2(1f, 0.5f), new Vector2(-22f, -30f), new Vector2(180f, 90f));
            }
        }

        private static Achievement Make(string id, string title, string description, Func<int> progress, int target,
            CurrencyType currency, int reward, Func<MenuPanelArt, Sprite> icon)
        {
            return new Achievement
            {
                Id = id, Title = title, Description = description, Progress = progress, Target = target,
                Currency = currency, Reward = reward, Icon = icon
            };
        }
    }
}
