using UnityEngine;

namespace FallingPlatformsSurvival
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlatformBehaviour : MonoBehaviour
    {
        [Header("Collapse")]
        [SerializeField] private float fallGravityScale = 2.25f;
        [SerializeField] private float vanishRecycleDelay = 0.12f;
        [SerializeField] private float fallAngularVelocity = 75f;

        [Header("Visuals")]
        [SerializeField] private Color idleColor = new(0.9f, 0.88f, 0.34f, 1f);
        [SerializeField] private Color warningColor = new(1f, 0.49f, 0.18f, 1f);
        [SerializeField] private Color fallingColor = new(0.85f, 0.28f, 0.22f, 1f);

        private PlatformManager manager;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private Rigidbody2D body;

        private float collapseDelay;
        private float countdownRemaining;
        private float recycleTimer;

        public int PlatformId { get; private set; }
        public PlatformCollapseMode CollapseMode { get; private set; }
        public PlatformCollapseState CollapseState { get; private set; } = PlatformCollapseState.Recycling;
        public float CountdownRemaining => countdownRemaining;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            body = GetComponent<Rigidbody2D>();

            spriteRenderer.sprite = PlaceholderSpriteLibrary.SquareSprite;
            boxCollider.size = Vector2.one;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = false;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (CollapseState == PlatformCollapseState.Triggered && manager != null && manager.SimulationActive)
            {
                countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.deltaTime);
                var lerp = collapseDelay <= Mathf.Epsilon ? 1f : 1f - (countdownRemaining / collapseDelay);
                spriteRenderer.color = Color.Lerp(idleColor, warningColor, lerp);

                if (countdownRemaining <= 0f)
                {
                    Collapse();
                }
            }

            if (CollapseState == PlatformCollapseState.Vanished)
            {
                recycleTimer -= Time.deltaTime;
                if (recycleTimer <= 0f && manager != null)
                {
                    manager.RecyclePlatform(this);
                }
            }
        }

        public void Initialize(PlatformManager owner, int id)
        {
            manager = owner;
            PlatformId = id;
        }

        public void Activate(Vector2 position, Vector2 scale, float timerSeconds, float fallWeight)
        {
            collapseDelay = timerSeconds;
            countdownRemaining = timerSeconds;
            CollapseMode = Random.value <= fallWeight ? PlatformCollapseMode.Fall : PlatformCollapseMode.Vanish;
            CollapseState = PlatformCollapseState.Idle;
            recycleTimer = 0f;

            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(scale.x, scale.y, 1f);

            spriteRenderer.enabled = true;
            spriteRenderer.color = idleColor;
            boxCollider.enabled = true;

            body.simulated = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            gameObject.SetActive(true);
        }

        public void NotifyPlayerLanded()
        {
            if (CollapseState != PlatformCollapseState.Idle)
            {
                return;
            }

            CollapseState = PlatformCollapseState.Triggered;
            countdownRemaining = collapseDelay;
        }

        public void SetSimulationEnabled(bool enabled)
        {
            body.simulated = enabled;
        }

        public void RecycleImmediately()
        {
            CollapseState = PlatformCollapseState.Recycling;
            body.simulated = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            gameObject.SetActive(false);
        }

        public PlatformRuntimeState GetRuntimeState(PlatformBehaviour currentPlatform)
        {
            return new PlatformRuntimeState(
                PlatformId,
                gameObject.activeSelf,
                transform.position,
                CollapseMode,
                countdownRemaining,
                CollapseState,
                currentPlatform == this);
        }

        private void Collapse()
        {
            if (CollapseMode == PlatformCollapseMode.Vanish)
            {
                CollapseState = PlatformCollapseState.Vanished;
                recycleTimer = vanishRecycleDelay;
                spriteRenderer.enabled = false;
                boxCollider.enabled = false;
                return;
            }

            CollapseState = PlatformCollapseState.Falling;
            spriteRenderer.color = fallingColor;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = fallGravityScale;
            body.linearVelocity = new Vector2(Random.Range(-1f, 1f), -1f);
            body.angularVelocity = Random.Range(-fallAngularVelocity, fallAngularVelocity);
        }
    }
}
