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
                var alphaKey = (KeyCode)((int)KeyCode.Alpha1 + i);
                var keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);
                if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
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
