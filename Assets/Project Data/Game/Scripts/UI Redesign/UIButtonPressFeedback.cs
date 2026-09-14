using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon.BusStop
{
    public sealed class UIButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float pressedScale = 0.92f;
        [SerializeField] float releaseOvershoot = 1.04f;
        [SerializeField] float speed = 18f;

        RectTransform rect;
        Graphic graphic;
        Color normalColor;
        Coroutine animationRoutine;

        public void Configure(RectTransform target, Graphic targetGraphic = null, float scale = 0.92f)
        {
            rect = target;
            graphic = targetGraphic;
            pressedScale = scale;
            if (graphic != null)
                normalColor = graphic.color;
        }

        void Awake()
        {
            if (rect == null)
                rect = transform as RectTransform;
            if (graphic == null)
                graphic = GetComponent<Graphic>();
            if (graphic != null)
                normalColor = graphic.color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(pressedScale, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StartReleaseBounce();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(1f, false);
        }

        void StartReleaseBounce()
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(ReleaseBounce());
        }

        void AnimateTo(float target, bool darken)
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(ScaleRoutine(target));
            if (graphic != null)
                graphic.color = darken ? normalColor * 0.88f : normalColor;
        }

        IEnumerator ReleaseBounce()
        {
            if (graphic != null)
                graphic.color = normalColor;
            yield return ScaleRoutine(releaseOvershoot);
            yield return ScaleRoutine(1f);
            animationRoutine = null;
        }

        IEnumerator ScaleRoutine(float targetScale)
        {
            if (rect == null)
                yield break;

            Vector3 target = Vector3.one * targetScale;
            while ((rect.localScale - target).sqrMagnitude > 0.0001f)
            {
                rect.localScale = Vector3.Lerp(rect.localScale, target, Time.unscaledDeltaTime * speed);
                yield return null;
            }
            rect.localScale = target;
        }
    }
}
