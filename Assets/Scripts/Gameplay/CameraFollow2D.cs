using UnityEngine;

namespace FallingPlatformsSurvival
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float verticalOffset = 1.75f;

        private Vector3 dampVelocity;
        private float minimumY;
        private bool initialized;

        public Transform Target { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void LateUpdate()
        {
            EnsureInitialized();

            if (Target == null)
            {
                return;
            }

            var targetPosition = GetTargetPosition();
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref dampVelocity, smoothTime);
        }

        public void SetTarget(Transform targetTransform)
        {
            Target = targetTransform;
        }

        public void SnapToTarget()
        {
            EnsureInitialized();

            if (Target == null)
            {
                return;
            }

            transform.position = GetTargetPosition();
            dampVelocity = Vector3.zero;
        }

        private Vector3 GetTargetPosition()
        {
            var targetY = Mathf.Max(minimumY, Target.position.y + verticalOffset);
            return new Vector3(Target.position.x, targetY, transform.position.z);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            minimumY = transform.position.y;
            initialized = true;
        }
    }
}
