using System.Collections.Generic;
using UnityEngine;

namespace FallingPlatformsSurvival
{
    public sealed class PlatformManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private Collider2D startGround;

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

        [Header("Special Platforms")]
        [SerializeField, Range(0f, 1f)] private float specialPlatformSpawnChance = 0.12f;
        [SerializeField] private int specialPlatformsStartAfterCount = 100;
        [SerializeField, Range(0f, 1f)] private float specialChanceIncreasePerMiss = 0.01f;
        [SerializeField, Range(0f, 1f)] private float specialMaxSpawnChance = 0.45f;
        [SerializeField] private GameObject fragilePlatformPrefab;
        [SerializeField] private GameObject springPlatformPrefab;
        [SerializeField] private GameObject movingPlatformPrefab;
        [SerializeField] private float minimumDistanceBetweenPlatforms = 0.4f;
        [SerializeField] private float fragileWeight = 0.4f;
        [SerializeField] private float springWeight = 0.3f;
        [SerializeField] private float movingWeight = 0.3f;

        private const int MaxNormalSpawnAttempts = 12;

        private readonly List<PlatformBehaviour> allPlatforms = new();
        private readonly List<PlatformBehaviour> normalPlatformPool = new();
        private readonly List<PlatformBehaviour> fragilePlatformPool = new();
        private readonly List<PlatformBehaviour> springPlatformPool = new();
        private readonly List<PlatformBehaviour> movingPlatformPool = new();
        private readonly List<PlatformBehaviour> activePlatforms = new();

        private PlayerController player;
        private float nextSpawnY;
        private float lastSpawnX;
        private int spawnedPlatformCount;
        private int normalPlatformsSinceLastSpecial;

        public bool SimulationActive { get; private set; } = true;
        public Collider2D StartGroundCollider => ResolveStartGroundCollider();

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
            var groundCollider = ResolveStartGroundCollider();

            for (var i = 0; i < allPlatforms.Count; i++)
            {
                allPlatforms[i].RecycleImmediately();
            }

            activePlatforms.Clear();
            var groundBounds = groundCollider.bounds;
            lastSpawnX = groundBounds.center.x;
            spawnedPlatformCount = 0;
            normalPlatformsSinceLastSpecial = 0;

            nextSpawnY = groundBounds.max.y + Random.Range(verticalSpacingRange.x, verticalSpacingRange.y);
            for (var i = 0; i < startingPlatformCount; i++)
            {
                SpawnProceduralPlatform();
            }

            return new Vector2(groundBounds.center.x, groundBounds.max.y + 1.45f);
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
            if (normalPlatformPool.Count >= poolSize)
            {
                return;
            }

            if (platformPrefab == null)
            {
                throw new MissingReferenceException("PlatformManager requires a Platform prefab reference.");
            }

            if (platformPrefab.GetComponent<PlatformBehaviour>() == null)
            {
                throw new MissingComponentException("PlatformManager platform prefab reference must point to a prefab with PlatformBehaviour.");
            }

