using CheeseTama.Gameplay.Input;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class BottomActionBarController : MonoBehaviour
    {
        [SerializeField] private Button[] actionButtons;

        public void Configure(params Button[] buttons)
        {
            actionButtons = buttons;
            RefreshInteractableState();
        }

        public void RefreshInteractableState()
        {
            if (actionButtons == null)
            {
                return;
            }

            foreach (var button in actionButtons)
            {
                if (button != null)
                {
                    button.interactable = true;
                }
            }
        }

        private void Update()
        {
            if (actionButtons == null)
            {
                return;
            }

            for (var i = 0; i < actionButtons.Length && i < 6; i += 1)
            {
                var actionId = i switch
                {
                    0 => GameInputActionIds.Care1,
                    1 => GameInputActionIds.Care2,
                    2 => GameInputActionIds.Care3,
                    3 => GameInputActionIds.Care4,
                    4 => GameInputActionIds.Care5,
                    _ => GameInputActionIds.Care6
                };
                if (GameInputRouter.WasPressed(actionId))
                {
                    var button = actionButtons[i];
                    if (button != null && button.interactable && button.gameObject.activeInHierarchy)
                    {
                        button.onClick.Invoke();
                    }
                }
            }
        }
    }
}
