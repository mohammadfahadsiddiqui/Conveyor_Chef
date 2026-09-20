using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace Watermelon.IAPStore
{
    public class IAPButton : MonoBehaviour
    {
        [SerializeField] Image backImage;
        [SerializeField] Button button;
        [SerializeField] TMP_Text priceText;
        [SerializeField] GameObject loadingObject;

        [Space]
        [SerializeField] Sprite activeBackSprite;
        [SerializeField] Sprite unactiveBackSprite;

        private ProductKeyType key;
        private ProductData product;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        public void Init(ProductKeyType key)
        {
            this.key = key;

            product = IAPManager.GetProductData(key);

            UpdateState();
        }

        private void UpdateState()
        {
            if (product != null)
            {
                if (button != null)
                    button.interactable = true;

                if (loadingObject != null)
                    loadingObject.SetActive(false);

                if (priceText != null)
                {
                    priceText.gameObject.SetActive(true);
                    priceText.text = IAPManager.GetProductLocalPriceString(key);
                }

                if (backImage != null)
                    backImage.sprite = activeBackSprite;
            }
            else
            {
                SetDisabledState();
            }
        }

        private void SetDisabledState()
        {
            if (button != null)
                button.interactable = false;

            if (loadingObject != null)
                loadingObject.SetActive(true);

            if (priceText != null)
                priceText.gameObject.SetActive(false);

            if (backImage != null)
                backImage.sprite = unactiveBackSprite;
        }

        private void OnButtonClicked()
        {
            if (product == null || !IAPManager.IsInitialised)
                return;

            AudioController.PlaySound(AudioController.Sounds.buttonSound);
            IAPManager.BuyProduct(key);
        }
    }
}