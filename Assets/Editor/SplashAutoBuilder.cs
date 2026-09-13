#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;

/// <summary>
/// Triggers splash scene build automatically upon recompilation when the flag file exists.
/// </summary>
[InitializeOnLoad]
public static class SplashAutoBuilder
{
    private const string FLAG_FILE = "Assets/Editor/.rebuild_splash";

    static SplashAutoBuilder()
    {
        EditorApplication.delayCall += CheckAndBuild;
    }

    private static void CheckAndBuild()
    {
        if (File.Exists(FLAG_FILE))
        {
            try
            {
                File.Delete(FLAG_FILE);
            }
            catch {}

            Debug.Log("[SplashAutoBuilder] Auto-build flag detected! Building Conveyor Chef splash screen...");
            SplashSceneBuilder.BuildSplashSceneSilent();
        }
    }
}
#endif
