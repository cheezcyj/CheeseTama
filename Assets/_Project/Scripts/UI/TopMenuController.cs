using System;
using CheeseTama.Collections;
using CheeseTama.Core;
using CheeseTama.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class TopMenuController : MonoBehaviour
    {
        private const string CollectionRewardBadgeName = "Collection Reward Notification Badge";

        [SerializeField] private Button collectionButton;
        [SerializeField] private Button decorateButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button collectionCloseButton;
        [SerializeField] private Button decorateCloseButton;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private GameObject collectionOverlay;
        [SerializeField] private GameObject decorateOverlay;
        [SerializeField] private GameObject settingsModal;
        [SerializeField] private CollectionUIController collectionUi;

        private readonly CollectionSystem collectionSystem = new CollectionSystem();
        private GameObject collectionRewardNotificationBadge;
        private int observedCollectionRecordCount = -1;
        private int observedClaimedRewardCount = -1;
        private int observedCollectionFragmentBalance = -1;

        public event Action CollectionOpening;

        public void Configure(
            Button collectionOpenButton,
            Button decorateOpenButton,
            Button settingsOpenButton,
            Button collectionClose,
            Button decorateClose,
            Button settingsClose,
            GameObject collectionRoot,
            GameObject decorateRoot,
            GameObject settingsRoot,
            CollectionUIController collectionController)
        {
            collectionButton = collectionOpenButton;
            decorateButton = decorateOpenButton;
            settingsButton = settingsOpenButton;
            collectionCloseButton = collectionClose;
            decorateCloseButton = decorateClose;
            settingsCloseButton = settingsClose;
            collectionOverlay = collectionRoot;
            decorateOverlay = decorateRoot;
            settingsModal = settingsRoot;
            collectionUi = collectionController;

            WireButtons();
            CloseAll();
            RefreshCollectionRewardNotification(true);
        }

        private void OnEnable()
        {
            WireButtons();
            CloseAll();
            RefreshCollectionRewardNotification(true);
        }

        private void WireButtons()
        {
            BindButton(collectionButton, OpenCollectionPage);
            BindButton(decorateButton, OpenDecorate);
            BindButton(settingsButton, OpenSettings);
            BindButton(collectionCloseButton, CloseAll);
            BindButton(decorateCloseButton, CloseAll);
            BindButton(settingsCloseButton, CloseAll);
        }

        private void Update()
        {
            RefreshCollectionRewardNotification(false);

            if (GameInputRouter.WasPressed(GameInputActionIds.Collection))
            {
                OpenCollectionPage();
            }
            else if (GameInputRouter.WasPressed(GameInputActionIds.Decorate))
            {
                OpenDecorate();
            }
            else if (GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                CloseAll();
            }
        }

        private void RefreshCollectionRewardNotification(bool force)
        {
            if (!Application.isPlaying)
            {
                SetActive(collectionRewardNotificationBadge, false);
                return;
            }

            var saveData = GameManager.Instance != null ? GameManager.Instance.CurrentSave : null;
            if (saveData == null)
            {
                observedCollectionRecordCount = -1;
                observedClaimedRewardCount = -1;
                observedCollectionFragmentBalance = -1;
                SetActive(collectionRewardNotificationBadge, false);
                return;
            }

            saveData.EnsureRuntimeDefaults();
            var recordCount = collectionSystem.CountDiscoveredRecords(saveData.collections);
            var claimedCount = saveData.collections.claimedFragmentRewardKeys.Count;
            var fragmentBalance = saveData.economy.collectionFragments;
            if (!force
                && recordCount == observedCollectionRecordCount
                && claimedCount == observedClaimedRewardCount
                && fragmentBalance == observedCollectionFragmentBalance)
            {
                return;
            }

            observedCollectionRecordCount = recordCount;
            observedClaimedRewardCount = claimedCount;
            observedCollectionFragmentBalance = fragmentBalance;
            EnsureCollectionRewardNotificationBadge();
            var hasClaimableReward = fragmentBalance < int.MaxValue
                && collectionSystem.CountUnclaimedFragmentRewards(saveData.collections) > 0;
            SetActive(collectionRewardNotificationBadge, hasClaimableReward);
        }

        private void EnsureCollectionRewardNotificationBadge()
        {
            if (collectionButton == null)
            {
                return;
            }

            var existing = collectionButton.transform.Find(CollectionRewardBadgeName);
            if (existing != null)
            {
                collectionRewardNotificationBadge = existing.gameObject;
            }
            else
            {
                collectionRewardNotificationBadge = new GameObject(CollectionRewardBadgeName);
                collectionRewardNotificationBadge.transform.SetParent(collectionButton.transform, false);
            }

            var badgeRect = collectionRewardNotificationBadge.GetComponent<RectTransform>();
            if (badgeRect == null)
            {
                badgeRect = collectionRewardNotificationBadge.AddComponent<RectTransform>();
            }

            badgeRect.anchorMin = Vector2.one;
            badgeRect.anchorMax = Vector2.one;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(16f, 16f);

            var badgeImage = collectionRewardNotificationBadge.GetComponent<Image>();
            if (badgeImage == null)
            {
                badgeImage = collectionRewardNotificationBadge.AddComponent<Image>();
            }

            badgeImage.color = new Color(0.92f, 0.12f, 0.1f, 1f);
            badgeImage.raycastTarget = false;
            StarterSceneBuilder.ApplyCircleImage(badgeImage);

            var outline = collectionRewardNotificationBadge.GetComponent<Outline>();
            if (outline == null)
            {
                outline = collectionRewardNotificationBadge.AddComponent<Outline>();
            }

            outline.effectColor = new Color(1f, 0.92f, 0.76f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            collectionRewardNotificationBadge.transform.SetAsLastSibling();
        }

        private void OpenCollectionPage()
        {
            CloseAll();
            if (!Application.CanStreamedLevelBeLoaded(SceneNames.Collection))
            {
                Debug.LogWarning($"'{SceneNames.Collection}' 씬이 빌드 설정에 없습니다. CheeseTama > 시작 씬 빌드를 실행하세요.");
                return;
            }

            if (GameManager.Instance != null)
            {
                CollectionOpening?.Invoke();
                GameManager.Instance.SaveGame();
            }

            SceneManager.LoadScene(SceneNames.Collection);
        }

        public void OpenCollection()
        {
            OpenCollectionPage();
        }

        private void OpenDecorate()
        {
            CloseAll();
            SetActive(decorateOverlay, true);
        }

        private void OpenSettings()
        {
            CloseAll();
            SetActive(settingsModal, true);
        }

        public void CloseAll()
        {
            SetActive(collectionOverlay, false);
            SetActive(decorateOverlay, false);
            SetActive(settingsModal, false);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
