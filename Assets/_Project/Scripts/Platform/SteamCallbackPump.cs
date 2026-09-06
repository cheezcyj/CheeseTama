using UnityEngine;

#if STEAMWORKS_NET && !UNITY_WEBGL
using Steamworks;
#endif

namespace CheeseTama.Platform
{
    public static partial class SteamPlatformRuntime
    {
#if STEAMWORKS_NET && !UNITY_WEBGL
        private sealed class SteamCallbackPump : MonoBehaviour
        {
            private void Update()
            {
                if (!IsInitialized)
                {
                    return;
                }

                try
                {
                    SteamAPI.RunCallbacks();
                }
                catch
                {
                    ShutdownSteamApi();
                    status = SteamPlatformRuntimeStatus.InitializationFailed;
                    message = "Steam callback processing failed. Local saving remains available.";
                }
            }

            private void OnApplicationQuit()
            {
                if (!steamApiInitialized)
                {
                    return;
                }

                ShutdownSteamApi();
                status = SteamPlatformRuntimeStatus.ShutDown;
            }
        }
#endif
    }
}
