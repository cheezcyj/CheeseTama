using System;
using System.Collections.Generic;

namespace CheeseTama.Gameplay.Dialogue
{
    public enum CheeseTamaDialogueContext
    {
        Ambient = 0,
        State = 1,
        Feed = 2,
        Pet = 3,
        Return = 4,
        Growth = 5,
        Evolution = 6,
        Event = 7
    }

    public enum CheeseTamaDialogueState
    {
        Any = -1,
        Normal = 0,
        Hungry = 1,
        Sleepy = 2,
        Messy = 3,
        Sick = 4,
        Happy = 5
    }

    public enum CheeseTamaDialogueTone
    {
        Any = -1,
        Neutral = 0,
        Positive = 1,
        Negative = 2
    }

    public enum CheeseTamaDialoguePriority
    {
        Ambient = 10,
        Pet = 30,
        Feed = 40,
        FeedMemory = 45,
        Return = 50,
        State = 60,
        Growth = 70,
        Evolution = 80,
        Event = 90
    }

    public readonly struct CheeseTamaDialogueRequest
    {
        public CheeseTamaDialogueRequest(
            CheeseTamaDialogueContext context,
            string subjectId = "",
            CheeseTamaDialogueState state = CheeseTamaDialogueState.Any,
            CheeseTamaDialogueTone tone = CheeseTamaDialogueTone.Any,
            int growthLevel = 0)
        {
            Context = context;
            SubjectId = subjectId ?? string.Empty;
            State = state;
            Tone = tone;
            GrowthLevel = Math.Max(0, growthLevel);
        }

        public CheeseTamaDialogueContext Context { get; }
        public string SubjectId { get; }
        public CheeseTamaDialogueState State { get; }
        public CheeseTamaDialogueTone Tone { get; }
        public int GrowthLevel { get; }

        public static CheeseTamaDialogueRequest ForState(CheeseTamaDialogueState state)
        {
            return new CheeseTamaDialogueRequest(
                CheeseTamaDialogueContext.State,
                state: state);
        }

        public static CheeseTamaDialogueRequest ForFeed(
            string milkId,
            int growthLevel,
            CheeseTamaDialogueTone tone = CheeseTamaDialogueTone.Positive)
        {
            return new CheeseTamaDialogueRequest(
                CheeseTamaDialogueContext.Feed,
                milkId,
                tone: tone,
                growthLevel: growthLevel);
        }

        public static CheeseTamaDialogueRequest ForPet()
        {
            return new CheeseTamaDialogueRequest(CheeseTamaDialogueContext.Pet);
        }

        public static CheeseTamaDialogueRequest ForReturn(string returnBandId = "")
        {
            return new CheeseTamaDialogueRequest(
                CheeseTamaDialogueContext.Return,
                returnBandId);
        }

        public static CheeseTamaDialogueRequest ForGrowth(string growthStageId = "")
        {
            return new CheeseTamaDialogueRequest(
                CheeseTamaDialogueContext.Growth,
                growthStageId);
        }

        public static CheeseTamaDialogueRequest ForEvolution(string evolutionId)
        {
            return new CheeseTamaDialogueRequest(
                CheeseTamaDialogueContext.Evolution,
                evolutionId);
        }

        public static CheeseTamaDialogueRequest ForEvent(string eventId)
        {
            return new CheeseTamaDialogueRequest(
                CheeseTamaDialogueContext.Event,
                eventId);
        }
    }

    public sealed class CheeseTamaDialogueLine
    {
        public CheeseTamaDialogueLine(
            string id,
            string text,
            CheeseTamaDialogueContext context,
            CheeseTamaDialoguePriority priority,
            float cooldownSeconds,
            float durationSeconds,
            string requiredSubjectId = "",
            CheeseTamaDialogueState requiredState = CheeseTamaDialogueState.Any,
            CheeseTamaDialogueTone requiredTone = CheeseTamaDialogueTone.Any,
            int minimumGrowthLevel = 0)
        {
            Id = id ?? string.Empty;
            Text = text ?? string.Empty;
            Context = context;
            Priority = priority;
            CooldownSeconds = Math.Max(0f, cooldownSeconds);
            DurationSeconds = Math.Max(0f, durationSeconds);
            RequiredSubjectId = requiredSubjectId ?? string.Empty;
            RequiredState = requiredState;
            RequiredTone = requiredTone;
            MinimumGrowthLevel = Math.Max(0, minimumGrowthLevel);
        }

