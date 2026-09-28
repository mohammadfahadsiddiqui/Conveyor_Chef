using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// LEADERBOARD: the player's Chef Score ranked against a fixed list of rival chefs from the
    /// five countries (offline; can be replaced by an online board later). The player climbs
    /// by earning stars, completing levels and serving dishes.
    /// </summary>
    public sealed class LeaderboardPanel : MenuPanel
    {
        private struct Entry
        {
            public string Name;
            public int Country;
            public int Score;
            public bool IsPlayer;
        }

        // Rival chefs: name, country (CountryCatalog order), score.
        private static readonly (string, int, int)[] Rivals =
        {
            ("Chef Mei", 0, 9850), ("Kenji", 1, 9120), ("Aarav", 2, 8400), ("Min-jun", 3, 7760),
            ("Somchai", 4, 7080), ("Yuki", 1, 6450), ("Priya", 2, 5900), ("Wei Lin", 0, 5320),
            ("Ji-woo", 3, 4780), ("Ploy", 4, 4250), ("Hana", 1, 3700), ("Rohan", 2, 3180),
            ("Chen", 0, 2650), ("Seo-yeon", 3, 2140), ("Niran", 4, 1700), ("Aiko", 1, 1280),
            ("Kiran", 2, 900), ("Bao", 0, 560), ("Da-eun", 3, 300), ("Mali", 4, 120),
        };

        private static readonly Color32[] Medals =
        {
            new Color32(255, 196, 40, 255), new Color32(196, 206, 220, 255), new Color32(214, 136, 70, 255)
        };

        protected override string Title => "LEADERBOARD";

        protected override void BuildContent()
        {
            int score = PlayerStats.ChefScore;
            List<Entry> entries = new List<Entry>();
            foreach ((string name, int country, int rivalScore) in Rivals)
                entries.Add(new Entry { Name = name, Country = country, Score = rivalScore });
            entries.Add(new Entry { Name = "YOU", Country = -1, Score = score, IsPlayer = true });
            entries.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.IsPlayer ? -1 : b.IsPlayer ? 1 : 0);

            int rank = entries.FindIndex(e => e.IsPlayer) + 1;
            BuildPlayerCard(score, rank, entries.Count);

            for (int i = 0; i < entries.Count; i++)
                BuildRow(entries[i], i + 1);
        }

        private void BuildPlayerCard(int score, int rank, int total)
        {
            RectTransform card = MenuUI.Card("You", Content, 250f, true);

            Image avatar = MenuUI.Image("Avatar", card, Art.chefAvatar);
            MenuUI.Anchor(avatar.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(130f, 148f));

            TextMeshProUGUI label = MenuUI.Text("Label", card, "YOUR CHEF SCORE", 30f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -22f), new Vector2(380f, 40f));
            TextMeshProUGUI value = MenuUI.Text("Score", card, score.ToString("N0"), 64f, new Color32(160, 56, 18, 255), TextAlignmentOptions.Left);
            MenuUI.Anchor(value.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -62f), new Vector2(380f, 80f));

            Image rankBack = MenuUI.Image("Rank", card, MenuUI.Rounded(30, 4, new Color32(90, 56, 30, 255), MenuUI.Gold), null, false);
            MenuUI.Anchor(rankBack.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -24f), new Vector2(190f, 130f));
            TextMeshProUGUI rankLabel = MenuUI.Text("Label", rankBack.transform, "RANK", 26f, new Color32(255, 220, 150, 255));
            MenuUI.Anchor(rankLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(170f, 34f));
            TextMeshProUGUI rankValue = MenuUI.Text("Value", rankBack.transform, "#" + rank, 56f, Color.white);
            MenuUI.Anchor(rankValue.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(170f, 70f));

            TextMeshProUGUI how = MenuUI.Text("How", card,
                "Star +100   |   Level +50   |   Dish +2", 28f, MenuUI.TextSoft);
            MenuUI.Anchor(how.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(720f, 36f));
        }

        private void BuildRow(Entry entry, int rank)
        {
            RectTransform card = MenuUI.Card("Rank " + rank, Content, 118f, entry.IsPlayer);

            if (rank <= Medals.Length)
            {
                Image medal = MenuUI.Image("Medal", card, MenuUI.Rounded(34, 4, Medals[rank - 1], new Color32(120, 80, 30, 255)), null, false);
                MenuUI.Anchor(medal.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(72f, 72f));
                TextMeshProUGUI number = MenuUI.OutlinedText("Number", medal.transform, rank.ToString(), 40f, new Color32(90, 50, 10, 255));
                MenuUI.Stretch(number.rectTransform);
            }
            else
            {
                TextMeshProUGUI number = MenuUI.Text("Number", card, rank.ToString(), 38f, MenuUI.TextSoft);
                MenuUI.Anchor(number.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(72f, 60f));
            }

            Image avatar = MenuUI.Image("Avatar", card, entry.IsPlayer ? Art.chefAvatar : Art.GetFlag(entry.Country));
            MenuUI.Anchor(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(82f, 82f));

            TextMeshProUGUI name = MenuUI.Text("Name", card, entry.IsPlayer ? "YOU" : entry.Name, 36f,
                entry.IsPlayer ? new Color32(170, 60, 20, 255) : MenuUI.TextDark, TextAlignmentOptions.Left);
            MenuUI.Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(212f, 0f), new Vector2(300f, 50f));

            RectTransform score = MenuUI.Pill("Score", card, Art.star, entry.Score.ToString("N0"), 34f, MenuUI.TextDark);
            MenuUI.Anchor(score, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(220f, 50f));
        }
    }
}
