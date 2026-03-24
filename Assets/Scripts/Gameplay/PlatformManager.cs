using System.Collections.Generic;
using UnityEngine;

namespace FallingPlatformsSurvival
{
    public sealed class PlatformManager : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private int poolSize = 24;
        [SerializeField] private int startingPlatformCount = 10;

        [Header("Layout")]
        [SerializeField] private float horizontalBounds = 7f;
        [SerializeField] private float maxHorizontalStep = 4.25f;
        [SerializeField] private Vector2 verticalSpacingRange = new(2.35f, 3.45f);
        [SerializeField] private Vector2 platformWidthRange = new(2.6f, 4.8f);
        [SerializeField] private float platformHeight = 0.75f;
        [SerializeField] private float spawnAheadDistance = 18f;
        [SerializeField] private float recycleBelowDistance = 12f;

        [Header("Collapse")]
        [SerializeField] private Vector2 collapseDelayRange = new(0.85f, 1.6f);
        [SerializeField, Range(0f, 1f)] private float fallWeight = 0.75f;

        private readonly List<PlatformBehaviour> allPlatforms = new();
        private readonly List<PlatformBehaviour> activePlatforms = new();

        private PlayerController player;
        private float nextSpawnY;
        private float lastSpawnX;

        public bool SimulationActive { get; private set; } = true;

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            if (SimulationActive)
            {
                EnsurePlatformsAhead();
            }

            RecyclePlatformsBelowThreshold();
        }

        public void SetPlayer(PlayerController playerController)
        {
            player = playerController;
        }

        public Vector2 ResetPlatforms()
        {
            EnsurePool();

            for (var i = 0; i < allPlatforms.Count; i++)
            {
                allPlatforms[i].RecycleImmediately();
            }

            activePlatforms.Clear();
            lastSpawnX = 0f;

            const float startPlatformY = -3f;
            var startScale = new Vector2(5.5f, platformHeight);
            SpawnPlatformAt(new Vector2(0f, startPlatformY), startScale, 1.9f);

            nextSpawnY = startPlatformY + Random.Range(verticalSpacingRange.x, verticalSpacingRange.y);
            for (var i = 1; i < startingPlatformCount; i++)
            {
                SpawnProceduralPlatform();
            }

            return new Vector2(0f, startPlatformY + 1.45f);
        }

        public void SetSimulationActive(bool isActive)
        {
            SimulationActive = isActive;
            for (var i = 0; i < activePlatforms.Count; i++)
            {
                activePlatforms[i].SetSimulationEnabled(isActive);
            }
        }

        public IReadOnlyList<PlatformRuntimeState> GetActivePlatformStates(PlatformBehaviour currentPlatform)
        {
            var snapshot = new List<PlatformRuntimeState>(activePlatforms.Count);
            for (var i = 0; i < activePlatforms.Count; i++)
            {
                var platform = activePlatforms[i];
                if (!platform.gameObject.activeSelf)
                {
                    continue;
                }

                snapshot.Add(platform.GetRuntimeState(currentPlatform));
            }

            return snapshot;
        }

        public void RecyclePlatform(PlatformBehaviour platform)
        {
            if (platform == null)
            {
                return;
            }

            activePlatforms.Remove(platform);
            platform.RecycleImmediately();
        }

        private void EnsurePool()
        {
            if (allPlatforms.Count >= poolSize)
            {
                return;
            }

            for (var i = allPlatforms.Count; i < poolSize; i++)
            {
                var platformObject = new GameObject($"Platform_{i:D2}");
                platformObject.transform.SetParent(transform, false);

                platformObject.AddComponent<SpriteRenderer>();
                platformObject.AddComponent<BoxCollider2D>();
                platformObject.AddComponent<Rigidbody2D>();
                var behaviour = platformObject.AddComponent<PlatformBehaviour>();
                behaviour.Initialize(this, i);
                behaviour.RecycleImmediately();
                allPlatforms.Add(behaviour);
            }
        }

        private void EnsurePlatformsAhead()
        {
            var cameraY = Camera.main != null ? Camera.main.transform.position.y : player.transform.position.y;
            var referenceY = Mathf.Max(player.transform.position.y, cameraY);
            while (nextSpawnY <= referenceY + spawnAheadDistance)
            {
                SpawnProceduralPlatform();
            }
        }

        private void RecyclePlatformsBelowThreshold()
        {
            if (player == null)
            {
                return;
            }

            var cameraY = Camera.main != null ? Camera.main.transform.position.y : player.transform.position.y;
            var referenceY = Mathf.Max(player.transform.position.y, cameraY);
            var recycleThreshold = referenceY - recycleBelowDistance;

            for (var i = activePlatforms.Count - 1; i >= 0; i--)
            {
                var platform = activePlatforms[i];
                if (!platform.gameObject.activeSelf || platform == player.CurrentPlatform)
                {
                    continue;
                }

                if (platform.transform.position.y < recycleThreshold)
                {
                    RecyclePlatform(platform);
                }
            }
        }

        private void SpawnProceduralPlatform()
        {
            var deltaX = Random.Range(-maxHorizontalStep, maxHorizontalStep);
            var width = Random.Range(platformWidthRange.x, platformWidthRange.y);

            lastSpawnX = Mathf.Clamp(lastSpawnX + deltaX, -horizontalBounds, horizontalBounds);
            var position = new Vector2(lastSpawnX, nextSpawnY);
            var scale = new Vector2(width, platformHeight);
            var timer = Random.Range(collapseDelayRange.x, collapseDelayRange.y);

            SpawnPlatformAt(position, scale, timer);
            nextSpawnY += Random.Range(verticalSpacingRange.x, verticalSpacingRange.y);
        }

        private void SpawnPlatformAt(Vector2 position, Vector2 scale, float timer)
        {
            var platform = GetReusablePlatform();
            platform.Activate(position, scale, timer, fallWeight);
            activePlatforms.Add(platform);
        }

        private PlatformBehaviour GetReusablePlatform()
        {
            for (var i = 0; i < allPlatforms.Count; i++)
            {
                if (!allPlatforms[i].gameObject.activeSelf)
                {
                    return allPlatforms[i];
                }
            }

            poolSize += 4;
            EnsurePool();
            return GetReusablePlatform();
        }
    }
}
