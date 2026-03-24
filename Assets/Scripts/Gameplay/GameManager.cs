using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingPlatformsSurvival
{
    public sealed class GameManager : MonoBehaviour
    {
        private PlayerController playerController;
        private PlatformManager platformManager;
        private UIManager uiManager;
        private CameraFollow2D cameraFollow;
        private DeathZoneFollower deathZoneFollower;
        private InputAction submitAction;

        private GameRunState runState = GameRunState.Waiting;
        private float elapsedSurvivalTime;

        public GameRunState RunState => runState;
        public float ElapsedSurvivalTime => elapsedSurvivalTime;

        private void Awake()
        {
            EnsureScene();
        }

        private void OnEnable()
        {
            var actionAsset = InputSystem.actions;
            if (actionAsset != null)
            {
                submitAction = actionAsset.FindAction("Submit", false);
                submitAction?.Enable();
            }
        }

        private void OnDisable()
        {
            submitAction?.Disable();
        }

        private void Start()
        {
            RestartRun();
        }

        private void Update()
        {
            if (runState == GameRunState.Playing)
            {
                elapsedSurvivalTime += Time.deltaTime;
                uiManager.UpdateTimer(elapsedSurvivalTime);
                return;
            }

            if (runState == GameRunState.GameOver && IsRestartPressedThisFrame())
            {
                RestartRun();
            }
        }

        public void RestartRun()
        {
            EnsureScene();

            runState = GameRunState.Waiting;
            elapsedSurvivalTime = 0f;

            platformManager.SetPlayer(playerController);
            platformManager.SetSimulationActive(true);

            var spawnPosition = platformManager.ResetPlatforms();
            playerController.ResetForRun(spawnPosition);

            cameraFollow.SetTarget(playerController.transform);
            cameraFollow.SnapToTarget();

            deathZoneFollower.SetGameManager(this);
            deathZoneFollower.SetFollowTarget(cameraFollow.transform);
            deathZoneFollower.SnapToTarget();

            uiManager.Bind(this, playerController);
            uiManager.ShowGameplay(elapsedSurvivalTime);

            runState = GameRunState.Playing;
        }

        public void HandlePlayerDeath()
        {
            if (runState != GameRunState.Playing)
            {
                return;
            }

            runState = GameRunState.GameOver;
            playerController.HandleDeath();
            platformManager.SetSimulationActive(false);
            uiManager.ShowGameOver(elapsedSurvivalTime);
        }

        public GameRuntimeSnapshot GetRuntimeSnapshot()
        {
            return new GameRuntimeSnapshot(
                runState,
                elapsedSurvivalTime,
                playerController.GetRuntimeState(),
                platformManager.GetActivePlatformStates(playerController.CurrentPlatform));
        }

        private void EnsureScene()
        {
            EnsureCamera();

            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                playerController = CreatePlayer();
            }

            platformManager = FindFirstObjectByType<PlatformManager>();
            if (platformManager == null)
            {
                var platformManagerObject = new GameObject("PlatformManager");
                platformManagerObject.transform.SetParent(transform, false);
                platformManager = platformManagerObject.AddComponent<PlatformManager>();
            }

            uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager == null)
            {
                var uiManagerObject = new GameObject("UIManager");
                uiManagerObject.transform.SetParent(transform, false);
                uiManager = uiManagerObject.AddComponent<UIManager>();
            }

            deathZoneFollower = FindFirstObjectByType<DeathZoneFollower>();
            if (deathZoneFollower == null)
            {
                var deathZoneObject = new GameObject("DeathZone");
                deathZoneObject.transform.SetParent(transform, false);
                deathZoneObject.AddComponent<BoxCollider2D>();
                deathZoneFollower = deathZoneObject.AddComponent<DeathZoneFollower>();
            }
        }

        private void EnsureCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 7.5f;
            mainCamera.backgroundColor = new Color(0.11f, 0.13f, 0.18f, 1f);
            cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
            if (cameraFollow == null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
            }
        }

        private PlayerController CreatePlayer()
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = new Vector3(0f, -1.5f, 0f);
            playerObject.AddComponent<SpriteRenderer>();
            playerObject.AddComponent<BoxCollider2D>();
            playerObject.AddComponent<Rigidbody2D>();
            return playerObject.AddComponent<PlayerController>();
        }

        private bool IsRestartPressedThisFrame()
        {
            return (submitAction != null && submitAction.WasPressedThisFrame())
                || Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        }
    }
}
