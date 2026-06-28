using CheeseTama.Data;
using CheeseTama.Save;
using CheeseTama.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static class StarterSceneBuilder
    {
        public static GameManager EnsureCoreSystems()
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentSave == null)
                {
                    GameManager.Instance.LoadOrCreateGame();
                }

                return GameManager.Instance;
            }

            var existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
            {
                if (existing.CurrentSave == null)
                {
                    existing.LoadOrCreateGame();
                }

                return existing;
            }

            var core = new GameObject("CoreSystems");
            core.AddComponent<DataRegistry>();
            core.AddComponent<SaveManager>();
            return core.AddComponent<GameManager>();
        }

        public static void BuildForScene(string sceneName)
        {
            if (sceneName == SceneNames.Boot)
            {
                BuildBootScene();
            }
            else if (sceneName == SceneNames.Milkroom)
            {
                BuildMilkroomScene();
            }
            else if (sceneName == SceneNames.Collection)
            {
                BuildCollectionScene();
            }
            else if (sceneName == SceneNames.Debug)
            {
                BuildDebugScene();
            }
        }

        public static void BuildBootScene()
        {
            EnsureCoreSystems();
            EnsureCamera("Boot Camera");
            EnsureEventSystem();
            EnsureCanvas("Boot Canvas");
            EnsureTitle("Boot Canvas", "CheeseTama", "Loading core systems");
        }

        public static void BuildMilkroomScene()
        {
            var manager = EnsureCoreSystems();
            EnsureCamera("Milkroom Camera");
            EnsureLight();
            EnsureEventSystem();
            var visualController = EnsureCheeseTamaPlaceholder();

            var canvas = EnsureCanvas("Milkroom Canvas");
            var controller = Object.FindFirstObjectByType<MilkroomUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<MilkroomUIController>();
            }

            var panel = GetOrCreatePanel(canvas.transform, "Status Panel", new Vector2(24, -24), new Vector2(320, 430));
            var panelTransform = panel.transform;

            var nameText = GetOrCreateText(panelTransform, "Name Text", "CheeseTama", 22, TextAnchor.UpperLeft, new Vector2(16, -14), new Vector2(260, 30));
            var levelText = GetOrCreateText(panelTransform, "Level Text", "Lv. 1 (0%)", 18, TextAnchor.UpperLeft, new Vector2(16, -48), new Vector2(260, 26));
            var formText = GetOrCreateText(panelTransform, "Form Text", "Form: egg", 16, TextAnchor.UpperLeft, new Vector2(16, -78), new Vector2(260, 24));
            var hungerText = GetOrCreateText(panelTransform, "Hunger Text", "Hunger: 80", 16, TextAnchor.UpperLeft, new Vector2(16, -112), new Vector2(260, 24));
            var moodText = GetOrCreateText(panelTransform, "Mood Text", "Mood: 70", 16, TextAnchor.UpperLeft, new Vector2(16, -140), new Vector2(260, 24));
            var cleanlinessText = GetOrCreateText(panelTransform, "Cleanliness Text", "Cleanliness: 90", 16, TextAnchor.UpperLeft, new Vector2(16, -168), new Vector2(260, 24));
            var sleepinessText = GetOrCreateText(panelTransform, "Sleepiness Text", "Sleepiness: 20", 16, TextAnchor.UpperLeft, new Vector2(16, -196), new Vector2(260, 24));
            var healthText = GetOrCreateText(panelTransform, "Health Text", "Health: 100", 16, TextAnchor.UpperLeft, new Vector2(16, -224), new Vector2(260, 24));
            var affectionText = GetOrCreateText(panelTransform, "Affection Text", "Affection: 10", 16, TextAnchor.UpperLeft, new Vector2(16, -252), new Vector2(260, 24));
            var maturationText = GetOrCreateText(panelTransform, "Maturation Text", "Maturation: 0", 16, TextAnchor.UpperLeft, new Vector2(16, -280), new Vector2(260, 24));
            var lastSavedText = GetOrCreateText(panelTransform, "Last Saved Text", "Last Saved: Never", 14, TextAnchor.UpperLeft, new Vector2(16, -310), new Vector2(280, 24));
            var messageText = GetOrCreateText(panelTransform, "Message Text", "Ready for care.", 14, TextAnchor.UpperLeft, new Vector2(16, -346), new Vector2(280, 56));

            controller.Configure(
                nameText,
                levelText,
                formText,
                hungerText,
                moodText,
                cleanlinessText,
                sleepinessText,
                healthText,
                affectionText,
                maturationText,
                lastSavedText,
                messageText);
            controller.Bind(manager.CurrentTama);
            controller.ShowMessage("Ready for care.");
            visualController.Bind(manager.CurrentTama);

            var feedButton = GetOrCreateButton(canvas.transform, "Feed Milk Button", "Feed Milk", new Vector2(-480, 36));
            ConfigureCareButton(feedButton, MilkroomCareAction.FeedMilk, controller, visualController);

            var playButton = GetOrCreateButton(canvas.transform, "Play Button", "Play", new Vector2(-320, 36));
            ConfigureCareButton(playButton, MilkroomCareAction.Play, controller, visualController);

            var cleanButton = GetOrCreateButton(canvas.transform, "Clean Button", "Clean", new Vector2(-160, 36));
            ConfigureCareButton(cleanButton, MilkroomCareAction.Clean, controller, visualController);

            var restButton = GetOrCreateButton(canvas.transform, "Rest Button", "Rest", new Vector2(0, 36));
            ConfigureCareButton(restButton, MilkroomCareAction.Rest, controller, visualController);

            var saveButton = GetOrCreateButton(canvas.transform, "Save Button", "Save", new Vector2(160, 36));
            ConfigureCareButton(saveButton, MilkroomCareAction.Save, controller, visualController);

            var reloadButton = GetOrCreateButton(canvas.transform, "Reload Button", "Reload", new Vector2(320, 36));
            ConfigureCareButton(reloadButton, MilkroomCareAction.Reload, controller, visualController);

            var resetButton = GetOrCreateButton(canvas.transform, "Reset Button", "Reset", new Vector2(480, 36));
            ConfigureCareButton(resetButton, MilkroomCareAction.Reset, controller, visualController);
        }

        public static void BuildCollectionScene()
        {
            EnsureCoreSystems();
            EnsureCamera("Collection Camera");
            EnsureEventSystem();
            var canvas = EnsureCanvas("Collection Canvas");

            if (Object.FindFirstObjectByType<CollectionUIController>() == null)
            {
                canvas.gameObject.AddComponent<CollectionUIController>();
            }

            EnsureTitle("Collection Canvas", "Collection", "Only discovered records appear here");
        }

        public static void BuildDebugScene()
        {
            EnsureCoreSystems();
            EnsureCamera("Debug Camera");
            EnsureEventSystem();
            EnsureTitle("Debug Canvas", "Debug", "Developer test surface");
        }

        private static Camera EnsureCamera(string name)
        {
            var existing = Object.FindFirstObjectByType<Camera>();
            if (existing != null)
            {
                return existing;
            }

            var cameraObject = new GameObject(name);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.96f, 0.92f, 0.84f);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            return camera;
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Milkroom Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private static CheeseTamaVisualController EnsureCheeseTamaPlaceholder()
        {
            var existing = GameObject.Find("CheeseTama Egg Placeholder");
            if (existing != null)
            {
                return GetOrCreateVisualController(existing);
            }

            var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            egg.name = "CheeseTama Egg Placeholder";
            egg.transform.position = new Vector3(0, -0.3f, 0);
            egg.transform.localScale = new Vector3(1.25f, 1.55f, 1.25f);

            return GetOrCreateVisualController(egg);
        }

        private static CheeseTamaVisualController GetOrCreateVisualController(GameObject target)
        {
            var controller = target.GetComponent<CheeseTamaVisualController>();
            if (controller == null)
            {
                controller = target.AddComponent<CheeseTamaVisualController>();
            }

            return controller;
        }

        private static Canvas EnsureCanvas(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            {
                return existingCanvas;
            }

            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureTitle(string canvasName, string title, string subtitle)
        {
            var canvas = EnsureCanvas(canvasName);
            if (canvas.transform.Find("Title Text") != null)
            {
                return;
            }

            CreateText(canvas.transform, "Title Text", title, 34, TextAnchor.MiddleCenter, new Vector2(0, -82), new Vector2(520, 44), true);
            CreateText(canvas.transform, "Subtitle Text", subtitle, 18, TextAnchor.MiddleCenter, new Vector2(0, -128), new Vector2(520, 32), true);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            ConfigurePanelRect(rect, anchoredPosition, size);

            var image = panel.AddComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            return panel;
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.TryGetComponent(out RectTransform rect))
                {
                    ConfigurePanelRect(rect, anchoredPosition, size);
                }

                if (!existing.TryGetComponent(out Image image))
                {
                    image = existing.gameObject.AddComponent<Image>();
                }

                image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
                return existing.gameObject;
            }

            return CreatePanel(parent, name, anchoredPosition, size);
        }

        private static void ConfigurePanelRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            bool centered = false)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            textObject.AddComponent<RectTransform>();
            var label = textObject.AddComponent<Text>();
            ConfigureText(label, text, fontSize, alignment, anchoredPosition, size, centered);
            return label;
        }

        private static Text GetOrCreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            bool centered = false)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Text existingText))
            {
                ConfigureText(existingText, text, fontSize, alignment, anchoredPosition, size, centered);
                return existingText;
            }

            return CreateText(parent, name, text, fontSize, alignment, anchoredPosition, size, centered);
        }

        private static void ConfigureText(
            Text label,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            bool centered)
        {
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.anchorMax = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.pivot = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            label.font = GetDefaultFont();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.22f, 0.17f, 0.12f);
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(136, 44);

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ConfigureButton(button, label, anchoredPosition);
            return button;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureButton(existingButton, label, anchoredPosition);
                return existingButton;
            }

            return CreateButton(parent, name, label, anchoredPosition);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController);
        }

        private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(136, 44);

            if (!button.TryGetComponent(out Image image))
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = new Color(1f, 0.78f, 0.34f);
            button.targetGraphic = image;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null)
            {
                CreateText(button.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, true);
                return;
            }

            if (!labelTransform.TryGetComponent(out Text labelText))
            {
                labelText = labelTransform.gameObject.AddComponent<Text>();
            }

            ConfigureText(labelText, label, 16, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, true);
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