        public string Id { get; }
        public string Text { get; }
        public CheeseTamaDialogueContext Context { get; }
        public CheeseTamaDialoguePriority Priority { get; }
        public float CooldownSeconds { get; }
        public float DurationSeconds { get; }
        public string RequiredSubjectId { get; }
        public CheeseTamaDialogueState RequiredState { get; }
        public CheeseTamaDialogueTone RequiredTone { get; }
        public int MinimumGrowthLevel { get; }

        public bool Matches(CheeseTamaDialogueRequest request)
        {
            return Context == request.Context
                && (string.IsNullOrWhiteSpace(RequiredSubjectId)
                    || string.Equals(RequiredSubjectId, request.SubjectId, StringComparison.Ordinal))
                && (RequiredState == CheeseTamaDialogueState.Any || RequiredState == request.State)
                && (RequiredTone == CheeseTamaDialogueTone.Any || RequiredTone == request.Tone)
                && request.GrowthLevel >= MinimumGrowthLevel;
        }
    }

    public readonly struct CheeseTamaDialogueSelection
    {
        public CheeseTamaDialogueSelection(CheeseTamaDialogueLine line)
        {
            LineId = line?.Id ?? string.Empty;
            Text = line?.Text ?? string.Empty;
            Context = line?.Context ?? CheeseTamaDialogueContext.Ambient;
            Priority = line?.Priority ?? CheeseTamaDialoguePriority.Ambient;
            DurationSeconds = ClampDuration(line?.DurationSeconds ?? 4f);
        }

        public string LineId { get; }
        public string Text { get; }
        public CheeseTamaDialogueContext Context { get; }
        public CheeseTamaDialoguePriority Priority { get; }
        public float DurationSeconds { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(LineId)
            && !string.IsNullOrWhiteSpace(Text);

        public static float ClampDuration(float seconds)
        {
            return Math.Max(3f, Math.Min(5f, seconds));
        }
    }

    /// <summary>
    /// Session-local dialogue memory. Persistent progression stays owned by the
    /// existing save and growth systems; this class only prevents noisy repeats.
    /// </summary>
    public sealed class CheeseTamaDialogueRules
    {
        public const int DefaultRecentCapacity = 3;
        public const double DefaultGlobalCooldownSeconds = 2.5d;

        private readonly IReadOnlyList<CheeseTamaDialogueLine> catalog;
        private readonly int recentCapacity;
        private readonly double globalCooldownSeconds;
        private readonly List<string> recentLineIds = new List<string>();
        private readonly Dictionary<string, double> lastShownAtByLineId =
            new Dictionary<string, double>(StringComparer.Ordinal);

        private long selectionCursor;
        private bool hasShownDialogue;
        private double lastShownAt;
        private CheeseTamaDialoguePriority lastShownPriority;

        public CheeseTamaDialogueRules(
            IReadOnlyList<CheeseTamaDialogueLine> dialogueCatalog = null,
            int recentHistoryCapacity = DefaultRecentCapacity,
            double minimumGlobalCooldownSeconds = DefaultGlobalCooldownSeconds)
        {
            catalog = dialogueCatalog ?? CheeseTamaDialogueCatalog.All;
            recentCapacity = Math.Max(0, recentHistoryCapacity);
            globalCooldownSeconds = Math.Max(0d, minimumGlobalCooldownSeconds);
        }

        public IReadOnlyList<string> RecentLineIds => recentLineIds;
        public CheeseTamaDialoguePriority LastShownPriority => lastShownPriority;

        public bool TrySelect(
            CheeseTamaDialogueRequest request,
            double nowSeconds,
            out CheeseTamaDialogueSelection selection)
        {
            selection = default;
            if (catalog == null || catalog.Count == 0)
            {
                return false;
            }

            var safeNow = SanitizeTime(nowSeconds);
            if (TrySelectCandidate(request, safeNow, excludeRecent: true, out selection))
            {
                return true;
            }

            // Small context pools (for example one evolution-specific line) must not
            // become permanently silent once every candidate is in recent history.
            // Per-line and global cooldowns still apply on this fallback pass.
            return TrySelectCandidate(request, safeNow, excludeRecent: false, out selection);
        }

        private bool TrySelectCandidate(
            CheeseTamaDialogueRequest request,
            double nowSeconds,
            bool excludeRecent,
            out CheeseTamaDialogueSelection selection)
        {
            selection = default;
            var highestPriority = int.MinValue;
            var candidateCount = 0;
            for (var index = 0; index < catalog.Count; index += 1)
            {
                var line = catalog[index];
                if (!IsEligible(line, request, nowSeconds, excludeRecent))
                {
                    continue;
                }

                var priority = (int)line.Priority;
                if (priority > highestPriority)
                {
                    highestPriority = priority;
                    candidateCount = 1;
                }
                else if (priority == highestPriority)
                {
                    candidateCount += 1;
                }
            }

            if (candidateCount <= 0)
            {
                return false;
            }

            var targetOrdinal = (int)(selectionCursor % candidateCount);
            selectionCursor = selectionCursor == long.MaxValue ? 0 : selectionCursor + 1;
            var currentOrdinal = 0;
            for (var index = 0; index < catalog.Count; index += 1)
            {
                var line = catalog[index];
                if (!IsEligible(line, request, nowSeconds, excludeRecent)
                    || (int)line.Priority != highestPriority)
                {
                    continue;
                }

                if (currentOrdinal == targetOrdinal)
                {
                    selection = new CheeseTamaDialogueSelection(line);
                    return selection.IsValid;
                }

                currentOrdinal += 1;
            }

            return false;
        }

        public bool TrySelectAndRemember(
            CheeseTamaDialogueRequest request,
            double nowSeconds,
            out CheeseTamaDialogueSelection selection)
        {
            if (!TrySelect(request, nowSeconds, out selection))
            {
                return false;
            }

            Remember(selection, nowSeconds);
            return true;
        }

        public void Remember(CheeseTamaDialogueSelection selection, double nowSeconds)
        {
            if (!selection.IsValid)
            {
                return;
            }

            var safeNow = SanitizeTime(nowSeconds);
            lastShownAtByLineId[selection.LineId] = safeNow;
            hasShownDialogue = true;
            lastShownAt = safeNow;
            lastShownPriority = selection.Priority;

            if (recentCapacity <= 0)
            {
                return;
            }

            for (var index = recentLineIds.Count - 1; index >= 0; index -= 1)
            {
                if (string.Equals(recentLineIds[index], selection.LineId, StringComparison.Ordinal))
                {
                    recentLineIds.RemoveAt(index);
                }
            }

            recentLineIds.Add(selection.LineId);
            while (recentLineIds.Count > recentCapacity)
            {
                recentLineIds.RemoveAt(0);
            }
        }

        public void ResetMemory()
        {
            recentLineIds.Clear();
            lastShownAtByLineId.Clear();
            selectionCursor = 0;
            hasShownDialogue = false;
            lastShownAt = 0d;
            lastShownPriority = default;
        }

        public static CheeseTamaDialogueState ResolveState(
            int health,
            int hunger,
            int cleanliness,
            int sleepiness,
            int mood)
        {
            if (health < 35)
            {
                return CheeseTamaDialogueState.Sick;
            }

            if (hunger < 25)
            {
                return CheeseTamaDialogueState.Hungry;
            }

            if (cleanliness < 35)
            {
                return CheeseTamaDialogueState.Messy;
            }

            if (sleepiness > 75)
            {
                return CheeseTamaDialogueState.Sleepy;
            }

            return mood > 80
                ? CheeseTamaDialogueState.Happy
                : CheeseTamaDialogueState.Normal;
        }

        private bool IsEligible(
            CheeseTamaDialogueLine line,
            CheeseTamaDialogueRequest request,
            double nowSeconds,
            bool excludeRecent)
        {
            if (line == null
                || string.IsNullOrWhiteSpace(line.Id)
                || string.IsNullOrWhiteSpace(line.Text)
                || !line.Matches(request)
                || (excludeRecent && IsRecent(line.Id)))
            {
                return false;
            }

            if (hasShownDialogue
                && nowSeconds - lastShownAt < globalCooldownSeconds
                && (int)line.Priority <= (int)lastShownPriority)
            {
                return false;
            }

            return !lastShownAtByLineId.TryGetValue(line.Id, out var lineLastShownAt)
                || nowSeconds - lineLastShownAt >= line.CooldownSeconds;
        }

        private bool IsRecent(string lineId)
        {
            for (var index = 0; index < recentLineIds.Count; index += 1)
            {
                if (string.Equals(recentLineIds[index], lineId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static double SanitizeTime(double nowSeconds)
        {
            return double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds)
                ? 0d
                : Math.Max(0d, nowSeconds);
        }
    }
}