            for (var i = normalPlatformPool.Count; i < poolSize; i++)
            {
                var platformObject = Instantiate(platformPrefab, transform);
                var behaviour = platformObject.GetComponent<PlatformBehaviour>();
                if (behaviour == null)
                {
                    throw new MissingComponentException("Instantiated platform prefab does not contain PlatformBehaviour.");
                }

                behaviour.gameObject.name = $"Platform_{i:D2}";
                behaviour.Initialize(this, allPlatforms.Count);
                behaviour.RecycleImmediately();
                normalPlatformPool.Add(behaviour);
                allPlatforms.Add(behaviour);
            }
        }

        private Collider2D ResolveStartGroundCollider()
        {
            if (startGround != null)
            {
                return startGround;
            }

            var groundObject = GameObject.Find("Ground");
            startGround = groundObject != null ? groundObject.GetComponent<Collider2D>() : null;
            if (startGround == null)
            {
                throw new MissingReferenceException("PlatformManager requires a Ground scene object with a Collider2D.");
            }

            return startGround;
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
            var specialPrefab = ChoosePlatformPrefabForNextSpawn();
            var scale = specialPrefab != null ? GetPrefabScale(specialPrefab) : GetRandomNormalPlatformScale();
            var timer = Random.Range(collapseDelayRange.x, collapseDelayRange.y);
            var position = ResolveNormalPlatformPosition(scale);

            SpawnPlatformAt(position, scale, timer, specialPrefab);
            RecordPlatformSpawned(specialPrefab != null);
            nextSpawnY += Random.Range(verticalSpacingRange.x, verticalSpacingRange.y);
        }

        private Vector2 GetRandomNormalPlatformScale()
        {
            var width = Random.Range(platformWidthRange.x, platformWidthRange.y);
            return new Vector2(width, platformHeight);
        }

        private Vector2 ResolveNormalPlatformPosition(Vector2 scale)
        {
            var spawnY = nextSpawnY;
            var candidateX = lastSpawnX;

            for (var i = 0; i < MaxNormalSpawnAttempts; i++)
            {
                var deltaX = Random.Range(-maxHorizontalStep, maxHorizontalStep);
                candidateX = ClampPlatformCenterX(lastSpawnX + deltaX, scale);
                var candidatePosition = new Vector2(candidateX, spawnY);

                if (IsPlatformPositionValid(candidatePosition, scale))
                {
                    lastSpawnX = candidateX;
                    nextSpawnY = spawnY;
                    return candidatePosition;
                }
            }

            spawnY += platformHeight + minimumDistanceBetweenPlatforms;
            candidateX = ClampPlatformCenterX(candidateX, scale);
            var fallbackPosition = new Vector2(candidateX, spawnY);
            while (!IsPlatformPositionValid(fallbackPosition, scale))
            {
                spawnY += platformHeight + minimumDistanceBetweenPlatforms;
                fallbackPosition.y = spawnY;
            }

            lastSpawnX = candidateX;
            nextSpawnY = spawnY;
            return fallbackPosition;
        }

        private PlatformBehaviour SpawnPlatformAt(Vector2 position, Vector2 scale, float timer, GameObject prefabOverride = null)
        {
            var platform = prefabOverride != null ? GetReusableSpecialPlatform(prefabOverride) : GetReusablePlatform();
            if (platform == null)
            {
                platform = GetReusablePlatform();
                scale = GetRandomNormalPlatformScale();
            }

            platform.Activate(position, scale, timer, fallWeight);
            activePlatforms.Add(platform);
            return platform;
        }

        private GameObject ChoosePlatformPrefabForNextSpawn()
        {
            if (spawnedPlatformCount < specialPlatformsStartAfterCount)
            {
                return null;
            }

            var chance = GetCurrentSpecialPlatformSpawnChance();
            return Random.value <= chance ? ChooseSpecialPlatformPrefab() : null;
        }

        private float GetCurrentSpecialPlatformSpawnChance()
        {
            var baseChance = Mathf.Clamp01(specialPlatformSpawnChance);
            var maxChance = Mathf.Max(baseChance, specialMaxSpawnChance);
            var increasedChance = baseChance + normalPlatformsSinceLastSpecial * Mathf.Max(0f, specialChanceIncreasePerMiss);
            return Mathf.Clamp(increasedChance, 0f, maxChance);
        }

        private void RecordPlatformSpawned(bool spawnedSpecialPlatform)
        {
            spawnedPlatformCount++;
            if (spawnedSpecialPlatform)
            {
                normalPlatformsSinceLastSpecial = 0;
                return;
            }

            normalPlatformsSinceLastSpecial++;
        }

        private Vector2 GetPrefabScale(GameObject prefab)
        {
            var prefabScale = prefab.transform.localScale;
            return new Vector2(prefabScale.x, prefabScale.y);
        }

        private bool IsPlatformPositionValid(Vector2 candidatePosition, Vector2 candidateScale)
        {
            var halfWidth = candidateScale.x * 0.5f;
            if (candidatePosition.x - halfWidth < -horizontalBounds || candidatePosition.x + halfWidth > horizontalBounds)
            {
                return false;
            }

            for (var i = 0; i < activePlatforms.Count; i++)
            {
                var platform = activePlatforms[i];
                if (platform == null || !platform.gameObject.activeSelf)
                {
                    continue;
                }

                var activeScale = new Vector2(platform.transform.localScale.x, platform.transform.localScale.y);
                if (PlatformsOverlap(candidatePosition, candidateScale, platform.transform.position, activeScale))
                {
                    return false;
                }
            }

            return true;
        }

        private float ClampPlatformCenterX(float centerX, Vector2 scale)
        {
            var halfWidth = scale.x * 0.5f;
            var minX = -horizontalBounds + halfWidth;
            var maxX = horizontalBounds - halfWidth;
            return minX <= maxX ? Mathf.Clamp(centerX, minX, maxX) : 0f;
        }

        private bool PlatformsOverlap(Vector2 firstPosition, Vector2 firstScale, Vector2 secondPosition, Vector2 secondScale)
        {
            var horizontalDistance = Mathf.Abs(firstPosition.x - secondPosition.x);
            var verticalDistance = Mathf.Abs(firstPosition.y - secondPosition.y);
            var minimumHorizontalDistance = (firstScale.x + secondScale.x) * 0.5f + minimumDistanceBetweenPlatforms;
            var minimumVerticalDistance = (firstScale.y + secondScale.y) * 0.5f + minimumDistanceBetweenPlatforms * 0.25f;

            return horizontalDistance < minimumHorizontalDistance && verticalDistance < minimumVerticalDistance;
        }

        private GameObject ChooseSpecialPlatformPrefab()
        {
            var safeFragileWeight = fragilePlatformPrefab != null ? Mathf.Max(0f, fragileWeight) : 0f;
            var safeSpringWeight = springPlatformPrefab != null ? Mathf.Max(0f, springWeight) : 0f;
            var safeMovingWeight = movingPlatformPrefab != null ? Mathf.Max(0f, movingWeight) : 0f;
            var totalWeight = safeFragileWeight + safeSpringWeight + safeMovingWeight;
            if (totalWeight <= 0f)
            {
                return null;
            }

            var roll = Random.value * totalWeight;
            var fragileThreshold = safeFragileWeight;
            if (roll < fragileThreshold)
            {
                return fragilePlatformPrefab;
            }

            var springThreshold = fragileThreshold + safeSpringWeight;
            return roll < springThreshold ? springPlatformPrefab : movingPlatformPrefab;
        }

        private PlatformBehaviour GetReusablePlatform()
        {
            for (var i = 0; i < normalPlatformPool.Count; i++)
            {
                if (!normalPlatformPool[i].gameObject.activeSelf)
                {
                    return normalPlatformPool[i];
                }
            }

            poolSize += 4;
            EnsurePool();
            return GetReusablePlatform();
        }

        private PlatformBehaviour GetReusableSpecialPlatform(GameObject prefab)
        {
            if (prefab == fragilePlatformPrefab)
            {
                return GetReusablePlatformFromPool(prefab, fragilePlatformPool, "FragilePlatform");
            }

            if (prefab == springPlatformPrefab)
            {
                return GetReusablePlatformFromPool(prefab, springPlatformPool, "SpringPlatform");
            }

            if (prefab == movingPlatformPrefab)
            {
                return GetReusablePlatformFromPool(prefab, movingPlatformPool, "MovingPlatform");
            }

            return null;
        }

        private PlatformBehaviour GetReusablePlatformFromPool(GameObject prefab, List<PlatformBehaviour> pool, string objectNamePrefix)
        {
            if (prefab == null)
            {
                return null;
            }

            if (prefab.GetComponent<PlatformBehaviour>() == null)
            {
                throw new MissingComponentException($"{objectNamePrefix} prefab reference must point to a prefab with PlatformBehaviour.");
            }

            for (var i = 0; i < pool.Count; i++)
            {
                if (!pool[i].gameObject.activeSelf)
                {
                    return pool[i];
                }
            }

            var platformObject = Instantiate(prefab, transform);
            var behaviour = platformObject.GetComponent<PlatformBehaviour>();
            behaviour.gameObject.name = $"{objectNamePrefix}_{pool.Count:D2}";
            behaviour.Initialize(this, allPlatforms.Count);
            behaviour.RecycleImmediately();

            pool.Add(behaviour);
            allPlatforms.Add(behaviour);
            return behaviour;
        }
    }
}
