using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Platform;
using UnityEngine;

namespace CheeseTama.Platform.Accounts
{
    /// <summary>
    /// A browser-local account registry. It persists only a normalized login identifier, a salted
    /// password hash, and the opaque active account ID. The version 1 JSON field keeps its original
    /// normalizedEmail name for backward compatibility. Passwords and session objects never enter
    /// PlayerPrefs or the ordinary CheeseTama save payload.
    /// </summary>
    public sealed class PlayerPrefsLocalAccountCredentialStore : ILocalAccountCredentialStore
    {
        public const string DefaultStorageKey = "cheesetama.local-accounts.v1";
        public const int MaximumAccounts = 8;
        private const int CurrentSchemaVersion = 1;
        private const int MaximumSerializedCharacters = 65536;

        private readonly string storageKey;

        public PlayerPrefsLocalAccountCredentialStore()
            : this(DefaultStorageKey)
        {
        }

        public PlayerPrefsLocalAccountCredentialStore(string storageKey)
        {
            if (!IsValidStorageKey(storageKey))
            {
                throw new ArgumentException(
                    "A short PlayerPrefs key without control characters is required.",
                    nameof(storageKey));
            }

            this.storageKey = storageKey;
        }

        public string StorageKey => storageKey;

        public LocalAccountCredentialStoreStatus TryGetByEmail(
            string normalizedEmail,
            out LocalAccountCredentialRecord credential)
        {
            credential = null;
            if (!LocalAccountLoginId.TryNormalize(normalizedEmail, out var canonical)
                || !string.Equals(canonical, normalizedEmail, StringComparison.Ordinal))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            var status = TryLoad(out _, out var records);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            for (var index = 0; index < records.Count; index += 1)
            {
                if (string.Equals(
                    records[index].NormalizedEmail,
                    normalizedEmail,
                    StringComparison.Ordinal))
                {
                    credential = records[index];
                    return LocalAccountCredentialStoreStatus.Success;
                }
            }

            return LocalAccountCredentialStoreStatus.NotFound;
        }

        public LocalAccountCredentialStoreStatus TryGetByAccountId(
            string accountId,
            out LocalAccountCredentialRecord credential)
        {
            credential = null;
            if (!LocalAccountId.IsValid(accountId))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            var status = TryLoad(out _, out var records);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            return FindByAccountId(records, accountId, out credential)
                ? LocalAccountCredentialStoreStatus.Success
                : LocalAccountCredentialStoreStatus.NotFound;
        }

        public LocalAccountCredentialStoreStatus TryGetActiveAccount(
            out LocalAccountCredentialRecord credential)
        {
            credential = null;
            var status = TryLoad(out var envelope, out var records);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (string.IsNullOrEmpty(envelope.activeAccountId))
            {
                return LocalAccountCredentialStoreStatus.NotFound;
            }

            if (!FindByAccountId(records, envelope.activeAccountId, out credential))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            return LocalAccountCredentialStoreStatus.Success;
        }

        public LocalAccountCredentialStoreStatus TryGetPendingDeletionAccountId(
            out string accountId)
        {
            accountId = string.Empty;
            var status = TryLoad(out var envelope, out _);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (string.IsNullOrEmpty(envelope.pendingDeletionAccountId))
            {
                return LocalAccountCredentialStoreStatus.NotFound;
            }

            accountId = envelope.pendingDeletionAccountId;
            return LocalAccountCredentialStoreStatus.Success;
        }

        public LocalAccountCredentialStoreStatus TryAddAndActivate(
            LocalAccountCredentialRecord credential)
        {
            if (credential?.IsStructurallyValid() != true)
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            var status = TryLoad(out var envelope, out var records);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (!string.IsNullOrEmpty(envelope.pendingDeletionAccountId))
            {
                return LocalAccountCredentialStoreStatus.PendingDeletion;
            }

            for (var index = 0; index < records.Count; index += 1)
            {
                if (string.Equals(
                        records[index].NormalizedEmail,
                        credential.NormalizedEmail,
                        StringComparison.Ordinal)
                    || string.Equals(
                        records[index].AccountId,
                        credential.AccountId,
                        StringComparison.Ordinal))
                {
                    return LocalAccountCredentialStoreStatus.AlreadyExists;
                }
            }

            if (records.Count >= MaximumAccounts)
            {
                return LocalAccountCredentialStoreStatus.CapacityReached;
            }

            envelope.accounts.Add(StoredAccount.FromDomain(credential));
            envelope.activeAccountId = credential.AccountId;
            return TryPersist(envelope);
        }

