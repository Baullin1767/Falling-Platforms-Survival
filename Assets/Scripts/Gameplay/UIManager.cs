using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FallingPlatformsSurvival
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);

        private Canvas rootCanvas;
        private GameObject gameplayHudRoot;
        private GameObject touchControlsRoot;
        private Text timerText;
        private Text controlsText;
        private GameObject gameOverPanel;
        private Text finalTimeText;
        private Text restartHintText;
        private Button restartButton;
        private PointerHoldButton moveLeftButton;
        private PointerHoldButton moveRightButton;
        private PointerHoldButton jumpButton;

        private GameManager gameManager;
        private PlayerController playerController;
        private bool uiBuilt;

        public bool IsGameOverVisible => gameOverPanel != null && gameOverPanel.activeSelf;
        public bool AreTouchControlsVisible => touchControlsRoot != null && touchControlsRoot.activeSelf;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(GameManager owner, PlayerController player)
        {
            EnsureUi();

            gameManager = owner;
            playerController = player;

            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(gameManager.RestartRun);

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
            EnsureUi();

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
            EnsureUi();
            timerText.text = $"Survival Time  {elapsedTime:0.0}s";
        }

        public void ShowGameOver(float elapsedTime)
        {
            EnsureUi();

            UpdateTimer(elapsedTime);
            finalTimeText.text = $"Final Time  {elapsedTime:0.0}s";
            restartHintText.text = "Tap Restart to drop back in.";
            SetTouchControlsActive(false);
            gameOverPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private void EnsureUi()
        {
            if (uiBuilt)
            {
                return;
            }

            EnsureEventSystem();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(transform, false);

            rootCanvas = canvasObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            gameplayHudRoot = CreateRectObject(rootCanvas.transform, "GameplayHud");
            Stretch((RectTransform)gameplayHudRoot.transform, Vector2.zero, Vector2.zero);

            var topBar = CreatePanel(
                gameplayHudRoot.transform,
                "TopBar",
                new Color(0.05f, 0.08f, 0.12f, 0.78f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 148f));

            timerText = CreateText(topBar.transform, "TimerText", font, 38, TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(timerText.rectTransform, new Vector2(38f, 26f), new Vector2(-860f, -26f));

            controlsText = CreateText(topBar.transform, "ControlsText", font, 22, TextAnchor.MiddleRight, FontStyle.Normal);
            Stretch(controlsText.rectTransform, new Vector2(720f, 26f), new Vector2(-38f, -26f));

            touchControlsRoot = CreateRectObject(rootCanvas.transform, "TouchControls");
            Stretch((RectTransform)touchControlsRoot.transform, new Vector2(0f, 0f), new Vector2(0f, 0f));

            var movementPanel = CreatePanel(
                touchControlsRoot.transform,
                "MovementPanel",
                new Color(0.04f, 0.07f, 0.1f, 0.58f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(40f, 40f),
                new Vector2(420f, 240f));

            moveLeftButton = CreateTouchButton(
                movementPanel.transform,
                "MoveLeftButton",
                font,
                "LEFT",
                new Vector2(24f, 24f),
                new Vector2(174f, 174f),
                new Color(0.18f, 0.44f, 0.86f, 0.92f));

            moveRightButton = CreateTouchButton(
                movementPanel.transform,
                "MoveRightButton",
                font,
                "RIGHT",
                new Vector2(222f, 24f),
                new Vector2(174f, 174f),
                new Color(0.2f, 0.55f, 0.9f, 0.92f));

            var jumpPanel = CreatePanel(
                touchControlsRoot.transform,
                "JumpPanel",
                new Color(0.04f, 0.07f, 0.1f, 0.58f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-300f, 40f),
                new Vector2(260f, 260f));

            jumpButton = CreateTouchButton(
                jumpPanel.transform,
                "JumpButton",
                font,
                "JUMP",
                new Vector2(26f, 26f),
                new Vector2(208f, 208f),
                new Color(0.98f, 0.62f, 0.2f, 0.95f));

            gameOverPanel = CreateRectObject(rootCanvas.transform, "GameOverPanel");
            Stretch((RectTransform)gameOverPanel.transform, Vector2.zero, Vector2.zero);

            var overlay = gameOverPanel.AddComponent<Image>();
            overlay.color = new Color(0.03f, 0.04f, 0.07f, 0.86f);

            var card = CreatePanel(
                gameOverPanel.transform,
                "GameOverCard",
                new Color(0.08f, 0.11f, 0.16f, 0.98f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(660f, 420f));

            var titleText = CreateText(card.transform, "TitleText", font, 62, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleText.text = "Run Over";
            Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(500f, 80f));

            finalTimeText = CreateText(card.transform, "FinalTimeText", font, 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(finalTimeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(480f, 50f));

            restartHintText = CreateText(card.transform, "RestartHintText", font, 24, TextAnchor.MiddleCenter, FontStyle.Normal);
            restartHintText.text = "Tap Restart to drop back in.";
            Place(restartHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(520f, 42f));

            var restartButtonObject = CreateButton(
                card.transform,
                "RestartButton",
                font,
                "Restart",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 72f),
                new Vector2(320f, 82f),
                new Color(0.2f, 0.72f, 0.52f, 1f));

            restartButton = restartButtonObject.GetComponent<Button>();

            var promptText = CreateText(card.transform, "PromptText", font, 22, TextAnchor.MiddleCenter, FontStyle.Normal);
            promptText.text = "Keyboard: R    Gamepad: Submit";
            Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(420f, 40f));

            gameOverPanel.SetActive(false);
            uiBuilt = true;
        }

        private void SetTouchControlsActive(bool active)
        {
            if (!active)
            {
                playerController?.ClearTouchMoveInput();
            }

            touchControlsRoot.SetActive(active);
        }

        private static GameObject CreatePanel(
            Transform parent,
            string objectName,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var panelObject = CreateRectObject(parent, objectName);
            var rect = (RectTransform)panelObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = panelObject.AddComponent<Image>();
            image.color = color;
            return panelObject;
        }

        private static PointerHoldButton CreateTouchButton(
            Transform parent,
            string objectName,
            Font font,
            string label,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            var buttonObject = CreateRectObject(parent, objectName);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = buttonObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var labelText = CreateText(buttonObject.transform, "Label", font, 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            labelText.text = label;
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.zero);

            return buttonObject.AddComponent<PointerHoldButton>();
        }

        private static GameObject CreateButton(
            Transform parent,
            string objectName,
            Font font,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            var buttonObject = CreateRectObject(parent, objectName);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var buttonLabel = CreateText(buttonObject.transform, "Label", font, 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            buttonLabel.text = label;
            Stretch(buttonLabel.rectTransform, Vector2.zero, Vector2.zero);
            return buttonObject;
        }

        private static GameObject CreateRectObject(Transform parent, string objectName)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Text CreateText(Transform parent, string objectName, Font font, int size, TextAnchor anchor, FontStyle fontStyle)
        {
            var textObject = CreateRectObject(parent, objectName);
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = string.Empty;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
