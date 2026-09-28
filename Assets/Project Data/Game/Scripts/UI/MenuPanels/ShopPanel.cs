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
            RectTransform card = MenuRow.Create("Daily Gift", Content, 190f, !taken, Art.coin);
            MenuRow.Title(card, "DAILY GIFT", new Color32(160, 56, 18, 255));

            TimeSpan left = PlayerStats.TimeUntilNextDay;
            MenuRow.Subtitle(card, taken ? $"Next gift in {(int)left.TotalHours}h {left.Minutes:00}m" : DailyGiftCoins + " free coins every day");

            if (taken)
            {
                MenuRow.Done(card, Art.check, "TAKEN");
                return;
            }

            MenuRow.Button(card, Art.buttonGreen, "FREE", () =>
            {
                if (PlayerStats.IsDailyFlagSet("shop_gift"))
                    return;
                PlayerStats.SetDailyFlag("shop_gift");
                Grant(CurrencyType.Coins, DailyGiftCoins);
                Refresh();
            }, new Color32(30, 90, 20, 255));
        }

        private void BuildPowerUp(int index, PUSettings settings)
        {
            PUSave save = GetPowerUpSave(settings);
            Sprite icon = Art.powerUpIcons != null && index < Art.powerUpIcons.Length && Art.powerUpIcons[index] != null
                ? Art.powerUpIcons[index] : settings.Icon;

            RectTransform card = MenuRow.Create("Power-up " + index, Content, 200f, false, icon);
            MenuRow.Title(card, index < PowerUpNames.Length ? PowerUpNames[index] : settings.name);
            MenuRow.Subtitle(card, settings.Description);

            TextMeshProUGUI owned = MenuUI.Text("Owned", card, "You have " + (save != null ? Mathf.Max(0, save.Amount) : 0), 30f, MenuUI.Green, TextAlignmentOptions.Left);
            MenuUI.Anchor(owned.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(MenuRow.TextX, 26f), new Vector2(MenuRow.TextWidth, 42f));

            TextMeshProUGUI amount = MenuUI.Text("Amount", card, "+" + settings.PurchaseAmount, 34f, MenuUI.TextDark);
            MenuUI.Anchor(amount.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-MenuRow.Margin, 58f), new Vector2(MenuRow.RightWidth, 42f));
            MenuRow.Button(card, Art.buttonOrange, settings.Price.ToString("N0"), () => BuyPowerUp(index, settings),
                new Color32(140, 70, 10, 255), CurrencyIcon(settings.CurrencyType), -24f, 92f);
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
            RectTransform card = MenuRow.Create("No Ads", Content, 190f, false, Art.chefAvatar);
            MenuRow.Title(card, "REMOVE ADS");
            MenuRow.Subtitle(card, active ? "Ads are off. Thank you!" : "No ads between levels");

            if (active)
                MenuRow.Done(card, Art.check, "ACTIVE");
            else
                MenuRow.Button(card, Art.buttonGreen, PriceOf(ProductKeyType.NoAds), () => Buy(ProductKeyType.NoAds), new Color32(30, 90, 20, 255));
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
            SaveNow();
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