        public LocalAccountCredentialStoreStatus TryActivate(string accountId)
        {
            if (!LocalAccountId.IsValid(accountId))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            var status = TryLoad(out var envelope, out var records);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (!string.IsNullOrEmpty(envelope.pendingDeletionAccountId))
            {
                return LocalAccountCredentialStoreStatus.PendingDeletion;
            }

            if (!FindByAccountId(records, accountId, out _))
            {
                return LocalAccountCredentialStoreStatus.NotFound;
            }

            if (string.Equals(envelope.activeAccountId, accountId, StringComparison.Ordinal))
            {
                return LocalAccountCredentialStoreStatus.Success;
            }

            envelope.activeAccountId = accountId;
            return TryPersist(envelope);
        }

        public LocalAccountCredentialStoreStatus TryClearActiveAccount()
        {
            var status = TryLoad(out var envelope, out _);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (string.IsNullOrEmpty(envelope.activeAccountId))
            {
                return LocalAccountCredentialStoreStatus.Success;
            }

            envelope.activeAccountId = string.Empty;
            return TryPersist(envelope);
        }

        public LocalAccountCredentialStoreStatus TryDeleteDeactivateAndCreateTombstone(
            string accountId)
        {
            if (!LocalAccountId.IsValid(accountId))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            var status = TryLoad(out var envelope, out _);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (!string.IsNullOrEmpty(envelope.pendingDeletionAccountId))
            {
                return LocalAccountCredentialStoreStatus.PendingDeletion;
            }

            if (!string.Equals(
                envelope.activeAccountId,
                accountId,
                StringComparison.Ordinal))
            {
                return LocalAccountCredentialStoreStatus.NotFound;
            }

            var removed = false;
            for (var index = envelope.accounts.Count - 1; index >= 0; index -= 1)
            {
                if (!string.Equals(
                    envelope.accounts[index]?.accountId,
                    accountId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                envelope.accounts.RemoveAt(index);
                removed = true;
                break;
            }

            if (!removed)
            {
                return LocalAccountCredentialStoreStatus.NotFound;
            }

            envelope.activeAccountId = string.Empty;
            envelope.pendingDeletionAccountId = accountId;

            return TryPersist(envelope);
        }

        public LocalAccountCredentialStoreStatus TryClearDeletionTombstone(string accountId)
        {
            if (!LocalAccountId.IsValid(accountId))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            var status = TryLoad(out var envelope, out _);
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                return status;
            }

            if (string.IsNullOrEmpty(envelope.pendingDeletionAccountId))
            {
                return LocalAccountCredentialStoreStatus.NotFound;
            }

            if (!string.Equals(
                envelope.pendingDeletionAccountId,
                accountId,
                StringComparison.Ordinal))
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }

            envelope.pendingDeletionAccountId = string.Empty;
            return TryPersist(envelope);
        }

        private LocalAccountCredentialStoreStatus TryLoad(
            out RegistryEnvelope envelope,
            out List<LocalAccountCredentialRecord> records)
        {
            envelope = null;
            records = null;
            try
            {
                if (!PlayerPrefs.HasKey(storageKey))
                {
                    envelope = RegistryEnvelope.CreateEmpty();
                    records = new List<LocalAccountCredentialRecord>();
                    return LocalAccountCredentialStoreStatus.Success;
                }

                var json = PlayerPrefs.GetString(storageKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json)
                    || json.Length > MaximumSerializedCharacters)
                {
                    return LocalAccountCredentialStoreStatus.CorruptData;
                }

                envelope = JsonUtility.FromJson<RegistryEnvelope>(json);
                if (envelope == null
                    || envelope.schemaVersion != CurrentSchemaVersion
                    || envelope.accounts == null
                    || envelope.accounts.Count > MaximumAccounts
                    || (envelope.activeAccountId?.Length ?? 0) > LocalAccountId.GuidNLength
                    || (envelope.pendingDeletionAccountId?.Length ?? 0)
                        > LocalAccountId.GuidNLength)
                {
                    return LocalAccountCredentialStoreStatus.CorruptData;
                }

                envelope.activeAccountId ??= string.Empty;
                envelope.pendingDeletionAccountId ??= string.Empty;
                if (!string.IsNullOrEmpty(envelope.activeAccountId)
                    && !LocalAccountId.IsValid(envelope.activeAccountId))
                {
                    return LocalAccountCredentialStoreStatus.CorruptData;
                }
                if (!string.IsNullOrEmpty(envelope.pendingDeletionAccountId)
                    && (!LocalAccountId.IsValid(envelope.pendingDeletionAccountId)
                        || !string.IsNullOrEmpty(envelope.activeAccountId)))
                {
                    return LocalAccountCredentialStoreStatus.CorruptData;
                }

                records = new List<LocalAccountCredentialRecord>(envelope.accounts.Count);
                var accountIds = new HashSet<string>(StringComparer.Ordinal);
                var loginIds = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < envelope.accounts.Count; index += 1)
                {
                    if (!StoredAccount.TryToDomain(
                            envelope.accounts[index],
                            out var record)
                        || !accountIds.Add(record.AccountId)
                        || !loginIds.Add(record.NormalizedEmail))
                    {
                        return LocalAccountCredentialStoreStatus.CorruptData;
                    }

                    records.Add(record);
                }

