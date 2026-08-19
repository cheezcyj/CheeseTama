using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CheeseTama.Gameplay.Reset
{
    public enum ProgressResetMode
    {
        CareProgressOnly = 0,
        FullLocalData = 1
    }

    public enum ProgressResetPreviewStatus
    {
        Supported = 0,
        UnsupportedMode = 1
    }

    public enum ProgressResetDataCategory
    {
        PlayerIdentity = 0,
        GameSettings = 1,
        MilkroomThemeAndDecorations = 2,
        CheeseTamaGrowthAndCare = 3,
        InventoryEconomyAndUnlocks = 4,
        CollectionsAndLifeRecords = 5,
        JourneysNpcAndStoryProgress = 6,
        ActiveSessionsAndPendingEvents = 7
    }

    public sealed class ProgressResetPreview
    {
        internal ProgressResetPreview(
            ProgressResetPreviewStatus status,
            ProgressResetMode mode,
            string title,
            string confirmationPhrase,
            IList<ProgressResetDataCategory> preservedCategories,
            IList<ProgressResetDataCategory> resetCategories)
        {
            Status = status;
            Mode = mode;
            Title = title ?? string.Empty;
            ConfirmationPhrase = confirmationPhrase ?? string.Empty;
            PreservedCategories = Copy(preservedCategories);
            ResetCategories = Copy(resetCategories);
        }

        public ProgressResetPreviewStatus Status { get; }
        public ProgressResetMode Mode { get; }
        public string Title { get; }
        public string ConfirmationPhrase { get; }
        public IReadOnlyList<ProgressResetDataCategory> PreservedCategories { get; }
        public IReadOnlyList<ProgressResetDataCategory> ResetCategories { get; }
        public bool IsSupported => Status == ProgressResetPreviewStatus.Supported;
        public bool IsDestructive => IsSupported && ResetCategories.Count > 0;

        public bool Preserves(ProgressResetDataCategory category)
        {
            return Contains(PreservedCategories, category);
        }

        public bool Resets(ProgressResetDataCategory category)
        {
            return Contains(ResetCategories, category);
        }

        private static IReadOnlyList<ProgressResetDataCategory> Copy(
            IList<ProgressResetDataCategory> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<ProgressResetDataCategory>());
            }

            var copy = new ProgressResetDataCategory[source.Count];
            source.CopyTo(copy, 0);
            return new ReadOnlyCollection<ProgressResetDataCategory>(copy);
        }

        private static bool Contains(
            IReadOnlyList<ProgressResetDataCategory> values,
            ProgressResetDataCategory expected)
        {
            for (var index = 0; index < values.Count; index += 1)
            {
                if (values[index] == expected)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum ProgressResetResultStatus
    {
        Applied = 0,
        NoChanges = 1,
        MissingState = 2,
        ConfirmationMismatch = 3,
        UnsupportedMode = 4,
        PersistenceFailed = 5
    }

    public sealed class ProgressResetResult
    {
        private ProgressResetResult(
            ProgressResetResultStatus status,
            ProgressResetPreview preview,
            string message)
        {
            Status = status;
            Preview = preview ?? ProgressResetPolicy.BuildPreview((ProgressResetMode)(-1));
            Message = message ?? string.Empty;
        }

        public ProgressResetResultStatus Status { get; }
        public ProgressResetPreview Preview { get; }
        public ProgressResetMode Mode => Preview.Mode;
        public string Message { get; }
        public bool Succeeded => Status == ProgressResetResultStatus.Applied
            || Status == ProgressResetResultStatus.NoChanges;
        public bool StateChanged => Status == ProgressResetResultStatus.Applied;

        public static ProgressResetResult CreateApplied(
            ProgressResetPreview preview,
            bool stateChanged,
            string message = "")
        {
            if (preview == null || !preview.IsSupported)
            {
                return CreateFailure(
                    ProgressResetResultStatus.UnsupportedMode,
                    preview,
                    string.IsNullOrWhiteSpace(message)
                        ? "지원하지 않는 초기화 방식입니다."
                        : message);
            }

            return new ProgressResetResult(
                stateChanged
                    ? ProgressResetResultStatus.Applied
                    : ProgressResetResultStatus.NoChanges,
                preview,
                message);
        }

        public static ProgressResetResult CreateFailure(
            ProgressResetResultStatus status,
            ProgressResetPreview preview,
            string message = "")
        {
            if (status == ProgressResetResultStatus.Applied
                || status == ProgressResetResultStatus.NoChanges)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "실패 결과에는 성공 상태를 사용할 수 없습니다.");
            }

            return new ProgressResetResult(status, preview, message);
        }
    }

    /// <summary>
    /// Pure reset policy used by UI preview and the authoritative save owner. It never
    /// mutates or persists a save. Unknown enum values fail closed with no reset categories.
    /// </summary>
    public static class ProgressResetPolicy
    {
        public const string CareProgressConfirmationPhrase = "RESET TAMA";
        public const string FullLocalDataConfirmationPhrase = "RESET ALL";

        private static readonly ProgressResetDataCategory[] CareProgressPreserved =
        {
            ProgressResetDataCategory.PlayerIdentity,
            ProgressResetDataCategory.GameSettings,
            ProgressResetDataCategory.MilkroomThemeAndDecorations,
            ProgressResetDataCategory.InventoryEconomyAndUnlocks,
            ProgressResetDataCategory.CollectionsAndLifeRecords,
            ProgressResetDataCategory.JourneysNpcAndStoryProgress
        };

        private static readonly ProgressResetDataCategory[] CareProgressReset =
        {
            ProgressResetDataCategory.CheeseTamaGrowthAndCare,
            ProgressResetDataCategory.ActiveSessionsAndPendingEvents
        };

        private static readonly ProgressResetDataCategory[] FullLocalDataReset =
        {
            ProgressResetDataCategory.PlayerIdentity,
            ProgressResetDataCategory.GameSettings,
            ProgressResetDataCategory.MilkroomThemeAndDecorations,
            ProgressResetDataCategory.CheeseTamaGrowthAndCare,
            ProgressResetDataCategory.InventoryEconomyAndUnlocks,
            ProgressResetDataCategory.CollectionsAndLifeRecords,
            ProgressResetDataCategory.JourneysNpcAndStoryProgress,
            ProgressResetDataCategory.ActiveSessionsAndPendingEvents
        };

        public static ProgressResetPreview BuildPreview(ProgressResetMode mode)
        {
            switch (mode)
            {
                case ProgressResetMode.CareProgressOnly:
                    return new ProgressResetPreview(
                        ProgressResetPreviewStatus.Supported,
                        mode,
                        "육성만 새로 시작",
                        CareProgressConfirmationPhrase,
                        CareProgressPreserved,
                        CareProgressReset);
                case ProgressResetMode.FullLocalData:
                    return new ProgressResetPreview(
                        ProgressResetPreviewStatus.Supported,
                        mode,
                        "전체 초기화",
                        FullLocalDataConfirmationPhrase,
                        null,
                        FullLocalDataReset);
                default:
                    return new ProgressResetPreview(
                        ProgressResetPreviewStatus.UnsupportedMode,
                        mode,
                        "지원하지 않는 초기화 방식",
                        string.Empty,
                        null,
                        null);
            }
        }

        public static bool MatchesConfirmation(
            ProgressResetPreview preview,
            string requestedConfirmation)
        {
            return preview != null
                && preview.IsSupported
                && !string.IsNullOrEmpty(preview.ConfirmationPhrase)
                && string.Equals(
                    requestedConfirmation?.Trim(),
                    preview.ConfirmationPhrase,
                    StringComparison.Ordinal);
        }

        public static string BuildSummary(ProgressResetPreview preview)
        {
            if (preview == null || !preview.IsSupported)
            {
                return "지원하지 않는 초기화 방식이라 실행할 수 없습니다.";
            }

            var builder = new StringBuilder();
            builder.Append(preview.Title);
            builder.Append("\n\n초기화: ");
            AppendLabels(builder, preview.ResetCategories);
            if (preview.PreservedCategories.Count > 0)
            {
                builder.Append("\n\n보존: ");
                AppendLabels(builder, preview.PreservedCategories);
            }

            builder.Append("\n\n계속하려면 ");
            builder.Append(preview.ConfirmationPhrase);
            builder.Append("을 정확히 입력하세요.");
            return builder.ToString();
        }

        public static string GetCategoryLabel(ProgressResetDataCategory category)
        {
            return category switch
            {
                ProgressResetDataCategory.PlayerIdentity => "로컬 플레이어 식별 정보",
                ProgressResetDataCategory.GameSettings => "화면·소리·조작·접근성 설정",
                ProgressResetDataCategory.MilkroomThemeAndDecorations => "밀크룸 테마와 꾸미기",
                ProgressResetDataCategory.CheeseTamaGrowthAndCare => "현재 치즈타마와 육성 기록",
                ProgressResetDataCategory.InventoryEconomyAndUnlocks => "재화·소지품·해금",
                ProgressResetDataCategory.CollectionsAndLifeRecords => "도감과 생활 기록 앨범",
                ProgressResetDataCategory.JourneysNpcAndStoryProgress => "여정·NPC 관계·이야기",
                ProgressResetDataCategory.ActiveSessionsAndPendingEvents => "진행 중 세션과 대기 이벤트",
                _ => string.Empty
            };
        }

        private static void AppendLabels(
            StringBuilder builder,
            IReadOnlyList<ProgressResetDataCategory> categories)
        {
            for (var index = 0; index < categories.Count; index += 1)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GetCategoryLabel(categories[index]));
            }
        }
    }
}
