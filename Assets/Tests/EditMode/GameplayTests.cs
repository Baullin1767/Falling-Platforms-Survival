using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FallingPlatformsSurvival.Tests
{
    public sealed class GameplayTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var root in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (root == null)
                {
                    continue;
                }

                var rootTransform = root.transform;
                if (rootTransform != null && rootTransform.parent == null)
                {
                    Object.DestroyImmediate(root);
                }
            }
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
        public void PlatformManagerResetPlatforms_CreatesActivePlatformSnapshot()
        {
            var managerObject = new GameObject("PlatformManager");
            var manager = managerObject.AddComponent<PlatformManager>();

            var playerObject = new GameObject("Player");
            playerObject.AddComponent<SpriteRenderer>();
            playerObject.AddComponent<BoxCollider2D>();
            playerObject.AddComponent<Rigidbody2D>();
            var player = playerObject.AddComponent<PlayerController>();

            manager.SetPlayer(player);
            var spawn = manager.ResetPlatforms();
            var snapshot = manager.GetActivePlatformStates(null);

            Assert.That(snapshot.Count, Is.GreaterThan(0));
            Assert.That(spawn.y, Is.GreaterThan(-2f));
        }

        [Test]
        public void GameManagerRestartRun_CreatesSingleRuntimeHudAndSystems()
        {
            var bootstrap = new GameObject("GameManager");
            var gameManager = bootstrap.AddComponent<GameManager>();

            gameManager.RestartRun();
            gameManager.RestartRun();

            var snapshot = gameManager.GetRuntimeSnapshot();
            var uiManager = Object.FindFirstObjectByType<UIManager>();

            Assert.That(snapshot.State, Is.EqualTo(GameRunState.Playing));
            Assert.That(snapshot.ActivePlatforms.Count, Is.GreaterThan(0));
            Assert.That(uiManager, Is.Not.Null);
            Assert.That(uiManager.AreTouchControlsVisible, Is.True);
            Assert.That(uiManager.IsGameOverVisible, Is.False);
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [Test]
        public void HandlePlayerDeath_ShowsOverlayAndRestartButtonResetsRun()
        {
            var bootstrap = new GameObject("GameManager");
            var gameManager = bootstrap.AddComponent<GameManager>();

            gameManager.RestartRun();

            var uiManager = Object.FindFirstObjectByType<UIManager>();
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
    }
}
