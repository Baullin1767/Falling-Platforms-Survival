using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FallingPlatformsSurvival.Editor
{
    public static class PrefabDrivenSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ArtFolder = "Assets/Art";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string PlaceholderSpritePath = ArtFolder + "/PlaceholderSquare.png";
        private const string PlayerPrefabPath = PrefabsFolder + "/Player.prefab";
        private const string PlatformPrefabPath = PrefabsFolder + "/Platform.prefab";

        [MenuItem("Tools/Falling Platforms/Setup Prefab Driven Scene")]
        public static void Setup()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabsFolder);

            var placeholderSprite = EnsurePlaceholderSprite();
            var playerPrefab = CreatePlayerPrefab(placeholderSprite);
            var platformPrefab = CreatePlatformPrefab(placeholderSprite);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearScene(scene);

            var cameraFollow = CreateMainCamera();
            CreateGlobalLight();
            var platformManager = CreatePlatformManager(platformPrefab);
            var deathZoneFollower = CreateDeathZone();
            var uiManager = CreateUi();
            CreateGameBootstrap(playerPrefab, platformManager, uiManager, cameraFollow, deathZoneFollower);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            var folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
        }

        private static void ClearScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePlayerPrefab(Sprite placeholderSprite)
        {
            var playerObject = new GameObject("Player");
            var spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = placeholderSprite;

            var playerCollider = playerObject.AddComponent<BoxCollider2D>();
            playerCollider.size = new Vector2(0.9f, 1.25f);

            var playerBody = playerObject.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 4f;
            playerBody.freezeRotation = true;
            playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            playerObject.transform.localScale = new Vector3(0.9f, 1.25f, 1f);
            playerObject.AddComponent<PlayerController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(playerObject, PlayerPrefabPath);
            Object.DestroyImmediate(playerObject);
            return prefab;
        }

        private static GameObject CreatePlatformPrefab(Sprite placeholderSprite)
        {
            var platformObject = new GameObject("Platform");
            var spriteRenderer = platformObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = placeholderSprite;

            var platformCollider = platformObject.AddComponent<BoxCollider2D>();
            platformCollider.size = Vector2.one;

            var platformBody = platformObject.AddComponent<Rigidbody2D>();
            platformBody.bodyType = RigidbodyType2D.Kinematic;
            platformBody.gravityScale = 0f;
            platformBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            platformObject.AddComponent<PlatformBehaviour>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(platformObject, PlatformPrefabPath);
            Object.DestroyImmediate(platformObject);
            return prefab;
        }

        private static Sprite EnsurePlaceholderSprite()
        {
            var absolutePath = GetAbsoluteAssetPath(PlaceholderSpritePath);

            if (!File.Exists(absolutePath))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixels(new[]
                {
                    Color.white, Color.white,
                    Color.white, Color.white
                });
                texture.Apply();

                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static CameraFollow2D CreateMainCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.18f, 1f);

            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            return cameraObject.AddComponent<CameraFollow2D>();
        }

        private static void CreateGlobalLight()
        {
            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        private static PlatformManager CreatePlatformManager(GameObject platformPrefab)
        {
            var platformManagerObject = new GameObject("PlatformManager");
            var platformManager = platformManagerObject.AddComponent<PlatformManager>();
            SetObjectReference(platformManager, "platformPrefab", platformPrefab);
            return platformManager;
        }

        private static DeathZoneFollower CreateDeathZone()
        {
            var deathZoneObject = new GameObject("DeathZone");
            var triggerCollider = deathZoneObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(30f, 2f);
            return deathZoneObject.AddComponent<DeathZoneFollower>();
        }

        private static UIManager CreateUi()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("UIRoot", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var uiManager = canvasObject.AddComponent<UIManager>();

            var gameplayHudRoot = CreateRectObject(canvasObject.transform, "GameplayHud");
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

            var timerText = CreateText(topBar.transform, "TimerText", font, 38, TextAnchor.MiddleLeft, FontStyle.Bold);
            Stretch(timerText.rectTransform, new Vector2(38f, 26f), new Vector2(-860f, -26f));

            var controlsText = CreateText(topBar.transform, "ControlsText", font, 22, TextAnchor.MiddleRight, FontStyle.Normal);
            Stretch(controlsText.rectTransform, new Vector2(720f, 26f), new Vector2(-38f, -26f));

            var touchControlsRoot = CreateRectObject(canvasObject.transform, "TouchControls");
            Stretch((RectTransform)touchControlsRoot.transform, Vector2.zero, Vector2.zero);

            var movementPanel = CreatePanel(
                touchControlsRoot.transform,
                "MovementPanel",
                new Color(0.04f, 0.07f, 0.1f, 0.58f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(40f, 40f),
                new Vector2(420f, 240f));

            var moveLeftButton = CreateTouchButton(
                movementPanel.transform,
                "MoveLeftButton",
                font,
                "LEFT",
                new Vector2(24f, 24f),
                new Vector2(174f, 174f),
                new Color(0.18f, 0.44f, 0.86f, 0.92f));

            var moveRightButton = CreateTouchButton(
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

            var jumpButton = CreateTouchButton(
                jumpPanel.transform,
                "JumpButton",
                font,
                "JUMP",
                new Vector2(26f, 26f),
                new Vector2(208f, 208f),
                new Color(0.98f, 0.62f, 0.2f, 0.95f));

            var gameOverPanel = CreateRectObject(canvasObject.transform, "GameOverPanel");
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

            var finalTimeText = CreateText(card.transform, "FinalTimeText", font, 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(finalTimeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(480f, 50f));

            var restartHintText = CreateText(card.transform, "RestartHintText", font, 24, TextAnchor.MiddleCenter, FontStyle.Normal);
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

            var promptText = CreateText(card.transform, "PromptText", font, 22, TextAnchor.MiddleCenter, FontStyle.Normal);
            promptText.text = "Keyboard: R    Gamepad: Submit";
            Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(420f, 40f));

            gameOverPanel.SetActive(false);

            SetObjectReference(uiManager, "rootCanvas", canvas);
            SetObjectReference(uiManager, "gameplayHudRoot", gameplayHudRoot);
            SetObjectReference(uiManager, "touchControlsRoot", touchControlsRoot);
            SetObjectReference(uiManager, "timerText", timerText);
            SetObjectReference(uiManager, "controlsText", controlsText);
            SetObjectReference(uiManager, "gameOverPanel", gameOverPanel);
            SetObjectReference(uiManager, "finalTimeText", finalTimeText);
            SetObjectReference(uiManager, "restartHintText", restartHintText);
            SetObjectReference(uiManager, "restartButton", restartButtonObject.GetComponent<Button>());
            SetObjectReference(uiManager, "moveLeftButton", moveLeftButton);
            SetObjectReference(uiManager, "moveRightButton", moveRightButton);
            SetObjectReference(uiManager, "jumpButton", jumpButton);

            return uiManager;
        }

        private static GameManager CreateGameBootstrap(
            GameObject playerPrefab,
            PlatformManager platformManager,
            UIManager uiManager,
            CameraFollow2D cameraFollow,
            DeathZoneFollower deathZoneFollower)
        {
            var bootstrapObject = new GameObject("GameBootstrap");
            var gameManager = bootstrapObject.AddComponent<GameManager>();

            SetObjectReference(gameManager, "playerPrefab", playerPrefab);
            SetObjectReference(gameManager, "platformManager", platformManager);
            SetObjectReference(gameManager, "uiManager", uiManager);
            SetObjectReference(gameManager, "cameraFollow", cameraFollow);
            SetObjectReference(gameManager, "deathZoneFollower", deathZoneFollower);

            return gameManager;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
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
    }
}
