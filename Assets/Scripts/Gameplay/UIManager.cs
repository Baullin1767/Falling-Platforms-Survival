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
        private Text timerText;
        private Text controlsText;
        private GameObject gameOverPanel;
        private Text finalTimeText;
        private Button restartButton;

        private GameManager gameManager;

        private void Awake()
        {
            EnsureUi();
        }

        public void Bind(GameManager owner)
        {
            gameManager = owner;
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(gameManager.RestartRun);
        }

        public void ShowGameplay(float elapsedTime)
        {
            UpdateTimer(elapsedTime);
            controlsText.text = "Move: A/D or Left Stick    Jump: Space / South Button";
            gameOverPanel.SetActive(false);
        }

        public void UpdateTimer(float elapsedTime)
        {
            timerText.text = $"Survival Time: {elapsedTime:0.0}s";
        }

        public void ShowGameOver(float elapsedTime)
        {
            finalTimeText.text = $"Final Time: {elapsedTime:0.0}s";
            gameOverPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private void EnsureUi()
        {
            EnsureEventSystem();

            if (rootCanvas == null)
            {
                var canvasObject = new GameObject("Canvas");
                canvasObject.transform.SetParent(transform, false);

                rootCanvas = canvasObject.AddComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().referenceResolution = referenceResolution;
                canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            timerText = CreateText(rootCanvas.transform, "TimerText", font, 34, TextAnchor.UpperLeft);
            timerText.rectTransform.anchorMin = new Vector2(0f, 1f);
            timerText.rectTransform.anchorMax = new Vector2(0f, 1f);
            timerText.rectTransform.pivot = new Vector2(0f, 1f);
            timerText.rectTransform.anchoredPosition = new Vector2(24f, -24f);
            timerText.rectTransform.sizeDelta = new Vector2(460f, 60f);

            controlsText = CreateText(rootCanvas.transform, "ControlsText", font, 22, TextAnchor.LowerCenter);
            controlsText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            controlsText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            controlsText.rectTransform.pivot = new Vector2(0.5f, 0f);
            controlsText.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            controlsText.rectTransform.sizeDelta = new Vector2(900f, 50f);

            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(rootCanvas.transform, false);

            var panelRect = gameOverPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.82f);

            var titleText = CreateText(gameOverPanel.transform, "TitleText", font, 54, TextAnchor.MiddleCenter);
            titleText.text = "Game Over";
            titleText.rectTransform.anchorMin = new Vector2(0.5f, 0.65f);
            titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.65f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleText.rectTransform.sizeDelta = new Vector2(500f, 80f);

            finalTimeText = CreateText(gameOverPanel.transform, "FinalTimeText", font, 32, TextAnchor.MiddleCenter);
            finalTimeText.rectTransform.anchorMin = new Vector2(0.5f, 0.54f);
            finalTimeText.rectTransform.anchorMax = new Vector2(0.5f, 0.54f);
            finalTimeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            finalTimeText.rectTransform.sizeDelta = new Vector2(500f, 60f);

            var buttonObject = new GameObject("RestartButton");
            buttonObject.transform.SetParent(gameOverPanel.transform, false);

            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.42f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.42f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(240f, 70f);

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.19f, 0.61f, 0.86f, 1f);

            restartButton = buttonObject.AddComponent<Button>();
            restartButton.targetGraphic = buttonImage;

            var buttonLabel = CreateText(buttonObject.transform, "Label", font, 28, TextAnchor.MiddleCenter);
            buttonLabel.text = "Restart";
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;

            var promptText = CreateText(gameOverPanel.transform, "PromptText", font, 24, TextAnchor.MiddleCenter);
            promptText.text = "Press Submit or click Restart";
            promptText.rectTransform.anchorMin = new Vector2(0.5f, 0.32f);
            promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.32f);
            promptText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            promptText.rectTransform.sizeDelta = new Vector2(520f, 50f);

            gameOverPanel.SetActive(false);
        }

        private static Text CreateText(Transform parent, string objectName, Font font, int size, TextAnchor anchor)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = string.Empty;
            return text;
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
