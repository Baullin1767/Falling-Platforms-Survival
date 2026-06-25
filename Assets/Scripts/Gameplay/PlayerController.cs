using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingPlatformsSurvival
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 14f;
        [SerializeField] private float coyoteTimeSeconds = 0.12f;
        [SerializeField] private float jumpBufferSeconds = 0.14f;
        [SerializeField] private float groundCheckDistance = 0.04f;

        [Header("Visuals")] 
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private readonly ContactFilter2D groundFilter = new() { useTriggers = false };

        private bool initialized;
        private Rigidbody2D body;
        private CapsuleCollider2D boxCollider;
        private InputAction moveAction;
        private InputAction jumpAction;

        private float keyboardHorizontalInput;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;
        private Vector2 spawnPosition;
        private Collider2D startGroundCollider;
        private Action expiredStartGroundTouched;
        private bool touchMoveLeftHeld;
        private bool touchMoveRightHeld;
        private bool hasJumpedFromStartGround;
        private bool isGrounded;
        private bool isAlive = true;

        public PlatformBehaviour CurrentPlatform { get; private set; }
        public bool IsGrounded => isGrounded;
        public bool IsAlive => isAlive;
        public bool HasBufferedJump => Time.time - lastJumpPressedTime <= jumpBufferSeconds;
        public bool CanJump => isGrounded || Time.time - lastGroundedTime <= coyoteTimeSeconds;
        public float HorizontalIntent => ResolveHorizontalInput();

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();

            var actionAsset = InputSystem.actions;
            if (actionAsset == null)
            {
                return;
            }

            moveAction = actionAsset.FindAction("Move", true);
            jumpAction = actionAsset.FindAction("Jump", true);

            moveAction?.Enable();
            jumpAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            jumpAction?.Disable();
        }

        private void Update()
        {
            if (!isAlive)
            {
                return;
            }

            keyboardHorizontalInput = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;

            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                QueueJumpPress();
            }

            spriteRenderer.flipX = HorizontalIntent switch
            {
                < 0 => true,
                > 0 => false,
                _ => spriteRenderer.flipX
            };
        }

        private void FixedUpdate()
        {
            EnsureInitialized();

            if (!isAlive)
            {
                return;
            }

            RefreshGroundState();

            var velocity = body.linearVelocity;
            velocity.x = isGrounded ? 0f : ResolveHorizontalInput() * moveSpeed;
            body.linearVelocity = velocity;

            if (isGrounded && CurrentPlatform != null && CurrentPlatform.MovementDelta != Vector2.zero)
            {
                body.position += CurrentPlatform.MovementDelta;
            }

            if (HasBufferedJump && CanJump)
            {
                velocity = body.linearVelocity;
                velocity.y = jumpForce;
                body.linearVelocity = velocity;

                isGrounded = false;
                CurrentPlatform = null;
                hasJumpedFromStartGround = true;
                lastGroundedTime = float.NegativeInfinity;
                lastJumpPressedTime = float.NegativeInfinity;
            }
        }

        public void ResetForRun(Vector2 newSpawnPosition)
        {
            ResetForRun(newSpawnPosition, null, null);
        }

        public void ResetForRun(Vector2 newSpawnPosition, Collider2D newStartGroundCollider, Action onExpiredStartGroundTouched)
        {
            EnsureInitialized();

            spawnPosition = newSpawnPosition;
            startGroundCollider = newStartGroundCollider;
            expiredStartGroundTouched = onExpiredStartGroundTouched;
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;

            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            isAlive = true;
            isGrounded = false;
            CurrentPlatform = null;
            hasJumpedFromStartGround = false;
            keyboardHorizontalInput = 0f;
            touchMoveLeftHeld = false;
            touchMoveRightHeld = false;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
        }

        public void HandleDeath()
        {
            EnsureInitialized();

            if (!isAlive)
            {
                return;
            }

            isAlive = false;
            keyboardHorizontalInput = 0f;
            touchMoveLeftHeld = false;
            touchMoveRightHeld = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        public void SetTouchMoveInput(float direction)
        {
            EnsureInitialized();

            var normalizedDirection = Mathf.Sign(direction);
            if (normalizedDirection < 0f)
            {
                touchMoveLeftHeld = true;
            }
            else if (normalizedDirection > 0f)
            {
                touchMoveRightHeld = true;
            }
        }

        public void ClearTouchMoveInput(float direction)
        {
            EnsureInitialized();

            var normalizedDirection = Mathf.Sign(direction);
            if (normalizedDirection < 0f)
            {
                touchMoveLeftHeld = false;
            }
            else if (normalizedDirection > 0f)
            {
                touchMoveRightHeld = false;
            }
            else
            {
                ClearTouchMoveInput();
            }
        }

        public void ClearTouchMoveInput()
        {
            EnsureInitialized();
            touchMoveLeftHeld = false;
            touchMoveRightHeld = false;
        }

        public void QueueJumpPress()
        {
            EnsureInitialized();

            if (!isAlive)
            {
                return;
            }

            lastJumpPressedTime = Time.time;
        }

        public void SetVerticalVelocity(float velocityY)
        {
            EnsureInitialized();

            var velocity = body.linearVelocity;
            velocity.y = velocityY;
            body.linearVelocity = velocity;

            isGrounded = false;
            CurrentPlatform = null;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
        }

        public PlayerRuntimeState GetRuntimeState()
        {
            EnsureInitialized();

            return new PlayerRuntimeState(
                (Vector2)transform.position,
                body != null ? body.linearVelocity : Vector2.zero,
                isGrounded,
                isAlive,
                CurrentPlatform != null ? CurrentPlatform.PlatformId : -1,
                CanJump,
                HasBufferedJump);
        }

        private void RefreshGroundState()
        {
            EnsureInitialized();

            var hitCount = boxCollider.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance);
            PlatformBehaviour groundedPlatform = null;
            var groundedStartGround = false;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.normal.y < 0.45f)
                {
                    continue;
                }

                groundedPlatform = hit.collider.GetComponent<PlatformBehaviour>();
                if (groundedPlatform != null)
                {
                    break;
                }

                if (startGroundCollider != null && hit.collider == startGroundCollider)
                {
                    groundedStartGround = true;
                }
            }

            var groundedNow = (groundedPlatform != null || groundedStartGround) && body.linearVelocity.y <= 0.5f;
            if (groundedNow)
            {
                if (groundedStartGround && groundedPlatform == null && hasJumpedFromStartGround)
                {
                    expiredStartGroundTouched?.Invoke();
                    return;
                }

                lastGroundedTime = Time.time;
                if (CurrentPlatform != groundedPlatform)
                {
                    CurrentPlatform = groundedPlatform;
                    CurrentPlatform?.NotifyPlayerLanded(this);
                }
            }
            else if (isGrounded && groundedPlatform == null && !groundedStartGround)
            {
                CurrentPlatform = null;
            }

            isGrounded = groundedNow;
            
            animator.SetBool("Jump", !groundedNow);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            body = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<CapsuleCollider2D>();


            body.gravityScale = 4f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            spawnPosition = transform.position;
            initialized = true;
        }

        private float ResolveHorizontalInput()
        {
            if (touchMoveLeftHeld == touchMoveRightHeld)
            {
                return keyboardHorizontalInput;
            }

            return touchMoveRightHeld ? 1f : -1f;
        }
    }
}
