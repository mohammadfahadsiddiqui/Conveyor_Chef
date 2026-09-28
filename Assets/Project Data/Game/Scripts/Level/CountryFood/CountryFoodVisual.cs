using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BusStop
{
    /// <summary>
    /// Shows a country dish image instead of the donut model on one food character.
    ///
    /// The dish is drawn on a card that always faces the camera (so the painted "3D" image
    /// never looks flat), sits under the character's Graphics so it follows every existing
    /// animation, and has a soft shadow and a ring in the food's game colour under it.
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

        private static Sprite shadowSprite;
        private static Sprite ringSprite;

        private BaseCharacterBehavior character;
        private Transform graphics;
        private GameObject customCover;

        private Transform card;
        private SpriteRenderer dishRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer ringRenderer;

        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private Vector3 centreInGraphics;
        private float worldSize = 1f;
        private bool active;

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

            dishRenderer.sprite = dish;
            FitToUnit(dishRenderer);

            Color shadow = new Color(0f, 0f, 0f, art != null ? art.shadowAlpha : 0.35f);
            shadowRenderer.color = shadow;

            typeColour.a = art != null ? art.ringAlpha : 0.85f;
            ringRenderer.color = typeColour;

            card.gameObject.SetActive(true);
            active = true;
            LateUpdate();
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
            Color blocked = art != null ? art.blockedTint : new Color(0.72f, 0.72f, 0.72f, 1f);
            dishRenderer.color = character.IsHighlighted || character.IsSubmitted ? Color.white : blocked;
        }

        private void Build()
        {
            card = new GameObject("Country Dish").transform;
            card.SetParent(transform, false);

            ringRenderer = CreateLayer("Colour Ring", GetRingSprite(), new Vector3(0f, -0.4f, 0.03f), new Vector3(1.05f, 0.32f, 1f));
            shadowRenderer = CreateLayer("Shadow", GetShadowSprite(), new Vector3(0f, -0.42f, 0.02f), new Vector3(0.95f, 0.26f, 1f));
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
                shadowSprite = CreateEllipse("Dish Shadow", 64, ring: false);
            return shadowSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (ringSprite == null)
                ringSprite = CreateEllipse("Dish Colour Ring", 64, ring: true);
            return ringSprite;
        }

        private static Sprite CreateEllipse(string name, int size, bool ring)
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
                    float a = ring
                        ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.14f)   // soft band near the edge
                        : Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 1.6f; // soft blob
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
