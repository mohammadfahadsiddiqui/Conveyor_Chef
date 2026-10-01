using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// A designed panel picture with its title ribbon and close button drawn in, in Resources as
    /// "MenuPanelFrames/&lt;key&gt;" (story, challenges, customize, shop, collection, achievements,
    /// leaderboard). The panel keeps the picture's proportions, fills its cream area with the
    /// content and puts the close hit area over the drawn X. Panels without a picture use the
    /// shared frame from MenuPanelArt.
    /// </summary>
    public sealed class MenuPanelFrame
    {
        public const string ResourceFolder = "MenuPanelFrames/";

        // Measured in the picture's pixels, origin top-left.
        private struct Layout
        {
            public RectInt Cream;          // the cream content area
            public Vector3 Close;          // x, y = centre of the X button, z = radius

            public Layout(int left, int top, int right, int bottom, int closeX, int closeY, int closeRadius)
            {
                Cream = new RectInt(left, top, right - left, bottom - top);
                Close = new Vector3(closeX, closeY, closeRadius);
            }
        }

        private static readonly Dictionary<string, Layout> Layouts = new Dictionary<string, Layout>
        {
            { "story",        new Layout(71, 236, 866, 1567, 877, 128, 46) },
            { "challenges",   new Layout(71, 222, 870, 1576, 874, 119, 56) },
            { "customize",    new Layout(74, 241, 867, 1549, 874, 137, 58) },
            { "shop",         new Layout(72, 263, 868, 1555, 868, 146, 50) },
            { "achievements", new Layout(78, 250, 860, 1531, 860, 136, 52) },
            { "leaderboard",  new Layout(73, 254, 864, 1519, 876, 160, 54) },
        };

        // Unknown pictures: the shared layout of these frames, as fractions of the picture.
        private static readonly Rect DefaultCream = new Rect(0.076f, 0.145f, 0.845f, 0.785f);
        private static readonly Vector3 DefaultClose = new Vector3(0.928f, 0.08f, 0.058f);

        public Sprite Sprite { get; private set; }

        /// <summary>Width / height of the picture.</summary>
        public float Aspect => Sprite.rect.width / Sprite.rect.height;

        /// <summary>Cream area as anchors (0-1, origin bottom-left) of the frame.</summary>
        public Vector2 CreamMin { get; private set; }
        public Vector2 CreamMax { get; private set; }

        /// <summary>Close button box as anchors of the frame.</summary>
        public Vector2 CloseMin { get; private set; }
        public Vector2 CloseMax { get; private set; }

        public static MenuPanelFrame Load(string key)
        {
            Sprite sprite = Resources.Load<Sprite>(ResourceFolder + key);
            if (sprite == null)
                return null;

            float width = sprite.rect.width;
            float height = sprite.rect.height;
            MenuPanelFrame frame = new MenuPanelFrame { Sprite = sprite };

            if (Layouts.TryGetValue(key, out Layout layout))
            {
                RectInt cream = layout.Cream;
                frame.CreamMin = new Vector2(cream.xMin / width, 1f - cream.yMax / height);
                frame.CreamMax = new Vector2(cream.xMax / width, 1f - cream.yMin / height);

                Vector3 close = layout.Close;
                frame.CloseMin = new Vector2((close.x - close.z) / width, 1f - (close.y + close.z) / height);
                frame.CloseMax = new Vector2((close.x + close.z) / width, 1f - (close.y - close.z) / height);
            }
            else
            {
                frame.CreamMin = new Vector2(DefaultCream.xMin, 1f - DefaultCream.yMax);
                frame.CreamMax = new Vector2(DefaultCream.xMax, 1f - DefaultCream.yMin);
                frame.CloseMin = new Vector2(DefaultClose.x - DefaultClose.z, 1f - DefaultClose.y - DefaultClose.z * width / height);
                frame.CloseMax = new Vector2(DefaultClose.x + DefaultClose.z, 1f - DefaultClose.y + DefaultClose.z * width / height);
            }

            return frame;
        }
    }
}
