using System.Collections;
using UnityEngine;

namespace FallingPlatformsSurvival
{
    public sealed class SpringPlatform : MonoBehaviour, IPlatformLandingResponder
    {
        [SerializeField] private float springVelocity = 28f;
        [SerializeField] private Transform bounceVisual;
        [SerializeField] private float bounceScaleY = 1.2f;
        [SerializeField] private float bounceDuration = 0.12f;

        private Coroutine bounceRoutine;
        private Vector3 originalScale;

        private void Awake()
        {
            if (bounceVisual == null)
            {
                var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                bounceVisual = spriteRenderer != null ? spriteRenderer.transform : transform;
            }

            originalScale = bounceVisual.localScale;
        }

        private void OnDisable()
        {
            if (bounceRoutine != null)
            {
                StopCoroutine(bounceRoutine);
                bounceRoutine = null;
            }

            if (bounceVisual != null)
            {
                bounceVisual.localScale = originalScale;
            }
        }

        public bool OnPlayerLanded(PlatformBehaviour platform, PlayerController player)
        {
            player.SetVerticalVelocity(springVelocity);

            if (bounceVisual != null && gameObject.activeInHierarchy && Application.isPlaying)
            {
                if (bounceRoutine != null)
                {
                    StopCoroutine(bounceRoutine);
                }

                bounceRoutine = StartCoroutine(PlayBounce());
            }

            return false;
        }

        private IEnumerator PlayBounce()
        {
            var stretchedScale = new Vector3(originalScale.x, originalScale.y * bounceScaleY, originalScale.z);
            var halfDuration = Mathf.Max(0.01f, bounceDuration * 0.5f);

            for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                bounceVisual.localScale = Vector3.Lerp(originalScale, stretchedScale, elapsed / halfDuration);
                yield return null;
            }

            for (var elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
            {
                bounceVisual.localScale = Vector3.Lerp(stretchedScale, originalScale, elapsed / halfDuration);
                yield return null;
            }

            bounceVisual.localScale = originalScale;
            bounceRoutine = null;
        }
    }
}
