using UnityEngine;

namespace FallingPlatformsSurvival
{
    public sealed class FragilePlatform : MonoBehaviour, IPlatformLandingResponder
    {
        [SerializeField] private float breakDelay = 0.3f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] breakingSprites;

        private bool isBreaking;
        private float breakStartedAt;

        private void Awake()
        {
            ResolveVisualReferences();
        }

        private void OnEnable()
        {
            isBreaking = false;
            ResolveVisualReferences();

            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }

        private void Update()
        {
            if (!isBreaking || spriteRenderer == null || breakingSprites == null || breakingSprites.Length == 0)
            {
                return;
            }

            var normalizedTime = breakDelay > 0f ? Mathf.Clamp01((Time.time - breakStartedAt) / breakDelay) : 1f;
            var frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * breakingSprites.Length), 0, breakingSprites.Length - 1);
            var frame = breakingSprites[frameIndex];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }

        public bool OnPlayerLanded(PlatformBehaviour platform, PlayerController player)
        {
            if (isBreaking)
            {
                return true;
            }

            isBreaking = true;
            breakStartedAt = Time.time;

            if (spriteRenderer != null && breakingSprites != null && breakingSprites.Length > 0 && breakingSprites[0] != null)
            {
                spriteRenderer.sprite = breakingSprites[0];
            }

            platform.StartCollapseCountdown(breakDelay, PlatformCollapseMode.Vanish);
            return true;
        }

        private void ResolveVisualReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (idleSprite == null && spriteRenderer != null)
            {
                idleSprite = spriteRenderer.sprite;
            }
        }
    }
}
