using UnityEngine;
using System.Collections;

namespace Watermelon.BusStop
{
    public class ScooterAnimationController : MonoBehaviour
    {
        [Header("Animated Elements")]
        [SerializeField] private RectTransform scooterTransform;
        [SerializeField] private RectTransform mapTransform;
        [SerializeField] private RectTransform shadowTransform;
        
        [Header("Scooter Animation")]
        [SerializeField] private float scooterBounceHeight = 20f;
        [SerializeField] private float scooterBounceDuration = 1f;
        
        [Header("Map Animation")]
        [SerializeField] private float mapRotationDuration = 8f;
        
        [Header("Shadow Animation")]
        [SerializeField] private float shadowScaleMin = 0.9f;
        [SerializeField] private float shadowScaleMax = 1.1f;
        
        [Header("Auto Start")]
        [SerializeField] private bool playOnEnable = true;
        
        private Vector3 scooterStartPos;
        private Vector3 shadowStartScale;
        private bool isAnimating = false;
        
        private void Awake()
        {
            // Store initial positions
            if (scooterTransform != null)
                scooterStartPos = scooterTransform.anchoredPosition;
            
            if (shadowTransform != null)
                shadowStartScale = shadowTransform.localScale;
        }
        
        private void OnEnable()
        {
            if (playOnEnable)
            {
                StartAnimations();
            }
        }
        
        private void OnDisable()
        {
            StopAnimations();
        }
        
        public void StartAnimations()
        {
            if (isAnimating) return;
            
            isAnimating = true;
            
            if (scooterTransform != null)
                StartCoroutine(AnimateScooterBounce());
            
            if (mapTransform != null)
                StartCoroutine(AnimateMapRotation());
            
            if (shadowTransform != null)
                StartCoroutine(AnimateShadowPulse());
        }
        
        public void StopAnimations()
        {
            isAnimating = false;
            StopAllCoroutines();
            
            // Reset to initial positions
            if (scooterTransform != null)
                scooterTransform.anchoredPosition = scooterStartPos;
            
            if (shadowTransform != null)
                shadowTransform.localScale = shadowStartScale;
        }
        
        private IEnumerator AnimateScooterBounce()
        {
            Vector3 startPos = scooterStartPos;
            Vector3 targetPos = scooterStartPos;
            targetPos.y += scooterBounceHeight;
            
            while (isAnimating)
            {
                // Animate up
                float elapsed = 0f;
                float halfDuration = scooterBounceDuration / 2f;
                
                while (elapsed < halfDuration && isAnimating)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfDuration;
                    // Ease out cubic
                    t = 1f - Mathf.Pow(1f - t, 3f);
                    scooterTransform.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
                    yield return null;
                }
                
                if (!isAnimating) yield break;
                
                // Animate down
                elapsed = 0f;
                while (elapsed < halfDuration && isAnimating)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfDuration;
                    // Ease in cubic
                    t = t * t * t;
                    scooterTransform.anchoredPosition = Vector3.Lerp(targetPos, startPos, t);
                    yield return null;
                }
                
                if (!isAnimating) yield break;
            }
        }
        
        private IEnumerator AnimateMapRotation()
        {
            float currentRotation = 0f;
            
            while (isAnimating)
            {
                currentRotation -= (360f / mapRotationDuration) * Time.deltaTime;
                if (currentRotation <= -360f) currentRotation += 360f;
                
                mapTransform.localEulerAngles = new Vector3(0f, 0f, currentRotation);
                
                yield return null;
            }
        }
        
        private IEnumerator AnimateShadowPulse()
        {
            Vector3 scaleMin = shadowStartScale * shadowScaleMin;
            Vector3 scaleMax = shadowStartScale * shadowScaleMax;
            
            while (isAnimating)
            {
                // Scale down (when scooter goes up)
                float elapsed = 0f;
                float halfDuration = scooterBounceDuration / 2f;
                
                while (elapsed < halfDuration && isAnimating)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfDuration;
                    // Ease out
                    t = 1f - Mathf.Pow(1f - t, 2f);
                    shadowTransform.localScale = Vector3.Lerp(shadowStartScale, scaleMin, t);
                    yield return null;
                }
                
                if (!isAnimating) yield break;
                
                // Scale up (when scooter comes down)
                elapsed = 0f;
                while (elapsed < halfDuration && isAnimating)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfDuration;
                    // Ease in
                    t = t * t;
                    shadowTransform.localScale = Vector3.Lerp(scaleMin, scaleMax, t);
                    yield return null;
                }
                
                if (!isAnimating) yield break;
            }
        }
    }
}