using CheeseTama.Gameplay.Bond;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Small optional status card for the derived bond profile. The panel shows
    /// preferences as flavor only and exposes no penalties or hidden conditions.
    /// </summary>
    public sealed class BondStatusPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text relationshipText;
        [SerializeField] private Text traitText;
        [SerializeField] private Text preferenceText;
        [SerializeField] private Button entryButton;
        [SerializeField] private Button closeButton;

        private BondProfileSnapshot profile;
        private GameObject previouslySelectedObject;
        private CheeseTamaProfileMenuController nestedProfileController;
        private bool openedFromProfile;

        public BondProfileSnapshot Profile => profile;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Configure(
            GameObject root,
            Text relationshipLabel,
            Text traitLabel,
            Text preferenceLabel,
            Button openButton,
            Button panelCloseButton)
        {
            UnbindButtons();
            panelRoot = root;
            relationshipText = relationshipLabel;
            traitText = traitLabel;
            preferenceText = preferenceLabel;
            entryButton = openButton;
            closeButton = panelCloseButton;
            BindButtons();
            Render();
            Close();
        }

        public void Bind(BondProfileSnapshot snapshot)
        {
            profile = snapshot;
            Render();
            if (profile == null)
            {
                Close();
            }
        }

        public bool Open()
        {
            if (profile == null || panelRoot == null)
            {
                return false;
            }

            previouslySelectedObject = EventSystem.current?.currentSelectedGameObject;
            var profileMenu = UnityEngine.Object.FindFirstObjectByType<CheeseTamaProfileMenuController>();
            openedFromProfile = profileMenu != null
                && profileMenu.BeginNestedChildNavigation(ProfileChildPage.BondStatus);
            nestedProfileController = openedFromProfile ? profileMenu : null;
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            Render();
            EventSystem.current?.SetSelectedGameObject(closeButton?.gameObject);
            return true;
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            var profileMenu = nestedProfileController;
            var shouldReturnToProfile = openedFromProfile;
            nestedProfileController = null;
            openedFromProfile = false;
            if (shouldReturnToProfile)
            {
                profileMenu?.ReturnFromNestedChildNavigation(ProfileChildPage.BondStatus);
            }
            else if (EventSystem.current != null
                     && previouslySelectedObject != null
                     && previouslySelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelectedObject);
            }

            previouslySelectedObject = null;
        }

        private void Update()
        {
            if (IsOpen
                && CheeseTama.Gameplay.Input.GameInputRouter.WasPressed(
                    CheeseTama.Gameplay.Input.GameInputActionIds.Cancel))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void BindButtons()
        {
            entryButton?.onClick.RemoveListener(HandleOpenClicked);
            entryButton?.onClick.AddListener(HandleOpenClicked);
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
        }

        private void UnbindButtons()
        {
            entryButton?.onClick.RemoveListener(HandleOpenClicked);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void HandleOpenClicked()
        {
            Open();
        }

        private void Render()
        {
            if (entryButton != null)
            {
                entryButton.gameObject.SetActive(profile != null);
            }

            SetText(
                relationshipText,
                profile != null
                    ? $"{profile.RelationshipTitle} · 애정 {profile.Affection}"
                    : string.Empty);
            SetText(
                traitText,
                profile != null
                    ? $"성향  {profile.TraitDisplayName}"
                    : string.Empty);
            SetText(preferenceText, profile?.PreferenceDescription ?? string.Empty);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
