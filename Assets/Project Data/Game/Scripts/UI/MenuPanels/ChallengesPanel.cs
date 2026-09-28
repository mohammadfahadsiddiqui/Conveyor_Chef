using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// CHALLENGES: three daily tasks (picked from the date, so everyone gets the same set each
    /// day), each with a progress bar, a reward and CLAIM / GO. Claiming all three unlocks a
    /// bonus. Everything resets at midnight; a countdown shows when.
    /// </summary>
    public sealed class ChallengesPanel : MenuPanel
    {
        private struct Challenge
        {
            public string Id;
            public string Title;
            public PlayerStats.Daily Counter;
            public int Target;
            public CurrencyType RewardCurrency;
            public int Reward;
        }

        private const int BonusDiamonds = 10;

        private TextMeshProUGUI timerText;
        private float timerRefresh;

        protected override string Title => "CHALLENGES";
        protected override bool ShowCurrencies => true;

        protected override void BuildContent()
        {
            Challenge[] today = GetTodaysChallenges();

            RectTransform header = MenuUI.Card("Header", Content, 150f, true);
            TextMeshProUGUI heading = MenuUI.OutlinedText("Heading", header, "DAILY CHALLENGES", 46f, new Color32(150, 60, 20, 255));
            MenuUI.Anchor(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(700f, 60f));
            timerText = MenuUI.Text("Timer", header, string.Empty, 30f, MenuUI.TextSoft);
            MenuUI.Anchor(timerText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(700f, 44f));
            UpdateTimer();

            int claimed = 0;
            foreach (Challenge challenge in today)
            {
                BuildChallenge(challenge);
                if (PlayerStats.IsDailyFlagSet("claim_" + challenge.Id))
                    claimed++;
            }

            BuildBonus(claimed, today.Length);
        }

        private void Update()
        {
            timerRefresh -= Time.unscaledDeltaTime;
            if (timerRefresh > 0f)
                return;

            timerRefresh = 1f;
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (timerText == null)
                return;

            TimeSpan left = PlayerStats.TimeUntilNextDay;
            timerText.text = $"New challenges in {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        private void BuildChallenge(Challenge challenge)
        {
            int progress = Mathf.Min(PlayerStats.GetDaily(challenge.Counter), challenge.Target);
            bool done = progress >= challenge.Target;
            bool claimed = PlayerStats.IsDailyFlagSet("claim_" + challenge.Id);

            RectTransform card = MenuUI.Card(challenge.Id, Content, 210f, done && !claimed);

            Image iconBack = MenuUI.Image("Icon Back", card, MenuUI.Rounded(24, 3, new Color32(255, 236, 190, 255), MenuUI.CardBorder), null, false);
            MenuUI.Anchor(iconBack.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(150f, 150f));
            Image icon = MenuUI.Image("Icon", iconBack.transform, GetIcon(challenge.Counter));
            MenuUI.Stretch(icon.rectTransform, 14f, 14f, 14f, 14f);

            TextMeshProUGUI title = MenuUI.Text("Title", card, challenge.Title, 36f, MenuUI.TextDark, TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -24f), new Vector2(360f, 50f));

            RectTransform reward = MenuUI.Pill("Reward", card, CurrencyIcon(challenge.RewardCurrency), challenge.Reward.ToString("N0"), 32f, MenuUI.TextDark);
            MenuUI.Anchor(reward, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -82f), new Vector2(170f, 46f));

            RectTransform bar = MenuUI.ProgressBar(card, (float)progress / challenge.Target, MenuUI.Green, progress + "/" + challenge.Target);
            MenuUI.Anchor(bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(190f, 24f), new Vector2(340f, 50f));

            Vector2 anchor = new Vector2(1f, 0.5f);
            if (claimed)
            {
                Image check = MenuUI.Image("Claimed", card, Art.check);
                MenuUI.Anchor(check.rectTransform, anchor, new Vector2(1f, 0.5f), new Vector2(-50f, 14f), new Vector2(110f, 96f));
                TextMeshProUGUI label = MenuUI.Text("Label", card, "CLAIMED", 28f, MenuUI.Green);
                MenuUI.Anchor(label.rectTransform, anchor, new Vector2(1f, 0.5f), new Vector2(-30f, -58f), new Vector2(150f, 36f));
            }
            else if (done)
            {
                Button claim = MenuUI.LabelButton("Claim", card, Art.buttonGreen, "CLAIM", 40f, () => Claim(challenge), null, new Color32(30, 90, 20, 255));
                MenuUI.Anchor((RectTransform)claim.transform, anchor, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(190f, 104f));
            }
            else
            {
                Button go = MenuUI.LabelButton("Go", card, Art.buttonOrange, "GO", 40f, Play, null, new Color32(140, 70, 10, 255));
                MenuUI.Anchor((RectTransform)go.transform, anchor, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(190f, 104f));
            }
        }

        private void BuildBonus(int claimed, int total)
        {
            bool ready = claimed >= total;
            bool taken = PlayerStats.IsDailyFlagSet("claim_bonus");

            RectTransform card = MenuUI.Card("Bonus", Content, 180f, ready && !taken);

            Image gem = MenuUI.Image("Gem", card, Art.gem);
            MenuUI.Anchor(gem.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(130f, 130f));

            TextMeshProUGUI title = MenuUI.Text("Title", card, "ALL-CLEAR BONUS", 36f, new Color32(170, 60, 20, 255), TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -26f), new Vector2(360f, 48f));
            TextMeshProUGUI body = MenuUI.Text("Body", card, $"Claim all {total} challenges today ({claimed}/{total})", 26f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(190f, -76f), new Vector2(360f, 36f));
            RectTransform reward = MenuUI.Pill("Reward", card, Art.diamond, BonusDiamonds.ToString(), 32f, MenuUI.TextDark);
            MenuUI.Anchor(reward, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(190f, 18f), new Vector2(120f, 46f));

            Vector2 anchor = new Vector2(1f, 0.5f);
            if (taken)
            {
                Image check = MenuUI.Image("Claimed", card, Art.check);
                MenuUI.Anchor(check.rectTransform, anchor, new Vector2(1f, 0.5f), new Vector2(-50f, 0f), new Vector2(110f, 96f));
            }
            else
            {
                Button claim = MenuUI.LabelButton("Claim", card, ready ? Art.buttonPurple : Art.buttonGray, "CLAIM", 40f, () =>
                {
                    if (!ready)
                    {
                        Toast("Claim all challenges first");
                        return;
                    }
                    PlayerStats.SetDailyFlag("claim_bonus");
                    Grant(CurrencyType.Diamonds, BonusDiamonds);
                    Refresh();
                }, null, ready ? new Color32(70, 20, 110, 255) : new Color32(70, 70, 70, 255));
                MenuUI.Anchor((RectTransform)claim.transform, anchor, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(190f, 104f));
            }
        }

        private void Claim(Challenge challenge)
        {
            if (PlayerStats.IsDailyFlagSet("claim_" + challenge.Id))
                return;

            PlayerStats.SetDailyFlag("claim_" + challenge.Id);
            Grant(challenge.RewardCurrency, challenge.Reward);
            Refresh();
        }

        private void Play()
        {
            Close();
            EnhancedLoadingScreen.LoadViaLoadingScreen("WorldMap");
        }

        private Sprite GetIcon(PlayerStats.Daily counter)
        {
            switch (counter)
            {
                case PlayerStats.Daily.Levels: return Art.iconTrophy;
                case PlayerStats.Daily.Stars: return Art.star;
                case PlayerStats.Daily.Dishes:
                    {
                        int country = Mathf.Max(0, CountryFood.CurrentCountry);
                        Sprite dish = CountryFood.Art != null ? CountryFood.Art.GetDish(country, 0) : null;
                        return dish != null ? dish : Art.chefAvatar;
                    }
                default:
                    return Art.powerUpIcons != null && Art.powerUpIcons.Length > 1 ? Art.powerUpIcons[1] : Art.iconShop;
            }
        }

        // One challenge of each of three different kinds, with a target picked from the date.
        private static Challenge[] GetTodaysChallenges()
        {
            Challenge[][] pool =
            {
                new[]
                {
                    Make("win2", "Win 2 levels", PlayerStats.Daily.Levels, 2, CurrencyType.Coins, 100),
                    Make("win3", "Win 3 levels", PlayerStats.Daily.Levels, 3, CurrencyType.Coins, 150),
                    Make("win5", "Win 5 levels", PlayerStats.Daily.Levels, 5, CurrencyType.Coins, 250),
                },
                new[]
                {
                    Make("stars6", "Earn 6 stars", PlayerStats.Daily.Stars, 6, CurrencyType.Coins, 120),
                    Make("stars9", "Earn 9 stars", PlayerStats.Daily.Stars, 9, CurrencyType.Coins, 180),
                    Make("stars12", "Earn 12 stars", PlayerStats.Daily.Stars, 12, CurrencyType.Diamonds, 5),
                },
                new[]
                {
                    Make("serve40", "Serve 40 dishes", PlayerStats.Daily.Dishes, 40, CurrencyType.Coins, 100),
                    Make("serve80", "Serve 80 dishes", PlayerStats.Daily.Dishes, 80, CurrencyType.Coins, 200),
                    Make("serve150", "Serve 150 dishes", PlayerStats.Daily.Dishes, 150, CurrencyType.Diamonds, 5),
                },
                new[]
                {
                    Make("power1", "Use a power-up", PlayerStats.Daily.PowerUps, 1, CurrencyType.Coins, 80),
                    Make("power3", "Use 3 power-ups", PlayerStats.Daily.PowerUps, 3, CurrencyType.Coins, 150),
                },
            };

            int seed = int.Parse(PlayerStats.Today);
            System.Random random = new System.Random(seed);
            int skipKind = random.Next(pool.Length);

            Challenge[] result = new Challenge[3];
            int n = 0;
            for (int kind = 0; kind < pool.Length && n < result.Length; kind++)
            {
                if (kind == skipKind)
                    continue;
                result[n++] = pool[kind][random.Next(pool[kind].Length)];
            }
            return result;
        }

        private static Challenge Make(string id, string title, PlayerStats.Daily counter, int target, CurrencyType currency, int reward)
        {
            return new Challenge { Id = id, Title = title, Counter = counter, Target = target, RewardCurrency = currency, Reward = reward };
        }
    }
}
