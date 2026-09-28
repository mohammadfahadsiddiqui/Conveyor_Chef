using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BusStop;
using Watermelon.SkinStore;

namespace Watermelon
{
    /// <summary>
    /// CUSTOMIZE: conveyor trays (the skin system's Bus skins), plate styles and glass-lid
    /// styles for the dishes. Options are bought once with coins and picked any time; previews
    /// use the same generated plates and lids as the board.
    /// </summary>
    public sealed class CustomizePanel : MenuPanel
    {
        private const float CellWidth = 364f;
        private const float CellHeight = 440f;
        private const float Spacing = 18f;

        private static readonly string[] CustomizeTabs = { "TRAYS", "PLATES", "LIDS" };

        protected override string Title => "CUSTOMIZE";
        protected override string[] Tabs => CustomizeTabs;
        protected override bool ShowCurrencies => true;

        protected override void BuildContent()
        {
            switch (SelectedTab)
            {
                case 0: BuildTrays(); break;
                case 1: BuildStyles(false); break;
                default: BuildStyles(true); break;
            }
        }

        #region Trays

        private void BuildTrays()
        {
            List<SkinData> trays = GetTraySkins(out string selectedId);
            if (trays.Count == 0)
            {
                RectTransform card = MenuUI.Card("Empty", Content, 160f);
                TextMeshProUGUI text = MenuUI.Text("Text", card, "No trays available yet", 34f, MenuUI.TextSoft);
                MenuUI.Stretch(text.rectTransform);
                return;
            }

            RectTransform grid = Grid(trays.Count);
            for (int i = 0; i < trays.Count; i++)
            {
                SkinData tray = trays[i];
                bool owned = i == 0 || tray.IsUnlocked;
                bool selected = string.IsNullOrEmpty(selectedId) ? i == 0 : tray.UniqueId == selectedId;

                Image cell = Cell(grid, "Tray " + i, selected);
                RectTransform preview = PreviewArea(cell.transform);
                Image picture = MenuUI.Image("Picture", preview, tray.Preview2DSprite);
                MenuUI.Stretch(picture.rectTransform, 10f, 10f, 10f, 10f);

                string name = string.IsNullOrEmpty(tray.Name) ? "Tray " + (i + 1) : tray.Name.Replace("Bus", "Tray");
                CellName(cell.transform, name);

                SkinData chosen = tray;
                bool rewarded = tray.PurchType == SkinData.PurchaseType.RewardedVideo;
                CellAction(cell.transform, selected, owned, rewarded ? 0 : tray.Cost, tray.Currency,
                    () => SelectTray(chosen), () => BuyTray(chosen), rewarded);
            }
        }

        private List<SkinData> GetTraySkins(out string selectedId)
        {
            selectedId = null;
            List<SkinData> result = new List<SkinData>();
            if (Art.skinsDatabase == null)
                return result;

            try
            {
                Dictionary<TabData, List<SkinData>> products = Art.skinsDatabase.Init();
                foreach (KeyValuePair<TabData, List<SkinData>> pair in products)
                {
                    if (pair.Key.Type != SkinTab.Bus)
                        continue;
                    foreach (SkinData skin in pair.Value)
                    {
                        if (!skin.IsDummy)
                            result.Add(skin);
                    }
                }

                selectedId = Art.skinsDatabase[SkinTab.Bus];
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Customize] Trays unavailable: " + ex.Message);
            }

            return result;
        }

        private void SelectTray(SkinData tray)
        {
            Art.skinsDatabase[SkinTab.Bus] = tray.UniqueId;
            SaveController.MarkAsSaveIsRequired();
            Toast(tray.Name.Replace("Bus", "Tray") + " selected", new Color32(40, 110, 30, 255));
            Refresh();
        }

        private void BuyTray(SkinData tray)
        {
            if (!Spend(tray.Currency, tray.Cost))
            {
                Toast(tray.Currency == CurrencyType.Coins ? "Not enough coins" : "Not enough diamonds");
                return;
            }

            tray.IsUnlocked = true;
            SelectTray(tray);
        }

        #endregion

        #region Plates and lids

        private void BuildStyles(bool lids)
        {
            string[] names = lids ? CountryFoodStyle.LidNames : CountryFoodStyle.PlateNames;
            int[] prices = lids ? CountryFoodStyle.LidPrices : CountryFoodStyle.PlatePrices;
            int selected = lids ? CountryFoodStyle.Lid : CountryFoodStyle.Plate;

            RectTransform grid = Grid(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                int style = i;
                bool owned = lids ? CountryFoodStyle.OwnsLid(i) : CountryFoodStyle.OwnsPlate(i);

                Image cell = Cell(grid, names[i], i == selected);
                BuildStylePreview(PreviewArea(cell.transform), lids ? CountryFoodStyle.Plate : i, lids ? i : -1);
                CellName(cell.transform, names[i]);
                CellAction(cell.transform, i == selected, owned, prices[i], CurrencyType.Coins,
                    () => SelectStyle(lids, style, names[style]),
                    () => BuyStyle(lids, style, prices[style], names[style]), false);
            }

            RectTransform note = MenuUI.Card("Note", Content, 110f);
            TextMeshProUGUI text = MenuUI.Text("Text", note,
                lids ? "The glass lid covers dishes you cannot pick yet." : "Plates always keep their tray colour.", 28f, MenuUI.TextSoft);
            MenuUI.Stretch(text.rectTransform, 20f, 10f, 20f, 10f);
        }

