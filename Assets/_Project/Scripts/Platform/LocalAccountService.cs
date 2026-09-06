using System;
using UnityEngine;

namespace CheeseTama.Platform.Accounts
{
    /// <summary>
    /// Coordinates local-only credentials, the active account registry, and the in-memory session.
    /// Account-scoped game data remains caller-owned through the sign-up and deletion callbacks.
    /// </summary>
    public sealed class LocalAccountService
    {
        public const string DeleteConfirmationText = "DELETE ACCOUNT";

        private readonly ILocalAccountCredentialStore credentialStore;
        private readonly ILocalAccountPasswordHasher passwordHasher;
        private readonly Func<DateTimeOffset> utcNow;
        private readonly Func<string> accountIdFactory;

        private bool initialized;
        private bool storageAvailable = true;
        private bool restoredFromRegistry;
        private LocalAccountCredentialRecord activeCredential;
        private string pendingDeletionAccountId = string.Empty;

        public LocalAccountService(
            ILocalAccountCredentialStore credentialStore,
            ILocalAccountPasswordHasher passwordHasher = null,
            Func<DateTimeOffset> utcNow = null,
            Func<string> accountIdFactory = null)
        {
            this.credentialStore = credentialStore
                ?? throw new ArgumentNullException(nameof(credentialStore));
            this.passwordHasher = passwordHasher ?? new Pbkdf2Sha256PasswordHasher();
            this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
            this.accountIdFactory = accountIdFactory ?? LocalAccountId.Create;
        }

        public LocalAccountSnapshot Snapshot => BuildSnapshot();

        public void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            var pendingStatus = SafeGetPendingDeletionAccountId(
                out pendingDeletionAccountId);
            if (pendingStatus == LocalAccountCredentialStoreStatus.Success)
            {
                activeCredential = null;
                restoredFromRegistry = false;
                storageAvailable = true;
                return;
            }

            if (pendingStatus != LocalAccountCredentialStoreStatus.NotFound)
            {
                pendingDeletionAccountId = string.Empty;
                activeCredential = null;
                restoredFromRegistry = false;
                storageAvailable = false;
                return;
            }

            pendingDeletionAccountId = string.Empty;
            var status = SafeGetActiveAccount(out var credential);
            switch (status)
            {
                case LocalAccountCredentialStoreStatus.Success:
                    activeCredential = credential;
                    restoredFromRegistry = true;
                    storageAvailable = true;
                    break;
                case LocalAccountCredentialStoreStatus.NotFound:
                    activeCredential = null;
                    restoredFromRegistry = false;
                    storageAvailable = true;
                    break;
                default:
                    activeCredential = null;
                    restoredFromRegistry = false;
                    storageAvailable = false;
                    break;
            }
        }

        public LocalAccountAttemptResult TrySignUp(
            string email,
            string password,
            string confirmation)
        {
            return TrySignUp(email, password, confirmation, _ => true);
        }

