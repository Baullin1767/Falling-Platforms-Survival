using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FallingPlatformsSurvival
{
    public sealed class SpringPlatform : MonoBehaviour, IPlatformLandingResponder
    {
        [SerializeField] private float springVelocity = 28f;
        [SerializeField] private Transform bounceVisual;
        [SerializeField] private float bounceScaleY = 1.2f;
        [SerializeField] private float bounceDuration = 0.12f;
        [SerializeField] private Animator springAnimator;
        [SerializeField] private AnimationClip jumpAnimationClip;
        [SerializeField] private string jumpAnimationStateName = "SpringPlatformJump";

        private Coroutine bounceRoutine;
        private Vector3 originalScale;
        private PlayableGraph jumpAnimationGraph;

        private void Awake()
        {
            ResolveVisualReferences();
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

            DestroyJumpAnimationGraph();
        }

        public bool OnPlayerLanded(PlatformBehaviour platform, PlayerController player)
        {
            player?.SetVerticalVelocity(springVelocity);
            PlayJumpAnimation();

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

        private void ResolveVisualReferences()
        {
            if (bounceVisual == null)
            {
                var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                bounceVisual = spriteRenderer != null ? spriteRenderer.transform : transform;
            }

            if (springAnimator == null && bounceVisual != null)
            {
                springAnimator = bounceVisual.GetComponent<Animator>();
            }

            if (springAnimator == null)
            {
                springAnimator = GetComponentInChildren<Animator>();
            }

            originalScale = bounceVisual.localScale;
        }

        private void PlayJumpAnimation()
        {
            if (springAnimator == null)
            {
                return;
            }

            springAnimator.enabled = true;

            if (jumpAnimationClip != null)
            {
                DestroyJumpAnimationGraph();
                AnimationPlayableUtilities.PlayClip(springAnimator, jumpAnimationClip, out jumpAnimationGraph);
                return;
            }

            if (!string.IsNullOrWhiteSpace(jumpAnimationStateName))
            {
                springAnimator.Play(jumpAnimationStateName, 0, 0f);
            }
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

        private void DestroyJumpAnimationGraph()
        {
            if (jumpAnimationGraph.IsValid())
            {
                jumpAnimationGraph.Destroy();
            }
        }
    }
}
