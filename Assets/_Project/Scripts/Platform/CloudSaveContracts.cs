using System;
using System.Security.Cryptography;
using System.Text;

namespace CheeseTama.Platform
{
    [Serializable]
    public sealed class CloudSavePayload
    {
        public string slotId = string.Empty;
        public string contentJson = string.Empty;
        public long revision;
        public long modifiedUtcTicks;
        public string contentHash = string.Empty;

        public static CloudSavePayload Create(
            string slotId,
            string contentJson,
            long revision,
            DateTimeOffset modifiedUtc)
        {
            var safeContent = contentJson ?? string.Empty;
            return new CloudSavePayload
            {
                slotId = slotId ?? string.Empty,
                contentJson = safeContent,
                revision = Math.Max(0L, revision),
                modifiedUtcTicks = modifiedUtc.UtcDateTime.Ticks,
                contentHash = ComputeContentHash(safeContent)
            };
        }

        public bool IsValid()
        {
            return CloudSaveSlotRules.IsValidSlotId(slotId)
                && contentJson != null
                && revision >= 0L
                && modifiedUtcTicks > 0L
                && string.Equals(
                    contentHash,
                    ComputeContentHash(contentJson),
                    StringComparison.OrdinalIgnoreCase);
        }

        public static string ComputeContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
            var hash = sha256.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);
            for (var index = 0; index < hash.Length; index += 1)
            {
                builder.Append(hash[index].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    public static class CloudSaveSlotRules
    {
        public const string PrimarySlotId = "primary";
        public const int MaximumSlotIdLength = 48;

        public static bool IsValidSlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId) || slotId.Length > MaximumSlotIdLength)
            {
                return false;
            }

            for (var index = 0; index < slotId.Length; index += 1)
            {
                var character = slotId[index];
                if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
                {
                    return false;
                }
            }

            return true;
        }
    }

    public enum CloudProviderAvailability
    {
        Offline,
        Available
    }

    public enum CloudTransferStatus
    {
        Success,
        NotFound,
        Offline,
        Failed,
        InvalidData
    }

    public readonly struct CloudTransferResult
    {
        private CloudTransferResult(CloudTransferStatus status, CloudSavePayload payload, string message)
        {
            Status = status;
            Payload = payload;
            Message = message ?? string.Empty;
        }

        public CloudTransferStatus Status { get; }
        public CloudSavePayload Payload { get; }
        public string Message { get; }
        public bool Succeeded => Status == CloudTransferStatus.Success;

        public static CloudTransferResult Success(CloudSavePayload payload = null)
        {
            return new CloudTransferResult(CloudTransferStatus.Success, payload, string.Empty);
        }

        public static CloudTransferResult NotFound()
        {
            return new CloudTransferResult(CloudTransferStatus.NotFound, null, string.Empty);
        }

        public static CloudTransferResult Offline(string message = null)
        {
            return new CloudTransferResult(CloudTransferStatus.Offline, null, message);
        }

        public static CloudTransferResult Failed(string message = null)
        {
            return new CloudTransferResult(CloudTransferStatus.Failed, null, message);
        }

        public static CloudTransferResult InvalidData(string message = null)
        {
            return new CloudTransferResult(CloudTransferStatus.InvalidData, null, message);
        }
    }

    public interface ICloudSaveProvider
    {
        string ProviderName { get; }
        CloudProviderAvailability Availability { get; }
        CloudTransferResult Upload(CloudSavePayload payload);
        CloudTransferResult Download(string slotId);
    }

    public enum CloudSyncAction
    {
        InSync,
        UploadedLocal,
        DownloadedRemote,
        KeptLocalOffline,
        KeptLocalAfterFailure,
        ConflictNeedsResolution,
        InvalidLocal
    }

    public readonly struct CloudSyncResult
    {
        public CloudSyncResult(
            CloudSyncAction action,
            CloudSavePayload effectiveLocal,
            CloudSavePayload remote,
            string message)
        {
            Action = action;
            EffectiveLocal = effectiveLocal;
            Remote = remote;
            Message = message ?? string.Empty;
        }

        public CloudSyncAction Action { get; }
        public CloudSavePayload EffectiveLocal { get; }
        public CloudSavePayload Remote { get; }
        public string Message { get; }
        public bool RequiresLocalWrite => Action == CloudSyncAction.DownloadedRemote;
        public bool RequiresUserChoice => Action == CloudSyncAction.ConflictNeedsResolution;
    }

    public sealed class CloudSaveSyncCoordinator
    {
        public CloudSyncResult Synchronize(CloudSavePayload local, ICloudSaveProvider provider)
        {
            if (local == null || !local.IsValid())
            {
                return new CloudSyncResult(
                    CloudSyncAction.InvalidLocal,
                    local,
                    null,
                    "Local save payload is invalid; cloud data was not touched.");
            }

            if (provider == null)
            {
                return new CloudSyncResult(
                    CloudSyncAction.KeptLocalOffline,
                    local,
                    null,
                    "Cloud provider is offline; the local save remains authoritative.");
            }

            CloudProviderAvailability availability;
            try
            {
                availability = provider.Availability;
            }
            catch
            {
                availability = CloudProviderAvailability.Offline;
            }

            if (availability != CloudProviderAvailability.Available)
            {
                return new CloudSyncResult(
                    CloudSyncAction.KeptLocalOffline,
                    local,
                    null,
                    "Cloud provider is offline; the local save remains authoritative.");
            }

            CloudTransferResult download;
            try
            {
                download = provider.Download(local.slotId);
            }
            catch
            {
                return new CloudSyncResult(
                    CloudSyncAction.KeptLocalAfterFailure,
                    local,
                    null,
                    "Cloud provider threw during download; the local save was preserved.");
            }
            if (download.Status == CloudTransferStatus.NotFound)
            {
                return UploadLocal(local, provider, null);
            }

            if (download.Status == CloudTransferStatus.Offline)
            {
                return new CloudSyncResult(CloudSyncAction.KeptLocalOffline, local, null, download.Message);
            }

            if (!download.Succeeded || download.Payload == null || !download.Payload.IsValid())
            {
                return new CloudSyncResult(
                    CloudSyncAction.KeptLocalAfterFailure,
                    local,
                    download.Payload,
                    string.IsNullOrWhiteSpace(download.Message)
                        ? "Cloud download failed or returned invalid data; the local save was preserved."
                        : download.Message);
            }

            var remote = download.Payload;
            if (!string.Equals(remote.slotId, local.slotId, StringComparison.Ordinal))
            {
                return new CloudSyncResult(
                    CloudSyncAction.KeptLocalAfterFailure,
                    local,
                    remote,
                    "Cloud slot mismatch; the local save was preserved.");
            }

            if (string.Equals(remote.contentHash, local.contentHash, StringComparison.OrdinalIgnoreCase))
            {
                return new CloudSyncResult(CloudSyncAction.InSync, local, remote, string.Empty);
            }

            var comparison = CompareRecency(local, remote);
            if (comparison > 0)
            {
                return UploadLocal(local, provider, remote);
            }

            if (comparison < 0)
            {
                return new CloudSyncResult(CloudSyncAction.DownloadedRemote, remote, remote, string.Empty);
            }

            return new CloudSyncResult(
                CloudSyncAction.ConflictNeedsResolution,
                local,
                remote,
                "Local and cloud saves have equal recency but different content; neither copy was overwritten.");
        }

        private static CloudSyncResult UploadLocal(
            CloudSavePayload local,
            ICloudSaveProvider provider,
            CloudSavePayload remote)
        {
            CloudTransferResult upload;
            try
            {
                upload = provider.Upload(local);
            }
            catch
            {
                return new CloudSyncResult(
                    CloudSyncAction.KeptLocalAfterFailure,
                    local,
                    remote,
                    "Cloud provider threw during upload; the local save was preserved.");
            }
            if (upload.Succeeded)
            {
                return new CloudSyncResult(CloudSyncAction.UploadedLocal, local, remote, string.Empty);
            }

            return new CloudSyncResult(
                upload.Status == CloudTransferStatus.Offline
                    ? CloudSyncAction.KeptLocalOffline
                    : CloudSyncAction.KeptLocalAfterFailure,
                local,
                remote,
                upload.Message);
        }

        private static int CompareRecency(CloudSavePayload local, CloudSavePayload remote)
        {
            var revisionComparison = local.revision.CompareTo(remote.revision);
            if (revisionComparison != 0)
            {
                return revisionComparison;
            }

            return local.modifiedUtcTicks.CompareTo(remote.modifiedUtcTicks);
        }
    }
}
