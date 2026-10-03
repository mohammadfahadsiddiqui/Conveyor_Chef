using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Keeps TextMeshPro text sharp when its canvas changes scale.
    ///
    /// TMP bakes an edge-sharpness value into each text mesh for the scale it was drawn at. On a
    /// Screen Space Overlay canvas, a later canvas-scale change (another device, the design-frame
    /// fit, text first drawn before the canvas was sized) adjusts that value the wrong way, and
    /// the letters turn into soft, blurry blocks. Redrawing the text at the current scale fixes it.
    /// </summary>
    public static class SharpText
    {
        private static readonly List<TMP_Text> texts = new List<TMP_Text>();

        /// <summary>Redraws every shown text under <paramref name="root"/> at the current scale.</summary>
        public static void Rebuild(Transform root)
        {
            if (root == null)
                return;

            root.GetComponentsInChildren(false, texts);
            for (int i = 0; i < texts.Count; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.isActiveAndEnabled)
                    text.ForceMeshUpdate(false, true);
            }
            texts.Clear();
        }
    }
}
