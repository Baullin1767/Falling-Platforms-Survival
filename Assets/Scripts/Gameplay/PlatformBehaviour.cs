using UnityEngine;

namespace FallingPlatformsSurvival
{
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlatformBehaviour : MonoBehaviour
    {
        [Header("Collapse")]
        [SerializeField] private float fallGravityScale = 2.25f;
        [SerializeField] private float vanishRecycleDelay = 0.12f;
        [SerializeField] private float fallAngularVelocity = 75f;

        private PlatformManager manager;
        private BoxCollider2D boxCollider;
        private Rigidbody2D body;
        private bool initialized;

        private float collapseDelay;
        private float countdownRemaining;
        private float recycleTimer;

        public int PlatformId { get; private set; }
        public PlatformCollapseMode CollapseMode { get; private set; }
        public PlatformCollapseState CollapseState { get; private set; } = PlatformCollapseState.Recycling;
        public float CountdownRemaining => countdownRemaining;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();

            if (!gameObject.activeSelf)
            {
                return;
            }

            if (CollapseState == PlatformCollapseState.Triggered && manager != null && manager.SimulationActive)
            {
                countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.deltaTime);

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
            EnsureInitialized();
            manager = owner;
            PlatformId = id;
        }

        public void Activate(Vector2 position, Vector2 scale, float timerSeconds, float fallWeight)
        {
            EnsureInitialized();

            collapseDelay = timerSeconds;
            countdownRemaining = timerSeconds;
            CollapseMode = Random.value <= fallWeight ? PlatformCollapseMode.Fall : PlatformCollapseMode.Vanish;
            CollapseState = PlatformCollapseState.Idle;
            recycleTimer = 0f;

            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = new Vector3(scale.x, scale.y, 1f);

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
            EnsureInitialized();

            if (CollapseState != PlatformCollapseState.Idle)
            {
                return;
            }

            CollapseState = PlatformCollapseState.Triggered;
            countdownRemaining = collapseDelay;
        }

        public void SetSimulationEnabled(bool enabled)
        {
            EnsureInitialized();
            body.simulated = enabled;
        }

        public void RecycleImmediately()
        {
            EnsureInitialized();
            CollapseState = PlatformCollapseState.Recycling;
            body.simulated = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            gameObject.SetActive(false);
        }

        public PlatformRuntimeState GetRuntimeState(PlatformBehaviour currentPlatform)
        {
            EnsureInitialized();
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
                boxCollider.enabled = false;
                return;
            }

            CollapseState = PlatformCollapseState.Falling;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = fallGravityScale;
            body.linearVelocity = new Vector2(Random.Range(-1f, 1f), -1f);
            body.angularVelocity = Random.Range(-fallAngularVelocity, fallAngularVelocity);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }
            
            boxCollider = GetComponent<BoxCollider2D>();
            body = GetComponent<Rigidbody2D>();

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = false;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            initialized = true;
        }
    }
}
