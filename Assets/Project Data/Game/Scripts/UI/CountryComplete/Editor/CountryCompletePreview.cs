#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Watermelon.EditorTools
{
    /// <summary>Play-mode previews of the Country Completed panel (buttons only log).</summary>
    public static class CountryCompletePreview
    {
        [MenuItem("Conveyor Chef/Country Complete/Preview China (Play Mode)", priority = 1)]
        private static void PreviewChina() => Preview(0);

        [MenuItem("Conveyor Chef/Country Complete/Preview Japan (Play Mode)", priority = 2)]
        private static void PreviewJapan() => Preview(1);

        [MenuItem("Conveyor Chef/Country Complete/Preview India (Play Mode)", priority = 3)]
        private static void PreviewIndia() => Preview(2);

        [MenuItem("Conveyor Chef/Country Complete/Preview South Korea (Play Mode)", priority = 4)]
        private static void PreviewSouthKorea() => Preview(3);

        [MenuItem("Conveyor Chef/Country Complete/Preview Thailand - last country (Play Mode)", priority = 5)]
        private static void PreviewThailand() => Preview(4);

        [MenuItem("Conveyor Chef/Country Complete/Reset 'already shown' flags", priority = 20)]
        private static void ResetShown()
        {
            for (int i = 0; i < CountryCatalog.CountryCount; i++)
                PlayerPrefs.DeleteKey("CC_CountryComplete_Shown_" + i);
            PlayerPrefs.Save();
            Debug.Log("[CountryComplete] The panel will show again for every country.");
        }

        [MenuItem("Conveyor Chef/Country Complete/Preview China (Play Mode)", true)]
        [MenuItem("Conveyor Chef/Country Complete/Preview Japan (Play Mode)", true)]
        [MenuItem("Conveyor Chef/Country Complete/Preview India (Play Mode)", true)]
        [MenuItem("Conveyor Chef/Country Complete/Preview South Korea (Play Mode)", true)]
        [MenuItem("Conveyor Chef/Country Complete/Preview Thailand - last country (Play Mode)", true)]
        private static bool CanPreview() => Application.isPlaying;

        private static void Preview(int country)
        {
            CountryCompletePanel.Show(
                country,
                () => Debug.Log("[CountryComplete] Preview: CONTINUE -> LevelSelection (next country)"),
                () => Debug.Log("[CountryComplete] Preview: HOME -> menu"));
        }
    }
}
#endif
