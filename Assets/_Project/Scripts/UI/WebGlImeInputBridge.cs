using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Routes browser composition input (including Korean IME) into a legacy Unity InputField.
    /// The hidden DOM input is only active while this field owns UI focus.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WebGlImeInputBridge : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        IPointerClickHandler
    {
        [SerializeField] private InputField inputField;

        public void Configure(InputField target)
        {
            inputField = target;
        }

        public void OnSelect(BaseEventData eventData)
        {
            OpenBrowserInput();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OpenBrowserInput();
        }

        private void OpenBrowserInput()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (inputField == null || !inputField.isActiveAndEnabled)
            {
                return;
            }

            WebGLInput.captureAllKeyboardInput = false;
            CheeseTamaOpenImeInput(gameObject.name, inputField.text ?? string.Empty);
#endif
        }

        public void OnDeselect(BaseEventData eventData)
        {
            CloseBrowserInput();
        }

        private void OnDisable()
        {
            CloseBrowserInput();
        }

        public void ApplyWebGlImeText(string value)
        {
            if (inputField == null)
            {
                return;
            }

            inputField.text = value ?? string.Empty;
            inputField.caretPosition = inputField.text.Length;
        }

        public void ReleaseWebGlImeCapture(string unused)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }

        public void HandleWebGlImeEscape(string unused)
        {
            inputField?.DeactivateInputField();
            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            GetComponentInParent<ConfirmResetDialog>()?.Close();
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }

        private void CloseBrowserInput()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CheeseTamaCloseImeInput(gameObject.name);
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CheeseTamaOpenImeInput(
            string gameObjectName,
            string initialValue);

        [DllImport("__Internal")]
        private static extern void CheeseTamaCloseImeInput(string gameObjectName);
#endif
    }
}
