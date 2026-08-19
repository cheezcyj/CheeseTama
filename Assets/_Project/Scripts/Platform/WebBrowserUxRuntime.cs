using System.Runtime.InteropServices;
using UnityEngine;

namespace CheeseTama.Platform
{
    public readonly struct WebBrowserUxCapabilities
    {
        public WebBrowserUxCapabilities(
            bool capturesAllKeyboardInput,
            bool blocksCanvasContextMenu,
            bool escapeMayBeConsumedByBrowser)
        {
            CapturesAllKeyboardInput = capturesAllKeyboardInput;
            BlocksCanvasContextMenu = blocksCanvasContextMenu;
            EscapeMayBeConsumedByBrowser = escapeMayBeConsumedByBrowser;
        }

        public bool CapturesAllKeyboardInput { get; }
        public bool BlocksCanvasContextMenu { get; }
        public bool EscapeMayBeConsumedByBrowser { get; }
    }

    /// <summary>
    /// Installs browser-only canvas behavior without adding scene or prefab dependencies.
    /// The JavaScript hook is idempotent so this remains safe across scene loads.
    /// </summary>
    public static class WebBrowserUxRuntime
    {
        public static WebBrowserUxCapabilities Current => Resolve(Application.platform);

        public static WebBrowserUxCapabilities Resolve(RuntimePlatform platform)
        {
            var isWebPlayer = platform == RuntimePlatform.WebGLPlayer;
            return new WebBrowserUxCapabilities(
                isWebPlayer,
                isWebPlayer,
                isWebPlayer);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Unity Web captures all page keyboard input by default. Keep that default so
            // Tab and Space remain game controls without requiring WebGLModule types here.
            CheeseTamaInstallBrowserUx();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CheeseTamaInstallBrowserUx();
#endif
    }
}
