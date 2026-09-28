using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Makes coins and diamonds work in every scene. The currency system (CurrenciesController)
    /// only lives in the Game scene, so before the first level every other scene showed
    /// fallback numbers and could not spend or reward currency. This starts it as soon as the
    /// game runs (from Resources/CurrencyBootstrapConfig) and keeps every "Coin Value" and
    /// "Diamond Value" text in every scene showing the live amount.
    /// </summary>
    public static class CurrencyBootstrap
    {
        private const string ConfigPath = "CurrencyBootstrapConfig";
        private static readonly string[] CoinTextNames = { "Coin Value" };
        private static readonly string[] DiamondTextNames = { "Diamond Value" };

        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnGameStart()
        {
            if (!subscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                subscribed = true;
            }

            Ensure();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnFirstSceneLoaded()
        {
            Ensure();
            BindCounters(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Ensure();
            BindCounters(scene);
        }

        /// <summary>Starts the save and currency systems if nothing has yet.</summary>
        public static bool Ensure()
        {
            if (CurrenciesController.Currencies != null)
                return true;

            try
            {
                if (!SaveController.IsSaveLoaded)
                    SaveController.Initialise(useAutoSave: false);

                CurrencyBootstrapConfig config = Resources.Load<CurrencyBootstrapConfig>(ConfigPath);
                if (config == null || config.database == null)
                {
                    Debug.LogWarning("[Currency] Resources/" + ConfigPath + " is missing; currencies start with the Game scene.");
                    return false;
                }

                GameObject go = new GameObject("[CURRENCIES]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                CurrenciesController controller = go.AddComponent<CurrenciesController>();

                FieldInfo field = typeof(CurrenciesController).GetField("currenciesDatabase", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                {
                    Debug.LogWarning("[Currency] CurrenciesController.currenciesDatabase not found.");
                    UnityEngine.Object.Destroy(go);
                    return false;
                }

                field.SetValue(controller, config.database);
                controller.Initialise();
                return CurrenciesController.Currencies != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Currency] Could not start currencies: " + ex.Message);
                return false;
            }
        }

        /// <summary>Attaches a live counter to every coin and diamond value text in the scene.</summary>
        public static void BindCounters(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (Matches(text.name, CoinTextNames))
                        Attach(text, CurrencyType.Coins);
                    else if (Matches(text.name, DiamondTextNames))
                        Attach(text, CurrencyType.Diamonds);
                }
            }
        }

        private static void Attach(TMP_Text text, CurrencyType type)
        {
            CurrencyCounterText counter = text.GetComponent<CurrencyCounterText>();
            if (counter == null)
                counter = text.gameObject.AddComponent<CurrencyCounterText>();
            counter.SetCurrency(type);
        }

        private static bool Matches(string name, string[] names)
        {
            foreach (string candidate in names)
            {
                if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /// <summary>Keeps a TMP text showing the live amount of one currency.</summary>
    [DisallowMultipleComponent]
    public sealed class CurrencyCounterText : MonoBehaviour
    {
        [SerializeField] private CurrencyType currency = CurrencyType.Coins;

        private TMP_Text text;
        private bool subscribed;
        private int shownAmount = int.MinValue;
        private string shownText;

        public void SetCurrency(CurrencyType type)
        {
            currency = type;
            Refresh();
        }

        private void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            CurrenciesController.InvokeOrSubcrtibe(OnCurrenciesReady);
            Refresh();
        }

        private void OnDisable()
        {
            if (!subscribed)
                return;

            try { CurrenciesController.UnsubscribeGlobalCallback(OnCurrencyChanged); } catch { }
            subscribed = false;
        }

        private void OnCurrenciesReady()
        {
            if (this == null || !isActiveAndEnabled)
                return;

            if (!subscribed)
            {
                CurrenciesController.SubscribeGlobalCallback(OnCurrencyChanged);
                subscribed = true;
            }
            Refresh();
        }

        private void OnCurrencyChanged(Currency changed, int difference)
        {
            if (changed != null && changed.CurrencyType == currency)
                Refresh();
        }

        // Other scripts may write the text too; keep it right every frame it is shown.
        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
            if (text == null || CurrenciesController.Currencies == null)
                return;

            int amount = CurrenciesController.Get(currency);
            if (amount != shownAmount || shownText == null)
            {
                shownAmount = amount;
                shownText = amount.ToString("N0");
            }

            if (text.text != shownText)
                text.text = shownText;
        }
    }
}
