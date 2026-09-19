using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon
{
    /// <summary>
    /// Small, dependency-free press feedback so World Map controls feel like
    /// real buttons instead of static images.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapButtonFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.75f, 1f)] private float pressedScale = 0.92f;
        [SerializeField, Min(1f)] private float responseSpeed = 18f;

        private RectTransform target;
        private Vector3 baseScale = Vector3.one;
        private Vector3 desiredScale = Vector3.one;

        private void Awake()
        {
            target = transform as RectTransform;
            if (target != null)
            {
                baseScale = target.localScale;
                desiredScale = baseScale;
            }
        }

        private void OnEnable()
        {
            if (target == null)
                target = transform as RectTransform;

            if (target != null)
            {
                baseScale = target.localScale;
                desiredScale = baseScale;
            }
        }

        private void Update()
        {
            if (target == null)
                return;

            target.localScale = Vector3.Lerp(
                target.localScale,
                desiredScale,
                1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            desiredScale = baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            desiredScale = baseScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            desiredScale = baseScale;
        }

        private void OnDisable()
        {
            if (target != null)
                target.localScale = baseScale;
        }
    }
}
