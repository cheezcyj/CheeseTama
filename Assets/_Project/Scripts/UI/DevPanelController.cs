using UnityEngine;

namespace CheeseTama.UI
{
    public sealed class DevPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;

        public void Configure(GameObject root)
        {
            panelRoot = root;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
#else
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            enabled = false;
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (panelRoot != null && Input.GetKeyDown(KeyCode.F12))
            {
                panelRoot.SetActive(!panelRoot.activeSelf);
            }
#endif
        }
    }
}
