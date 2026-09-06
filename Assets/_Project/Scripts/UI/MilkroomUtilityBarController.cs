using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class MilkroomUtilityBarController : MonoBehaviour
    {
        public const string ToggleObjectName = "Milkroom Utility Toggle Button";
        public const string CollapsedNotificationBadgeObjectName =
            "Collapsed Utility Notification Badge";

        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject collapsedNotificationBadge;

        private readonly List<CanvasGroup> buttonVisibilityGroups = new List<CanvasGroup>();
        private System.Action layoutChanged;
        private int visibleButtonSignature;

        public bool IsCollapsed { get; private set; }
        public bool HasCollapsedNotification => collapsedNotificationBadge != null
            && collapsedNotificationBadge.activeSelf;

        public void Configure(
            Button collapseToggle,
            IReadOnlyList<Button> utilityButtons,
            System.Action onLayoutChanged = null)
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }

            toggleButton = collapseToggle;
            layoutChanged = onLayoutChanged;
            EnsureCollapsedNotificationBadge();
            buttonVisibilityGroups.Clear();
            if (utilityButtons != null)
            {
                for (var index = 0; index < utilityButtons.Count; index += 1)
                {
                    RegisterVisibilityGroup(utilityButtons[index]);
                }
            }

            // Repeated scene builders and unlock bridges can discover the same bar through
            // different paths. Treat the bar's direct children as the final authority so a
            // temporarily hidden launcher never escapes the side-collapse state.
            var directButtons = GetComponentsInChildren<Button>(true);
            for (var index = 0; index < directButtons.Length; index += 1)
            {
                var button = directButtons[index];
                if (button != null && button.transform.parent == transform)
                {
                    RegisterVisibilityGroup(button);
                }
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
                toggleButton.onClick.AddListener(Toggle);
            }

            ApplyState(notifyLayout: false);
            visibleButtonSignature = CalculateVisibleButtonSignature();
        }

        private void LateUpdate()
        {
            var currentSignature = CalculateVisibleButtonSignature();
            if (currentSignature == visibleButtonSignature)
            {
                return;
            }

            visibleButtonSignature = currentSignature;
            RefreshCollapsedNotificationBadge();
            layoutChanged?.Invoke();
        }

        private int CalculateVisibleButtonSignature()
        {
            unchecked
            {
                var signature = 17;
                for (var index = 0; index < buttonVisibilityGroups.Count; index += 1)
                {
                    var group = buttonVisibilityGroups[index];
                    signature = signature * 31 + (group != null ? group.GetInstanceID() : 0);
                    signature = signature * 31
                        + (group != null && group.gameObject.activeSelf ? 1 : 0);
                    signature = signature * 31
                        + (group != null && ContainsVisibleNotification(group.transform) ? 1 : 0);
                }

                return signature;
            }
        }

        private void RegisterVisibilityGroup(Button button)
        {
            if (button == null || button == toggleButton)
            {
                return;
            }

            var group = button.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = button.gameObject.AddComponent(typeof(CanvasGroup)) as CanvasGroup;
            }

            if (group == null)
            {
                return;
            }

            if (!buttonVisibilityGroups.Contains(group))
            {
                buttonVisibilityGroups.Add(group);
            }
        }

        public void SetCollapsed(bool collapsed)
        {
            if (IsCollapsed == collapsed)
            {
                ApplyState(notifyLayout: false);
                return;
            }

            IsCollapsed = collapsed;
            ApplyState(notifyLayout: true);
        }

        public void Toggle()
        {
            SetCollapsed(!IsCollapsed);
        }

        private void ApplyState(bool notifyLayout)
        {
            for (var index = 0; index < buttonVisibilityGroups.Count; index += 1)
            {
                var group = buttonVisibilityGroups[index];
                if (group == null)
                {
                    continue;
                }

                group.alpha = IsCollapsed ? 0f : 1f;
                group.interactable = !IsCollapsed;
                group.blocksRaycasts = !IsCollapsed;
            }

            var label = toggleButton != null
                ? toggleButton.transform.Find("Label")?.GetComponent<Text>()
                : null;
            if (label != null)
            {
                label.text = IsCollapsed ? ">" : "<";
                label.alignment = TextAnchor.MiddleCenter;
            }

            if (toggleButton != null)
            {
                toggleButton.gameObject.name = ToggleObjectName;
            }

            RefreshCollapsedNotificationBadge();

            if (notifyLayout)
            {
                layoutChanged?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }
        }

        private void EnsureCollapsedNotificationBadge()
        {
            if (toggleButton == null)
            {
                collapsedNotificationBadge = null;
                return;
            }

            var existing = toggleButton.transform.Find(CollapsedNotificationBadgeObjectName);
            if (existing != null)
            {
                collapsedNotificationBadge = existing.gameObject;
            }
            else
            {
                collapsedNotificationBadge = new GameObject(
                    CollapsedNotificationBadgeObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Outline));
                collapsedNotificationBadge.transform.SetParent(toggleButton.transform, false);
            }

            var badgeRect = collapsedNotificationBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = Vector2.one;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-2f, -2f);
            badgeRect.sizeDelta = new Vector2(22f, 22f);

            var badgeImage = collapsedNotificationBadge.GetComponent<Image>();
            badgeImage.color = new Color(0.92f, 0.12f, 0.1f, 1f);
            badgeImage.raycastTarget = false;
            CheeseTama.Core.StarterSceneBuilder.ApplyCircleImage(badgeImage);

            var outline = collapsedNotificationBadge.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.98f, 0.9f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
            collapsedNotificationBadge.transform.SetAsLastSibling();
            RefreshCollapsedNotificationBadge();
        }

        private void RefreshCollapsedNotificationBadge()
        {
            if (collapsedNotificationBadge == null)
            {
                return;
            }

            var hasNotification = false;
            for (var index = 0; index < buttonVisibilityGroups.Count; index += 1)
            {
                var group = buttonVisibilityGroups[index];
                if (group != null
                    && group.gameObject.activeSelf
                    && ContainsVisibleNotification(group.transform))
                {
                    hasNotification = true;
                    break;
                }
            }

            collapsedNotificationBadge.SetActive(IsCollapsed && hasNotification);
        }

        private static bool ContainsVisibleNotification(Transform root)
        {
            if (root == null)
            {
                return false;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var child = root.GetChild(index);
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                var childName = child.name;
                if (childName.IndexOf("Badge", StringComparison.OrdinalIgnoreCase) >= 0
                    || childName.IndexOf("Notification", StringComparison.OrdinalIgnoreCase) >= 0
                    || childName.IndexOf("Attention", StringComparison.OrdinalIgnoreCase) >= 0
                    || childName.IndexOf("알림", StringComparison.Ordinal) >= 0
                    || ContainsVisibleNotification(child))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
