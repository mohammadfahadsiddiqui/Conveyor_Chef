using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Shows a country dish image instead of the donut model on one food character.
    ///
    /// The dish is drawn on a card that always faces the camera (so the painted "3D" image
    /// never looks flat), sits under the character's Graphics so it follows every existing
    /// animation, and stands on a solid plate in the food's game colour (the colour of the tray it
    /// belongs to), so players can tell which dish goes where.
    /// Added at runtime by <see cref="HumanoidCharacterBehavior"/>; pooled characters are
    /// re-skinned every time they are placed, so the dish follows the level's country.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountryFoodVisual : MonoBehaviour
    {
        // The card is moved this fraction of its size towards the camera so the board
        // never cuts off its lower half.
        private const float TowardsCamera = 0.55f;

        // Tile footprint used when the character has no usable collider.
        private const float DefaultFootprint = 0.8f;

        // Dish and plate layout on the card (the card is one unit wide).
        private const float DishScaleOnPlate = 0.9f;
        // Where the bottom of the food sits: in the middle of the plate, or (plates switched
        // off in CountryFoodArt) just above the shadow.
        private const float DishBottomOnPlate = -0.326f;
        private const float DishBottomWithoutPlate = -0.36f;
        private static readonly Vector3 PlatePosition = new Vector3(0f, -0.28f, 0.03f);
        private static readonly Vector3 PlateShadowPosition = new Vector3(0f, -0.38f, 0.05f);
        private static readonly Vector3 PlateShadowScale = new Vector3(1.02f, 0.34f, 1f);
        private static readonly Vector3 DishShadowPosition = new Vector3(0f, -0.42f, 0.02f);
        private static readonly Vector3 DishShadowScale = new Vector3(0.95f, 0.26f, 1f);

        private const int PlateTextureWidth = 256;
        private const int PlateTextureHeight = 128;

        private static Sprite shadowSprite;
        private static readonly Dictionary<Color32, Sprite> plateSprites = new Dictionary<Color32, Sprite>();

        private BaseCharacterBehavior character;
        private Transform graphics;
        private GameObject customCover;

        private Transform card;
        private SpriteRenderer dishRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer plateRenderer;

        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private Vector3 centreInGraphics;
        private float worldSize = 1f;
        private bool active;
        private Color plateColour;
        private float dishBottom = CountryFoodArt.DefaultDishBottom;

        public void Apply(BaseCharacterBehavior owner, Transform graphicsRoot, GameObject coverObject, Sprite dish, Color typeColour)
        {
            character = owner;
            graphics = graphicsRoot != null ? graphicsRoot : owner.transform;
            customCover = coverObject;

            if (dish == null)
            {
                Restore();
                return;
            }

            if (card == null)
                Build();

            if (!active)
                MeasureAndHideModel();

            CountryFoodArt art = CountryFood.Art;

            plateColour = typeColour;
            dishBottom = art != null ? art.GetDishBottom(dish) : CountryFoodArt.DefaultDishBottom;
            dishRenderer.sprite = dish;
            ApplyPlateLayout();

            Color shadow = new Color(0f, 0f, 0f, art != null ? art.shadowAlpha : 0.35f);
            shadowRenderer.color = shadow;

            card.gameObject.SetActive(true);
            active = true;
            LateUpdate();
        }

        private void ApplyPlateLayout()
        {
            CountryFoodArt art = CountryFood.Art;
            bool showPlate = art == null || art.showColourPlate;
            float plateWidth = art != null ? art.plateWidth : 0.96f;

            plateRenderer.enabled = showPlate;
            if (showPlate)
            {
                plateRenderer.sprite = GetPlateSprite(plateColour);
                FitToUnit(plateRenderer, new Vector3(plateWidth, plateWidth, 1f));
            }

            // Stand the food itself (not the image's empty margin) in the middle of the plate.
            float dishScale = showPlate ? DishScaleOnPlate : 1f;
            FitToUnit(dishRenderer, Vector3.one * dishScale);
            Vector3 spriteSize = dishRenderer.sprite != null ? dishRenderer.sprite.bounds.size : Vector3.one;
            float heightInCard = dishScale * spriteSize.y / Mathf.Max(0.0001f, spriteSize.x);
            float bottomTarget = showPlate ? DishBottomOnPlate : DishBottomWithoutPlate;
            dishRenderer.transform.localPosition = new Vector3(0f, bottomTarget - heightInCard * (dishBottom - 0.5f), 0f);

            shadowRenderer.transform.localPosition = showPlate ? PlateShadowPosition : DishShadowPosition;
            FitToUnit(shadowRenderer, showPlate ? PlateShadowScale : DishShadowScale);
        }

        public void Restore()
        {
            if (!active)
                return;

            foreach (Renderer r in hiddenRenderers)
            {
                if (r != null)
                    r.enabled = true;
            }

            hiddenRenderers.Clear();

            if (card != null)
                card.gameObject.SetActive(false);

            active = false;
        }

        private void LateUpdate()
        {
            if (!active || card == null)
                return;

            // A covered ("unknown") piece keeps its colour secret until revealed.
            bool covered = customCover != null && customCover.activeInHierarchy;
            card.gameObject.SetActive(!covered);
            if (covered)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 centre = graphics.TransformPoint(centreInGraphics);
            float scale = worldSize * Mathf.Abs(character.transform.lossyScale.x);

            card.rotation = cam.transform.rotation;
            card.position = centre - cam.transform.forward * (scale * TowardsCamera);
            card.localScale = Vector3.one;
            SetWorldScale(card, scale);

            CountryFoodArt art = CountryFood.Art;
            Color blocked = art != null ? art.blockedTint : new Color(0.86f, 0.86f, 0.86f, 1f);
            Color tint = character.IsHighlighted || character.IsSubmitted ? Color.white : blocked;
            dishRenderer.color = tint;
            plateRenderer.color = tint;
        }

        private void Build()
        {
            card = new GameObject("Country Dish").transform;
            card.SetParent(transform, false);

            shadowRenderer = CreateLayer("Shadow", GetShadowSprite(), PlateShadowPosition, PlateShadowScale);
            plateRenderer = CreateLayer("Colour Plate", null, PlatePosition, Vector3.one);
            dishRenderer = CreateLayer("Dish", null, Vector3.zero, Vector3.one);
        }

        private SpriteRenderer CreateLayer(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale)
        {
            GameObject go = new GameObject(name);
            go.layer = GetDishLayer();
            go.transform.SetParent(card, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            if (sprite != null)
                FitToUnit(sr, localScale);
            return sr;
        }

        // The URP renderer only draws transparent objects (sprites are always transparent) on
        // the TransparentFX and UI layers, so the card must not stay on the character's layer.
        private static int GetDishLayer()
        {
            int layer = LayerMask.NameToLayer("TransparentFX");
            return layer >= 0 ? layer : 1;
        }

        // Hides the visible donut model and records where the dish goes. The size comes from
        // the character's tap collider (the tile footprint): the prefabs keep many inactive
        // leftover models at large scales, so measuring meshes would give a giant card.
        // Everything is measured through local transforms so it also works while the spawn
        // animation has the character at scale 0.
        private void MeasureAndHideModel()
        {
            hiddenRenderers.Clear();

            foreach (Renderer r in graphics.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is SpriteRenderer)
                    continue;
                if (customCover != null && r.transform.IsChildOf(customCover.transform))
                    continue;
                if (card != null && r.transform.IsChildOf(card))
                    continue;

                if (r.enabled)
                {
                    r.enabled = false;
                    hiddenRenderers.Add(r);
                }
            }

            Vector3 footprintCentre = new Vector3(0f, 0.3f, 0f);
            float footprint = DefaultFootprint;
            if (character.TryGetComponent(out BoxCollider box))
            {
                footprintCentre = box.center;
                footprint = Mathf.Max(box.size.x, box.size.z);
            }

            if (footprint < 0.1f || footprint > 3f)
                footprint = DefaultFootprint;

            Matrix4x4 graphicsToRoot = LocalChain(graphics, character.transform);
            centreInGraphics = graphicsToRoot.inverse.MultiplyPoint3x4(footprintCentre);

            CountryFoodArt art = CountryFood.Art;
            worldSize = footprint * (art != null ? art.sizeMultiplier : 1.35f);
        }

        private static Matrix4x4 LocalChain(Transform from, Transform to)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (Transform t = from; t != null && t != to; t = t.parent)
                m = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * m;
            return m;
        }

        private static void SetWorldScale(Transform t, float scale)
        {
            Vector3 parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(
                SafeDivide(scale, parentScale.x),
                SafeDivide(scale, parentScale.y),
                SafeDivide(scale, parentScale.z));
        }

        private static float SafeDivide(float a, float b) => Mathf.Abs(b) < 0.0001f ? 0f : a / Mathf.Abs(b);

        // Scale a sprite renderer so its sprite is one unit wide (times extra).
        private static void FitToUnit(SpriteRenderer sr, Vector3? extra = null)
        {
            if (sr.sprite == null)
                return;

            float w = Mathf.Max(0.0001f, sr.sprite.bounds.size.x);
            Vector3 e = extra ?? Vector3.one;
            sr.transform.localScale = new Vector3(e.x / w, e.y / w, 1f);
        }

        private static Sprite GetShadowSprite()
        {
            if (shadowSprite == null)
                shadowSprite = CreateEllipse("Dish Shadow", 64);
            return shadowSprite;
        }

        // A plate seen from the game camera: coloured side (thickness), lighter rim, a
        // recessed centre shaded by the rim, and a shine on the upper-left rim. One sprite
        // per colour, made once and shared by every dish of that colour.
        private static Sprite GetPlateSprite(Color colour)
        {
            Color32 key = colour;
            key.a = 255;
            if (plateSprites.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Sprite sprite = CreatePlate(key);
            plateSprites[key] = sprite;
            return sprite;
        }

        private static Sprite CreatePlate(Color32 colour32)
        {
            const int w = PlateTextureWidth;
            const int h = PlateTextureHeight;
            Color c = colour32;

            Color side = c * 0.58f;
            Color rim = Color.Lerp(c, Color.white, 0.22f);
            Color well = c * 0.93f;
            side.a = rim.a = well.a = 1f;

            float cx = w * 0.5f, rx = w * 0.48f, ry = h * 0.40f;
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    Color result = Color.clear;

                    result = Over(result, side, Ellipse(px, py, cx, h * 0.45f, rx, ry, out _, out _));

                    float topAlpha = Ellipse(px, py, cx, h * 0.555f, rx, ry, out float nx, out float ny);
                    result = Over(result, rim, topAlpha);

                    float wellAlpha = Ellipse(px, py, cx, h * 0.575f, rx * 0.76f, ry * 0.70f, out _, out float wy);
                    Color shadedWell = well * (1f - 0.22f * SmoothStep(0f, 0.9f, wy));
                    shadedWell.a = 1f;
                    result = Over(result, shadedWell, wellAlpha);

                    float band = Mathf.Clamp01(topAlpha - wellAlpha);
                    float shine = band * SmoothStep(0.55f, 0.95f, Mathf.Cos(Mathf.Atan2(ny, nx) - 2.2f)) * 0.55f;
                    result = Over(result, Color.white, shine);

                    float spot = Ellipse(px, py, cx - rx * 0.18f, h * 0.62f, rx * 0.30f, ry * 0.22f, out _, out _);
                    result = Over(result, Color.white, spot * 0.10f);

                    pixels[y * w + x] = result;
                }
            }

            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Dish Plate",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        // Anti-aliased coverage of an ellipse; also returns the normalised offset from its centre.
        private static float Ellipse(float px, float py, float cx, float cy, float rx, float ry, out float nx, out float ny)
        {
            nx = (px - cx) / rx;
            ny = (py - cy) / ry;
            float d = Mathf.Sqrt(nx * nx + ny * ny);
            return Mathf.Clamp01((1f - d) * ry / 1.2f);
        }

        private static Color Over(Color under, Color over, float alpha)
        {
            if (alpha <= 0f)
                return under;

            float a = alpha + under.a * (1f - alpha);
            Color result = (over * alpha + under * under.a * (1f - alpha)) / a;
            result.a = a;
            return result;
        }

        private static float SmoothStep(float from, float to, float x)
        {
            float t = Mathf.Clamp01((x - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        private static Sprite CreateEllipse(string name, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            Color32[] pixels = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 1.6f; // soft blob
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
