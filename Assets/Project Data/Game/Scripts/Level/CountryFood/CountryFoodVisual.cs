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

        // Tile footprint used when the character has no usable collider.
        private const float DefaultFootprint = 0.8f;

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
        private const float ClocheWidth = 0.9f;
        private const float ClocheBaseY = -0.254f;
        private const float ClocheLiftHeight = 0.35f;
        private const float ClocheLiftTime = 0.3f;
        private const float ClocheDropTime = 0.12f;
        private const int ClocheTextureSize = 256;
        private const float ClocheBase = 26f;      // base line in the cloche texture (its pivot)

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
        private float worldSize = 1f;
        private bool active;
        private Color plateColour;
        private Vector4 dishRect = CountryFoodArt.DefaultDishRect;
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
            float plateWidth = art != null ? art.plateWidth : 0.96f;

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
            FitToUnit(dishRenderer, Vector3.one * dishScale);

            float bottomTarget = showPlate ? DishBottomOnPlate : DishBottomWithoutPlate;
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
            // World size of the character's footprint; also right on a rotated, unevenly scaled seat.
            Transform root = character.transform;
            float rootScale = 0.5f * (root.TransformVector(Vector3.right).magnitude + root.TransformVector(Vector3.forward).magnitude);
            float scale = worldSize * rootScale;

            card.rotation = cam.transform.rotation;
            card.position = centre - cam.transform.forward * (scale * TowardsCamera);
            card.localScale = new Vector3(scale, scale, scale);

            CountryFoodArt art = CountryFood.Art;
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
            FitToUnit(clocheRenderer, Vector3.one * (ClocheWidth * (1f + 0.08f * rise)));
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
            clocheRenderer = CreateLayer("Glass Cloche", GetClocheSprite(), new Vector3(0f, ClocheBaseY, -0.01f), Vector3.one * ClocheWidth, 3);
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

        // A clear glass serving dome: faint glass that thickens towards its edge, a bright
        // outline, the base rim (back half seen through the glass, front half on top), two
        // highlights and a knob. The pivot is the base line, so it stands on the plate.
        private static Sprite CreateCloche()
        {
            const int size = ClocheTextureSize;
            const float rx = 120f, ry = 196f, rimRy = 16f, knobR = 13f;
            float cx = size * 0.5f;
            Color glass = new Color(0.90f, 0.95f, 1f, 1f);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    Color c = Color.clear;

                    float rnx = (px - cx) / rx, rny = (py - ClocheBase) / rimRy;
                    float rimD = Mathf.Sqrt(rnx * rnx + rny * rny);
                    float rimBand = Mathf.Max(0f, 1f - Mathf.Abs(rimD - 1f) * rimRy / 2.2f);

                    if (py >= ClocheBase)
                    {
                        c = Over(c, Color.white, rimBand * 0.35f);

                        float dx = (px - cx) / rx, dy = (py - ClocheBase) / ry;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float inside = Mathf.Clamp01((1f - d) * rx / 1.5f);
                        float edge = Mathf.Max(0f, 1f - Mathf.Abs(d - 0.985f) * rx / 3f);

                        if (inside > 0f)
                        {
                            c = Over(c, glass, inside * (0.10f + 0.40f * Mathf.Pow(d, 6f)));
                            c = Over(c, Color.white, edge * 0.85f * inside);

                            float angle = Mathf.Atan2(dy, dx);
                            float streak = SmoothStep(0.55f, 0.62f, d) * (1f - SmoothStep(0.80f, 0.88f, d)) *
                                           SmoothStep(0.35f, 0.8f, Mathf.Cos(angle - 2.25f));
                            c = Over(c, Color.white, streak * 0.55f);

                            float small = SmoothStep(0.55f, 0.6f, d) * (1f - SmoothStep(0.66f, 0.71f, d)) *
                                          SmoothStep(0.8f, 0.95f, Mathf.Cos(angle - 0.75f));
                            c = Over(c, Color.white, small * 0.35f);
                        }
                        else
                        {
                            c = Over(c, Color.white, edge * 0.85f);
                        }
                    }
                    else
                    {
                        c = Over(c, Color.white, rimBand * 0.85f);
                    }

                    float kx = px - cx, ky = py - (ClocheBase + ry + knobR * 0.55f);
                    float kd = Mathf.Sqrt(kx * kx + ky * ky) / knobR;
                    float knob = Mathf.Clamp01((1f - kd) * knobR / 1.2f);
                    if (knob > 0f)
                    {
                        float shade = 0.78f + 0.22f * SmoothStep(-0.8f, 0.8f, (ky - kx * 0.5f) / knobR);
                        c = Over(c, new Color(0.80f * shade, 0.86f * shade, 0.92f * shade, 1f), knob);
                        c = Over(c, Color.white, knob * Mathf.Max(0f, 1f - Mathf.Abs(kd - 0.85f) * 5f) * 0.8f);
                    }

                    pixels[y * size + x] = c;
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Dish Cloche",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, ClocheBase / size), size);
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
