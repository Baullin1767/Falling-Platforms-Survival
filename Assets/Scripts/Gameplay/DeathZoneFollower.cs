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

        public Transform FollowTarget { get; private set; }

        private void Awake()
        {
            triggerCollider = GetComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = zoneSize;
        }

        private void Update()
        {
            SnapToTarget();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
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
            if (FollowTarget == null)
            {
                return;
            }

            var targetPosition = FollowTarget.position;
            transform.position = new Vector3(targetPosition.x, targetPosition.y + verticalOffset, 0f);
        }
    }
}
