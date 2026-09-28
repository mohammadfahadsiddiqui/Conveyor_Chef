using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;

namespace Watermelon
{
    /// <summary>
    /// COLLECTION: every country dish, one tab per country. Discovered dishes show their
    /// picture on a tile in their tray colour with how often they were served; the rest are
    /// silhouettes until the player serves them (or completes a level of that country).
    /// </summary>
    public sealed class CollectionPanel : MenuPanel
    {
        private static readonly string[] CountryTabs = { "CHINA", "JAPAN", "INDIA", "KOREA", "THAI" };

        private const int Columns = 3;
        private const float CellWidth = 238f;
        private const float CellHeight = 300f;
        private const float Spacing = 16f;

        protected override string Title => "COLLECTION";
        protected override string[] Tabs => CountryTabs;

        protected override void OnOpened()
        {
            // Open on the country being played.
            int country = CountryFood.CurrentCountry;
            if (country >= 0 && country != SelectedTab)
                SelectTab(country);
        }

        protected override void BuildContent()
        {
            BuildSummary(SelectedTab);
            BuildGrid(SelectedTab);
        }

        private void BuildSummary(int country)
        {
            int discovered = PlayerStats.DishesDiscovered;
            int inCountry = 0;
            for (int colour = 0; colour < CountryFoodArt.ColoursPerCountry; colour++)
            {
                if (PlayerStats.IsDishDiscovered(PlayerStats.GetDishIndex(country, colour)))
                    inCountry++;
            }

            RectTransform card = MenuUI.Card("Summary", Content, 200f, true);

            Image book = MenuUI.Image("Book", card, Art.iconCollection);
            MenuUI.Anchor(book.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(160f, 150f));

            TextMeshProUGUI title = MenuUI.Text("Title", card, $"DISHES DISCOVERED  {discovered}/{PlayerStats.TotalDishes}", 36f, new Color32(160, 56, 18, 255), TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(196f, -26f), new Vector2(540f, 46f));

            RectTransform bar = MenuUI.ProgressBar(card, (float)discovered / PlayerStats.TotalDishes, MenuUI.Gold);
            MenuUI.Anchor(bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(196f, -84f), new Vector2(540f, 40f));

            Image flag = MenuUI.Image("Flag", card, Art.GetFlag(country));
            MenuUI.Anchor(flag.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(196f, 18f), new Vector2(56f, 56f));
            TextMeshProUGUI countryText = MenuUI.Text("Country", card, $"{CountryCatalog.GetName(country)}: {inCountry}/{CountryFoodArt.ColoursPerCountry}", 30f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(countryText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(262f, 24f), new Vector2(470f, 44f));
        }

        private void BuildGrid(int country)
        {
            int count = CountryFoodArt.ColoursPerCountry;
            int rows = Mathf.CeilToInt(count / (float)Columns);

            RectTransform grid = MenuUI.Rect("Grid", Content);
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * CellHeight + (rows - 1) * Spacing;
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(CellWidth, CellHeight);
            layout.spacing = new Vector2(Spacing, Spacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;

            for (int colour = 0; colour < count; colour++)
                BuildDish(grid, country, colour);
        }

        private void BuildDish(Transform grid, int country, int colour)
        {
            int dish = PlayerStats.GetDishIndex(country, colour);
            bool discovered = PlayerStats.IsDishDiscovered(dish);
            int served = PlayerStats.GetDishServed(dish);
            Color tint = CountryFood.GetTypeColour((LevelElement.Type)((int)LevelElement.Type.Block_Red + colour));

            Image cell = MenuUI.Image("Dish " + dish, grid, MenuUI.Rounded(26, 4, MenuUI.CardFill, MenuUI.CardBorder), null, false);

            Color tileColour = discovered ? Color.Lerp(tint, Color.white, 0.55f) : new Color32(214, 206, 196, 255);
            Image tile = MenuUI.Image("Tile", cell.transform, MenuUI.Rounded(22, 0, tileColour, tileColour), null, false);
            MenuUI.Anchor(tile.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(CellWidth - 24f, 190f));

            Sprite sprite = CountryFood.Art != null ? CountryFood.Art.GetDish(country, colour) : null;
            Image picture = MenuUI.Image("Picture", tile.transform, sprite);
            MenuUI.Stretch(picture.rectTransform, 10f, 8f, 10f, 8f);
            if (!discovered)
            {
                picture.color = new Color(0.18f, 0.12f, 0.1f, 0.55f);
                TextMeshProUGUI mark = MenuUI.OutlinedText("Unknown", tile.transform, "?", 90f, new Color32(60, 40, 30, 255));
                MenuUI.Stretch(mark.rectTransform);
            }

            TextMeshProUGUI name = MenuUI.Text("Name", cell.transform, discovered ? DishCatalog.GetName(dish) : "???", 31f, MenuUI.TextDark);
            MenuUI.Anchor(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(CellWidth - 20f, 40f));

            TextMeshProUGUI detail = MenuUI.Text("Served", cell.transform,
                discovered ? (served > 0 ? "Served x" + served.ToString("N0") : "Discovered") : "Not found yet", 26f, MenuUI.TextSoft);
            MenuUI.Anchor(detail.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(CellWidth - 20f, 32f));
        }
    }
}
