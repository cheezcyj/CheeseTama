using System.Collections.Generic;
using CheeseTama.Audio;
using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Records;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Gameplay.Story;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Platform;
using CheeseTama.Platform.Accounts;
using CheeseTama.Save;
using CheeseTama.UI;
using CheeseTama.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static partial class StarterSceneBuilder
    {
        private static ScrollRect EnsureVerticalScrollArea(
            Transform parent,
            string scrollViewName,
            string viewportName,
            string contentName,
            Vector2 position,
            Vector2 size,
            out RectTransform content)
        {
            var scrollView = GetOrCreatePanel(parent, scrollViewName, position, size);
            var scrollViewImage = scrollView.GetComponent<Image>();
            scrollViewImage.color = Color.clear;
            scrollViewImage.raycastTarget = false;

            var viewport = GetOrCreateRect(scrollView.transform, viewportName);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-18f, 0f);
            var viewportImage = viewport.GetComponent<Image>()
                ?? viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            var viewportMask = viewport.GetComponent<RectMask2D>()
                ?? viewport.gameObject.AddComponent<RectMask2D>();
            viewportMask.padding = Vector4.zero;

            content = GetOrCreateRect(viewport, contentName);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, Mathf.Max(1f, size.y));
            var layout = content.GetComponent<VerticalLayoutGroup>()
                ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>()
                ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbarRect = GetOrCreateRect(
                scrollView.transform,
                $"{scrollViewName} Vertical Scrollbar");
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-12f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);
            var scrollbarImage = scrollbarRect.GetComponent<Image>()
                ?? scrollbarRect.gameObject.AddComponent<Image>();
            scrollbarImage.color = new Color(0.56f, 0.36f, 0.12f, 0.2f);
            ApplyRoundedImage(scrollbarImage);

            var slidingArea = GetOrCreateRect(scrollbarRect, "Sliding Area");
            slidingArea.anchorMin = Vector2.zero;
            slidingArea.anchorMax = Vector2.one;
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);
            var handleRect = GetOrCreateRect(slidingArea, "Handle");
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handleRect.GetComponent<Image>()
                ?? handleRect.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.94f, 0.54f, 0.12f, 0.94f);
            ApplyRoundedImage(handleImage);

            var scrollbar = scrollbarRect.GetComponent<Scrollbar>()
                ?? scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            var scrollRect = scrollView.GetComponent<ScrollRect>()
                ?? scrollView.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 34f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.verticalScrollbarSpacing = 4f;
            return scrollRect;
        }

        private static void ConfigureScrollableLayoutElement(Text text, float minimumHeight)
        {
            if (text == null)
            {
                return;
            }

            var layout = text.GetComponent<LayoutElement>()
                ?? text.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(1f, minimumHeight);
            layout.preferredHeight = -1f;
            layout.flexibleHeight = 0f;
        }

        private static void ConfigureJourneyBodyScrollLayout(
            RectTransform content,
            Text body,
            float viewportHeight)
        {
            if (content == null || body == null)
            {
                return;
            }

            const int horizontalPadding = 12;
            const int topPadding = 20;
            const int bottomPadding = 12;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = new Vector2(12f, 0f);
            content.sizeDelta = new Vector2(1000f, Mathf.Max(1f, viewportHeight));

            var layout = content.GetComponent<VerticalLayoutGroup>()
                ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                horizontalPadding,
                horizontalPadding,
                topPadding,
                bottomPadding);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>()
                ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ConfigureScrollableLayoutElement(
                body,
                Mathf.Max(1f, viewportHeight - topPadding - bottomPadding));
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
            var titleText = GetOrCreateText(
                canvas.transform,
                "Title Text",
                title,
                34,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -82f),
                new Vector2(520f, 44f),
                true);
            var subtitleText = GetOrCreateText(
                canvas.transform,
                "Subtitle Text",
                subtitle,
                18,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -128f),
                new Vector2(520f, 32f),
                true);
            titleText.gameObject.SetActive(true);
            subtitleText.gameObject.SetActive(true);
            titleText.enabled = true;
            subtitleText.enabled = true;
        }

        private static void RemoveMilkroomPrototypeButtons(Transform canvasTransform)
        {
            RemoveChildIfExists(canvasTransform, "Catch Drops Button");
            RemoveChildIfExists(canvasTransform, "Snack Button");
            RemoveChildIfExists(canvasTransform, "Feed Milk Button");
            RemoveChildIfExists(canvasTransform, "Star Milk Button");
            RemoveChildIfExists(canvasTransform, "Play Button");
            RemoveChildIfExists(canvasTransform, "Clean Button");
            RemoveChildIfExists(canvasTransform, "Rest Button");
            RemoveChildIfExists(canvasTransform, "Wait Hour Button");
            RemoveChildIfExists(canvasTransform, "Save Button");
            RemoveChildIfExists(canvasTransform, "Reload Button");
            RemoveChildIfExists(canvasTransform, "Reset Button");
            RemoveChildIfExists(canvasTransform, "Collection Button");
            RemoveChildIfExists(canvasTransform, "Debug Button");
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            ConfigurePanelRect(rect, anchoredPosition, size);

            var image = panel.AddComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            ApplyRoundedImage(image);
            return panel;
        }

        private static GameObject GetOrCreateFullScreenOverlay(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name);
            var overlay = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null)
            {
                overlay.transform.SetParent(parent, false);
            }

            var rect = overlay.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = overlay.AddComponent<RectTransform>();
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = overlay.GetComponent<Image>();
            if (image == null)
            {
                image = overlay.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;
            var group = overlay.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = overlay.AddComponent<CanvasGroup>();
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            return overlay;
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
                ApplyRoundedImage(image);
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

        private static RectTransform GetOrCreateCollectionScrollContent(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var scrollView = GetOrCreatePanel(parent, "Collection Scroll View", anchoredPosition, size);
            if (scrollView.TryGetComponent(out Image scrollImage))
            {
                scrollImage.color = new Color(1f, 0.95f, 0.78f, 0.48f);
            }

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scrollView.AddComponent<ScrollRect>();
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 28f;

            var viewport = GetOrCreatePanel(scrollView.transform, "Viewport", Vector2.zero, size);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.offsetMin = new Vector2(18f, 18f);
            viewportRect.offsetMax = new Vector2(-18f, -18f);

            if (viewport.TryGetComponent(out Image viewportImage))
            {
                viewportImage.color = new Color(1f, 0.98f, 0.9f, 0.35f);
            }

            var mask = viewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = viewport.AddComponent<Mask>();
            }

            mask.showMaskGraphic = false;

            var contentTransform = viewport.transform.Find("Collection Scroll Content");
            RectTransform contentRect;
            if (contentTransform != null && contentTransform.TryGetComponent(out contentRect))
            {
                ConfigureCollectionScrollContentRect(contentRect, size.y - 36f);
            }
            else
            {
                var contentObject = new GameObject("Collection Scroll Content");
                contentObject.transform.SetParent(viewport.transform, false);
                contentRect = contentObject.AddComponent<RectTransform>();
                ConfigureCollectionScrollContentRect(contentRect, size.y - 36f);
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalNormalizedPosition = 1f;
            return contentRect;
        }

        private static void ConfigureCollectionScrollContentRect(RectTransform rect, float minimumHeight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, Mathf.Max(360f, minimumHeight));
        }

        private static RectTransform GetOrCreateVerticalScrollContent(
            Transform parent,
            string scrollViewName,
            string contentName,
            Vector2 anchoredPosition,
            Vector2 size,
            float contentHeight,
            Color backgroundColor)
        {
            var scrollView = GetOrCreatePanel(parent, scrollViewName, anchoredPosition, size);
            if (scrollView.TryGetComponent(out Image scrollImage))
            {
                scrollImage.color = backgroundColor;
            }

            RemoveChildIfExists(scrollView.transform, $"{scrollViewName} Viewport");

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = scrollView.AddComponent<ScrollRect>();
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var viewportRect = scrollView.GetComponent<RectTransform>();
            RemoveComponentIfExists<Mask>(scrollView);
            if (scrollView.GetComponent<RectMask2D>() == null)
            {
                scrollView.AddComponent<RectMask2D>();
            }

            var contentTransform = scrollView.transform.Find(contentName);
            RectTransform contentRect;
            if (contentTransform != null && contentTransform.TryGetComponent(out contentRect))
            {
                ConfigureVerticalScrollContentRect(contentRect, contentHeight);
            }
            else
            {
                var contentObject = new GameObject(contentName);
                contentObject.transform.SetParent(scrollView.transform, false);
                contentRect = contentObject.AddComponent<RectTransform>();
                ConfigureVerticalScrollContentRect(contentRect, contentHeight);
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalNormalizedPosition = 1f;
            return contentRect;
        }

        private static void ConfigureVerticalScrollContentRect(RectTransform rect, float contentHeight)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -12f);
            rect.sizeDelta = new Vector2(600f, Mathf.Max(120f, contentHeight));
        }

        private static Text GetOrCreateCollectionRecordText(Transform parent, string name, string text, int fontSize)
        {
            var label = GetOrCreateText(parent, name, text, fontSize, TextAnchor.UpperLeft, Vector2.zero, new Vector2(0f, 520f));
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 520f);
            ApplyCollectionRecordTextStyle(label);
            return label;
        }

        // Anchors a panel to the bottom-center of the screen so it stays pinned to the
        // bottom edge regardless of the game's aspect ratio. This keeps the care buttons,
        // stat values and status message from drifting over the character / milkroom.
        private static GameObject GetOrCreateBottomPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.TryGetComponent(out RectTransform rect))
                {
                    ConfigureBottomPanelRect(rect, anchoredPosition, size);
                }

                if (!existing.TryGetComponent(out Image image))
                {
                    image = existing.gameObject.AddComponent<Image>();
                }

                image.color = new Color(1f, 0.98f, 0.9f, 0.92f);
                ApplyRoundedImage(image);
                return existing.gameObject;
            }

            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var newRect = panel.AddComponent<RectTransform>();
            ConfigureBottomPanelRect(newRect, anchoredPosition, size);

            var newImage = panel.AddComponent<Image>();
            newImage.color = new Color(1f, 0.98f, 0.9f, 0.92f);
            ApplyRoundedImage(newImage);
            return panel;
        }

        private static void ConfigureBottomPanelRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static GameObject GetOrCreateRightPanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (existing.TryGetComponent(out RectTransform rect))
                {
                    ConfigureRightPanelRect(rect, anchoredPosition, size);
                }

                if (!existing.TryGetComponent(out Image image))
                {
                    image = existing.gameObject.AddComponent<Image>();
                }

                image.color = new Color(1f, 0.98f, 0.9f, 0.86f);
                ApplyRoundedImage(image);
                return existing.gameObject;
            }

            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var newRect = panel.AddComponent<RectTransform>();
            ConfigureRightPanelRect(newRect, anchoredPosition, size);

            var newImage = panel.AddComponent<Image>();
            newImage.color = new Color(1f, 0.98f, 0.9f, 0.86f);
            ApplyRoundedImage(newImage);
            return panel;
        }

        private static void ConfigureRightPanelRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void RemoveChildIfExists(Transform parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            var child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            child.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                DestroyObjectSafely(child.gameObject);
            }
            else
            {
                DestroyObjectSafely(child.gameObject);
            }
        }

        private static void RemoveRootObjectIfExists(string name)
        {
            foreach (var rootObject in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (rootObject.name != name || rootObject.transform.parent != null || !rootObject.scene.IsValid())
                {
                    continue;
                }

                DestroyObjectSafely(rootObject);
                return;
            }
        }

        private static void RemoveComponentIfExists<T>(GameObject target) where T : Component
        {
            if (target == null || !target.TryGetComponent<T>(out var component))
            {
                return;
            }

            DestroyObjectSafely(component);
        }

        private static void DestroyObjectSafely(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }

            Object.DestroyImmediate(target);
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

        private static InputField GetOrCreateInputField(
            Transform parent,
            string name,
            string placeholder,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out InputField existingInput))
            {
                ConfigureInputField(existingInput, placeholder, anchoredPosition, size);
                return existingInput;
            }

            var inputObject = new GameObject(name);
            inputObject.transform.SetParent(parent, false);
            inputObject.AddComponent<RectTransform>();
            var image = inputObject.AddComponent<Image>();
            image.color = new Color(1f, 0.98f, 0.9f);
            ApplyRoundedImage(image);

            var input = inputObject.AddComponent<InputField>();
            var text = CreateText(inputObject.transform, "Text", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
            var placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
            placeholderText.color = new Color(0.45f, 0.34f, 0.24f, 0.45f);
            input.textComponent = text;
            input.placeholder = placeholderText;
            ConfigureInputField(input, placeholder, anchoredPosition, size);
            return input;
        }

        private static void ConfigureInputField(
            InputField input,
            string placeholder,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var rect = input.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1f, 0.98f, 0.9f);
                ApplyRoundedImage(image);
            }

            if (input.placeholder is Text placeholderText)
            {
                ConfigureText(placeholderText, placeholder, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
                placeholderText.color = new Color(0.45f, 0.34f, 0.24f, 0.45f);
            }

            if (input.textComponent != null)
            {
                ConfigureText(input.textComponent, input.text, 18, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(size.x - 24, size.y), false);
            }
        }

        private static Slider GetOrCreateSettingsSlider(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float minValue,
            float maxValue,
            bool wholeNumbers)
        {
            var existing = parent.Find(name);
            Slider slider;
            if (existing != null && existing.TryGetComponent(out slider))
            {
                ConfigureTopLeftRect(slider.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }
            else
            {
                var sliderObject = new GameObject(name);
                sliderObject.transform.SetParent(parent, false);
                sliderObject.AddComponent<RectTransform>();
                slider = sliderObject.AddComponent<Slider>();
                ConfigureTopLeftRect(slider.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }

            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = wholeNumbers;
            slider.direction = Slider.Direction.LeftToRight;

            var backgroundImage = GetOrCreateSettingsImage(slider.transform, "Background", new Color(1f, 0.93f, 0.68f, 0.95f));
            ConfigureStretchRect(backgroundImage.rectTransform, 0f, 0f, 7f);
            ApplyRoundedImage(backgroundImage);

            var fillArea = GetOrCreateRect(slider.transform, "Fill Area");
            ConfigureStretchRect(fillArea, 7f, 7f, 0f);

            var fillImage = GetOrCreateSettingsImage(fillArea, "Fill", new Color(1f, 0.58f, 0.12f, 1f));
            ConfigureStretchRect(fillImage.rectTransform, 0f, 0f, 8f);
            ApplyRoundedImage(fillImage);

            var handleArea = GetOrCreateRect(slider.transform, "Handle Slide Area");
            ConfigureStretchRect(handleArea, 7f, 7f, 0f);

            var handleImage = GetOrCreateSettingsImage(handleArea, "Handle", new Color(1f, 0.72f, 0.18f, 1f));
            ConfigureCenteredRect(handleImage.rectTransform, new Vector2(20f, 24f));
            ApplyRoundedImage(handleImage);

            slider.fillRect = fillImage.rectTransform;
            slider.handleRect = handleImage.rectTransform;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Toggle GetOrCreateSettingsToggle(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            Toggle toggle;
            if (existing != null && existing.TryGetComponent(out toggle))
            {
                ConfigureTopLeftRect(toggle.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }
            else
            {
                var toggleObject = new GameObject(name);
                toggleObject.transform.SetParent(parent, false);
                toggleObject.AddComponent<RectTransform>();
                toggle = toggleObject.AddComponent<Toggle>();
                ConfigureTopLeftRect(toggle.GetComponent<RectTransform>(), anchoredPosition.x, -anchoredPosition.y, size.x, size.y);
            }

            var boxImage = GetOrCreateSettingsImage(toggle.transform, "Box", new Color(1f, 0.94f, 0.76f, 1f));
            ConfigureTopLeftRect(boxImage.rectTransform, 0f, 4f, 22f, 22f);
            ApplyRoundedImage(boxImage);
            boxImage.color = new Color(1f, 0.985f, 0.91f, 1f);
            var boxOutline = boxImage.GetComponent<Outline>()
                ?? boxImage.gameObject.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.45f, 0.28f, 0.08f, 1f);
            boxOutline.effectDistance = new Vector2(2f, -2f);
            boxOutline.useGraphicAlpha = false;

            var checkImage = GetOrCreateSettingsImage(boxImage.transform, "Checkmark", new Color(0.55f, 0.24f, 0.04f, 1f));
            ConfigureTopLeftRect(checkImage.rectTransform, 4f, 4f, 14f, 14f);
            checkImage.color = new Color(0.55f, 0.24f, 0.04f, 1f);
            ApplyRoundedImage(checkImage);

            var labelText = GetOrCreateText(toggle.transform, "Label", label, 14, TextAnchor.MiddleLeft, new Vector2(32f, 0f), new Vector2(size.x - 32f, size.y));
            labelText.color = new Color(0.24f, 0.16f, 0.08f);
            labelText.raycastTarget = false;

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.transition = Selectable.Transition.ColorTint;
            return toggle;
        }

        private static void ApplyCompactSettingsToggleTypography(Toggle toggle)
        {
            var label = toggle != null
                ? toggle.transform.Find("Label")?.GetComponent<Text>()
                : null;
            if (label == null)
            {
                return;
            }

            label.fontSize = 13;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 13;
            label.GetComponent<AccessibilityTextProfile>()?.Rebase(label);
        }

        private static Image GetOrCreateSettingsImage(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name);
            Image image;
            if (existing != null && existing.TryGetComponent(out image))
            {
                image.color = color;
                return image;
            }

            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            imageObject.AddComponent<RectTransform>();
            image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static RectTransform GetOrCreateRect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            {
                return existingRect;
            }

            var rectObject = new GameObject(name);
            rectObject.transform.SetParent(parent, false);
            return rectObject.AddComponent<RectTransform>();
        }

        private static void ConfigureStretchRect(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, -height * 0.5f);
            rect.offsetMax = new Vector2(-right, height * 0.5f);
        }

        private static void ConfigureCenteredRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
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
            label.raycastTarget = false;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            return CreateButton(parent, name, label, anchoredPosition, new Vector2(136, 44));
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ConfigureButton(button, label, anchoredPosition, size);
            return button;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            return GetOrCreateButton(parent, name, label, anchoredPosition, new Vector2(136, 44));
        }

        private static Button GetOrCreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureButton(existingButton, label, anchoredPosition, size);
                return existingButton;
            }

            return CreateButton(parent, name, label, anchoredPosition, size);
        }

        private static Button CreateTopLeftButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            buttonObject.AddComponent<RectTransform>();
            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ConfigureTopLeftButton(button, label, anchoredPosition, size);
            return button;
        }

        private static Button GetOrCreateTopLeftButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                ConfigureTopLeftButton(existingButton, label, anchoredPosition, size);
                return existingButton;
            }

            return CreateTopLeftButton(parent, name, label, anchoredPosition, size);
        }

        private static void ConfigureTopLeftButton(Button button, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            ConfigureButtonVisuals(button, label, size);
        }
    }
}
