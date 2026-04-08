using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FallingPlatformsSurvival
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlatformManager platformManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private DeathZoneFollower deathZoneFollower;
        [SerializeField] private InfiniteVerticalBackground _infiniteVerticalBackground;

        [Header("Runtime Prefabs")]
        [SerializeField] private GameObject playerPrefab;

        private PlayerController playerController;
        private InputAction submitAction;

        private GameRunState runState = GameRunState.Waiting;
        private float elapsedSurvivalTime;

        public GameRunState RunState => runState;
        public float ElapsedSurvivalTime => elapsedSurvivalTime;

        private void Awake()
        {
            Time.timeScale = 1f;
            ValidateConfiguration();
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
            ValidateConfiguration();
            EnsurePlayerInstance();

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
            _infiniteVerticalBackground.ResetBG();

            if (Time.timeScale <= 0)
            {
                Resume();
            }
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

        private void EnsurePlayerInstance()
        {
            if (playerController != null)
            {
                return;
            }

            var spawnedPlayer = Instantiate(playerPrefab);
            spawnedPlayer.name = playerPrefab.name;
            playerController = spawnedPlayer.GetComponent<PlayerController>();
            if (playerController == null)
            {
                throw new MissingComponentException("Player prefab must include a PlayerController component.");
            }
        }

        private void ValidateConfiguration()
        {
            if (platformManager == null)
            {
                throw new MissingReferenceException("GameManager requires a PlatformManager scene reference.");
            }

            if (uiManager == null)
            {
                throw new MissingReferenceException("GameManager requires a UIManager scene reference.");
            }

            if (cameraFollow == null)
            {
                throw new MissingReferenceException("GameManager requires a CameraFollow2D scene reference.");
            }

            if (deathZoneFollower == null)
            {
                throw new MissingReferenceException("GameManager requires a DeathZoneFollower scene reference.");
            }

            if (playerPrefab == null)
            {
                throw new MissingReferenceException("GameManager requires a Player prefab reference.");
            }

            if (playerPrefab.GetComponent<PlayerController>() == null)
            {
                throw new MissingComponentException("GameManager player prefab reference must point to a prefab with PlayerController.");
            }
        }

        private bool IsRestartPressedThisFrame()
        {
            return (submitAction != null && submitAction.WasPressedThisFrame())
                || Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        }

        public void GoMainMenu()
        {
            SceneManager.LoadScene("MainMenuScene");
        }

        public void Pause()
        {
            Time.timeScale = 0f;
            uiManager.ShowPause();
        }

        public void Resume()
        {
            Time.timeScale = 1f;
            uiManager.HidePause();
        }
    }
}
