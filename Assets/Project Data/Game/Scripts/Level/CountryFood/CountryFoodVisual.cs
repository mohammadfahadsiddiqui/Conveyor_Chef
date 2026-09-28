using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Shows a country dish image instead of the donut model on one food character.
    ///
    /// The dish is drawn on a card that always faces the camera (so the painted "3D" image
    /// never looks flat), follows the character's Graphics every frame so it keeps every existing
    /// animation, and stands on a solid plate in the food's game colour (the colour of the tray it
    /// belongs to), so players can tell which dish goes where. Dishes that cannot be picked
    /// yet wait under a clear glass cloche, which lifts off when the dish becomes pickable.
    /// Added at runtime by <see cref="HumanoidCharacterBehavior"/>; pooled characters are
    /// re-skinned every time they are placed, so the dish follows the level's country.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountryFoodVisual : MonoBehaviour
    {
        // The card is moved this fraction of its size towards the camera so the board
        // never cuts off its lower half.
        private const float TowardsCamera = 0.55f;


        // Dish and plate layout on the card (the card is one unit wide).
        // Where the bottom of the food sits: in the middle of the plate, or (plates switched
        // off in CountryFoodArt) just above the shadow.
        private const float DishBottomOnPlate = -0.326f;
        private const float DishBottomWithoutPlate = -0.36f;
        private static readonly Vector3 PlatePosition = new Vector3(0f, -0.28f, 0.03f);
        private static readonly Vector3 PlateShadowPosition = new Vector3(0f, -0.38f, 0.05f);
        private static readonly Vector3 PlateShadowScale = new Vector3(1.02f, 0.34f, 1f);
        private static readonly Vector3 DishShadowPosition = new Vector3(0f, -0.42f, 0.02f);
        private static readonly Vector3 DishShadowScale = new Vector3(0.95f, 0.26f, 1f);

        // Glass cloche over dishes that cannot be picked yet: it stands on the plate and lifts
        // off (rises and fades) when the dish becomes pickable.
        private const float ClocheRadius = 0.48f;           // round dome, card units (plate top rim is 0.48)
        private const float ClocheTilt = 0.417f;            // base ellipse height / width, as the plate rim
        private const float ClocheBaseY = -0.254f;          // plate top-surface centre
        private const float ClocheLiftHeight = 0.35f;
        private const float ClocheLiftTime = 0.3f;
        private const float ClocheDropTime = 0.12f;
        private const int ClocheTextureWidth = 256;
        private const float ClochePadding = 6f;

        private const int PlateTextureWidth = 256;
        private const int PlateTextureHeight = 128;

        private static Sprite shadowSprite;
        private static Sprite clocheSprite;
        private static readonly Dictionary<Color32, Sprite> plateSprites = new Dictionary<Color32, Sprite>();

        private BaseCharacterBehavior character;
        private Transform graphics;
        private GameObject customCover;

        private Transform card;
        private SpriteRenderer dishRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer plateRenderer;
        private SpriteRenderer clocheRenderer;

        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private Vector3 centreInGraphics;
        private bool active;
        private Color plateColour;
        private Vector4 dishRect = CountryFoodArt.DefaultDishRect;
        private Vector2 dishFit;
        private float clocheLift;        // 0 = on the plate, 1 = lifted off and gone
        private bool clocheReady;

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
            dishRect = art != null ? art.GetDishRect(dish) : CountryFoodArt.DefaultDishRect;
            dishFit = art != null ? art.GetDishFit(dish) : Vector2.zero;
            clocheReady = false;
            dishRenderer.sprite = dish;
            ApplyPlateLayout();

            Color shadow = new Color(0f, 0f, 0f, art != null ? art.shadowAlpha : 0.35f);
            shadowRenderer.color = shadow;

            card.gameObject.SetActive(true);
            active = true;
            LateUpdate();

            // The level marks the pickable pieces after placing them, so the lid takes its
            // starting state on the first real frame instead of lifting off at level start.
            clocheReady = false;
        }

        private void ApplyPlateLayout()
        {
            CountryFoodArt art = CountryFood.Art;
            bool showPlate = art == null || art.showColourPlate;
            float plateWidth = art != null ? art.plateWidth : 1f;

            plateRenderer.enabled = showPlate;
            if (showPlate)
            {
                plateRenderer.sprite = GetPlateSprite(plateColour);
                FitToUnit(plateRenderer, new Vector3(plateWidth, plateWidth, 1f));
            }

            // Size and place the food itself (not the image's empty margins): every dish gets the
            // same width, tall drinks are limited in height, and the food stands centred in the
            // middle of the plate.
            float foodWidth = art != null ? art.foodWidth : 0.8f;
            float foodMaxHeight = art != null ? art.foodMaxHeight : 0.82f;
            Vector3 spriteSize = dishRenderer.sprite != null ? dishRenderer.sprite.bounds.size : Vector3.one;
            float aspect = spriteSize.y / Mathf.Max(0.0001f, spriteSize.x);
            float rectWidth = Mathf.Max(0.05f, dishRect.z - dishRect.x);
            float rectHeight = Mathf.Max(0.05f, (dishRect.w - dishRect.y) * aspect);

            float dishScale = Mathf.Min(foodWidth / rectWidth, foodMaxHeight / rectHeight);
            float bottomTarget = showPlate ? DishBottomOnPlate : DishBottomWithoutPlate;

            // Measured fit: the largest size at which the food stays inside the glass cloche.
            if (showPlate && dishFit.x > 0f)
            {
                dishScale = dishFit.x;
                bottomTarget = dishFit.y;
            }

            FitToUnit(dishRenderer, Vector3.one * dishScale);

            float x = -dishScale * ((dishRect.x + dishRect.z) * 0.5f - 0.5f);
            float y = bottomTarget - dishScale * aspect * (dishRect.y - 0.5f);
            dishRenderer.transform.localPosition = new Vector3(x, y, 0f);

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

        // The card is not a child of the character (see Build), so it follows the character's
        // life: hidden while the character is pooled, destroyed with it.
        private void OnDisable()
        {
            if (card != null)
                card.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (card != null)
                Destroy(card.gameObject);
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
            CountryFoodArt art = CountryFood.Art;

            // The plate fills this share of a board tile. The character's own scale is kept, so
            // the spawn animation and the smaller seats on the tray still apply (measured along
            // its axes, which also works on a rotated, unevenly scaled seat).
            Transform root = character.transform;
            float rootScale = 0.5f * (root.TransformVector(Vector3.right).magnitude + root.TransformVector(Vector3.forward).magnitude);
            float plateWidth = art != null ? art.plateWidth : 1f;
            float tileFill = art != null ? art.tileFill : 0.94f;
            float scale = LevelController.ElementSize * tileFill / Mathf.Max(0.1f, plateWidth) * rootScale;

            // The card is moved towards the camera (so the board never cuts it), which would make
            // it look bigger on a perspective camera; shrink it so it looks exactly tile-sized.
            if (!cam.orthographic)
            {
                float depth = Vector3.Dot(centre - cam.transform.position, cam.transform.forward);
                if (depth > 0.01f)
                    scale = scale * depth / (depth + TowardsCamera * scale);
            }

            card.rotation = cam.transform.rotation;
            card.position = centre - cam.transform.forward * (scale * TowardsCamera);
            card.localScale = new Vector3(scale, scale, scale);

            Color blocked = art != null ? art.blockedTint : new Color(0.95f, 0.95f, 0.95f, 1f);
            bool pickable = character.IsHighlighted || character.IsSubmitted;
            Color tint = pickable ? Color.white : blocked;
            dishRenderer.color = tint;
            plateRenderer.color = tint;

            UpdateCloche(!pickable && (art == null || art.showCloche));
        }

        // Pieces placed at level start take their state at once; later a lid lifts off with a
        // quick rise and fade when the dish becomes pickable (and drops back fast if needed).
        private void UpdateCloche(bool covered)
        {
            if (!clocheReady)
            {
                clocheLift = covered ? 0f : 1f;
                clocheReady = true;
            }
            else if (covered)
            {
                clocheLift = Mathf.Max(0f, clocheLift - Time.deltaTime / ClocheDropTime);
            }
            else
            {
                clocheLift = Mathf.Min(1f, clocheLift + Time.deltaTime / ClocheLiftTime);
            }

            clocheRenderer.enabled = clocheLift < 1f;
            if (!clocheRenderer.enabled)
                return;

            float rise = 1f - (1f - clocheLift) * (1f - clocheLift) * (1f - clocheLift);   // ease out
            clocheRenderer.transform.localPosition = new Vector3(0f, ClocheBaseY + rise * ClocheLiftHeight, -0.01f);
            FitToUnit(clocheRenderer, Vector3.one * (ClocheSpriteWidth * (1f + 0.08f * rise)));
            clocheRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((clocheLift - 0.25f) / 0.75f));
        }

        private void Build()
        {
            // Not parented to the character: on the tray it sits on a rotated seat with uneven
            // scale, which would shear the card and slide the dish off its plate.
            card = new GameObject("Country Dish").transform;
            card.gameObject.layer = GetDishLayer();

            // Sorted as one piece against other dishes; inside it always shadow < plate < dish.
            card.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();

            shadowRenderer = CreateLayer("Shadow", GetShadowSprite(), PlateShadowPosition, PlateShadowScale, 0);
            plateRenderer = CreateLayer("Colour Plate", null, PlatePosition, Vector3.one, 1);
            dishRenderer = CreateLayer("Dish", null, Vector3.zero, Vector3.one, 2);
            clocheRenderer = CreateLayer("Glass Cloche", GetClocheSprite(), new Vector3(0f, ClocheBaseY, -0.01f), Vector3.one * ClocheSpriteWidth, 3);
        }

        private SpriteRenderer CreateLayer(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, int order)
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
            sr.sortingOrder = order;
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

        // Hides the visible donut model and records where the dish goes: the centre of the
        // character's tap collider. (The prefabs keep many inactive leftover models at large
        // scales, so their meshes are not measured.) Measured through local transforms so it
        // also works while the spawn animation has the character at scale 0.
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
            if (character.TryGetComponent(out BoxCollider box))
                footprintCentre = box.center;

            Matrix4x4 graphicsToRoot = LocalChain(graphics, character.transform);
            centreInGraphics = graphicsToRoot.inverse.MultiplyPoint3x4(footprintCentre);
        }

        private static Matrix4x4 LocalChain(Transform from, Transform to)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (Transform t = from; t != null && t != to; t = t.parent)
                m = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * m;
            return m;
        }

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

        private static Sprite GetClocheSprite()
        {
            if (clocheSprite == null)
                clocheSprite = CreateCloche();
            return clocheSprite;
        }

        // Width of the cloche sprite on the card: the dome diameter plus the texture padding.
        private static float ClocheSpriteWidth => 2f * ClocheRadius * ClocheTextureWidth / (ClocheTextureWidth - 2f * ClochePadding);

        // A round glass serving dome seen from the game camera: the top outline is a circle and
        // the base is an ellipse matching the plate rim. Faint glass that thickens towards the
        // edge, a bright outline, the base rim (back half through the glass, front half on top),
        // two highlights and a knob on a short neck. The pivot is the base centre.
        private static Sprite CreateCloche()
        {
            const int w = ClocheTextureWidth;
            float r = (w - 2f * ClochePadding) * 0.5f;
            float knobR = r * 0.085f;
            float baseLine = r * ClocheTilt + ClochePadding + 2f;
            int h = (int)(baseLine + r + knobR * 2.6f + ClochePadding);
            float cx = w * 0.5f;
            Color glass = new Color(0.88f, 0.94f, 1f, 1f);
            Color neckColour = new Color(0.78f, 0.84f, 0.90f, 1f);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f - cx, py = y + 0.5f - baseLine;
                    Color c = Color.clear;

                    float ellipse = Mathf.Sqrt(px * px + (py / ClocheTilt) * (py / ClocheTilt)) / r;
                    float circle = Mathf.Sqrt(px * px + py * py) / r;
                    float d = py >= 0f ? circle : ellipse;
                    float inside = Mathf.Clamp01((1f - d) * r / 1.3f);
                    float rimBand = Mathf.Max(0f, 1f - Mathf.Abs(ellipse - 1f) * r * ClocheTilt / 1.8f);

                    if (py >= 0f)
                        c = Over(c, Color.white, rimBand * 0.30f);

                    if (inside > 0f)
                    {
                        c = Over(c, glass, inside * (0.07f + 0.42f * Mathf.Pow(d, 5f)));

                        if (py >= 0f)
                        {
                            float angle = Mathf.Atan2(py, px);
                            float streak = SmoothStep(0.58f, 0.64f, circle) * (1f - SmoothStep(0.80f, 0.86f, circle)) *
                                           SmoothStep(0.45f, 0.85f, Mathf.Cos(angle - 2.3f));
                            c = Over(c, Color.white, streak * 0.60f);

                            float spot = SmoothStep(0.60f, 0.64f, circle) * (1f - SmoothStep(0.70f, 0.74f, circle)) *
                                         SmoothStep(0.85f, 0.97f, Mathf.Cos(angle - 0.62f));
                            c = Over(c, Color.white, spot * 0.40f);
                        }
                    }

                    if (py >= 0f)
                        c = Over(c, Color.white, Mathf.Max(0f, 1f - Mathf.Abs(circle - 0.99f) * r / 2.2f) * 0.80f);
                    else
                        c = Over(c, Color.white, rimBand * 0.85f);

                    // Knob on a short neck.
                    if (Mathf.Abs(px) < knobR * 0.45f && py >= r - 2f && py <= r + knobR * 0.6f)
                        c = Over(c, neckColour, 0.95f);

                    float ky = py - (r + knobR * 1.25f);
                    float kd = Mathf.Sqrt(px * px + ky * ky) / knobR;
                    float knob = Mathf.Clamp01((1f - kd) * knobR / 1.1f);
                    if (knob > 0f)
                    {
                        float shade = 0.72f + 0.28f * SmoothStep(-0.9f, 0.9f, (ky - px * 0.6f) / knobR);
                        c = Over(c, new Color(0.82f * shade, 0.88f * shade, 0.95f * shade, 1f), knob);
                        float shine = SmoothStep(0.55f, 0f, Mathf.Sqrt((px + knobR * 0.35f) * (px + knobR * 0.35f) + (ky - knobR * 0.35f) * (ky - knobR * 0.35f)) / knobR);
                        c = Over(c, Color.white, knob * shine * 0.8f);
                    }

                    pixels[y * w + x] = c;
                }
            }

            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Dish Cloche",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, baseLine / h), w);
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
