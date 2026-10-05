using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Edit-mode preview of <see cref="DesignFrame"/>: picking another device in the Simulator (or
    /// another Game view size) re-fits the open scenes right away, exactly as the game will.
    ///
    /// The fitted anchors are a preview only. They are put back as authored before every save,
    /// Play, script reload, build and scene close, so scene files always keep the design-phone
    /// layout. Anchors changed by hand while previewing another device are converted back to
    /// design values. Edit the layout with the Galaxy S20 Ultra selected to see the true values.
    /// Toggle with Conveyor Chef > Preview Device Fit In Edit Mode.
    /// </summary>
    [InitializeOnLoad]
    public static class DesignFramePreview
    {
        private const string MenuPath = "Conveyor Chef/Preview Device Fit In Edit Mode";
        private const string EnabledKey = "ConveyorChef.DesignFramePreview.Enabled";
        private const double TickInterval = 0.2;

        private static readonly Dictionary<Canvas, DesignFrameFitter> fitters = new Dictionary<Canvas, DesignFrameFitter>();
        private static readonly List<Canvas> dead = new List<Canvas>();
        private static readonly Dictionary<Canvas, float> textScales = new Dictionary<Canvas, float>();
        private static double nextTick;

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        static DesignFramePreview()
        {
            EditorApplication.update += Tick;
            EditorSceneManager.sceneSaving += (scene, path) => RestoreScene(scene);
            EditorSceneManager.sceneClosing += (scene, removing) => RestoreScene(scene);
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    RestoreAll();
            };
            AssemblyReloadEvents.beforeAssemblyReload += RestoreAll;
            EditorApplication.quitting += RestoreAll;
        }

        [MenuItem(MenuPath, priority = 200)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled)
                RestoreAll();
            nextTick = 0;
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (EditorApplication.timeSinceStartup < nextTick)
                return;
            nextTick = EditorApplication.timeSinceStartup + TickInterval;

            if (!Enabled)
                return;

            PlayModeWindow.GetRenderingResolution(out uint width, out uint height);
            Vector2 playView = new Vector2(width, height);

            bool changed = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    {
                        if (!DesignFrameFitter.ShouldFit(canvas))
                            continue;

                        if (!fitters.TryGetValue(canvas, out DesignFrameFitter fitter))
                        {
                            fitter = new DesignFrameFitter(canvas);
                            fitters[canvas] = fitter;
                        }

                        // The screen this canvas is really laid out for (its size times its scale).
                        // The play view's resolution can belong to another window (a Game tab
                        // next to the Simulator) and fit the layout for the wrong screen.
                        Vector2 screen = ((RectTransform)canvas.transform).rect.size * canvas.scaleFactor;
                        if (screen.x < 1f || screen.y < 1f)
                            screen = playView;
                        if (screen.x < 1f || screen.y < 1f)
                            continue;

                        changed |= fitter.Apply(screen, true);

                        // Text drawn at another canvas scale stays soft: redraw it at the new scale.
                        if (canvas.scaleFactor > 0f &&
                            (!textScales.TryGetValue(canvas, out float drawnAt) || !Mathf.Approximately(drawnAt, canvas.scaleFactor)))
                        {
                            textScales[canvas] = canvas.scaleFactor;
                            SharpText.Rebuild(canvas.transform);
                            changed = true;
                        }
                    }
                }
            }

            foreach (Canvas canvas in fitters.Keys)
            {
                if (canvas == null)
                    dead.Add(canvas);
            }
            for (int i = 0; i < dead.Count; i++)
            {
                fitters.Remove(dead[i]);
                textScales.Remove(dead[i]);
            }
            dead.Clear();

            if (changed)
            {
                Canvas.ForceUpdateCanvases();
                EditorApplication.QueuePlayerLoopUpdate();
                InternalEditorUtility.RepaintAllViews();
            }
        }

        private static void RestoreScene(Scene scene)
        {
            foreach (KeyValuePair<Canvas, DesignFrameFitter> pair in fitters)
            {
                if (pair.Key != null && pair.Key.gameObject.scene == scene)
                {
                    pair.Value.Restore();
                    dead.Add(pair.Key);
                }
            }

            for (int i = 0; i < dead.Count; i++)
                fitters.Remove(dead[i]);
            dead.Clear();

            // Saved, closed or reloaded scenes are fitted again on the next tick.
            nextTick = EditorApplication.timeSinceStartup + TickInterval;
        }

        public static void RestoreAll()
        {
            foreach (KeyValuePair<Canvas, DesignFrameFitter> pair in fitters)
            {
                if (pair.Key != null)
                    pair.Value.Restore();
            }
            fitters.Clear();
        }

        /// <summary>Builds always use the scenes as authored.</summary>
        private sealed class BuildGuard : IPreprocessBuildWithReport
        {
            public int callbackOrder => -1000;

            public void OnPreprocessBuild(BuildReport report)
            {
                RestoreAll();
            }
        }
    }
}
