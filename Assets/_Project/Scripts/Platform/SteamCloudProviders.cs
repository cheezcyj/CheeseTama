using System.Text;
using UnityEngine;

#if STEAMWORKS_NET && !UNITY_WEBGL
using Steamworks;
#endif

namespace CheeseTama.Platform
{
    /// <summary>
    /// Keeps the existing local save authoritative when no platform cloud SDK is available.
    /// It intentionally performs no file I/O because local persistence remains SaveManager's responsibility.
    /// </summary>
    public sealed class LocalOnlyCloudSaveProvider : ICloudSaveProvider
    {
        public string ProviderName => "LocalOnly";
        public CloudProviderAvailability Availability => CloudProviderAvailability.Offline;

        public CloudTransferResult Upload(CloudSavePayload payload)
        {
            return CloudTransferResult.Offline("Steam Cloud is unavailable; the local save was preserved.");
        }

        public CloudTransferResult Download(string slotId)
        {
            return CloudTransferResult.Offline("Steam Cloud is unavailable; the local save was preserved.");
        }
    }

    public static class SteamCloudProviderFactory
    {
        public static ICloudSaveProvider CreateDefault()
        {
#if STEAMWORKS_NET && !UNITY_WEBGL
            return new SteamworksCloudSaveProvider();
#else
            return new LocalOnlyCloudSaveProvider();
#endif
        }
    }

#if STEAMWORKS_NET && !UNITY_WEBGL
    /// <summary>
    /// Thin Steamworks.NET adapter. The project only compiles this branch when the SDK assembly
    /// is installed and STEAMWORKS_NET is explicitly defined; no AppID is stored in source.
    /// </summary>
    public sealed class SteamworksCloudSaveProvider : ICloudSaveProvider
    {
        private const int MaximumCloudPayloadBytes = 8 * 1024 * 1024;

        public string ProviderName => "Steamworks.NET";

        public CloudProviderAvailability Availability
        {
            get
            {
                try
                {
                    return SteamPlatformRuntime.EnsureInitialized()
                        && SteamAPI.IsSteamRunning()
                        ? CloudProviderAvailability.Available
                        : CloudProviderAvailability.Offline;
                }
                catch
                {
                    return CloudProviderAvailability.Offline;
                }
            }
        }

        public CloudTransferResult Upload(CloudSavePayload payload)
        {
            if (Availability != CloudProviderAvailability.Available)
            {
                return CloudTransferResult.Offline();
            }

            if (payload == null || !payload.IsValid())
            {
                return CloudTransferResult.InvalidData("Cloud payload is invalid.");
            }

            try
            {
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
                if (bytes.Length > MaximumCloudPayloadBytes)
                {
                    return CloudTransferResult.Failed("Cloud payload exceeds the adapter size limit.");
                }

                return SteamRemoteStorage.FileWrite(GetRemoteFileName(payload.slotId), bytes, bytes.Length)
                    ? CloudTransferResult.Success()
                    : CloudTransferResult.Failed("Steam Cloud rejected the upload.");
            }
            catch
            {
                return CloudTransferResult.Failed("Steam Cloud upload failed.");
            }
        }

        public CloudTransferResult Download(string slotId)
        {
            if (Availability != CloudProviderAvailability.Available)
            {
                return CloudTransferResult.Offline();
            }

            if (!CloudSaveSlotRules.IsValidSlotId(slotId))
            {
                return CloudTransferResult.InvalidData("Cloud slot is invalid.");
            }

            try
            {
                var remoteFileName = GetRemoteFileName(slotId);
                if (!SteamRemoteStorage.FileExists(remoteFileName))
                {
                    return CloudTransferResult.NotFound();
                }

                var size = SteamRemoteStorage.GetFileSize(remoteFileName);
                if (size <= 0 || size > MaximumCloudPayloadBytes)
                {
                    return CloudTransferResult.InvalidData("Steam Cloud file size is invalid.");
                }

                var bytes = new byte[size];
                if (SteamRemoteStorage.FileRead(remoteFileName, bytes, size) != size)
                {
                    return CloudTransferResult.Failed("Steam Cloud returned an incomplete file.");
                }

                var payload = JsonUtility.FromJson<CloudSavePayload>(Encoding.UTF8.GetString(bytes));
                return payload != null && payload.IsValid()
                    ? CloudTransferResult.Success(payload)
                    : CloudTransferResult.InvalidData("Steam Cloud payload validation failed.");
            }
            catch
            {
                return CloudTransferResult.Failed("Steam Cloud download failed.");
            }
        }

        private static string GetRemoteFileName(string slotId)
        {
            return $"cheesetama_cloud_{slotId}.json";
        }
    }
#endif
}
