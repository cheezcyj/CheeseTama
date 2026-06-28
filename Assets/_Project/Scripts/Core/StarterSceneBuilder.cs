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
            var existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null)
            {
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
            EnsureCheeseTamaPlaceholder();

            var canvas = EnsureCanvas("Milkroom Canvas");
            var controller = Object.FindFirstObjectByType<MilkroomUIController>();
            if (controller == null)
            {
                controller = canvas.gameObject.AddComponent<MilkroomUIController>();
            }

            var panel = GetOrCreatePanel(canvas.transform, "Status Panel", new Vector2(24, -24), new Vector2(260, 254));
            var panelTransform = panel.transform;

            var nameText = GetOrCreateText(panelTransform, "Name Text", "CheeseTama", 22, TextAnchor.UpperLeft, new Vector2(16, -14), new Vector2(228, 30));
            var levelText = GetOrCreateText(panelTransform, "Level Text", "Lv. 1", 18, TextAnchor.UpperLeft, new Vector2(16, -48), new Vector2(228, 26));
            var hungerText = GetOrCreateText(panelTransform, "Hunger Text", "Hunger: 80", 16, TextAnchor.UpperLeft, new Vector2(16, -84), new Vector2(228, 24));
            var moodText = GetOrCreateText(panelTransform, "Mood Text", "Mood: 70", 16, TextAnchor.UpperLeft, new Vector2(16, -112), new Vector2(228, 24));
            var cleanlinessText = GetOrCreateText(panelTransform, "Cleanliness Text", "Cleanliness: 90", 16, TextAnchor.UpperLeft, new Vector2(16, -140), new Vector2(228, 24));
            var sleepinessText = GetOrCreateText(panelTransform, "Sleepiness Text", "Sleepiness: 20", 16, TextAnchor.UpperLeft, new Vector2(16, -168), new Vector2(228, 24));
            var healthText = GetOrCreateText(panelTransform, "Health Text", "Health: 100", 16, TextAnchor.UpperLeft, new Vector2(16, -196), new Vector2(228, 24));

            controller.Configure(nameText, levelText, hungerText, moodText, cleanlinessText, sleepinessText, healthText);
            controller.Bind(manager.CurrentTama);

            var feedButton = GetOrCreateButton(canvas.transform, "Feed Milk Button", "Feed Milk", new Vector2(-150, 36));
            feedButton.onClick.RemoveAllListeners();
            feedButton.onClick.AddListener(() => FeedTestMilk(manager, controller));

            var cleanButton = GetOrCreateButton(canvas.transform, "Clean Button", "Clean", new Vector2(0, 36));
            cleanButton.onClick.RemoveAllListeners();
            cleanButton.onClick.AddListener(() => CleanTestRoom(manager, controller));

            var saveButton = GetOrCreateButton(canvas.transform, "Save Button", "Save", new Vector2(150, 36));
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => SaveTestGame(manager));
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

        private static void FeedTestMilk(GameManager manager, MilkroomUIController controller)
        {
            var tama = manager != null ? manager.CurrentTama : null;
            if (tama == null)
            {
                return;
            }

            tama.stats.hunger = Mathf.Clamp(tama.stats.hunger + 15, 0, 100);
            tama.stats.mood = Mathf.Clamp(tama.stats.mood + 3, 0, 100);
            tama.stats.affection = Mathf.Clamp(tama.stats.affection + 1, 0, 100);
            controller.Refresh();
            Debug.Log("Fed test milk.");
        }

        private static void CleanTestRoom(GameManager manager, MilkroomUIController controller)
        {
            var tama = manager != null ? manager.CurrentTama : null;
            if (tama == null)
            {
                return;
            }

            tama.stats.cleanliness = Mathf.Clamp(tama.stats.cleanliness + 20, 0, 100);
            tama.stats.health = Mathf.Clamp(tama.stats.health + 2, 0, 100);
            controller.Refresh();
            Debug.Log("Cleaned the test milkroom.");
        }

        private static void SaveTestGame(GameManager manager)
        {
            manager?.SaveGame();
            Debug.Log("Saved CheeseTama test data.");
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

        private static void EnsureCheeseTamaPlaceholder()
        {
            if (GameObject.Find("CheeseTama Egg Placeholder") != null)
            {
                return;
            }

            var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            egg.name = "CheeseTama Egg Placeholder";
            egg.transform.position = new Vector3(0, -0.3f, 0);
            egg.transform.localScale = new Vector3(1.25f, 1.55f, 1.25f);

            var renderer = egg.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                renderer.sharedMaterial = new Material(shader)
                {
                    color = new Color(1f, 0.84f, 0.28f)
                };
            }
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
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = panel.AddComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            return panel;
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            return CreatePanel(parent, name, anchoredPosition, size);
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

            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.anchorMax = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.pivot = centered ? new Vector2(0.5f, 1) : new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var label = textObject.AddComponent<Text>();
            label.font = GetDefaultFont();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.22f, 0.17f, 0.12f);
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
                return existingText;
            }

            return CreateText(parent, name, text, fontSize, alignment, anchoredPosition, size, centered);
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(136, 44);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 0.78f, 0.34f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText(buttonObject.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, rect.sizeDelta, true);
            return button;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                return existingButton;
            }

            return CreateButton(parent, name, label, anchoredPosition);
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
