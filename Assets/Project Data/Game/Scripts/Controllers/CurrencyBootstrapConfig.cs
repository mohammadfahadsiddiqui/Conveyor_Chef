using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Points the currency bootstrap at the game's currencies database, so coins and diamonds
    /// work in every scene. Lives in Resources as "CurrencyBootstrapConfig".
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyBootstrapConfig", menuName = "Conveyor Chef/Currency Bootstrap Config")]
    public sealed class CurrencyBootstrapConfig : ScriptableObject
    {
        public CurrenciesDatabase database;
    }
}
