using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTama.Gameplay.Input
{
    [DefaultExecutionOrder(-100)]
    public sealed class KeyboardFocusScope : MonoBehaviour
    {
        private static readonly List<KeyboardFocusScope> ActiveScopes = new List<KeyboardFocusScope>();

        [SerializeField] private Transform focusRoot;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private bool modalScope = true;
        [SerializeField] private bool focusOnActivation = true;

        private GameObject previouslySelected;

        public Transform FocusRoot => focusRoot != null ? focusRoot : transform;
        public bool IsModalScope => modalScope;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ActiveScopes.Clear();
        }

        public void Configure(
            Transform root,
            bool isModalScope = true,
            bool shouldFocusOnActivation = true,
            EventSystem targetEventSystem = null)
        {
            focusRoot = root != null ? root : transform;
            eventSystem = targetEventSystem != null ? targetEventSystem : EventSystem.current;
            modalScope = isModalScope;
            focusOnActivation = shouldFocusOnActivation;
            if (!isActiveAndEnabled)
            {
                return;
            }

            // Configure is also the deterministic registration point for callers that construct
            // scopes before Play Mode (tests and editor tooling do not receive runtime Update).
            Register();
            if (focusOnActivation && CanOwnFocus())
            {
                EnsureFocusWithinScope();
            }
        }

        public bool EnsureFocusWithinScope()
        {
            return CanOwnFocus()
                && KeyboardFocusNavigation.EnsureFocusWithin(FocusRoot, ResolveEventSystem());
        }

        public bool CycleFocus(bool backwards = false)
        {
            return CanOwnFocus()
                && KeyboardFocusNavigation.TryCycle(FocusRoot, ResolveEventSystem(), backwards);
        }

        public static bool IsInteractionAllowed(GameObject candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var topModal = ResolveTopModalScope();
            return topModal == null || candidate.transform.IsChildOf(topModal.FocusRoot);
        }

        private void OnEnable()
        {
            var resolvedEventSystem = ResolveEventSystem();
            previouslySelected = resolvedEventSystem != null
                ? resolvedEventSystem.currentSelectedGameObject
                : null;
            Register();
            if (focusOnActivation && CanOwnFocus())
            {
                EnsureFocusWithinScope();
            }
        }

        private void Start()
        {
            if (focusOnActivation && CanOwnFocus())
            {
                EnsureFocusWithinScope();
            }
        }

        private void Update()
        {
            if (!CanOwnFocus())
            {
                return;
            }

            if (modalScope)
            {
                EnsureFocusWithinScope();
            }

            if (GameInputRouter.WasNextPanelPressed())
            {
                var backwards = UnityEngine.Input.GetKey(KeyCode.LeftShift)
                    || UnityEngine.Input.GetKey(KeyCode.RightShift);
                CycleFocus(backwards);
            }
        }

        private void OnDisable()
        {
            Unregister();
            RestorePreviousSelection();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            ActiveScopes.Remove(this);
            ActiveScopes.Add(this);
        }

        private void Unregister()
        {
            ActiveScopes.Remove(this);
        }

        private bool CanOwnFocus()
        {
            if (!isActiveAndEnabled || !FocusRoot.gameObject.activeInHierarchy)
            {
                return false;
            }

            var topModal = ResolveTopModalScope();
            if (topModal != null)
            {
                return ReferenceEquals(topModal, this);
            }

            var resolvedEventSystem = ResolveEventSystem();
            var current = resolvedEventSystem != null
                ? resolvedEventSystem.currentSelectedGameObject
                : null;
            for (var index = ActiveScopes.Count - 1; index >= 0; index -= 1)
            {
                var scope = ActiveScopes[index];
                if (!IsUsable(scope) || scope.modalScope)
                {
                    continue;
                }

                if (current == null || current.transform.IsChildOf(scope.FocusRoot))
                {
                    return ReferenceEquals(scope, this);
                }
            }

            return false;
        }

        private void RestorePreviousSelection()
        {
            var resolvedEventSystem = ResolveEventSystem();
            if (resolvedEventSystem != null
                && previouslySelected != null
                && previouslySelected.activeInHierarchy
                && IsInteractionAllowed(previouslySelected))
            {
                resolvedEventSystem.SetSelectedGameObject(previouslySelected);
            }

            previouslySelected = null;
        }

        private EventSystem ResolveEventSystem()
        {
            if (eventSystem == null)
            {
                eventSystem = EventSystem.current;
            }

            return eventSystem;
        }

        private static KeyboardFocusScope ResolveTopModalScope()
        {
            for (var index = ActiveScopes.Count - 1; index >= 0; index -= 1)
            {
                var scope = ActiveScopes[index];
                if (IsUsable(scope) && scope.modalScope)
                {
                    return scope;
                }
            }

            return null;
        }

        private static bool IsUsable(KeyboardFocusScope scope)
        {
            return scope != null
                && scope.isActiveAndEnabled
                && scope.FocusRoot != null
                && scope.FocusRoot.gameObject.activeInHierarchy;
        }
    }
}