        public LocalAccountAttemptResult TrySignUp(
            string email,
            string password,
            string confirmation,
            Func<string, bool> tryInitializeAccountData)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(pendingDeletionAccountId))
            {
                return Failure(LocalAccountAttemptStatus.DeletionPending);
            }

            if (!LocalAccountLoginId.TryNormalize(email, out var normalizedEmail))
            {
                return Failure(LocalAccountAttemptStatus.InvalidEmail);
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                return Failure(LocalAccountAttemptStatus.ConfirmationMismatch);
            }

            if (!LocalAccountPasswordPolicy.TryValidate(password, out var passwordFailure))
            {
                return Failure(LocalAccountAttemptStatus.WeakPassword, passwordFailure);
            }

            var lookupStatus = SafeGetByEmail(normalizedEmail, out _);
            if (lookupStatus == LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = true;
                return Failure(LocalAccountAttemptStatus.AccountAlreadyExists);
            }

            if (lookupStatus != LocalAccountCredentialStoreStatus.NotFound)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.StorageUnavailable);
            }

            storageAvailable = true;
            if (!TryCreateAccountId(out var accountId))
            {
                return Failure(LocalAccountAttemptStatus.SecurityUnavailable);
            }

            var accountIdLookup = SafeGetByAccountId(accountId, out _);
            if (accountIdLookup == LocalAccountCredentialStoreStatus.Success)
            {
                return Failure(LocalAccountAttemptStatus.SecurityUnavailable);
            }

            if (accountIdLookup != LocalAccountCredentialStoreStatus.NotFound)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.StorageUnavailable);
            }

            if (!TryReadUtcNow(out var createdAtUtc)
                || !TryCreatePasswordHash(password, out var passwordHash))
            {
                return Failure(LocalAccountAttemptStatus.SecurityUnavailable);
            }

            var credential = new LocalAccountCredentialRecord(
                accountId,
                normalizedEmail,
                passwordHash,
                createdAtUtc);
            if (!credential.IsStructurallyValid())
            {
                return Failure(LocalAccountAttemptStatus.SecurityUnavailable);
            }

            if (tryInitializeAccountData == null
                || !SafeInvokeAccountDataCallback(tryInitializeAccountData, accountId))
            {
                return Failure(LocalAccountAttemptStatus.AccountDataOperationFailed);
            }

            var addStatus = SafeAddAndActivate(credential);
            if (addStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = addStatus == LocalAccountCredentialStoreStatus.AlreadyExists
                    || addStatus == LocalAccountCredentialStoreStatus.CapacityReached;
                return Failure(MapSignUpStoreFailure(addStatus));
            }

            storageAvailable = true;
            Activate(credential, false);
            return Success();
        }

        public LocalAccountAttemptResult TrySignIn(string email, string password)
        {
            return TrySignIn(email, password, () => true);
        }

        public LocalAccountAttemptResult TrySignIn(
            string email,
            string password,
            Func<bool> tryPrepareCurrentAccountData)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(pendingDeletionAccountId))
            {
                return Failure(LocalAccountAttemptStatus.DeletionPending);
            }

            if (!LocalAccountLoginId.TryNormalize(email, out var normalizedEmail))
            {
                return Failure(LocalAccountAttemptStatus.InvalidEmail);
            }

            if (!LocalAccountPasswordPolicy.IsAcceptableForVerification(password))
            {
                return Failure(LocalAccountAttemptStatus.InvalidCredentials);
            }

            var lookupStatus = SafeGetByEmail(normalizedEmail, out var credential);
            if (lookupStatus == LocalAccountCredentialStoreStatus.NotFound)
            {
                storageAvailable = true;
                return Failure(LocalAccountAttemptStatus.InvalidCredentials);
            }

            if (lookupStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.StorageUnavailable);
            }

            storageAvailable = true;
            var verification = SafeVerify(password, credential.PasswordHash);
            if (verification == LocalAccountPasswordVerificationStatus.Mismatch)
            {
                return Failure(LocalAccountAttemptStatus.InvalidCredentials);
            }

            if (verification != LocalAccountPasswordVerificationStatus.Verified)
            {
                return Failure(LocalAccountAttemptStatus.SecurityUnavailable);
            }

            // The caller still owns the currently selected save scope at this point.
            // Commit it only after credentials have been verified, but before changing
            // the persisted active account, so invalid credentials remain mutation-free.
            if (tryPrepareCurrentAccountData == null
                || !SafeInvokeAccountDataCallback(tryPrepareCurrentAccountData))
            {
                return Failure(LocalAccountAttemptStatus.AccountDataOperationFailed);
            }

            var activateStatus = SafeActivate(credential.AccountId);
            if (activateStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.StorageUnavailable);
            }

            storageAvailable = true;
            Activate(credential, false);
            return Success();
        }

        public LocalAccountAttemptResult TrySignOut()
        {
            EnsureInitialized();
            if (activeCredential == null)
            {
                return Failure(LocalAccountAttemptStatus.SessionRequired);
            }

            var status = SafeClearActiveAccount();
            if (status != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.StorageUnavailable);
            }

            storageAvailable = true;
            activeCredential = null;
            restoredFromRegistry = false;
            return Success();
        }

        public LocalAccountAttemptResult TryDelete(string password, string confirmation)
        {
            return TryDelete(password, confirmation, _ => true);
        }

        public LocalAccountAttemptResult TryDelete(
            string password,
            string confirmation,
            Func<string, bool> tryPurgeAccountData)
        {
            EnsureInitialized();
            if (activeCredential == null)
            {
                return Failure(LocalAccountAttemptStatus.SessionRequired);
            }

            if (!string.Equals(
                confirmation,
                DeleteConfirmationText,
                StringComparison.Ordinal))
            {
                return Failure(LocalAccountAttemptStatus.ConfirmationMismatch);
            }

            if (!LocalAccountPasswordPolicy.IsAcceptableForVerification(password))
            {
                return Failure(LocalAccountAttemptStatus.InvalidCredentials);
            }

            var lookupStatus = SafeGetByAccountId(
                activeCredential.AccountId,
                out var storedCredential);
            if (lookupStatus == LocalAccountCredentialStoreStatus.NotFound)
            {
                return Failure(LocalAccountAttemptStatus.InvalidCredentials);
            }

            if (lookupStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.StorageUnavailable);
            }

            storageAvailable = true;
            var verification = SafeVerify(password, storedCredential.PasswordHash);
            if (verification == LocalAccountPasswordVerificationStatus.Mismatch)
            {
                return Failure(LocalAccountAttemptStatus.InvalidCredentials);
            }

            if (verification != LocalAccountPasswordVerificationStatus.Verified)
            {
                return Failure(LocalAccountAttemptStatus.SecurityUnavailable);
            }

            var accountId = storedCredential.AccountId;
            var beginDeleteStatus = SafeDeleteDeactivateAndCreateTombstone(accountId);
            if (beginDeleteStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(beginDeleteStatus == LocalAccountCredentialStoreStatus.PendingDeletion
                    ? LocalAccountAttemptStatus.DeletionPending
                    : LocalAccountAttemptStatus.StorageUnavailable);
            }

            storageAvailable = true;
            activeCredential = null;
            restoredFromRegistry = false;
            pendingDeletionAccountId = accountId;
            if (tryPurgeAccountData == null
                || !SafeInvokeAccountDataCallback(tryPurgeAccountData, accountId))
            {
                return Failure(LocalAccountAttemptStatus.AccountDataOperationFailed);
            }

            var clearStatus = SafeClearDeletionTombstone(accountId);
            if (clearStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.DeletionPending);
            }

            storageAvailable = true;
            pendingDeletionAccountId = string.Empty;
            return Success();
        }

        public LocalAccountAttemptResult TryResumePendingDeletion(
            Func<string, bool> tryPurgeAccountData)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(pendingDeletionAccountId))
            {
                return Failure(LocalAccountAttemptStatus.NoPendingDeletion);
            }

            var accountId = pendingDeletionAccountId;
            if (tryPurgeAccountData == null
                || !SafeInvokeAccountDataCallback(tryPurgeAccountData, accountId))
            {
                return Failure(LocalAccountAttemptStatus.AccountDataOperationFailed);
            }

            var clearStatus = SafeClearDeletionTombstone(accountId);
            if (clearStatus != LocalAccountCredentialStoreStatus.Success)
            {
                storageAvailable = false;
                return Failure(LocalAccountAttemptStatus.DeletionPending);
            }

            storageAvailable = true;
            pendingDeletionAccountId = string.Empty;
            return Success();
        }

        private void Activate(LocalAccountCredentialRecord credential, bool restored)
        {
            activeCredential = credential;
            restoredFromRegistry = restored;
        }

        private LocalAccountAttemptResult Success()
        {
            return new LocalAccountAttemptResult(
                LocalAccountAttemptStatus.Success,
                BuildSnapshot());
        }

        private LocalAccountAttemptResult Failure(
            LocalAccountAttemptStatus status,
            LocalAccountPasswordFailure passwordFailure = LocalAccountPasswordFailure.None)
        {
            return new LocalAccountAttemptResult(status, BuildSnapshot(), passwordFailure);
        }

        private LocalAccountSnapshot BuildSnapshot()
        {
            return new LocalAccountSnapshot(
                initialized,
                storageAvailable,
                activeCredential != null,
                activeCredential != null && restoredFromRegistry,
                activeCredential?.AccountId,
                activeCredential?.NormalizedEmail,
                pendingDeletionAccountId);
        }

        private bool TryCreateAccountId(out string accountId)
        {
            accountId = string.Empty;
            try
            {
                var candidate = accountIdFactory();
                if (!LocalAccountId.IsValid(candidate))
                {
                    return false;
                }

                accountId = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryReadUtcNow(out DateTimeOffset now)
        {
            now = default;
            try
            {
                var candidate = utcNow();
                if (candidate == default)
                {
                    return false;
                }

                now = candidate.ToUniversalTime();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryCreatePasswordHash(
            string password,
            out LocalAccountPasswordHash passwordHash)
        {
            passwordHash = null;
            try
            {
                passwordHash = passwordHasher.CreateHash(password);
                return passwordHash?.IsStructurallyValid() == true;
            }
            catch (Exception)
            {
                passwordHash = null;
                return false;
            }
        }

        private LocalAccountPasswordVerificationStatus SafeVerify(
            string password,
            LocalAccountPasswordHash passwordHash)
        {
            try
            {
                return passwordHasher.Verify(password, passwordHash);
            }
            catch (Exception)
            {
                return LocalAccountPasswordVerificationStatus.UnsupportedHash;
            }
        }

        private LocalAccountCredentialStoreStatus SafeGetByEmail(
            string normalizedEmail,
            out LocalAccountCredentialRecord credential)
        {
            try
            {
                return credentialStore.TryGetByEmail(normalizedEmail, out credential);
            }
            catch (Exception)
            {
                credential = null;
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeGetByAccountId(
            string accountId,
            out LocalAccountCredentialRecord credential)
        {
            try
            {
                return credentialStore.TryGetByAccountId(accountId, out credential);
            }
            catch (Exception)
            {
                credential = null;
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeGetActiveAccount(
            out LocalAccountCredentialRecord credential)
        {
            try
            {
                return credentialStore.TryGetActiveAccount(out credential);
            }
            catch (Exception)
            {
                credential = null;
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeGetPendingDeletionAccountId(
            out string accountId)
        {
            try
            {
                return credentialStore.TryGetPendingDeletionAccountId(out accountId);
            }
            catch (Exception)
            {
                accountId = string.Empty;
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeAddAndActivate(
            LocalAccountCredentialRecord credential)
        {
            try
            {
                return credentialStore.TryAddAndActivate(credential);
            }
            catch (Exception)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeActivate(string accountId)
        {
            try
            {
                return credentialStore.TryActivate(accountId);
            }
            catch (Exception)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeClearActiveAccount()
        {
            try
            {
                return credentialStore.TryClearActiveAccount();
            }
            catch (Exception)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeDeleteDeactivateAndCreateTombstone(
            string accountId)
        {
            try
            {
                return credentialStore.TryDeleteDeactivateAndCreateTombstone(accountId);
            }
            catch (Exception)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private LocalAccountCredentialStoreStatus SafeClearDeletionTombstone(string accountId)
        {
            try
            {
                return credentialStore.TryClearDeletionTombstone(accountId);
            }
            catch (Exception)
            {
                return LocalAccountCredentialStoreStatus.Unavailable;
            }
        }

        private static bool SafeInvokeAccountDataCallback(
            Func<string, bool> callback,
            string accountId)
        {
            try
            {
                return callback(accountId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool SafeInvokeAccountDataCallback(Func<bool> callback)
        {
            try
            {
                return callback();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static LocalAccountAttemptStatus MapSignUpStoreFailure(
            LocalAccountCredentialStoreStatus status)
        {
            switch (status)
            {
                case LocalAccountCredentialStoreStatus.AlreadyExists:
                    return LocalAccountAttemptStatus.AccountAlreadyExists;
                case LocalAccountCredentialStoreStatus.CapacityReached:
                    return LocalAccountAttemptStatus.AccountLimitReached;
                case LocalAccountCredentialStoreStatus.PendingDeletion:
                    return LocalAccountAttemptStatus.DeletionPending;
                default:
                    return LocalAccountAttemptStatus.StorageUnavailable;
            }
        }
    }

    public static class LocalAccountRuntime
    {
        private static LocalAccountService service;

        public static string DeleteConfirmationText => LocalAccountService.DeleteConfirmationText;

        public static LocalAccountSnapshot Snapshot
        {
            get
            {
                EnsureInitialized();
                return service.Snapshot;
            }
        }

        public static void EnsureInitialized()
        {
            service ??= CreateDefaultService();
            service.EnsureInitialized();
        }

        public static LocalAccountAttemptResult TrySignUp(
            string email,
            string password,
            string confirmation)
        {
            EnsureInitialized();
            return service.TrySignUp(email, password, confirmation);
        }

        public static LocalAccountAttemptResult TrySignUp(
            string email,
            string password,
            string confirmation,
            Func<string, bool> tryInitializeAccountData)
        {
            EnsureInitialized();
            return service.TrySignUp(
                email,
                password,
                confirmation,
                tryInitializeAccountData);
        }

        public static LocalAccountAttemptResult TrySignIn(string email, string password)
        {
            EnsureInitialized();
            return service.TrySignIn(email, password);
        }

        public static LocalAccountAttemptResult TrySignIn(
            string email,
            string password,
            Func<bool> tryPrepareCurrentAccountData)
        {
            EnsureInitialized();
            return service.TrySignIn(
                email,
                password,
                tryPrepareCurrentAccountData);
        }

        public static LocalAccountAttemptResult TrySignOut()
        {
            EnsureInitialized();
            return service.TrySignOut();
        }

        public static LocalAccountAttemptResult TryDelete(
            string password,
            string confirmation)
        {
            EnsureInitialized();
            return service.TryDelete(password, confirmation);
        }

        public static LocalAccountAttemptResult TryDelete(
            string password,
            string confirmation,
            Func<string, bool> tryPurgeAccountData)
        {
            EnsureInitialized();
            return service.TryDelete(password, confirmation, tryPurgeAccountData);
        }

        public static LocalAccountAttemptResult TryResumePendingDeletion(
            Func<string, bool> tryPurgeAccountData)
        {
            EnsureInitialized();
            return service.TryResumePendingDeletion(tryPurgeAccountData);
        }

        public static bool IsValidAccountId(string accountId)
        {
            return LocalAccountId.IsValid(accountId);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAtSubsystemRegistration()
        {
            service = null;
        }

        private static LocalAccountService CreateDefaultService()
        {
            return new LocalAccountService(
                new PlayerPrefsLocalAccountCredentialStore(),
                new Pbkdf2Sha256PasswordHasher());
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public static void ConfigureForTests(
            ILocalAccountCredentialStore credentialStore,
            ILocalAccountPasswordHasher passwordHasher,
            Func<DateTimeOffset> utcNow = null,
            Func<string> accountIdFactory = null)
        {
            service = new LocalAccountService(
                credentialStore,
                passwordHasher,
                utcNow,
                accountIdFactory);
        }

        public static void ResetForTests()
        {
            service = null;
        }
#endif
    }
}
