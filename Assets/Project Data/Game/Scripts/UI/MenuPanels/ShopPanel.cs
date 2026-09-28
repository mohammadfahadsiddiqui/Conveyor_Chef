using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// SHOP: a free daily gift, power-ups for coins, coins for diamonds, and the real-money
    /// items (coin packs, No Ads) through the IAP module. Power-up counts are written to the
    /// same saves the game's power-up buttons use.
    /// </summary>
    public sealed class ShopPanel : MenuPanel
    {
        private const int DailyGiftCoins = 50;

        private static readonly string[] PowerUpNames = { "UNDO", "HINT", "SHUFFLE" };

        // Coins bought with diamonds: coins, diamonds.
        private static readonly (int, int)[] DiamondTrades = { (500, 10), (1500, 25), (4000, 60) };

        // Coin packs from the IAP settings (same amounts as the template store).
        private static readonly (ProductKeyType, int)[] CoinPacks =
        {
            (ProductKeyType.GoldSmall, 150), (ProductKeyType.GoldMedium, 450), (ProductKeyType.GoldBig, 1000)
        };

        private bool purchaseSubscribed;

        protected override string Title => "SHOP";
        protected override bool ShowCurrencies => true;

        protected override void OnOpened()
        {
            if (!purchaseSubscribed)
            {
                IAPManager.OnPurchaseComplete += OnPurchaseComplete;
                purchaseSubscribed = true;
            }
        }

        protected override void OnClosed()
        {
            if (purchaseSubscribed)
            {
                IAPManager.OnPurchaseComplete -= OnPurchaseComplete;
                purchaseSubscribed = false;
            }
        }

        protected override void BuildContent()
        {
            BuildDailyGift();

            MenuUI.Section(Content, "POWER-UPS");
            if (Art.powerUps != null)
            {
                for (int i = 0; i < Art.powerUps.Length; i++)
                {
                    if (Art.powerUps[i] != null)
                        BuildPowerUp(i, Art.powerUps[i]);
                }
            }

            MenuUI.Section(Content, "COINS");
            RectTransform trades = Row("Trades", 330f);
            foreach ((int coins, int diamonds) in DiamondTrades)
                BuildOfferCard(trades, Art.coin, coins.ToString("N0"), Art.diamond, diamonds.ToString(), Art.buttonPurple,
                    new Color32(70, 20, 110, 255), () => TradeDiamonds(coins, diamonds));

            MenuUI.Section(Content, "COIN PACKS");
            RectTransform packs = Row("Packs", 330f);
            foreach ((ProductKeyType key, int coins) in CoinPacks)
            {
                ProductKeyType product = key;
                BuildOfferCard(packs, Art.coin, coins.ToString("N0"), null, PriceOf(product), Art.buttonGreen,
                    new Color32(30, 90, 20, 255), () => Buy(product));
            }

            BuildNoAds();
        }

        #region Sections

        private void BuildDailyGift()
        {
            bool taken = PlayerStats.IsDailyFlagSet("shop_gift");
            RectTransform card = MenuUI.Card("Daily Gift", Content, 180f, !taken);

            Image icon = MenuUI.Image("Coin", card, Art.coin);
            MenuUI.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(120f, 120f));

            TextMeshProUGUI title = MenuUI.Text("Title", card, "DAILY GIFT", 38f, new Color32(170, 60, 20, 255), TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(176f, -30f), new Vector2(340f, 50f));

            TimeSpan left = PlayerStats.TimeUntilNextDay;
            TextMeshProUGUI body = MenuUI.Text("Body", card,
                taken ? $"Next gift in {(int)left.TotalHours}h {left.Minutes:00}m" : DailyGiftCoins + " free coins every day",
                28f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(176f, -86f), new Vector2(340f, 40f));

            Button claim = MenuUI.LabelButton("Claim", card, taken ? Art.buttonGray : Art.buttonGreen, taken ? "TAKEN" : "FREE", 40f, () =>
            {
                if (PlayerStats.IsDailyFlagSet("shop_gift"))
                    return;
                PlayerStats.SetDailyFlag("shop_gift");
                Grant(CurrencyType.Coins, DailyGiftCoins);
                Refresh();
            }, null, taken ? new Color32(70, 70, 70, 255) : new Color32(30, 90, 20, 255));
            claim.interactable = !taken;
            MenuUI.Anchor((RectTransform)claim.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(200f, 104f));
        }

        private void BuildPowerUp(int index, PUSettings settings)
        {
            PUSave save = GetPowerUpSave(settings);
            RectTransform card = MenuUI.Card("Power-up " + index, Content, 190f);

            Sprite icon = Art.powerUpIcons != null && index < Art.powerUpIcons.Length && Art.powerUpIcons[index] != null
                ? Art.powerUpIcons[index] : settings.Icon;
            Image image = MenuUI.Image("Icon", card, icon);
            MenuUI.Anchor(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(140f, 140f));

            string name = index < PowerUpNames.Length ? PowerUpNames[index] : settings.name;
            TextMeshProUGUI title = MenuUI.Text("Title", card, name, 38f, MenuUI.TextDark, TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(184f, -24f), new Vector2(330f, 48f));

            TextMeshProUGUI description = MenuUI.Text("Description", card, settings.Description, 26f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(184f, -74f), new Vector2(330f, 36f));

            TextMeshProUGUI owned = MenuUI.Text("Owned", card, "Owned: " + (save != null ? Mathf.Max(0, save.Amount) : 0), 28f, MenuUI.Green, TextAlignmentOptions.Left);
            MenuUI.Anchor(owned.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(184f, 24f), new Vector2(330f, 40f));

            Button buy = MenuUI.LabelButton("Buy", card, Art.buttonOrange, "x" + settings.PurchaseAmount + "  " + settings.Price, 34f,
                () => BuyPowerUp(index, settings), CurrencyIcon(settings.CurrencyType), new Color32(140, 70, 10, 255));
            MenuUI.Anchor((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(230f, 104f));
        }

        private RectTransform Row(string name, float height)
        {
            RectTransform row = MenuUI.Rect(name, Content);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private void BuildOfferCard(Transform row, Sprite icon, string amount, Sprite priceIcon, string price, Sprite button,
            Color outline, UnityEngine.Events.UnityAction action)
        {
            Image card = MenuUI.Image("Offer", row, MenuUI.Rounded(26, 4, MenuUI.CardFill, MenuUI.CardBorder), null, false);

            Image image = MenuUI.Image("Icon", card.transform, icon);
            MenuUI.Anchor(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(110f, 110f));

            TextMeshProUGUI text = MenuUI.OutlinedText("Amount", card.transform, amount, 44f, new Color32(150, 90, 10, 255));
            MenuUI.Anchor(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(220f, 56f));

            Button buy = MenuUI.LabelButton("Buy", card.transform, button, price, 32f, action, priceIcon, outline);
            MenuUI.Anchor((RectTransform)buy.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(200f, 96f));
        }

        private void BuildNoAds()
        {
            bool active = IsNoAdsOwned();
            MenuUI.Section(Content, "NO ADS");
            RectTransform card = MenuUI.Card("No Ads", Content, 180f);

            Image icon = MenuUI.Image("Icon", card, Art.chefAvatar);
            MenuUI.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(116f, 132f));

            TextMeshProUGUI title = MenuUI.Text("Title", card, "REMOVE ADS", 38f, MenuUI.TextDark, TextAlignmentOptions.Left);
            MenuUI.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(176f, -30f), new Vector2(340f, 50f));
            TextMeshProUGUI body = MenuUI.Text("Body", card, active ? "Thank you! Ads are off." : "No more ads between levels", 28f, MenuUI.TextSoft, TextAlignmentOptions.Left);
            MenuUI.Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(176f, -86f), new Vector2(340f, 40f));

            if (active)
            {
                Image check = MenuUI.Image("Owned", card, Art.check);
                MenuUI.Anchor(check.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(100f, 88f));
            }
            else
            {
                Button buy = MenuUI.LabelButton("Buy", card, Art.buttonGreen, PriceOf(ProductKeyType.NoAds), 34f,
                    () => Buy(ProductKeyType.NoAds), null, new Color32(30, 90, 20, 255));
                MenuUI.Anchor((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(210f, 104f));
            }
        }

        #endregion

        #region Actions

        private void BuyPowerUp(int index, PUSettings settings)
        {
            PUSave save = GetPowerUpSave(settings);
            if (save == null)
            {
                Toast("Not available right now");
                return;
            }

            if (!Spend(settings.CurrencyType, settings.Price))
            {
                Toast("Not enough coins");
                return;
            }

            save.Amount = Mathf.Max(0, save.Amount) + settings.PurchaseAmount;
            SaveController.MarkAsSaveIsRequired();
            try { AudioController.PlaySound(AudioController.Sounds.buttonSound); } catch { }
            Toast("+" + settings.PurchaseAmount + " " + (index < PowerUpNames.Length ? PowerUpNames[index] : "POWER-UPS"), new Color32(40, 110, 30, 255));
            Refresh();
        }

        private void TradeDiamonds(int coins, int diamonds)
        {
            if (!Spend(CurrencyType.Diamonds, diamonds))
            {
                Toast("Not enough diamonds");
                return;
            }

            Grant(CurrencyType.Coins, coins);
            Refresh();
        }

        private void Buy(ProductKeyType product)
        {
            if (!IAPManager.IsInitialised)
            {
                Toast("Store not available. Try again later.");
                return;
            }

            IAPManager.BuyProduct(product);
        }

        private void OnPurchaseComplete(ProductKeyType product)
        {
            foreach ((ProductKeyType key, int coins) in CoinPacks)
            {
                if (key == product)
                {
                    Grant(CurrencyType.Coins, coins);
                    Refresh();
                    return;
                }
            }

            if (product == ProductKeyType.NoAds)
            {
                try
                {
                    SaveController.GetSaveObject<SimpleBoolSave>("Product_" + ProductKeyType.NoAds).Value = true;
                    AdsManager.DisableForcedAd();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Shop] Could not apply No Ads: " + ex.Message);
                }

                Toast("Ads removed!", new Color32(40, 110, 30, 255));
                Refresh();
            }
        }

        #endregion

        private static PUSave GetPowerUpSave(PUSettings settings)
        {
            try
            {
                PUSave save = SaveController.GetSaveObject<PUSave>(string.Format("powerUp_{0}", settings.Type));
                if (save.Amount == -1)
                    save.Amount = settings.DefaultAmount;
                return save;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsNoAdsOwned()
        {
            try
            {
                if (SaveController.GetSaveObject<SimpleBoolSave>("Product_" + ProductKeyType.NoAds).Value)
                    return true;
                ProductData data = IAPManager.GetProductData(ProductKeyType.NoAds);
                return data != null && data.IsPurchased;
            }
            catch
            {
                return false;
            }
        }

        private static string PriceOf(ProductKeyType product)
        {
            try
            {
                string price = IAPManager.GetProductLocalPriceString(product);
                return string.IsNullOrWhiteSpace(price) ? "BUY" : price;
            }
            catch
            {
                return "BUY";
            }
        }
    }
}
