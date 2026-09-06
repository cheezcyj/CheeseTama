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
        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            ConfigureCareButton(button, action, controller, visualController, (CookingPanelController)null);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            CookingPanelController cookingPanelController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController, cookingPanelController);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            MilkPanelController milkPanelController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController, milkPanelController);
        }

        private static void ConfigureCareButton(
            Button button,
            MilkroomCareAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController,
            SnackPanelController snackPanelController)
        {
            button.onClick.RemoveAllListeners();
            var careButton = button.GetComponent<MilkroomCareButton>();
            if (careButton == null)
            {
                careButton = button.gameObject.AddComponent<MilkroomCareButton>();
            }

            careButton.Configure(action, controller, visualController, snackPanelController);
        }

        private static void ConfigureDebugButton(
            Button button,
            DebugAction action,
            DebugUIController controller,
            CheeseTamaVisualController visualController)
        {
            button.onClick.RemoveAllListeners();
            var debugButton = button.GetComponent<DebugActionButton>();
            if (debugButton == null)
            {
                debugButton = button.gameObject.AddComponent<DebugActionButton>();
            }

            debugButton.Configure(action, controller, visualController);
        }

        private static void ConfigureDebugButton(
            Button button,
            DebugAction action,
            MilkroomUIController controller,
            CheeseTamaVisualController visualController)
        {
            button.onClick.RemoveAllListeners();
            var debugButton = button.GetComponent<DebugActionButton>();
            if (debugButton == null)
            {
                debugButton = button.gameObject.AddComponent<DebugActionButton>();
            }

            debugButton.Configure(action, controller, visualController);
        }

        private static void ConfigureNavigationButton(Button button, string targetSceneName, bool saveBeforeLoad)
        {
            button.onClick.RemoveAllListeners();
            var navigationButton = button.GetComponent<SceneNavigationButton>();
            if (navigationButton == null)
            {
                navigationButton = button.gameObject.AddComponent<SceneNavigationButton>();
            }

            navigationButton.Configure(targetSceneName, saveBeforeLoad);
        }

        private static void ConfigureGameHomeButton(Button button)
        {
            button.onClick.RemoveAllListeners();
            var navigationButton = button.GetComponent<SceneNavigationButton>();
            if (navigationButton == null)
            {
                navigationButton = button.gameObject.AddComponent<SceneNavigationButton>();
            }

            navigationButton.ConfigureGameHome(SceneNames.Boot, true);
        }

        private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition)
        {
            ConfigureButton(button, label, anchoredPosition, new Vector2(136, 44));
        }

        private static void ConfigureButton(Button button, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            ConfigureButtonVisuals(button, label, rect.sizeDelta);
        }

        private static void ConfigureButtonVisuals(Button button, string label, Vector2 size)
        {
            if (!button.TryGetComponent(out Image image))
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.96f, 0.78f, 0.35f);
            ApplyRoundedImage(image);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(0.96f, 0.78f, 0.35f);
            colors.highlightedColor = new Color(1f, 0.86f, 0.46f);
            colors.pressedColor = new Color(0.91f, 0.61f, 0.2f);
            colors.selectedColor = new Color(1f, 0.86f, 0.46f);
            colors.disabledColor = new Color(0.72f, 0.66f, 0.56f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null)
            {
                var createdLabel = CreateText(button.transform, "Label", label, 16, TextAnchor.MiddleCenter, Vector2.zero, size, true);
                ConfigureButtonLabel(createdLabel);
                return;
            }

            if (!labelTransform.TryGetComponent(out Text labelText))
            {
                labelText = labelTransform.gameObject.AddComponent<Text>();
            }

            ConfigureText(labelText, label, 16, TextAnchor.MiddleCenter, Vector2.zero, size, true);
            ConfigureButtonLabel(labelText);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null || !labelTransform.TryGetComponent(out Text labelText))
            {
                return;
            }

            labelText.text = label;
        }

        private static void SetButtonIcon(Button button, string iconId)
        {
            if (button == null)
            {
                return;
            }

            HideLegacyButtonIcons(button.transform);

            var iconTransform = button.transform.Find("Pictogram Icon");
            if (iconTransform == null)
            {
                var iconObject = new GameObject("Pictogram Icon");
                var iconRect = iconObject.AddComponent<RectTransform>();
                iconObject.transform.SetParent(button.transform, false);
                iconTransform = iconRect;
            }

            iconTransform.gameObject.SetActive(true);
            ClearButtonIconParts(iconTransform);

            var iconImage = iconTransform.GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = iconTransform.gameObject.AddComponent<Image>();
            }

            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.type = Image.Type.Simple;
            iconImage.color = Color.white;
            iconImage.sprite = LoadButtonIconSprite(iconId);

            if (iconImage.sprite == null)
            {
                iconImage.enabled = false;
                CreateButtonPictogram(iconTransform, iconId);
            }
            else
            {
                iconImage.enabled = true;
            }

            ApplyButtonIconLayout(button, iconTransform as RectTransform);
        }

        private static Sprite LoadButtonIconSprite(string iconId)
        {
            var resourceName = iconId switch
            {
                "collection" => "collection",
                "settings" => "settings",
                "milk" => "milk",
                "decorate" => "themes",
                "cook" => "cooking",
                "snack" => "snackbag",
                "play" => "playing",
                "clean" => "cleaning",
                "rest" => "resting",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(resourceName)
                ? null
                : Resources.Load<Sprite>($"UI/ButtonIcons/{resourceName}");
        }

        private static void ApplyButtonIconLayout(Button button, RectTransform iconRect)
        {
            if (button == null || iconRect == null)
            {
                return;
            }

            var buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect == null)
            {
                return;
            }

            var isTopMenuButton = string.Equals(
                    button.name,
                    "Top Collection Button",
                    System.StringComparison.Ordinal)
                || string.Equals(
                    button.name,
                    "Top Decorate Button",
                    System.StringComparison.Ordinal)
                || string.Equals(
                    button.name,
                    "Settings Button",
                    System.StringComparison.Ordinal);
            var isCompactTopMenuButton = isTopMenuButton || buttonRect.sizeDelta.x <= 122f;
            var iconSize = isCompactTopMenuButton ? 38f : 42f;
            var leftPadding = isCompactTopMenuButton ? 7f : 8f;
            var labelGap = isCompactTopMenuButton ? 3f : 4f;
            var rightPadding = isCompactTopMenuButton ? 6f : 8f;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(leftPadding, 0f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null || !labelTransform.TryGetComponent(out Text label))
            {
                return;
            }

            var labelRect = label.GetComponent<RectTransform>();
            if (labelRect == null)
            {
                return;
            }

            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new Vector2(leftPadding + iconSize + labelGap, 4f);
            labelRect.offsetMax = new Vector2(-rightPadding, -4f);
            label.alignment = TextAnchor.MiddleCenter;
        }

        private static void HideLegacyButtonIcons(Transform buttonTransform)
        {
            if (buttonTransform == null)
            {
                return;
            }

            try
            {
                var legacyIcon = buttonTransform.Find("Icon");
                if (legacyIcon != null)
                {
                    legacyIcon.gameObject.SetActive(false);
                }
            }
            catch (MissingReferenceException)
            {
                // Legacy generated icon was already destroyed by a previous editor rebuild.
            }
        }

        private static void ClearButtonIconParts(Transform iconTransform)
        {
            for (var i = iconTransform.childCount - 1; i >= 0; i -= 1)
            {
                var child = iconTransform.GetChild(i);
                if (child != null && child.name.StartsWith("Icon Part"))
                {
                    DestroyObjectSafely(child.gameObject);
                }
            }
        }

        private static void CreateButtonPictogram(Transform iconTransform, string iconId)
        {
            var color = new Color(0.36f, 0.20f, 0.07f);
            switch (iconId)
            {
                case "collection":
                    CreateIconPart(iconTransform, "Icon Part Book Left Page", new Vector2(-5.5f, -1f), new Vector2(10f, 20f), color);
                    CreateIconPart(iconTransform, "Icon Part Book Right Page", new Vector2(5.5f, -1f), new Vector2(10f, 20f), color);
                    CreateIconPart(iconTransform, "Icon Part Book Spine", new Vector2(0f, -1f), new Vector2(2f, 21f), new Color(1f, 0.76f, 0.27f));
                    CreateIconPart(iconTransform, "Icon Part Book Top Fold", new Vector2(0f, 9f), new Vector2(19f, 2f), color);
                    CreateIconPart(iconTransform, "Icon Part Book Left Line 1", new Vector2(-5f, 3.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Book Left Line 2", new Vector2(-5f, -2.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Book Right Line 1", new Vector2(5f, 3.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Book Right Line 2", new Vector2(5f, -2.5f), new Vector2(5f, 1.8f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "decorate":
                    CreateIconPart(iconTransform, "Icon Part Brush Handle", new Vector2(-1f, -2f), new Vector2(4f, 22f), color, -32f);
                    CreateIconPart(iconTransform, "Icon Part Brush Tip", new Vector2(7f, 7f), new Vector2(9f, 6f), new Color(1f, 0.86f, 0.48f), -32f);
                    CreateIconPart(iconTransform, "Icon Part Spark 1", new Vector2(-8f, 8f), new Vector2(3f, 9f), color);
                    CreateIconPart(iconTransform, "Icon Part Spark 2", new Vector2(-8f, 8f), new Vector2(9f, 3f), color);
                    break;
                case "settings":
                    CreateIconPart(iconTransform, "Icon Part Gear Core", new Vector2(0f, 0f), new Vector2(13f, 13f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth N", new Vector2(0f, 11f), new Vector2(5f, 6f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth S", new Vector2(0f, -11f), new Vector2(5f, 6f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth W", new Vector2(-11f, 0f), new Vector2(6f, 5f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth E", new Vector2(11f, 0f), new Vector2(6f, 5f), color);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth NW", new Vector2(-7.5f, 7.5f), new Vector2(5f, 5f), color, 45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth NE", new Vector2(7.5f, 7.5f), new Vector2(5f, 5f), color, -45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth SW", new Vector2(-7.5f, -7.5f), new Vector2(5f, 5f), color, -45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Tooth SE", new Vector2(7.5f, -7.5f), new Vector2(5f, 5f), color, 45f);
                    CreateIconPart(iconTransform, "Icon Part Gear Hole", new Vector2(0f, 0f), new Vector2(5.5f, 5.5f), new Color(1f, 0.75f, 0.24f));
                    break;
                case "milk":
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Body", new Vector2(0f, -3f), new Vector2(16f, 20f), color);
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Roof Left", new Vector2(-4f, 9f), new Vector2(9f, 8f), color, -28f);
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Roof Right", new Vector2(4f, 9f), new Vector2(9f, 8f), color, 28f);
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Fold", new Vector2(0f, 7f), new Vector2(2f, 9f), new Color(1f, 0.76f, 0.27f));
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Label", new Vector2(0f, -4f), new Vector2(10f, 6f), new Color(1f, 0.89f, 0.56f));
                    CreateIconPart(iconTransform, "Icon Part Milk Carton Stripe", new Vector2(0f, 0f), new Vector2(10f, 2f), new Color(1f, 0.76f, 0.27f));
                    break;
                case "cook":
                    CreateIconPart(iconTransform, "Icon Part Pot Body", new Vector2(0f, -3f), new Vector2(19f, 12f), color);
                    CreateIconPart(iconTransform, "Icon Part Pot Lid", new Vector2(0f, 5f), new Vector2(14f, 3f), color);
                    CreateIconPart(iconTransform, "Icon Part Pot Handle L", new Vector2(-12f, -3f), new Vector2(4f, 7f), color);
                    CreateIconPart(iconTransform, "Icon Part Pot Handle R", new Vector2(12f, -3f), new Vector2(4f, 7f), color);
                    CreateIconPart(iconTransform, "Icon Part Steam 1", new Vector2(-5f, 12f), new Vector2(3f, 8f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Steam 2", new Vector2(4f, 13f), new Vector2(3f, 7f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "snack":
                    CreateIconPart(iconTransform, "Icon Part Bag Body", new Vector2(0f, -2f), new Vector2(18f, 19f), color);
                    CreateIconPart(iconTransform, "Icon Part Bag Fold", new Vector2(0f, 8f), new Vector2(14f, 4f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Snack Dot 1", new Vector2(-4f, -2f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Snack Dot 2", new Vector2(4f, -2f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "play":
                    CreateIconPart(iconTransform, "Icon Part Basketball Border", Vector2.zero, new Vector2(22f, 22f), color, 0f, true);
                    CreateIconPart(iconTransform, "Icon Part Basketball Fill", Vector2.zero, new Vector2(17f, 17f), new Color(1f, 0.82f, 0.38f), 0f, true);
                    CreateIconPart(iconTransform, "Icon Part Basketball Horizontal", Vector2.zero, new Vector2(17f, 2f), color);
                    CreateIconPart(iconTransform, "Icon Part Basketball Vertical", Vector2.zero, new Vector2(2f, 17f), color);
                    CreateIconPart(iconTransform, "Icon Part Basketball Left Seam", new Vector2(-5f, 0f), new Vector2(2f, 16f), color, -18f);
                    CreateIconPart(iconTransform, "Icon Part Basketball Right Seam", new Vector2(5f, 0f), new Vector2(2f, 16f), color, 18f);
                    break;
                case "clean":
                    CreateIconPart(iconTransform, "Icon Part Spray Body", new Vector2(-2f, -4f), new Vector2(12f, 16f), color);
                    CreateIconPart(iconTransform, "Icon Part Spray Neck", new Vector2(1f, 6f), new Vector2(6f, 6f), color);
                    CreateIconPart(iconTransform, "Icon Part Spray Nozzle", new Vector2(7f, 10f), new Vector2(10f, 4f), color);
                    CreateIconPart(iconTransform, "Icon Part Spray Dot 1", new Vector2(12f, 3f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Spray Dot 2", new Vector2(15f, -3f), new Vector2(3f, 3f), new Color(1f, 0.86f, 0.48f));
                    break;
                case "rest":
                    CreateIconPart(iconTransform, "Icon Part Bed Base", new Vector2(0f, -5f), new Vector2(22f, 7f), color);
                    CreateIconPart(iconTransform, "Icon Part Bed Back", new Vector2(-10f, 1f), new Vector2(4f, 14f), color);
                    CreateIconPart(iconTransform, "Icon Part Pillow", new Vector2(-3f, 2f), new Vector2(8f, 5f), new Color(1f, 0.86f, 0.48f));
                    CreateIconPart(iconTransform, "Icon Part Moon", new Vector2(8f, 7f), new Vector2(8f, 8f), color);
                    CreateIconPart(iconTransform, "Icon Part Moon Cut", new Vector2(11f, 8f), new Vector2(6f, 7f), new Color(1f, 0.75f, 0.24f));
                    break;
                default:
                    CreateIconPart(iconTransform, "Icon Part Default", Vector2.zero, new Vector2(16f, 16f), color);
                    break;
            }
        }

        private static void CreateIconPart(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color,
            float rotation = 0f,
            bool circle = false)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            var rect = part.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);

            var image = part.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (circle)
            {
                ApplyCircleImage(image);
            }
            else
            {
                ApplyRoundedImage(image);
            }
        }

        private static void ConfigureButtonLabel(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.color = new Color(0.31f, 0.22f, 0.14f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 16;
        }

        private static void ApplyRecordSectionStyle(GameObject section, Color color)
        {
            if (section == null)
            {
                return;
            }

            section.transform.SetAsFirstSibling();
            if (section.TryGetComponent(out Image image))
            {
                image.color = color;
                image.raycastTarget = false;
                ApplyRoundedImage(image);
            }
        }

        private static Text GetOrCreateRecordText(
            Transform section,
            string name,
            string text,
            int fontSize,
            TextAnchor alignment,
            Vector2 size)
        {
            var viewport = section != null ? section.Find("Record Text Viewport") : null;
            var nested = viewport != null
                ? (viewport.Find($"Record Text Content/{name}") ?? viewport.Find(name))?.GetComponent<Text>()
                : null;
            if (nested != null)
            {
                ConfigureText(nested, text, fontSize, alignment, new Vector2(0f, -4f), size, false);
                return nested;
            }

            return GetOrCreateText(
                section,
                name,
                text,
                fontSize,
                alignment,
                new Vector2(10f, -18f),
                size);
        }

        private static void ApplyRecordLineStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.supportRichText = true;
            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.25f, 0.17f, 0.09f);
            label.lineSpacing = 1.04f;
        }

        private static void ApplyCollectionRecordTextStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.supportRichText = true;
            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.25f, 0.17f, 0.09f);
            label.lineSpacing = 1.18f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void ApplyTopInfoTextStyle(Text label, int maxFontSize)
        {
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.23f, 0.14f, 0.07f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(12, maxFontSize - 12);
            label.resizeTextMaxSize = maxFontSize;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ApplyTopSessionTextStyle(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.23f, 0.14f, 0.07f);
            label.fontSize = 15;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 15;
            label.lineSpacing = 1.36f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ApplyTopMenuButtonStyle(Button button)
        {
            ApplyReadableButtonStyle(
                button,
                new Color(1f, 0.75f, 0.24f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.88f, 0.53f, 0.13f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.26f, 0.16f, 0.08f),
                TopMenuLabelMaxFontSize,
                TopMenuLabelMinFontSize);
        }

        private static void ApplyCareButtonStyle(Button button)
        {
            ApplyReadableButtonStyle(
                button,
                new Color(1f, 0.75f, 0.24f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.88f, 0.53f, 0.13f),
                new Color(1f, 0.86f, 0.39f),
                new Color(0.26f, 0.16f, 0.08f),
                21,
                15);
        }

        private static void ApplyCollectionTabButtonStyle(params Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (var button in buttons)
            {
                ApplyReadableButtonStyle(
                    button,
                    new Color(1f, 0.9f, 0.62f, 0.88f),
                    new Color(1f, 0.84f, 0.36f),
                    new Color(0.88f, 0.53f, 0.13f),
                    new Color(1f, 0.74f, 0.24f),
                    new Color(0.26f, 0.16f, 0.08f),
                    17,
                    12);
            }
        }

        private static void ApplyCookingRecipeButtonStyle(params Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (var button in buttons)
            {
                ApplyReadableButtonStyle(
                    button,
                    new Color(1f, 0.87f, 0.54f, 0.96f),
                    new Color(1f, 0.92f, 0.66f, 1f),
                    new Color(0.95f, 0.68f, 0.28f, 1f),
                    new Color(1f, 0.78f, 0.30f, 1f),
                    new Color(0.18f, 0.10f, 0.04f),
                    16,
                    11);
            }
        }

        private static void ApplyReadableButtonStyle(
            Button button,
            Color normal,
            Color highlighted,
            Color pressed,
            Color selected,
            Color labelColor,
            int labelMaxFontSize,
            int labelMinFontSize)
        {
            if (button == null)
            {
                return;
            }

            if (!button.TryGetComponent(out Image image))
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = normal;
            ApplyRoundedImage(image);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = selected;
            colors.disabledColor = new Color(0.72f, 0.66f, 0.56f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform == null || !labelTransform.TryGetComponent(out Text label))
            {
                return;
            }

            var labelRect = label.GetComponent<RectTransform>();
            var buttonRect = button.GetComponent<RectTransform>();
            if (labelRect != null && buttonRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.offsetMin = new Vector2(10f, 4f);
                labelRect.offsetMax = new Vector2(-10f, -4f);
            }

            label.fontStyle = FontStyle.Bold;
            label.color = labelColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = labelMinFontSize;
            label.resizeTextMaxSize = labelMaxFontSize;
        }

        private static void ApplyDangerButtonStyle(Button button)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.84f, 0.35f, 0.29f);
                ApplyRoundedImage(image);
            }

            var colors = button.colors;
            colors.normalColor = new Color(0.84f, 0.35f, 0.29f);
            colors.highlightedColor = new Color(0.95f, 0.45f, 0.38f);
            colors.pressedColor = new Color(0.68f, 0.24f, 0.2f);
            colors.selectedColor = new Color(0.95f, 0.45f, 0.38f);
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
            {
                label.color = new Color(1f, 0.96f, 0.9f);
            }
        }

        private static void ReapplyRoundedImages(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var images = root.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (ShouldUseRoundedRuntimeStyle(image))
                {
                    ApplyRoundedImage(image);
                }
            }
        }

        private static bool ShouldUseRoundedRuntimeStyle(Image image)
        {
            if (image == null)
            {
                return false;
            }

            var target = image.gameObject;
            if (target.GetComponent<Button>() != null)
            {
                return true;
            }

            var name = target.name;
            return name.Contains("Panel")
                || name.Contains("Bar")
                || name.Contains("Modal")
                || name.Contains("Dialog")
                || name.Contains("Menu")
                || name.Contains("View")
                || name.Contains("Row")
                || name.Contains("Section");
        }

        internal static void ApplyRoundedImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetRoundedUiSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
        }

        internal static void ApplyCircleImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetCircleUiSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.pixelsPerUnitMultiplier = 1f;
        }

        internal static void ApplyRingImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetRingUiSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.pixelsPerUnitMultiplier = 1f;
        }

        private static Sprite GetRoundedUiSprite()
        {
            if (roundedUiSprite != null)
            {
                return roundedUiSprite;
            }

            roundedUiTexture = new Texture2D(RoundedUiSpriteSize, RoundedUiSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "CheeseTama Rounded UI Sprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < RoundedUiSpriteSize; y++)
            {
                for (var x = 0; x < RoundedUiSpriteSize; x++)
                {
                    var alpha = GetRoundedRectAlpha(x, y);
                    roundedUiTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            roundedUiTexture.Apply(false, false);

            var border = new Vector4(RoundedUiCornerRadius, RoundedUiCornerRadius, RoundedUiCornerRadius, RoundedUiCornerRadius);
            roundedUiSprite = Sprite.Create(
                roundedUiTexture,
                new Rect(0, 0, RoundedUiSpriteSize, RoundedUiSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            roundedUiSprite.name = "CheeseTama Rounded UI Sprite";
            roundedUiSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedUiSprite;
        }

        private static Sprite GetCircleUiSprite()
        {
            if (circleUiSprite != null)
            {
                return circleUiSprite;
            }

            circleUiTexture = new Texture2D(RoundedUiSpriteSize, RoundedUiSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "CheeseTama Circle UI Sprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((RoundedUiSpriteSize - 1) * 0.5f, (RoundedUiSpriteSize - 1) * 0.5f);
            var radius = RoundedUiSpriteSize * 0.5f - 1f;
            for (var y = 0; y < RoundedUiSpriteSize; y++)
            {
                for (var x = 0; x < RoundedUiSpriteSize; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    circleUiTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            circleUiTexture.Apply(false, false);
            circleUiSprite = Sprite.Create(
                circleUiTexture,
                new Rect(0, 0, RoundedUiSpriteSize, RoundedUiSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f);
            circleUiSprite.name = "CheeseTama Circle UI Sprite";
            circleUiSprite.hideFlags = HideFlags.HideAndDontSave;
            return circleUiSprite;
        }

        private static Sprite GetRingUiSprite()
        {
            if (ringUiSprite != null)
            {
                return ringUiSprite;
            }

            ringUiTexture = new Texture2D(RingUiSpriteSize, RingUiSpriteSize, TextureFormat.RGBA32, false)
            {
                name = "CheeseTama Ring UI Texture",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = new Vector2((RingUiSpriteSize - 1) * 0.5f, (RingUiSpriteSize - 1) * 0.5f);
            var outerRadius = RingUiSpriteSize * 0.5f - 1f;
            var innerRadius = outerRadius - RingUiThickness;
            for (var y = 0; y < RingUiSpriteSize; y++)
            {
                for (var x = 0; x < RingUiSpriteSize; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var outerAlpha = Mathf.Clamp01(outerRadius + 0.5f - distance);
                    var innerAlpha = Mathf.Clamp01(distance - innerRadius + 0.5f);
                    var alpha = Mathf.Min(outerAlpha, innerAlpha);
                    ringUiTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            ringUiTexture.Apply(false, false);
            ringUiSprite = Sprite.Create(
                ringUiTexture,
                new Rect(0, 0, RingUiSpriteSize, RingUiSpriteSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            ringUiSprite.name = "CheeseTama Ring UI Sprite";
            ringUiSprite.hideFlags = HideFlags.HideAndDontSave;
            return ringUiSprite;
        }

        private static float GetRoundedRectAlpha(int x, int y)
        {
            var radius = RoundedUiCornerRadius;
            var size = RoundedUiSpriteSize;
            var centerX = Mathf.Clamp(x, radius, size - radius - 1);
            var centerY = Mathf.Clamp(y, radius, size - radius - 1);
            var distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
            return Mathf.Clamp01(radius + 0.5f - distance);
        }

        private static Font GetDefaultFont()
        {
            return KoreanUiFontRuntime.GetDefaultFont();
        }
    }
}
