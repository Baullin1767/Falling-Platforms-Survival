using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FallingPlatformsSurvival
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GameObject gameplayHudRoot;
        [SerializeField] private GameObject touchControlsRoot;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Text controlsText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Text finalTimeText;
        [SerializeField] private Text restartHintText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button restartButtonPause;
        [SerializeField] private Button mainMenuButtonPause;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private PointerHoldButton moveLeftButton;
        [SerializeField] private PointerHoldButton moveRightButton;
        [SerializeField] private PointerHoldButton jumpButton;

        private GameManager gameManager;
        private PlayerController playerController;
        private bool isConfigured;

        public bool IsGameOverVisible => gameOverPanel != null && gameOverPanel.activeSelf;
        public bool AreTouchControlsVisible => touchControlsRoot != null && touchControlsRoot.activeSelf;

        private void Awake()
        {
            EnsureConfigured();
        }

        public void Bind(GameManager owner, PlayerController player)
        {
            EnsureConfigured();

            gameManager = owner;
            playerController = player;

            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(gameManager.RestartRun);
            restartButtonPause.onClick.RemoveAllListeners();
            restartButtonPause.onClick.AddListener(gameManager.RestartRun);
            
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(gameManager.Pause);
            
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(gameManager.Resume);
            
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(gameManager.GoMainMenu);
            mainMenuButtonPause.onClick.RemoveAllListeners();
            mainMenuButtonPause.onClick.AddListener(gameManager.GoMainMenu);

            moveLeftButton.Bind(
                () => playerController?.SetTouchMoveInput(-1f),
                () => playerController?.ClearTouchMoveInput(-1f));
            moveRightButton.Bind(
                () => playerController?.SetTouchMoveInput(1f),
                () => playerController?.ClearTouchMoveInput(1f));
            jumpButton.Bind(() => playerController?.QueueJumpPress());
        }

        public void ShowGameplay(float elapsedTime)
        {
            EnsureConfigured();

            UpdateTimer(elapsedTime);
            controlsText.text = "Touch: Left / Right / Jump    Keyboard/Gamepad: A/D or arrows, Space / South Button";
            gameOverPanel.SetActive(false);
            SetTouchControlsActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void UpdateTimer(float elapsedTime)
        {
            EnsureConfigured();
            timerText.text = $"Survival Time  {elapsedTime:0.0}s";
        }

        public void ShowGameOver(float elapsedTime)
        {
            EnsureConfigured();

            UpdateTimer(elapsedTime);
            finalTimeText.text = $"Final Time  {elapsedTime:0.0}s";
            // restartHintText.text = "Tap Restart to drop back in.";
            SetTouchControlsActive(false);
            gameOverPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private void EnsureConfigured()
        {
            if (isConfigured)
            {
                return;
            }

            if (rootCanvas == null
                || gameplayHudRoot == null
                || touchControlsRoot == null
                || timerText == null
                || controlsText == null
                || gameOverPanel == null
                || finalTimeText == null
                || restartHintText == null
                || restartButton == null
                || mainMenuButton == null
                || moveLeftButton == null
                || moveRightButton == null
                || jumpButton == null)
            {
                throw new MissingReferenceException("UIManager requires all authored UI references to be assigned in the scene.");
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                throw new MissingReferenceException("UIManager requires an EventSystem in the scene.");
            }

            isConfigured = true;
        }

        private void SetTouchControlsActive(bool active)
        {
            if (!active)
            {
                playerController?.ClearTouchMoveInput();
            }

            touchControlsRoot.SetActive(active);
        }

        public void ShowPause()
        {
            SetTouchControlsActive(false);
            pausePanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButtonPause.gameObject);
            }
        }

        public void HidePause()
        {
            SetTouchControlsActive(true);
            pausePanel.SetActive(false);
        }
    }
}