        // Plate (in the red food colour) with a dish on it and, for lids, the glass lid.
        private void BuildStylePreview(RectTransform area, int plateStyle, int lidStyle)
        {
            Color colour = CountryFood.GetTypeColour(LevelElement.Type.Block_Red);
            int country = Mathf.Max(0, CountryFood.CurrentCountry);
            Sprite dish = CountryFood.Art != null ? CountryFood.Art.GetDish(country, 0) : null;

            const float plateWidth = 250f;
            float plateY = -40f;

            Image plate = MenuUI.Image("Plate", area, CountryFoodVisual.GetPlateSprite(colour, plateStyle));
            MenuUI.Place(plate.rectTransform, new Vector2(0f, plateY), new Vector2(plateWidth, plateWidth * 0.5f));

            if (dish != null)
            {
                Image food = MenuUI.Image("Dish", area, dish);
                MenuUI.Place(food.rectTransform, new Vector2(0f, plateY + plateWidth * 0.2f), new Vector2(plateWidth * 0.78f, plateWidth * 0.78f));
            }

            if (lidStyle >= 0)
            {
                Sprite lid = CountryFoodVisual.GetClocheSprite(lidStyle);
                Image cover = MenuUI.Image("Lid", area, lid);
                float width = plateWidth * CountryFoodVisual.ClochePreviewWidth;
                RectTransform rect = cover.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(lid.pivot.x / lid.rect.width, lid.pivot.y / lid.rect.height);
                rect.sizeDelta = new Vector2(width, width * lid.rect.height / lid.rect.width);
                rect.anchoredPosition = new Vector2(0f, plateY + CountryFoodVisual.ClochePreviewBase * plateWidth);
            }
        }

        private void SelectStyle(bool lids, int style, string name)
        {
            if (lids)
                CountryFoodStyle.Lid = style;
            else
                CountryFoodStyle.Plate = style;

            Toast(name + " selected", new Color32(40, 110, 30, 255));
            Refresh();
        }

        private void BuyStyle(bool lids, int style, int price, string name)
        {
            if (!Spend(CurrencyType.Coins, price))
            {
                Toast("Not enough coins");
                return;
            }

            if (lids)
                CountryFoodStyle.UnlockLid(style);
            else
                CountryFoodStyle.UnlockPlate(style);

            SelectStyle(lids, style, name);
        }

        #endregion

        #region Cells

        private RectTransform Grid(int count)
        {
            int rows = Mathf.CeilToInt(count / 2f);
            RectTransform grid = MenuUI.Rect("Grid", Content);
            grid.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * CellHeight + (rows - 1) * Spacing;
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(CellWidth, CellHeight);
            layout.spacing = new Vector2(Spacing, Spacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            return grid;
        }

        private static Image Cell(Transform grid, string name, bool selected)
        {
            return MenuUI.Image(name, grid, MenuUI.Rounded(26, selected ? 6 : 4, selected ? MenuUI.CardHighlight : MenuUI.CardFill,
                selected ? MenuUI.Gold : MenuUI.CardBorder), null, false);
        }

        private static RectTransform PreviewArea(Transform cell)
        {
            Image back = MenuUI.Image("Preview", cell, MenuUI.Rounded(22, 0, new Color32(214, 236, 240, 255), Color.clear), null, false);
            MenuUI.Anchor(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(CellWidth - 28f, 250f));
            return back.rectTransform;
        }

        private static void CellName(Transform cell, string name)
        {
            TextMeshProUGUI text = MenuUI.Text("Name", cell, name.ToUpperInvariant(), 34f, MenuUI.TextDark);
            MenuUI.Anchor(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -272f), new Vector2(CellWidth - 24f, 46f));
        }

        private void CellAction(Transform cell, bool selected, bool owned, int price, CurrencyType currency,
            UnityEngine.Events.UnityAction select, UnityEngine.Events.UnityAction buy, bool rewardedOnly)
        {
            Vector2 anchor = new Vector2(0.5f, 0f);
            Vector2 size = new Vector2(250f, 96f);
            Vector2 offset = new Vector2(0f, 16f);

            if (selected)
            {
                Image check = MenuUI.Image("Selected", cell, Art.check);
                MenuUI.Anchor(check.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-34f, -30f), new Vector2(76f, 66f));
                Button label = MenuUI.LabelButton("In Use", cell, Art.buttonGray, "IN USE", 34f, null, null, new Color32(70, 70, 70, 255));
                label.interactable = false;
                MenuUI.Anchor((RectTransform)label.transform, anchor, new Vector2(0.5f, 0f), offset, size);
            }
            else if (owned)
            {
                Button button = MenuUI.LabelButton("Select", cell, Art.buttonGreen, "SELECT", 36f, select, null, new Color32(30, 90, 20, 255));
                MenuUI.Anchor((RectTransform)button.transform, anchor, new Vector2(0.5f, 0f), offset, size);
            }
            else if (rewardedOnly)
            {
                Image lockBadge = MenuUI.Image("Lock", cell, Art.lockBadge);
                MenuUI.Anchor(lockBadge.rectTransform, anchor, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(110f, 90f));
            }
            else
            {
                Button button = MenuUI.LabelButton("Buy", cell, Art.buttonOrange, price.ToString("N0"), 36f, buy, CurrencyIcon(currency),
                    new Color32(140, 70, 10, 255));
                MenuUI.Anchor((RectTransform)button.transform, anchor, new Vector2(0.5f, 0f), offset, size);
            }
        }

        #endregion
    }
}
