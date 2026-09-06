using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileWebUiCoordinator : MonoBehaviour
    {
        private bool isBrowser;
        private bool touchPreferred;
        private float nextCanvasDiscoveryAt;

        public void Configure(bool browser, bool preferTouch)
        {
            isBrowser = browser;
            touchPreferred = preferTouch;
            RefreshLoadedCanvases();
        }

        public int RefreshLoadedCanvases()
        {
            var configured = 0;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (var index = 0; index < canvases.Length; index += 1)
            {
                var canvas = canvases[index];
                if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                var runtime = canvas.GetComponent<ResponsiveCanvasRuntime>();
                if (runtime == null)
                {
                    runtime = canvas.gameObject.AddComponent<ResponsiveCanvasRuntime>();
                }

                runtime.Configure(isBrowser, touchPreferred);
                configured += 1;
            }

            nextCanvasDiscoveryAt = Time.unscaledTime + 0.75f;
            return configured;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextCanvasDiscoveryAt)
            {
                RefreshLoadedCanvases();
            }
        }
    }
}
