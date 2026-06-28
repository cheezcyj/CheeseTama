using CheeseTama.Data;
using CheeseTama.Save;
using CheeseTama.UI;
using UnityEngine;
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
            EnsureCanvas("Boot Canvas");
            EnsureTitle("Boot Canvas", "CheeseTama", "Loading core systems");
        }

        public static void BuildMilkroomScene()
        {
            var manager = EnsureCoreSystems();
            EnsureCamera("Milkroom Camera");
            EnsureLight();
            EnsureCheeseTamaPlaceholder();

            var canvas = EnsureCanvas("Milkroom Canvas");
            if (Object.FindFirstObjectByType<MilkroomUIController>() != null)
            {
                return;
            }

            var controller = canvas.gameObject.AddComponent<MilkroomUIController>();
            var panel = CreatePanel(canvas.transform, "Status Panel", new Vector2(24, -24), new Vector2(260, 254));

            var nameText = CreateText(panel.transform, "Name Text", "CheeseTama", 22, TextAnchor.UpperLeft, new Vector2(16, -14), new Vector2(228, 30));
            var levelText = CreateText(panel.transform, "Level Text", "Lv. 1", 18, TextAnchor.UpperLeft, new Vector2(16, -48), new Vector2(228, 26));
            var hungerText = CreateText(panel.transform, "Hunger Text", "Hunger: 80", 16, TextAnchor.UpperLeft, new Vector2(16, -84), new Vector2(228, 24));
            var moodText = CreateText(panel.transform, "Mood Text", "Mood: 70", 16, TextAnchor.UpperLeft, new Vector2(16, -112), new Vector2(228, 24));
            var cleanlinessText = CreateText(panel.transform, "Cleanliness Text", "Cleanliness: 90", 16, TextAnchor.UpperLeft, new Vector2(16, -140), new Vector2(228, 24));
            var sleepinessText = CreateText(panel.transform, "Sleepiness Text", "Sleepiness: 20", 16, TextAnchor.UpperLeft, new Vector2(16, -168), new Vector2(228, 24));
            var healthText = CreateText(panel.transform, "Health Text", "Health: 100", 16, TextAnchor.UpperLeft, new Vector2(16, -196), new Vector2(228, 24));

            controller.Configure(nameText, levelText, hungerText, moodText, cleanlinessText, sleepinessText, healthText);
            controller.Bind(manager.CurrentTama);

            CreateButton(canvas.transform, "Feed Milk Button", "Feed Milk", new Vector2(-150, 36));
            CreateButton(canvas.transform, "Clean Button", "Clean", new Vector2(0, 36));
            CreateButton(canvas.transform, "Save Button", "Save", new Vector2(150, 36)).onClick.AddListener(manager.SaveGame);
        }

        public static void BuildCollectionScene()
        {
            EnsureCoreSystems();
            EnsureCamera("Collection Camera");
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
