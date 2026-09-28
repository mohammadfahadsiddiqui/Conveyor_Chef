using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// The art that already exists in the project (the Asian pack), registered under the same
    /// names WorldArt uses (e.g. "World/asia/india/levelselect_hero"), so it keeps working in
    /// every screen while new continents' files are still being made. Resources/WorldArtLibrary.
    /// A file with the same name in Resources/World always wins over an entry here.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldArtLibrary", menuName = "Conveyor Chef/World Art Library")]
    public sealed class WorldArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "WorldArtLibrary";

        [Serializable]
        public struct Entry
        {
            public string path;
            public Sprite sprite;
        }

        public Entry[] entries = new Entry[0];

        public Sprite Get(string path)
        {
            if (entries == null)
                return null;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].sprite != null && string.Equals(entries[i].path, path, StringComparison.OrdinalIgnoreCase))
                    return entries[i].sprite;
            }
            return null;
        }
    }
}
