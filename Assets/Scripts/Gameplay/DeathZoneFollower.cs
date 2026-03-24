using UnityEngine;

namespace FallingPlatformsSurvival
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DeathZoneFollower : MonoBehaviour
    {
        [SerializeField] private float verticalOffset = -7f;
        [SerializeField] private Vector2 zoneSize = new(30f, 2f);

        private BoxCollider2D triggerCollider;
        private GameManager gameManager;
        private bool initialized;

        public Transform FollowTarget { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            SnapToTarget();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            EnsureInitialized();

            if (other.GetComponent<PlayerController>() != null)
            {
                gameManager?.HandlePlayerDeath();
            }
        }

        public void SetGameManager(GameManager owner)
        {
            gameManager = owner;
        }

        public void SetFollowTarget(Transform targetTransform)
        {
            FollowTarget = targetTransform;
        }

        public void SnapToTarget()
        {
            EnsureInitialized();

            if (FollowTarget == null)
            {
                return;
            }

            var targetPosition = FollowTarget.position;
            transform.position = new Vector3(targetPosition.x, targetPosition.y + verticalOffset, 0f);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            triggerCollider = GetComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = zoneSize;
            initialized = true;
        }
    }
}
