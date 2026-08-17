using System;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.Sleep
{
    public enum SleepScheduleStartStatus
    {
        Started = 0,
        MissingSaveData = 1,
        MissingTama = 2,
        NotHatched = 3,
        InvalidDuration = 4,
        MissingReceiptKey = 5,
        ReceiptAlreadyUsed = 6,
        AlreadySleeping = 7,
        InvalidClock = 8
    }

    public enum SleepScheduleWakeStatus
    {
        Completed = 0,
        WokeEarly = 1,
        AlreadyApplied = 2,
        NotDue = 3,
        NoActiveSession = 4,
        MissingSaveData = 5,
        MissingTama = 6,
        NotHatched = 7,
        InvalidState = 8,
        InvalidClock = 9
    }

    public readonly struct SleepScheduleStartResult
    {
        internal SleepScheduleStartResult(
            SleepScheduleStartStatus status,
            string receiptKey,
            int scheduledHours,
            DateTimeOffset sleepStartedAt,
            DateTimeOffset plannedWakeAt,
            bool stateChanged,
            string message)
        {
            Status = status;
            ReceiptKey = receiptKey ?? string.Empty;
            ScheduledHours = Math.Max(0, scheduledHours);
            SleepStartedAt = sleepStartedAt;
            PlannedWakeAt = plannedWakeAt;
            StateChanged = stateChanged;
            Message = message ?? string.Empty;
        }

        public SleepScheduleStartStatus Status { get; }
        public string ReceiptKey { get; }
        public int ScheduledHours { get; }
        public DateTimeOffset SleepStartedAt { get; }
        public DateTimeOffset PlannedWakeAt { get; }
        public bool StateChanged { get; }
        public string Message { get; }
        public bool Started => Status == SleepScheduleStartStatus.Started;
    }

    public readonly struct SleepScheduleWakeResult
    {
        internal SleepScheduleWakeResult(
            SleepScheduleWakeStatus status,
            string receiptKey,
            int scheduledHours,
            int elapsedMinutes,
            int sleepinessDelta,
            int healthDelta,
            int moodDelta,
            bool stateChanged,
            SleepRecoveryReceiptSaveEntry receipt,
            string message)
        {
            Status = status;
            ReceiptKey = receiptKey ?? string.Empty;
            ScheduledHours = Math.Max(0, scheduledHours);
            ElapsedMinutes = Math.Max(0, elapsedMinutes);
            SleepinessDelta = Math.Min(0, sleepinessDelta);
            HealthDelta = Math.Max(0, healthDelta);
            MoodDelta = Math.Max(0, moodDelta);
            StateChanged = stateChanged;
            Receipt = receipt;
            Message = message ?? string.Empty;
        }

        public SleepScheduleWakeStatus Status { get; }
        public string ReceiptKey { get; }
        public int ScheduledHours { get; }
        public int ElapsedMinutes { get; }
        public int SleepinessDelta { get; }
        public int HealthDelta { get; }
        public int MoodDelta { get; }
        public bool StateChanged { get; }
        public SleepRecoveryReceiptSaveEntry Receipt { get; }
        public string Message { get; }
        public bool Applied => Status == SleepScheduleWakeStatus.Completed
            || Status == SleepScheduleWakeStatus.WokeEarly;
        public bool WasEarlyWake => Status == SleepScheduleWakeStatus.WokeEarly;
    }

    public readonly struct SleepScheduleSnapshot
    {
        internal SleepScheduleSnapshot(
            bool stateWasNormalized,
            bool isHatched,
            bool isSleeping,
            string receiptKey,
            int scheduledHours,
            DateTimeOffset sleepStartedAt,
            DateTimeOffset plannedWakeAt,
            int elapsedMinutes,
            int remainingMinutes,
            bool isDue)
        {
            StateWasNormalized = stateWasNormalized;
            IsHatched = isHatched;
            IsSleeping = isSleeping;
            ReceiptKey = receiptKey ?? string.Empty;
            ScheduledHours = Math.Max(0, scheduledHours);
            SleepStartedAt = sleepStartedAt;
            PlannedWakeAt = plannedWakeAt;
            ElapsedMinutes = Math.Max(0, elapsedMinutes);
            RemainingMinutes = Math.Max(0, remainingMinutes);
            IsDue = isDue;
        }

        public bool StateWasNormalized { get; }
        public bool IsHatched { get; }
        public bool IsSleeping { get; }
        public string ReceiptKey { get; }
        public int ScheduledHours { get; }
        public DateTimeOffset SleepStartedAt { get; }
        public DateTimeOffset PlannedWakeAt { get; }
        public int ElapsedMinutes { get; }
        public int RemainingMinutes { get; }
        public bool IsDue { get; }
        public bool CanStart => IsHatched && !IsSleeping;
        // An invalid legacy/cross-generation session must remain dismissible so
        // the player can reach the system's safe NotHatched cleanup path.
        public bool CanWake => IsSleeping;
        public bool CanWakeEarly => CanWake && !IsDue;
    }

    /// <summary>
    /// Deterministic sleep scheduling and recovery rules. Save ownership, wall
    /// clock ownership, visual presentation, and persistence remain with callers.
    /// </summary>
    public sealed class SleepScheduleSystem
    {
        public const int SleepinessRecoveryPerHour = 15;
        public const int HealthRecoveryPerHour = 1;
        public const int MoodRecoveryIntervalMinutes = 120;

        public SleepScheduleStartResult TryStart(
            SleepScheduleSaveData saveData,
            CheeseTamaModel tama,
            int scheduledHours,
            string receiptKey,
            DateTimeOffset now)
        {
            if (saveData == null)
            {
                return StartRejected(
                    SleepScheduleStartStatus.MissingSaveData,
                    "수면 저장 정보를 찾을 수 없어요.");
            }

            var stateChanged = saveData.EnsureRuntimeDefaults(now);
            if (tama?.stats == null)
            {
                return StartRejected(
                    SleepScheduleStartStatus.MissingTama,
                    "치즈타마 상태를 확인할 수 없어요.",
                    stateChanged);
            }

            if (!tama.isHatched)
            {
                return StartRejected(
                    SleepScheduleStartStatus.NotHatched,
                    "부화한 뒤 수면 예약을 이용할 수 있어요.",
                    stateChanged);
            }

            if (scheduledHours < SleepScheduleSaveData.MinimumScheduledHours
                || scheduledHours > SleepScheduleSaveData.MaximumScheduledHours)
            {
                return StartRejected(
                    SleepScheduleStartStatus.InvalidDuration,
                    "수면 시간은 1시간부터 8시간까지 선택해 주세요.",
                    stateChanged);
            }

            if (saveData.HasActiveSession)
            {
                return StartRejected(
                    SleepScheduleStartStatus.AlreadySleeping,
                    "이미 수면 예약이 진행 중이에요.",
                    stateChanged);
            }

            var normalizedReceiptKey = NormalizeKey(receiptKey);
            if (string.IsNullOrEmpty(normalizedReceiptKey))
            {
                return StartRejected(
                    SleepScheduleStartStatus.MissingReceiptKey,
                    "수면 기록 키가 없어 예약을 시작하지 않았어요.",
                    stateChanged);
            }

            if (saveData.HasAppliedReceipt(normalizedReceiptKey))
            {
                return StartRejected(
                    SleepScheduleStartStatus.ReceiptAlreadyUsed,
                    "이미 처리된 수면 기록이에요.",
                    stateChanged);
            }

            DateTimeOffset plannedWakeAt;
            try
            {
                plannedWakeAt = now.AddHours(scheduledHours);
            }
            catch (ArgumentOutOfRangeException)
            {
                return StartRejected(
                    SleepScheduleStartStatus.InvalidClock,
                    "현재 시각을 확인할 수 없어 예약을 시작하지 않았어요.",
                    stateChanged);
            }

            if (!saveData.TryBeginSession(
                normalizedReceiptKey,
                now,
                plannedWakeAt,
                scheduledHours))
            {
                return StartRejected(
                    saveData.HasActiveSession
                        ? SleepScheduleStartStatus.AlreadySleeping
                        : SleepScheduleStartStatus.InvalidClock,
                    saveData.HasActiveSession
                        ? "이미 수면 예약이 진행 중이에요."
                        : "수면 예약 정보를 안전하게 저장하지 못했어요.",
                    stateChanged);
            }

            return new SleepScheduleStartResult(
                SleepScheduleStartStatus.Started,
                normalizedReceiptKey,
                scheduledHours,
                now,
                plannedWakeAt,
                true,
                $"{scheduledHours}시간 수면 예약을 시작했어요.");
        }

        public SleepScheduleWakeResult TryCompleteDue(
            SleepScheduleSaveData saveData,
            CheeseTamaModel tama,
            DateTimeOffset now)
        {
            return TryWake(saveData, tama, now, allowEarlyWake: false);
        }

        public SleepScheduleWakeResult TryWakeEarly(
            SleepScheduleSaveData saveData,
            CheeseTamaModel tama,
            DateTimeOffset now)
        {
            return TryWake(saveData, tama, now, allowEarlyWake: true);
        }

        public SleepScheduleSnapshot BuildSnapshot(
            SleepScheduleSaveData saveData,
            CheeseTamaModel tama,
            DateTimeOffset now)
        {
            if (saveData == null)
            {
                return new SleepScheduleSnapshot(
                    false,
                    tama?.isHatched == true,
                    false,
                    string.Empty,
                    0,
                    default,
                    default,
                    0,
                    0,
                    false);
            }

            var stateWasNormalized = saveData.EnsureRuntimeDefaults(now);
            var session = saveData.activeSession;
            if (session == null
                || !TryReadSession(
                    session,
                    out var startedAt,
                    out var plannedWakeAt))
            {
                return new SleepScheduleSnapshot(
                    stateWasNormalized,
                    tama?.isHatched == true,
                    false,
                    string.Empty,
                    0,
                    default,
                    default,
                    0,
                    0,
                    false);
            }

            var creditedThrough = now < plannedWakeAt ? now : plannedWakeAt;
            var elapsedMinutes = CalculateElapsedMinutes(startedAt, creditedThrough);
            var isDue = now >= plannedWakeAt;
            var remainingMinutes = isDue
                ? 0
                : Math.Max(
                    1,
                    (int)Math.Ceiling((plannedWakeAt - now).TotalMinutes));
            return new SleepScheduleSnapshot(
                stateWasNormalized,
                tama?.isHatched == true,
                true,
                session.receiptKey,
                session.scheduledHours,
                startedAt,
                plannedWakeAt,
                elapsedMinutes,
                remainingMinutes,
                isDue);
        }

        private SleepScheduleWakeResult TryWake(
            SleepScheduleSaveData saveData,
            CheeseTamaModel tama,
            DateTimeOffset now,
            bool allowEarlyWake)
        {
            if (saveData == null)
            {
                return WakeRejected(
                    SleepScheduleWakeStatus.MissingSaveData,
                    "수면 저장 정보를 찾을 수 없어요.");
            }

            var originalSession = saveData.activeSession;
            var originalIssue = InspectSession(originalSession, now);
            var originalReceiptKey = NormalizeKey(originalSession?.receiptKey);
            if (!string.IsNullOrEmpty(originalReceiptKey)
                && saveData.HasAppliedReceipt(originalReceiptKey))
            {
                saveData.ClearActiveSession();
                return new SleepScheduleWakeResult(
                    SleepScheduleWakeStatus.AlreadyApplied,
                    originalReceiptKey,
                    originalSession?.scheduledHours ?? 0,
                    0,
                    0,
                    0,
                    0,
                    true,
                    saveData.FindReceipt(originalReceiptKey),
                    "이미 반영된 수면 회복이에요.");
            }

            var stateChanged = saveData.EnsureRuntimeDefaults(now);
            var session = saveData.activeSession;
            if (session == null)
            {
                if (originalIssue == ActiveSessionIssue.FutureStart)
                {
                    return WakeRejected(
                        SleepScheduleWakeStatus.InvalidClock,
                        "취침 시각이 현재보다 미래라 예약을 안전하게 취소했어요.",
                        true);
                }

                if (originalIssue == ActiveSessionIssue.Invalid)
                {
                    return WakeRejected(
                        SleepScheduleWakeStatus.InvalidState,
                        "잘못된 수면 예약을 안전하게 정리했어요.",
                        true);
                }

                return WakeRejected(
                    SleepScheduleWakeStatus.NoActiveSession,
                    "진행 중인 수면 예약이 없어요.",
                    stateChanged);
            }

            if (tama?.stats == null)
            {
                return WakeRejected(
                    SleepScheduleWakeStatus.MissingTama,
                    "치즈타마 상태를 확인할 수 없어요.",
                    stateChanged);
            }

            if (!tama.isHatched)
            {
                saveData.ClearActiveSession();
                return WakeRejected(
                    SleepScheduleWakeStatus.NotHatched,
                    "부화 전 수면 예약은 회복 없이 안전하게 취소했어요.",
                    true);
            }

            if (!TryReadSession(
                session,
                out var startedAt,
                out var plannedWakeAt))
            {
                saveData.ClearActiveSession();
                return WakeRejected(
                    SleepScheduleWakeStatus.InvalidState,
                    "잘못된 수면 예약을 안전하게 정리했어요.",
                    true);
            }

            var isEarlyWake = now < plannedWakeAt;
            if (isEarlyWake && !allowEarlyWake)
            {
                return WakeRejected(
                    SleepScheduleWakeStatus.NotDue,
                    "아직 예정 기상 시각이 되지 않았어요.",
                    stateChanged);
            }

            var creditedWakeAt = isEarlyWake ? now : plannedWakeAt;
            var elapsedMinutes = CalculateElapsedMinutes(startedAt, creditedWakeAt);
            var recovery = CalculateRecovery(tama, elapsedMinutes);
            var receipt = new SleepRecoveryReceiptSaveEntry
            {
                receiptKey = session.receiptKey,
                sleepStartedAtIso = session.sleepStartedAtIso,
                plannedWakeAtIso = session.plannedWakeAtIso,
                wokeAtIso = SleepSessionSaveData.FormatTimestamp(creditedWakeAt),
                claimedAtIso = SleepSessionSaveData.FormatTimestamp(now),
                scheduledHours = session.scheduledHours,
                elapsedMinutes = elapsedMinutes,
                sleepinessDelta = recovery.SleepinessDelta,
                healthDelta = recovery.HealthDelta,
                moodDelta = recovery.MoodDelta,
                wasEarlyWake = isEarlyWake
            };

            if (!saveData.TryAddRecoveryReceipt(receipt))
            {
                saveData.ClearActiveSession();
                return new SleepScheduleWakeResult(
                    SleepScheduleWakeStatus.AlreadyApplied,
                    session.receiptKey,
                    session.scheduledHours,
                    0,
                    0,
                    0,
                    0,
                    true,
                    saveData.FindReceipt(session.receiptKey),
                    "이미 반영된 수면 회복이에요.");
            }

            tama.stats.sleepiness += recovery.SleepinessDelta;
            tama.stats.health += recovery.HealthDelta;
            tama.stats.mood += recovery.MoodDelta;
            tama.stats.ClampAll();
            saveData.RecordLastWake(creditedWakeAt);
            saveData.ClearActiveSession();

            var status = isEarlyWake
                ? SleepScheduleWakeStatus.WokeEarly
                : SleepScheduleWakeStatus.Completed;
            var prefix = isEarlyWake ? "일찍 일어났어요." : "예약한 수면을 마쳤어요.";
            return new SleepScheduleWakeResult(
                status,
                session.receiptKey,
                session.scheduledHours,
                elapsedMinutes,
                recovery.SleepinessDelta,
                recovery.HealthDelta,
                recovery.MoodDelta,
                true,
                receipt,
                BuildRecoveryMessage(prefix, elapsedMinutes, recovery));
        }

        private static RecoveryDelta CalculateRecovery(
            CheeseTamaModel tama,
            int elapsedMinutes)
        {
            var minutes = Math.Max(
                0,
                Math.Min(
                    SleepScheduleSaveData.MaximumScheduledHours * 60,
                    elapsedMinutes));
            var intendedSleepiness = -Math.Min(
                100,
                minutes * SleepinessRecoveryPerHour / 60);
            var intendedHealth = minutes / 60 * HealthRecoveryPerHour;
            var intendedMood = minutes / MoodRecoveryIntervalMinutes;

            var stats = tama.stats;
            var finalSleepiness = ClampStat(stats.sleepiness + intendedSleepiness);
            var finalHealth = ClampStat(stats.health + intendedHealth);
            var finalMood = ClampStat(stats.mood + intendedMood);
            return new RecoveryDelta(
                finalSleepiness - stats.sleepiness,
                finalHealth - stats.health,
                finalMood - stats.mood);
        }

        private static string BuildRecoveryMessage(
            string prefix,
            int elapsedMinutes,
            RecoveryDelta recovery)
        {
            var hours = elapsedMinutes / 60;
            var minutes = elapsedMinutes % 60;
            var durationText = hours > 0
                ? minutes > 0
                    ? $"{hours}시간 {minutes}분"
                    : $"{hours}시간"
                : $"{minutes}분";
            return $"{prefix} 실제 휴식 {durationText} · 졸림 {recovery.SleepinessDelta}, 건강 +{recovery.HealthDelta}, 기분 +{recovery.MoodDelta}";
        }

        private static int CalculateElapsedMinutes(
            DateTimeOffset startedAt,
            DateTimeOffset creditedThrough)
        {
            if (creditedThrough <= startedAt)
            {
                return 0;
            }

            return Math.Max(
                0,
                Math.Min(
                    SleepScheduleSaveData.MaximumScheduledHours * 60,
                    (int)Math.Floor((creditedThrough - startedAt).TotalMinutes)));
        }

        private static ActiveSessionIssue InspectSession(
            SleepSessionSaveData session,
            DateTimeOffset now)
        {
            if (session == null)
            {
                return ActiveSessionIssue.None;
            }

            if (string.IsNullOrWhiteSpace(session.receiptKey)
                || !SleepSessionSaveData.TryParseTimestamp(
                    session.sleepStartedAtIso,
                    out var startedAt)
                || !SleepSessionSaveData.TryParseTimestamp(
                    session.plannedWakeAtIso,
                    out var plannedWakeAt)
                || plannedWakeAt <= startedAt)
            {
                return ActiveSessionIssue.Invalid;
            }

            if (startedAt > now)
            {
                return ActiveSessionIssue.FutureStart;
            }

            var hours = (plannedWakeAt - startedAt).TotalHours;
            var roundedHours = (int)Math.Round(hours);
            return roundedHours < SleepScheduleSaveData.MinimumScheduledHours
                || roundedHours > SleepScheduleSaveData.MaximumScheduledHours
                || Math.Abs(hours - roundedHours) > 0.000001d
                ? ActiveSessionIssue.Invalid
                : ActiveSessionIssue.None;
        }

        private static bool TryReadSession(
            SleepSessionSaveData session,
            out DateTimeOffset startedAt,
            out DateTimeOffset plannedWakeAt)
        {
            startedAt = default;
            plannedWakeAt = default;
            return session != null
                && SleepSessionSaveData.TryParseTimestamp(
                    session.sleepStartedAtIso,
                    out startedAt)
                && SleepSessionSaveData.TryParseTimestamp(
                    session.plannedWakeAtIso,
                    out plannedWakeAt)
                && plannedWakeAt > startedAt;
        }

        private static SleepScheduleStartResult StartRejected(
            SleepScheduleStartStatus status,
            string message,
            bool stateChanged = false)
        {
            return new SleepScheduleStartResult(
                status,
                string.Empty,
                0,
                default,
                default,
                stateChanged,
                message);
        }

        private static SleepScheduleWakeResult WakeRejected(
            SleepScheduleWakeStatus status,
            string message,
            bool stateChanged = false)
        {
            return new SleepScheduleWakeResult(
                status,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                stateChanged,
                null,
                message);
        }

        private static int ClampStat(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private enum ActiveSessionIssue
        {
            None = 0,
            Invalid = 1,
            FutureStart = 2
        }

        private readonly struct RecoveryDelta
        {
            public RecoveryDelta(
                int sleepinessDelta,
                int healthDelta,
                int moodDelta)
            {
                SleepinessDelta = Math.Min(0, sleepinessDelta);
                HealthDelta = Math.Max(0, healthDelta);
                MoodDelta = Math.Max(0, moodDelta);
            }

            public int SleepinessDelta { get; }
            public int HealthDelta { get; }
            public int MoodDelta { get; }
        }
    }
}
