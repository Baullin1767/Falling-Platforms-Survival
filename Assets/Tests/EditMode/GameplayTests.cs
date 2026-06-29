using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FallingPlatformsSurvival.Tests
{
    public sealed class GameplayTests
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string PlatformPrefabPath = "Assets/Prefabs/Platform.prefab";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void PlayerControllerTouchInput_TracksDirectionsAndJumpBuffer()
        {
            var playerObject = new GameObject("Player");
            playerObject.AddComponent<SpriteRenderer>();
            playerObject.AddComponent<BoxCollider2D>();
            playerObject.AddComponent<Rigidbody2D>();
            var player = playerObject.AddComponent<PlayerController>();

            player.SetTouchMoveInput(-1f);
            Assert.That(player.HorizontalIntent, Is.EqualTo(-1f));

            player.SetTouchMoveInput(1f);
            Assert.That(player.HorizontalIntent, Is.EqualTo(0f));

            player.ClearTouchMoveInput(-1f);
            Assert.That(player.HorizontalIntent, Is.EqualTo(1f));

            player.ClearTouchMoveInput(1f);
            Assert.That(player.HorizontalIntent, Is.EqualTo(0f));

            player.QueueJumpPress();
            Assert.That(player.GetRuntimeState().HasBufferedJump, Is.True);
        }

        [Test]
        public void PlatformManagerResetPlatforms_UsesPrefabPool()
        {
            var platformManager = FindRequired<PlatformManager>();
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var player = Object.Instantiate(playerPrefab).GetComponent<PlayerController>();
            var platformManagerSerializedObject = new SerializedObject(platformManager);

            platformManager.SetPlayer(player);
            var spawn = platformManager.ResetPlatforms();
            var snapshot = platformManager.GetActivePlatformStates(null);
            var platforms = Object.FindObjectsByType<PlatformBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(platformManagerSerializedObject.FindProperty("platformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(snapshot.Count, Is.GreaterThan(0));
            Assert.That(spawn.y, Is.GreaterThan(-2f));
            Assert.That(platforms.Length, Is.GreaterThanOrEqualTo(snapshot.Count));
            Assert.That(AssetDatabase.GetAssetPath(platformManagerSerializedObject.FindProperty("platformPrefab").objectReferenceValue), Is.EqualTo(PlatformPrefabPath));
            AssertActivePlatformCollidersDoNotOverlap();
        }

        [Test]
        public void PlatformManager_HasConfiguredSpecialPlatformPrefabsAndWeights()
        {
            var platformManager = FindRequired<PlatformManager>();
            var serializedObject = new SerializedObject(platformManager);

            Assert.That(serializedObject.FindProperty("specialPlatformSpawnChance").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("specialPlatformSpawnChance").floatValue, Is.LessThanOrEqualTo(0.15f));
            Assert.That(serializedObject.FindProperty("fragilePlatformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedObject.FindProperty("springPlatformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedObject.FindProperty("movingPlatformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedObject.FindProperty("minimumDistanceBetweenPlatforms").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("fragileWeight").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("springWeight").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("movingWeight").floatValue, Is.GreaterThan(0f));
        }

        [Test]
        public void PlatformManager_UsesPrefabScaleForSpecialPlatforms()
        {
            var platformManager = FindRequired<PlatformManager>();
            var serializedObject = new SerializedObject(platformManager);
            var springPrefab = (GameObject)serializedObject.FindProperty("springPlatformPrefab").objectReferenceValue;
            var getPrefabScale = typeof(PlatformManager).GetMethod("GetPrefabScale", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(springPrefab, Is.Not.Null);
            Assert.That(getPrefabScale, Is.Not.Null);

            var scale = (Vector2)getPrefabScale.Invoke(platformManager, new object[] { springPrefab });
            Assert.That(scale.x, Is.EqualTo(springPrefab.transform.localScale.x));
            Assert.That(scale.y, Is.EqualTo(springPrefab.transform.localScale.y));
        }

        [Test]
        public void PlatformManager_BlocksSpecialPlatformsBeforeConfiguredCount()
        {
            var platformManager = FindRequired<PlatformManager>();
            var choosePlatform = typeof(PlatformManager).GetMethod("ChoosePlatformPrefabForNextSpawn", BindingFlags.Instance | BindingFlags.NonPublic);
            var serializedObject = new SerializedObject(platformManager);

            serializedObject.FindProperty("specialPlatformSpawnChance").floatValue = 1f;
            serializedObject.FindProperty("specialPlatformsStartAfterCount").intValue = 100;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(choosePlatform, Is.Not.Null);
            Assert.That(choosePlatform.Invoke(platformManager, null), Is.Null);
        }

        [Test]
        public void PlatformManager_SpecialChanceIncreasesAfterMisses()
        {
            var platformManager = FindRequired<PlatformManager>();
            var serializedObject = new SerializedObject(platformManager);
            var chanceMethod = typeof(PlatformManager).GetMethod("GetCurrentSpecialPlatformSpawnChance", BindingFlags.Instance | BindingFlags.NonPublic);
            var missesField = typeof(PlatformManager).GetField("normalPlatformsSinceLastSpecial", BindingFlags.Instance | BindingFlags.NonPublic);

            serializedObject.FindProperty("specialPlatformSpawnChance").floatValue = 0.1f;
            serializedObject.FindProperty("specialChanceIncreasePerMiss").floatValue = 0.05f;
            serializedObject.FindProperty("specialMaxSpawnChance").floatValue = 0.45f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            missesField.SetValue(platformManager, 4);

            Assert.That(chanceMethod, Is.Not.Null);
            Assert.That((float)chanceMethod.Invoke(platformManager, null), Is.EqualTo(0.3f).Within(0.001f));
        }

        [Test]
        public void FragilePlatform_TriggersShortVanishCountdownOnce()
        {
            var platformObject = CreatePlatformObject("FragilePlatform");
            var platform = platformObject.GetComponent<PlatformBehaviour>();
            var fragile = platformObject.AddComponent<FragilePlatform>();
            var spriteRenderer = platformObject.AddComponent<SpriteRenderer>();
            var idleSprite = CreateTestSprite(Color.white);
            var breakingSprite = CreateTestSprite(Color.red);
            var serializedObject = new SerializedObject(fragile);
            serializedObject.FindProperty("breakDelay").floatValue = 0.25f;
            serializedObject.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serializedObject.FindProperty("idleSprite").objectReferenceValue = idleSprite;
            var breakingSprites = serializedObject.FindProperty("breakingSprites");
            breakingSprites.arraySize = 1;
            breakingSprites.GetArrayElementAtIndex(0).objectReferenceValue = breakingSprite;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            platform.Activate(Vector2.zero, Vector2.one, 1.5f, 1f);
            spriteRenderer.sprite = idleSprite;

            Assert.That(fragile.OnPlayerLanded(platform, null), Is.True);
            Assert.That(platform.CollapseState, Is.EqualTo(PlatformCollapseState.Triggered));
            Assert.That(platform.CollapseMode, Is.EqualTo(PlatformCollapseMode.Vanish));
            Assert.That(platform.CountdownRemaining, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(spriteRenderer.sprite, Is.SameAs(breakingSprite));

            Assert.That(fragile.OnPlayerLanded(platform, null), Is.True);
            Assert.That(platform.CountdownRemaining, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void PlatformBehaviour_DefaultLandingDoesNotStartCollapse()
        {
            var platformObject = CreatePlatformObject("Platform");
            var platform = platformObject.GetComponent<PlatformBehaviour>();

            platform.Activate(Vector2.zero, Vector2.one, 1.5f, 1f);
            platform.NotifyPlayerLanded(null);

            Assert.That(platform.CollapseState, Is.EqualTo(PlatformCollapseState.Idle));
            Assert.That(platform.CountdownRemaining, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void SpringPlatform_SetsPlayerVerticalVelocity()
        {
            var playerObject = new GameObject("Player");
            playerObject.AddComponent<SpriteRenderer>();
            playerObject.AddComponent<CapsuleCollider2D>();
            playerObject.AddComponent<Rigidbody2D>();
            var player = playerObject.AddComponent<PlayerController>();

            var springObject = new GameObject("SpringPlatform");
            var spring = springObject.AddComponent<SpringPlatform>();
            var serializedObject = new SerializedObject(spring);
            serializedObject.FindProperty("springVelocity").floatValue = 33f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(spring.OnPlayerLanded(null, player), Is.False);
            Assert.That(player.GetRuntimeState().Velocity.y, Is.EqualTo(33f).Within(0.001f));
        }

        [Test]
        public void MovingPlatform_MovesHorizontallyAndReportsDelta()
        {
            var platformObject = CreatePlatformObject("MovingPlatform");
            var platform = platformObject.GetComponent<PlatformBehaviour>();
            var moving = platformObject.AddComponent<MovingPlatform>();
            var serializedObject = new SerializedObject(moving);
            serializedObject.FindProperty("movementDistance").floatValue = 2f;
            serializedObject.FindProperty("movementSpeed").floatValue = 2f;
            serializedObject.FindProperty("startMovingRight").boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            platform.Activate(Vector2.zero, Vector2.one, 1.5f, 1f);
            platformObject.SendMessage("FixedUpdate");

            Assert.That(platformObject.transform.position.x, Is.GreaterThan(0f));
            Assert.That(platform.MovementDelta.x, Is.GreaterThan(0f));
        }

        [Test]
        public void MovingPlatform_UsesRailWayLengthAsTravelBounds()
        {
            var platformObject = CreatePlatformObject("MovingPlatform", false);
            var platform = platformObject.GetComponent<PlatformBehaviour>();
            var moving = platformObject.AddComponent<MovingPlatform>();

            var railObject = new GameObject("railWay");
            railObject.transform.SetParent(platformObject.transform, false);
            var railRenderer = railObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(40, 4);
            railRenderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, 40f, 4f), new Vector2(0.5f, 0.5f), 10f);

            var serializedObject = new SerializedObject(moving);
            serializedObject.FindProperty("movementSpeed").floatValue = 100f;
            serializedObject.FindProperty("startMovingRight").boolValue = true;
            serializedObject.FindProperty("railWay").objectReferenceValue = railObject.transform;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            platform.Activate(Vector2.zero, Vector2.one, 1.5f, 1f);
            platformObject.SendMessage("FixedUpdate");

            Assert.That(platformObject.transform.position.x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(railObject.transform.position.x, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GameManagerRestartRun_UsesConfiguredSceneSystemsWithoutDuplicates()
        {
            var gameManager = FindRequired<GameManager>();

            gameManager.RestartRun();
            gameManager.RestartRun();

            var snapshot = gameManager.GetRuntimeSnapshot();
            var uiManager = FindRequired<UIManager>();
            var player = FindRequired<PlayerController>();

            Assert.That(snapshot.State, Is.EqualTo(GameRunState.Playing));
            Assert.That(snapshot.ActivePlatforms.Count, Is.GreaterThan(0));
            Assert.That(uiManager.AreTouchControlsVisible, Is.True);
            Assert.That(uiManager.IsGameOverVisible, Is.False);
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DeathZoneFollower>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PlatformManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            var gameManagerSerializedObject = new SerializedObject(gameManager);
            Assert.That(AssetDatabase.GetAssetPath(gameManagerSerializedObject.FindProperty("playerPrefab").objectReferenceValue), Is.EqualTo(PlayerPrefabPath));
        }

        [Test]
        public void RestartRun_InstantiatesPlayerPrefabOnceAndReusesIt()
        {
            var gameManager = FindRequired<GameManager>();

            Assert.That(Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length, Is.EqualTo(0));

            gameManager.RestartRun();
            var firstPlayer = FindRequired<PlayerController>();

            gameManager.RestartRun();
            var secondPlayer = FindRequired<PlayerController>();

            Assert.That(Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(secondPlayer, Is.SameAs(firstPlayer));
        }

        [Test]
        public void HandlePlayerDeath_ShowsOverlayAndRestartButtonResetsRun()
        {
            var gameManager = FindRequired<GameManager>();

            gameManager.RestartRun();

            var uiManager = FindRequired<UIManager>();
            gameManager.HandlePlayerDeath();

            Assert.That(gameManager.RunState, Is.EqualTo(GameRunState.GameOver));
            Assert.That(uiManager.IsGameOverVisible, Is.True);
            Assert.That(uiManager.AreTouchControlsVisible, Is.False);

            var restartButton = GameObject.Find("RestartButton").GetComponent<Button>();
            restartButton.onClick.Invoke();

            Assert.That(gameManager.RunState, Is.EqualTo(GameRunState.Playing));
            Assert.That(gameManager.ElapsedSurvivalTime, Is.EqualTo(0f));
            Assert.That(uiManager.IsGameOverVisible, Is.False);
            Assert.That(uiManager.AreTouchControlsVisible, Is.True);
            Assert.That(gameManager.GetRuntimeSnapshot().Player.IsAlive, Is.True);
        }

        private static T FindRequired<T>() where T : Object
        {
            var instance = Object.FindFirstObjectByType<T>();
            Assert.That(instance, Is.Not.Null);
            return instance;
        }

        private static GameObject CreatePlatformObject(string objectName, bool active = true)
        {
            var platformObject = new GameObject(objectName);
            platformObject.SetActive(active);
            platformObject.AddComponent<BoxCollider2D>();
            platformObject.AddComponent<Rigidbody2D>();
            platformObject.AddComponent<PlatformEffector2D>();
            platformObject.AddComponent<PlatformBehaviour>();
            return platformObject;
        }

        private static void AssertActivePlatformCollidersDoNotOverlap()
        {
            var platforms = Object.FindObjectsByType<PlatformBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < platforms.Length; i++)
            {
                var firstCollider = platforms[i].GetComponent<Collider2D>();
                if (firstCollider == null)
                {
                    continue;
                }

                for (var j = i + 1; j < platforms.Length; j++)
                {
                    var secondCollider = platforms[j].GetComponent<Collider2D>();
                    if (secondCollider == null)
                    {
                        continue;
                    }

                    Assert.That(
                        firstCollider.bounds.Intersects(secondCollider.bounds),
                        Is.False,
                        $"{platforms[i].name} overlaps {platforms[j].name}");
                }
            }
        }

        private static Sprite CreateTestSprite(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
