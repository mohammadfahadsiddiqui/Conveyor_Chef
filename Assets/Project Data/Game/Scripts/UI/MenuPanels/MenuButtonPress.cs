using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon
{
    /// <summary>Small press-down scale on menu buttons.</summary>
    public sealed class MenuButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 baseScale = Vector3.one;
        private bool pressed;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            transform.localScale = baseScale * 0.94f;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            transform.localScale = baseScale;
        }
    }
}
