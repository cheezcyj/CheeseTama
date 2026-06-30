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
    }
}
