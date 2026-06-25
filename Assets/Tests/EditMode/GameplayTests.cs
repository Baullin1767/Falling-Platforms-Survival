using NUnit.Framework;
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
        }

        [Test]
        public void PlatformManager_HasConfiguredSpecialPlatformPrefabsAndWeights()
        {
            var platformManager = FindRequired<PlatformManager>();
            var serializedObject = new SerializedObject(platformManager);

            Assert.That(serializedObject.FindProperty("specialPlatformSpawnChance").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("fragilePlatformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedObject.FindProperty("springPlatformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedObject.FindProperty("movingPlatformPrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedObject.FindProperty("minimumDistanceBetweenPlatforms").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("fragileWeight").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("springWeight").floatValue, Is.GreaterThan(0f));
            Assert.That(serializedObject.FindProperty("movingWeight").floatValue, Is.GreaterThan(0f));
        }

        [Test]
        public void FragilePlatform_TriggersShortVanishCountdownOnce()
        {
            var platformObject = CreatePlatformObject("FragilePlatform");
            var platform = platformObject.GetComponent<PlatformBehaviour>();
            var fragile = platformObject.AddComponent<FragilePlatform>();
            var serializedObject = new SerializedObject(fragile);
            serializedObject.FindProperty("breakDelay").floatValue = 0.25f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            platform.Activate(Vector2.zero, Vector2.one, 1.5f, 1f);
            Assert.That(fragile.OnPlayerLanded(platform, null), Is.True);
            Assert.That(platform.CollapseState, Is.EqualTo(PlatformCollapseState.Triggered));
            Assert.That(platform.CollapseMode, Is.EqualTo(PlatformCollapseMode.Vanish));
            Assert.That(platform.CountdownRemaining, Is.EqualTo(0.25f).Within(0.001f));

            Assert.That(fragile.OnPlayerLanded(platform, null), Is.True);
            Assert.That(platform.CountdownRemaining, Is.EqualTo(0.25f).Within(0.001f));
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

        private static GameObject CreatePlatformObject(string objectName)
        {
            var platformObject = new GameObject(objectName);
            platformObject.AddComponent<BoxCollider2D>();
            platformObject.AddComponent<Rigidbody2D>();
            platformObject.AddComponent<PlatformEffector2D>();
            platformObject.AddComponent<PlatformBehaviour>();
            return platformObject;
        }
    }
}
