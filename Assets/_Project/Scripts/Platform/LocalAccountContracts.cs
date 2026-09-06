using System;
using System.Security.Cryptography;
using System.Text;

namespace CheeseTama.Platform.Accounts
{
    public enum LocalAccountAttemptStatus
    {
        Success = 0,
        InvalidEmail = 1,
        WeakPassword = 2,
        ConfirmationMismatch = 3,
        AccountAlreadyExists = 4,
        InvalidCredentials = 5,
        SessionRequired = 6,
        AccountLimitReached = 7,
        AccountDataOperationFailed = 8,
        StorageUnavailable = 9,
        SecurityUnavailable = 10,
        DeletionPending = 11,
        NoPendingDeletion = 12
    }

    public enum LocalAccountPasswordFailure
    {
        None = 0,
        TooShort = 1,
        TooLong = 2,
        MissingLetter = 3,
        MissingDigit = 4,
        MissingSymbol = 5,
        ContainsControlCharacter = 6
    }

    public enum LocalAccountPasswordVerificationStatus
    {
        Verified = 0,
        Mismatch = 1,
        UnsupportedHash = 2
    }

    public enum LocalAccountCredentialStoreStatus
    {
        Success = 0,
        NotFound = 1,
        AlreadyExists = 2,
        CapacityReached = 3,
        CorruptData = 4,
        Unavailable = 5,
        PendingDeletion = 6
    }

    public readonly struct LocalAccountSnapshot
    {
        internal LocalAccountSnapshot(
            bool initialized,
            bool storageAvailable,
            bool signedIn,
            bool restoredFromRegistry,
            string accountId,
            string normalizedEmail,
            string pendingDeletionAccountId)
        {
            Initialized = initialized;
            StorageAvailable = storageAvailable;
            IsSignedIn = signedIn;
            RestoredFromRegistry = restoredFromRegistry;
            AccountId = accountId ?? string.Empty;
            NormalizedEmail = normalizedEmail ?? string.Empty;
            PendingDeletionAccountId = pendingDeletionAccountId ?? string.Empty;
        }

        public bool Initialized { get; }
        public bool StorageAvailable { get; }
        public bool IsSignedIn { get; }
        public bool RestoredFromRegistry { get; }
        public string AccountId { get; }
        public string NormalizedEmail { get; }
        public string NormalizedLoginId => NormalizedEmail;
        public string PendingDeletionAccountId { get; }
        public bool HasPendingDeletion => !string.IsNullOrEmpty(PendingDeletionAccountId);

        public static LocalAccountSnapshot Uninitialized { get; } =
            new LocalAccountSnapshot(
                false,
                true,
                false,
                false,
                string.Empty,
                string.Empty,
                string.Empty);
    }

    public readonly struct LocalAccountAttemptResult
    {
        internal LocalAccountAttemptResult(
            LocalAccountAttemptStatus status,
            LocalAccountSnapshot snapshot,
            LocalAccountPasswordFailure passwordFailure = LocalAccountPasswordFailure.None)
        {
            Status = status;
            Snapshot = snapshot;
            PasswordFailure = passwordFailure;
        }

        public LocalAccountAttemptStatus Status { get; }
        public LocalAccountSnapshot Snapshot { get; }
        public LocalAccountPasswordFailure PasswordFailure { get; }
        public bool Succeeded => Status == LocalAccountAttemptStatus.Success;
    }

    public sealed class LocalAccountPasswordHash
    {
        public const int MaximumStoredIterations = 1000000;
        public const int MaximumAlgorithmIdLength = 64;
        public const int MaximumBase64Length = 256;

        public LocalAccountPasswordHash(
            string algorithmId,
            int iterations,
            string saltBase64,
            string hashBase64)
        {
            AlgorithmId = algorithmId ?? string.Empty;
            Iterations = iterations;
            SaltBase64 = saltBase64 ?? string.Empty;
            HashBase64 = hashBase64 ?? string.Empty;
        }

        public string AlgorithmId { get; }
        public int Iterations { get; }
        public string SaltBase64 { get; }
        public string HashBase64 { get; }

