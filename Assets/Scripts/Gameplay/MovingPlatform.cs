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
        private BoxCollider2D platformCollider;
        private Vector2 centerPosition;
        private Vector2 railAxis = Vector2.right;
        private Vector3 railInitialLocalPosition;
        private bool hasRailInitialLocalPosition;
        private bool compensateChildRailMovement;
        private float offset;
        private float halfTravelDistance;
        private int direction;

        private void Awake()
        {
            platform = GetComponent<PlatformBehaviour>();
            platformCollider = GetComponent<BoxCollider2D>();
            CacheRailInitialLocalPosition();
        }

        private void OnEnable()
        {
            ConfigureRailMovement();
            direction = startMovingRight ? 1 : -1;
        }

        private void OnDisable()
        {
            ResetRailLocalPosition();
        }

        private void FixedUpdate()
        {
            if (platform == null)
            {
                return;
            }

            if (halfTravelDistance <= 0f || movementSpeed <= 0f)
            {
                platform.SetMovementDelta(Vector2.zero);
                return;
            }

            offset += direction * movementSpeed * Time.fixedDeltaTime;
            if (offset > halfTravelDistance)
            {
                offset = halfTravelDistance;
                direction = -1;
            }
            else if (offset < -halfTravelDistance)
            {
                offset = -halfTravelDistance;
                direction = 1;
            }

            var previousPosition = (Vector2)transform.position;
            var nextPosition = centerPosition + railAxis * offset;
            transform.position = nextPosition;

            if (compensateChildRailMovement)
            {
                KeepChildRailInPlace(nextPosition - previousPosition);
            }

            platform.SetMovementDelta(nextPosition - previousPosition);
        }

        private void ConfigureRailMovement()
        {
            centerPosition = transform.position;
            railAxis = Vector2.right;
            offset = 0f;
            halfTravelDistance = Mathf.Max(0f, movementDistance) * 0.5f;
            compensateChildRailMovement = false;

            if (railWay == null)
            {
                return;
            }

            CacheRailInitialLocalPosition();
            ResetRailLocalPosition();

            centerPosition = railWay.position;
            railAxis = railWay.right;
            if (railAxis.sqrMagnitude < 0.001f)
            {
                railAxis = Vector2.right;
            }
            else
            {
                railAxis.Normalize();
            }

            compensateChildRailMovement = railWay.IsChildOf(transform);

            var railLength = ResolveRailLength();
            var platformLength = ResolvePlatformLengthAlongRail();
            halfTravelDistance = Mathf.Max(0f, (railLength - platformLength) * 0.5f);
            offset = Mathf.Clamp(Vector2.Dot((Vector2)transform.position - centerPosition, railAxis), -halfTravelDistance, halfTravelDistance);
        }

        private float ResolveRailLength()
        {
            var spriteRenderer = railWay.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return Mathf.Abs(spriteRenderer.sprite.bounds.size.x * railWay.lossyScale.x);
            }

            return Mathf.Max(0f, movementDistance);
        }

        private float ResolvePlatformLengthAlongRail()
        {
            if (platformCollider == null)
            {
                return 0f;
            }

            var bounds = platformCollider.bounds;
            return Mathf.Abs(railAxis.x) * bounds.size.x + Mathf.Abs(railAxis.y) * bounds.size.y;
        }

        private void KeepChildRailInPlace(Vector2 platformDelta)
        {
            var parentScale = transform.lossyScale;
            var localCompensation = new Vector3(
                parentScale.x != 0f ? -platformDelta.x / parentScale.x : 0f,
                parentScale.y != 0f ? -platformDelta.y / parentScale.y : 0f,
                0f);

            railWay.localPosition += localCompensation;
        }

        private void CacheRailInitialLocalPosition()
        {
            if (railWay == null || hasRailInitialLocalPosition)
            {
                return;
            }

            railInitialLocalPosition = railWay.localPosition;
            hasRailInitialLocalPosition = true;
        }

        private void ResetRailLocalPosition()
        {
            if (railWay != null && hasRailInitialLocalPosition)
            {
                railWay.localPosition = railInitialLocalPosition;
            }
        }
    }
}
