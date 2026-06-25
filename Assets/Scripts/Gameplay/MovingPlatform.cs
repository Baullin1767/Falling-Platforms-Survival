using UnityEngine;

namespace FallingPlatformsSurvival
{
    [RequireComponent(typeof(PlatformBehaviour))]
    public sealed class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private float movementDistance = 2.5f;
        [SerializeField] private float movementSpeed = 1.75f;
        [SerializeField] private bool startMovingRight = true;
        [SerializeField] private Transform railWay;

        private PlatformBehaviour platform;
        private Vector2 centerPosition;
        private float offset;
        private int direction;

        private void Awake()
        {
            platform = GetComponent<PlatformBehaviour>();
        }

        private void OnEnable()
        {
            centerPosition = transform.position;
            offset = 0f;
            direction = startMovingRight ? 1 : -1;
        }

        private void FixedUpdate()
        {
            if (platform == null)
            {
                return;
            }

            var halfDistance = Mathf.Max(0f, movementDistance) * 0.5f;
            if (halfDistance <= 0f || movementSpeed <= 0f)
            {
                platform.SetMovementDelta(Vector2.zero);
                return;
            }

            offset += direction * movementSpeed * Time.fixedDeltaTime;
            if (offset > halfDistance)
            {
                offset = halfDistance;
                direction = -1;
            }
            else if (offset < -halfDistance)
            {
                offset = -halfDistance;
                direction = 1;
            }

            var previousPosition = (Vector2)transform.position;
            var nextPosition = centerPosition + Vector2.right * offset;
            transform.position = nextPosition;
            platform.SetMovementDelta(nextPosition - previousPosition);
        }
    }
}
