using CheeseTama.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class TopMenuController : MonoBehaviour
    {
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

            RemoveSceneNavigation(collectionButton);
            BindButton(collectionButton, OpenCollection);
            BindButton(decorateButton, OpenDecorate);
            BindButton(settingsButton, OpenSettings);
            BindButton(collectionCloseButton, CloseAll);
            BindButton(decorateCloseButton, CloseAll);
            BindButton(settingsCloseButton, CloseAll);

            CloseAll();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                OpenCollection();
            }
            else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.D))
            {
                OpenDecorate();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseAll();
            }
        }

        private void OpenCollection()
        {
            CloseAll();
            RefreshCollection();
            SetActive(collectionOverlay, true);
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

        private void CloseAll()
        {
            SetActive(collectionOverlay, false);
            SetActive(decorateOverlay, false);
            SetActive(settingsModal, false);
        }

        private void RefreshCollection()
        {
            if (collectionUi == null)
            {
                return;
            }

            var manager = StarterSceneBuilder.EnsureCoreSystems();
            manager.RefreshDerivedCollectionRecords();
            collectionUi.Bind(manager.CurrentSave);
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

        private static void RemoveSceneNavigation(Button button)
        {
            if (button == null)
            {
                return;
            }

            var navigation = button.GetComponent<SceneNavigationButton>();
            if (navigation == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(navigation);
            }
            else
            {
                DestroyImmediate(navigation);
            }
        }
    }
}