                if (!string.IsNullOrEmpty(envelope.activeAccountId)
                    && !FindByAccountId(records, envelope.activeAccountId, out _))
                {
                    return LocalAccountCredentialStoreStatus.CorruptData;
                }
                if (!string.IsNullOrEmpty(envelope.pendingDeletionAccountId)
                    && FindByAccountId(records, envelope.pendingDeletionAccountId, out _))
                {
                    return LocalAccountCredentialStoreStatus.CorruptData;
                }

                return LocalAccountCredentialStoreStatus.Success;
            }
            catch (ArgumentException)
            {
                return LocalAccountCredentialStoreStatus.CorruptData;
            }
            catch (PlayerPrefsException)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus TryPersist(RegistryEnvelope envelope)
        {
            try
            {
                var json = JsonUtility.ToJson(envelope, false);
                if (string.IsNullOrWhiteSpace(json)
                    || json.Length > MaximumSerializedCharacters)
                {
                    return LocalAccountCredentialStoreStatus.Unavailable;
                }

                PlayerPrefs.SetString(storageKey, json);
                PlayerPrefs.Save();
                BrowserPersistence.RequestSync();
                return LocalAccountCredentialStoreStatus.Success;
            }
            catch (ArgumentException)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
            catch (PlayerPrefsException)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private static bool FindByAccountId(
            IReadOnlyList<LocalAccountCredentialRecord> records,
            string accountId,
            out LocalAccountCredentialRecord credential)
        {
            for (var index = 0; index < records.Count; index += 1)
            {
                if (string.Equals(
                    records[index].AccountId,
                    accountId,
                    StringComparison.Ordinal))
                {
                    credential = records[index];
                    return true;
                }
            }

            credential = null;
            return false;
        }

        private static bool IsValidStorageKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                if (char.IsControl(value[index]))
                {
                    return false;
                }
            }

            return true;
        }

        [Serializable]
        private sealed class RegistryEnvelope
        {
            public int schemaVersion = CurrentSchemaVersion;
            public string activeAccountId = string.Empty;
            public string pendingDeletionAccountId = string.Empty;
            public List<StoredAccount> accounts = new List<StoredAccount>();

            public static RegistryEnvelope CreateEmpty()
            {
                return new RegistryEnvelope();
            }
        }

        [Serializable]
        private sealed class StoredAccount
        {
            public string accountId = string.Empty;
            public string normalizedEmail = string.Empty;
            public string algorithmId = string.Empty;
            public int iterations;
            public string saltBase64 = string.Empty;
            public string hashBase64 = string.Empty;
            public string createdAtUtcIso = string.Empty;

            public static StoredAccount FromDomain(LocalAccountCredentialRecord record)
            {
                return new StoredAccount
                {
                    accountId = record.AccountId,
                    normalizedEmail = record.NormalizedEmail,
                    algorithmId = record.PasswordHash.AlgorithmId,
                    iterations = record.PasswordHash.Iterations,
                    saltBase64 = record.PasswordHash.SaltBase64,
                    hashBase64 = record.PasswordHash.HashBase64,
                    createdAtUtcIso = record.CreatedAtUtc.ToString(
                        "O",
                        CultureInfo.InvariantCulture)
                };
            }

            public static bool TryToDomain(
                StoredAccount source,
                out LocalAccountCredentialRecord record)
            {
                record = null;
                if (source == null
                    || !DateTimeOffset.TryParse(
                        source.createdAtUtcIso,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var createdAt))
                {
                    return false;
                }

                var passwordHash = new LocalAccountPasswordHash(
                    source.algorithmId,
                    source.iterations,
                    source.saltBase64,
                    source.hashBase64);
                var candidate = new LocalAccountCredentialRecord(
                    source.accountId,
                    source.normalizedEmail,
                    passwordHash,
                    createdAt);
                if (!candidate.IsStructurallyValid())
                {
                    return false;
                }

                record = candidate;
                return true;
            }
        }
    }
}
