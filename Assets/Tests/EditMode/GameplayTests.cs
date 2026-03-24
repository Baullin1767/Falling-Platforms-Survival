using NUnit.Framework;
using UnityEngine;

namespace FallingPlatformsSurvival.Tests
{
    public sealed class GameplayTests
    {
        [Test]
        public void PlatformRecycleImmediately_DisablesPlatformAndResetsState()
        {
            var platformObject = new GameObject("Platform");
            platformObject.AddComponent<SpriteRenderer>();
            platformObject.AddComponent<BoxCollider2D>();
            platformObject.AddComponent<Rigidbody2D>();
            var platform = platformObject.AddComponent<PlatformBehaviour>();

            platform.Initialize(null, 42);
            platform.Activate(Vector2.zero, new Vector2(3f, 0.75f), 1.2f, 1f);

            platform.RecycleImmediately();

            Assert.That(platform.CollapseState, Is.EqualTo(PlatformCollapseState.Recycling));
            Assert.That(platform.gameObject.activeSelf, Is.False);

            Object.DestroyImmediate(platformObject);
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

            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void GameManagerRuntimeSnapshot_ReturnsPlayingStateAndActivePlatforms()
        {
            var bootstrap = new GameObject("GameManager");
            var gameManager = bootstrap.AddComponent<GameManager>();

            gameManager.RestartRun();
            var snapshot = gameManager.GetRuntimeSnapshot();

            Assert.That(snapshot.State, Is.EqualTo(GameRunState.Playing));
            Assert.That(snapshot.Player.IsAlive, Is.True);
            Assert.That(snapshot.ActivePlatforms.Count, Is.GreaterThan(0));

            Object.DestroyImmediate(bootstrap);
            foreach (var root in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (root.name == "Main Camera" || root.name == "Player" || root.name == "PlatformManager" || root.name == "UIManager" || root.name == "DeathZone" || root.name == "EventSystem" || root.name == "Canvas")
                {
                    Object.DestroyImmediate(root);
                }
            }
        }
    }
}