        public bool IsStructurallyValid()
        {
            return IsAlgorithmId(AlgorithmId)
                && Iterations >= 1
                && Iterations <= MaximumStoredIterations
                && HasDecodedLength(SaltBase64, 16, 64)
                && HasDecodedLength(HashBase64, 16, 128);
        }

        private static bool IsAlgorithmId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaximumAlgorithmIdLength)
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var valid = (character >= 'a' && character <= 'z')
                    || (character >= '0' && character <= '9')
                    || character == '-'
                    || character == '_'
                    || character == '.';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasDecodedLength(string value, int minimum, int maximum)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaximumBase64Length)
            {
                return false;
            }

            byte[] decoded = null;
            try
            {
                decoded = Convert.FromBase64String(value);
                return decoded.Length >= minimum && decoded.Length <= maximum;
            }
            catch (FormatException)
            {
                return false;
            }
            finally
            {
                if (decoded != null)
                {
                    Array.Clear(decoded, 0, decoded.Length);
                }
            }
        }
    }

    public sealed class LocalAccountCredentialRecord
    {
        public LocalAccountCredentialRecord(
            string accountId,
            string normalizedEmail,
            LocalAccountPasswordHash passwordHash,
            DateTimeOffset createdAtUtc)
        {
            AccountId = accountId ?? string.Empty;
            NormalizedEmail = normalizedEmail ?? string.Empty;
            PasswordHash = passwordHash;
            CreatedAtUtc = createdAtUtc.ToUniversalTime();
        }

        public string AccountId { get; }
        public string NormalizedEmail { get; }
        public string NormalizedLoginId => NormalizedEmail;
        public LocalAccountPasswordHash PasswordHash { get; }
        public DateTimeOffset CreatedAtUtc { get; }

        public bool IsStructurallyValid()
        {
            return LocalAccountId.IsValid(AccountId)
                && LocalAccountLoginId.TryNormalize(NormalizedEmail, out var normalized)
                && string.Equals(normalized, NormalizedEmail, StringComparison.Ordinal)
                && PasswordHash?.IsStructurallyValid() == true
                && CreatedAtUtc != default;
        }
    }

    public interface ILocalAccountPasswordHasher
    {
        LocalAccountPasswordHash CreateHash(string password);

        LocalAccountPasswordVerificationStatus Verify(
            string password,
            LocalAccountPasswordHash passwordHash);
    }

    public interface ILocalAccountCredentialStore
    {
        LocalAccountCredentialStoreStatus TryGetByEmail(
            string normalizedEmail,
            out LocalAccountCredentialRecord credential);

        LocalAccountCredentialStoreStatus TryGetByAccountId(
            string accountId,
            out LocalAccountCredentialRecord credential);

        LocalAccountCredentialStoreStatus TryGetActiveAccount(
            out LocalAccountCredentialRecord credential);

        LocalAccountCredentialStoreStatus TryGetPendingDeletionAccountId(
            out string accountId);

        LocalAccountCredentialStoreStatus TryAddAndActivate(
            LocalAccountCredentialRecord credential);

        LocalAccountCredentialStoreStatus TryActivate(string accountId);

        LocalAccountCredentialStoreStatus TryClearActiveAccount();

        LocalAccountCredentialStoreStatus TryDeleteDeactivateAndCreateTombstone(
            string accountId);

        LocalAccountCredentialStoreStatus TryClearDeletionTombstone(string accountId);
    }

    public static class LocalAccountId
    {
        public const int GuidNLength = 32;

        public static string Create()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static bool IsValid(string value)
        {
            return value != null
                && value.Length == GuidNLength
                && Guid.TryParseExact(value, "N", out var parsed)
                && parsed != Guid.Empty
                && string.Equals(value, parsed.ToString("N"), StringComparison.Ordinal);
        }
    }

    public static class LocalAccountEmail
    {
        public const int MaximumLength = 254;
        private const int MaximumLocalPartLength = 64;
        private const int MaximumRawLength = 512;

        public static bool TryNormalize(string value, out string normalizedEmail)
        {
            normalizedEmail = string.Empty;
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumRawLength)
            {
                return false;
            }

            var normalized = value.Trim().ToLowerInvariant();
            if (normalized.Length == 0 || normalized.Length > MaximumLength)
            {
                return false;
            }

            var atIndex = normalized.IndexOf('@');
            if (atIndex <= 0
                || atIndex > MaximumLocalPartLength
                || atIndex != normalized.LastIndexOf('@')
                || atIndex >= normalized.Length - 1)
            {
                return false;
            }

            var localPart = normalized.Substring(0, atIndex);
            var domain = normalized.Substring(atIndex + 1);
            if (!IsLocalPart(localPart) || !IsDomain(domain))
            {
                return false;
            }

            normalizedEmail = normalized;
            return true;
        }

        private static bool IsLocalPart(string value)
        {
            if (value.Length == 0
                || value[0] == '.'
                || value[value.Length - 1] == '.'
                || value.Contains(".."))
            {
                return false;
            }

            for (var index = 0; index < value.Length; index += 1)
            {
                var character = value[index];
                var valid = IsAsciiAlphaNumeric(character)
                    || character == '.'
                    || character == '_'
                    || character == '%'
                    || character == '+'
                    || character == '-';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDomain(string value)
        {
            if (value.Length < 3
                || value[0] == '.'
                || value[value.Length - 1] == '.'
                || value.Contains(".."))
            {
                return false;
            }

            var labels = value.Split('.');
            if (labels.Length < 2 || labels[labels.Length - 1].Length < 2)
            {
                return false;
            }

            for (var labelIndex = 0; labelIndex < labels.Length; labelIndex += 1)
            {
                var label = labels[labelIndex];
                if (label.Length == 0
                    || label.Length > 63
                    || label[0] == '-'
                    || label[label.Length - 1] == '-')
                {
                    return false;
                }

                for (var index = 0; index < label.Length; index += 1)
                {
                    if (!IsAsciiAlphaNumeric(label[index]) && label[index] != '-')
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsAsciiAlphaNumeric(char value)
        {
            return (value >= 'a' && value <= 'z')
                || (value >= '0' && value <= '9');
        }
    }

    /// <summary>
    /// Normalizes the player-facing login identifier. Existing email-shaped identifiers keep
    /// their strict validation, while simple local identifiers may use letters from any language,
    /// numbers, periods, underscores, and hyphens. The persisted property remains
    /// <c>NormalizedEmail</c> so version 1 browser registries continue to load unchanged.
    /// </summary>
    public static class LocalAccountLoginId
    {
        public const int MaximumSimpleIdLength = 32;
        private const int MaximumRawLength = 512;

        public static bool TryNormalize(string value, out string normalizedLoginId)
        {
            normalizedLoginId = string.Empty;
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumRawLength)
            {
                return false;
            }

            var trimmed = value.Trim();
            if (trimmed.IndexOf('@') >= 0)
            {
                return LocalAccountEmail.TryNormalize(trimmed, out normalizedLoginId);
            }

            string normalized;
            try
            {
                normalized = trimmed.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (normalized.Length == 0 || normalized.Length > MaximumSimpleIdLength)
            {
                return false;
            }

            for (var index = 0; index < normalized.Length; index += 1)
            {
                var character = normalized[index];
                var valid = char.IsLetterOrDigit(character)
                    || character == '.'
                    || character == '_'
                    || character == '-';
                if (!valid)
                {
                    return false;
                }
            }

            if (!char.IsLetterOrDigit(normalized[0])
                || !char.IsLetterOrDigit(normalized[normalized.Length - 1]))
            {
                return false;
            }

            normalizedLoginId = normalized;
            return true;
        }
    }

    public static class LocalAccountPasswordPolicy
    {
        public const int MinimumLength = 6;
        public const int MaximumLength = 128;
        public const string Description =
            "비밀번호는 글자 종류에 상관없이 6~128글자로 만들어 주세요.";

        public static bool TryValidate(
            string password,
            out LocalAccountPasswordFailure failure)
        {
            failure = LocalAccountPasswordFailure.None;
            if (password == null || password.Length < MinimumLength)
            {
                failure = LocalAccountPasswordFailure.TooShort;
                return false;
            }

            if (password.Length > MaximumLength)
            {
                failure = LocalAccountPasswordFailure.TooLong;
                return false;
            }

            for (var index = 0; index < password.Length; index += 1)
            {
                var character = password[index];
                if (char.IsControl(character))
                {
                    failure = LocalAccountPasswordFailure.ContainsControlCharacter;
                    return false;
                }
            }

            return true;
        }

        internal static bool IsAcceptableForVerification(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length > MaximumLength)
            {
                return false;
            }

            for (var index = 0; index < password.Length; index += 1)
            {
                if (char.IsControl(password[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class Pbkdf2Sha256PasswordHasher : ILocalAccountPasswordHasher
    {
        public const string AlgorithmId = "pbkdf2-sha256-v1";
        public const int DefaultIterations = 120000;
        public const int MinimumAcceptedIterations = 10000;
        public const int SaltSizeBytes = 16;
        public const int HashSizeBytes = 32;

        public LocalAccountPasswordHash CreateHash(string password)
        {
            if (!LocalAccountPasswordPolicy.IsAcceptableForVerification(password))
            {
                throw new ArgumentException("A bounded password is required.", nameof(password));
            }

            var salt = new byte[SaltSizeBytes];
            byte[] hash = null;
            try
            {
                using (var random = RandomNumberGenerator.Create())
                {
                    random.GetBytes(salt);
                }

                using (var derive = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    DefaultIterations,
                    HashAlgorithmName.SHA256))
                {
                    hash = derive.GetBytes(HashSizeBytes);
                }

                return new LocalAccountPasswordHash(
                    AlgorithmId,
                    DefaultIterations,
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(hash));
            }
            finally
            {
                Array.Clear(salt, 0, salt.Length);
                if (hash != null)
                {
                    Array.Clear(hash, 0, hash.Length);
                }
            }
        }

        public LocalAccountPasswordVerificationStatus Verify(
            string password,
            LocalAccountPasswordHash passwordHash)
        {
            if (!LocalAccountPasswordPolicy.IsAcceptableForVerification(password)
                || passwordHash?.IsStructurallyValid() != true
                || !string.Equals(
                    passwordHash.AlgorithmId,
                    AlgorithmId,
                    StringComparison.Ordinal)
                || passwordHash.Iterations < MinimumAcceptedIterations
                || passwordHash.Iterations > LocalAccountPasswordHash.MaximumStoredIterations)
            {
                return LocalAccountPasswordVerificationStatus.UnsupportedHash;
            }

            byte[] salt = null;
            byte[] expected = null;
            byte[] actual = null;
            try
            {
                salt = Convert.FromBase64String(passwordHash.SaltBase64);
                expected = Convert.FromBase64String(passwordHash.HashBase64);
                if (salt.Length < SaltSizeBytes || expected.Length != HashSizeBytes)
                {
                    return LocalAccountPasswordVerificationStatus.UnsupportedHash;
                }

                using (var derive = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    passwordHash.Iterations,
                    HashAlgorithmName.SHA256))
                {
                    actual = derive.GetBytes(expected.Length);
                }

                return FixedTimeEquals(actual, expected)
                    ? LocalAccountPasswordVerificationStatus.Verified
                    : LocalAccountPasswordVerificationStatus.Mismatch;
            }
            catch (FormatException)
            {
                return LocalAccountPasswordVerificationStatus.UnsupportedHash;
            }
            finally
            {
                Clear(salt);
                Clear(expected);
                Clear(actual);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            var different = left.Length ^ right.Length;
            var length = Math.Max(left.Length, right.Length);
            for (var index = 0; index < length; index += 1)
            {
                var leftValue = index < left.Length ? left[index] : 0;
                var rightValue = index < right.Length ? right[index] : 0;
                different |= leftValue ^ rightValue;
            }

            return different == 0;
        }

        private static void Clear(byte[] value)
        {
            if (value != null)
            {
                Array.Clear(value, 0, value.Length);
            }
        }
    }
}
