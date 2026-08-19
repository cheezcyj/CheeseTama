using System.Runtime.InteropServices;
using UnityEngine;

namespace CheeseTama.Platform
{
    /// <summary>
    /// Describes the persistence surface that is safe to expose on the current player.
    /// Web builds use the browser's site-scoped IndexedDB storage and never expose Steam.
    /// </summary>
    public readonly struct PersistencePlatformCapabilities
    {
        public PersistencePlatformCapabilities(
            bool usesBrowserLocalStorage,
            bool requiresFileSystemSync,
            bool supportsSteamCloudUi,
            string localStorageLabel)
        {
            UsesBrowserLocalStorage = usesBrowserLocalStorage;
            RequiresFileSystemSync = requiresFileSystemSync;
            SupportsSteamCloudUi = supportsSteamCloudUi;
            LocalStorageLabel = localStorageLabel ?? string.Empty;
        }

        public bool UsesBrowserLocalStorage { get; }
        public bool RequiresFileSystemSync { get; }
        public bool SupportsSteamCloudUi { get; }
        public string LocalStorageLabel { get; }
    }

    public static class RuntimePlatformCapabilities
    {
        public const string BrowserLocalStorageLabel = "브라우저 로컬 저장";
        public const string BrowserLocalStorageNotice =
            "진행도는 이 브라우저와 현재 게임 주소에 저장됩니다. 사이트 데이터 삭제나 주소 변경 시 초기화될 수 있습니다.";
        public const string DeviceLocalStorageLabel = "기기 로컬 저장";

        public static PersistencePlatformCapabilities Current => Resolve(Application.platform);

        public static PersistencePlatformCapabilities Resolve(RuntimePlatform platform)
        {
            var isWebPlayer = platform == RuntimePlatform.WebGLPlayer;
            return new PersistencePlatformCapabilities(
                isWebPlayer,
                isWebPlayer,
                !isWebPlayer && SteamPlatformRuntime.IsSdkCompiled,
                isWebPlayer ? BrowserLocalStorageLabel : DeviceLocalStorageLabel);
        }
    }

    /// <summary>
    /// Flushes Web's in-memory virtual file system to IndexedDB. The JavaScript plug-in
    /// coalesces overlapping requests because IDBFS synchronization is asynchronous.
    /// All non-Web players intentionally treat this API as a no-op.
    /// </summary>
    public static class BrowserPersistence
    {
        public static bool RequiresExplicitSync
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void RequestSync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CheeseTamaSyncFileSystem();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CheeseTamaSyncFileSystem();
#endif
    }
}
