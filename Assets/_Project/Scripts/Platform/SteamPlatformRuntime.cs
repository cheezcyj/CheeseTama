using UnityEngine;

#if STEAMWORKS_NET && !UNITY_WEBGL
using Steamworks;
#endif

namespace CheeseTama.Platform
{
    public enum SteamPlatformRuntimeStatus
    {
        NotInitialized = 0,
        SdkUnavailable = 1,
        Initialized = 2,
        InitializationFailed = 3,
        ShutDown = 4
    }

    /// <summary>
    /// Optional Steamworks.NET lifecycle. The source contains no AppID; Playtest/production
    /// identifiers remain external platform configuration.
    /// </summary>
    public static class SteamPlatformRuntime
    {
        private static SteamPlatformRuntimeStatus status;
        private static string message = string.Empty;
#if STEAMWORKS_NET && !UNITY_WEBGL
        private static bool steamApiInitialized;
#endif

        public static SteamPlatformRuntimeStatus Status => status;
        public static string Message => message;
        public static bool IsInitialized => status == SteamPlatformRuntimeStatus.Initialized;
        public static bool IsSdkCompiled
        {
            get
            {
#if STEAMWORKS_NET && !UNITY_WEBGL
                return true;
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            status = SteamPlatformRuntimeStatus.NotInitialized;
            message = string.Empty;
#if STEAMWORKS_NET && !UNITY_WEBGL
            steamApiInitialized = false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            EnsureInitialized();
        }

        public static bool EnsureInitialized()
        {
            if (status == SteamPlatformRuntimeStatus.Initialized)
            {
                return true;
            }

            if (status == SteamPlatformRuntimeStatus.InitializationFailed
                || status == SteamPlatformRuntimeStatus.SdkUnavailable
                || status == SteamPlatformRuntimeStatus.ShutDown)
            {
                return false;
            }

#if STEAMWORKS_NET && !UNITY_WEBGL
            try
            {
                if (!SteamAPI.Init())
                {
                    status = SteamPlatformRuntimeStatus.InitializationFailed;
                    message = "Steamworks initialization was rejected. Local-only mode remains active.";
                    return false;
                }

                steamApiInitialized = true;
                status = SteamPlatformRuntimeStatus.Initialized;
                message = string.Empty;
                var host = new GameObject("CheeseTama Steam Platform Runtime");
                Object.DontDestroyOnLoad(host);
                host.AddComponent<SteamCallbackPump>();
                return true;
            }
            catch
            {
                ShutdownSteamApi();
                status = SteamPlatformRuntimeStatus.InitializationFailed;
                message = "Steamworks initialization failed. Local-only mode remains active.";
                return false;
            }
#else
            status = SteamPlatformRuntimeStatus.SdkUnavailable;
#if UNITY_WEBGL
            message = "Steam integration is unavailable in Web builds; browser-local saving remains active.";
#else
            message = "Steamworks.NET is not installed; local-only mode remains active.";
#endif
            return false;
#endif
        }

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

        private static void ShutdownSteamApi()
        {
            if (!steamApiInitialized)
            {
                return;
            }

            try
            {
                SteamAPI.Shutdown();
            }
            catch
            {
                // Local saving remains authoritative even if native shutdown fails.
            }
            finally
            {
                steamApiInitialized = false;
            }
        }
#endif
    }
}
