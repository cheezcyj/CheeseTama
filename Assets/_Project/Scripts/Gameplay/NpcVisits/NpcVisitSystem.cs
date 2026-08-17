using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Data;
using CheeseTama.Save;

namespace CheeseTama.Gameplay.NpcVisits
{
    public sealed class NpcVisitChoiceDefinition
    {
        public NpcVisitChoiceDefinition(
            string id,
            string label,
            string resultMessage,
            StatEffect effect,
            int milkCoins = 0,
            int collectionFragments = 0,
            int affinity = 1)
        {
            Id = id;
            Label = label;
            ResultMessage = resultMessage;
            Effect = effect;
            MilkCoins = Math.Max(0, milkCoins);
            CollectionFragments = Math.Max(0, collectionFragments);
            Affinity = Math.Max(1, affinity);
        }

        public string Id { get; }
        public string Label { get; }
        public string ResultMessage { get; }
        public StatEffect Effect { get; }
        public int MilkCoins { get; }
        public int CollectionFragments { get; }
        public int Affinity { get; }
    }

    public sealed class NpcVisitDefinition
    {
        public NpcVisitDefinition(
            string id,
            string displayName,
            string role,
            string[] introductions,
            NpcVisitChoiceDefinition[] choices)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            Introductions = introductions;
            Choices = choices;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public IReadOnlyList<string> Introductions { get; }
        public IReadOnlyList<NpcVisitChoiceDefinition> Choices { get; }
    }

    public sealed class NpcVisitOffer
    {
        public NpcVisitOffer(
            string occurrenceId,
            NpcVisitDefinition visitor,
            int storyStep,
            bool stateChanged)
        {
            OccurrenceId = occurrenceId ?? string.Empty;
            Visitor = visitor;
            StoryStep = Math.Max(0, Math.Min(2, storyStep));
            StateChanged = stateChanged;
        }

        public string OccurrenceId { get; }
        public NpcVisitDefinition Visitor { get; }
        public int StoryStep { get; }
        public bool StateChanged { get; }
        public bool HasOffer => Visitor != null && !string.IsNullOrWhiteSpace(OccurrenceId);
        public string Title => Visitor?.DisplayName ?? string.Empty;
        public string Role => Visitor?.Role ?? string.Empty;
        public string Message => Visitor == null || Visitor.Introductions.Count == 0
            ? string.Empty
            : Visitor.Introductions[Math.Min(StoryStep, Visitor.Introductions.Count - 1)];
    }

    public sealed class NpcVisitResolutionResult
    {
        public NpcVisitResolutionResult(
            bool applied,
            string occurrenceId,
            string npcId,
            string choiceId,
            string message,
            int relationshipLevel,
            int milkCoins,
            int collectionFragments)
        {
            Applied = applied;
            OccurrenceId = occurrenceId ?? string.Empty;
            NpcId = npcId ?? string.Empty;
            ChoiceId = choiceId ?? string.Empty;
            Message = message ?? string.Empty;
            RelationshipLevel = Math.Max(0, relationshipLevel);
            MilkCoins = Math.Max(0, milkCoins);
            CollectionFragments = Math.Max(0, collectionFragments);
        }

        public bool Applied { get; }
        public string OccurrenceId { get; }
        public string NpcId { get; }
        public string ChoiceId { get; }
        public string Message { get; }
        public int RelationshipLevel { get; }
        public int MilkCoins { get; }
        public int CollectionFragments { get; }
    }

    public sealed class NpcVisitSystem
    {
        public const int MaximumVisitsPerDay = 1;
        public const int MinimumCareActionsBeforeVisit = 3;
        public const int VisitCooldownHours = 6;
        public const double VisitChance = 0.2d;

        public const string MilkyDoctorId = "milky_doctor";
        public const string FermentationFairyId = "fermentation_fairy";
        public const string MilkCatId = "milk_cat";

        private static readonly NpcVisitDefinition[] Definitions =
        {
            new NpcVisitDefinition(
                MilkyDoctorId,
                "밀키 박사",
                "포근한 돌봄 연구자",
                new[]
                {
                    "작은 청진기를 든 밀키 박사가 상태를 살펴보러 왔어요.",
                    "밀키 박사가 지난 돌봄 기록을 펼치며 반갑게 인사했어요.",
                    "이제 익숙한 친구처럼 밀키 박사가 조용히 건강 수첩을 건넸어요."
                },
                new[]
                {
                    new NpcVisitChoiceDefinition(
                        "gentle_checkup",
                        "상태 살펴보기",
                        "꼼꼼한 진찰 덕분에 몸이 한결 편안해졌어요.",
                        new StatEffect { health = 6, hunger = 3 },
                        affinity: 2),
                    new NpcVisitChoiceDefinition(
                        "quiet_rest",
                        "잠깐 쉬게 하기",
                        "포근한 담요 아래에서 피로를 조금 덜었어요.",
                        new StatEffect { sleepiness = -8, mood = 3 },
                        affinity: 1)
                }),
            new NpcVisitDefinition(
                FermentationFairyId,
                "발효요정",
                "향기를 돌보는 작은 방문자",
                new[]
                {
                    "은은한 향을 따라 발효요정이 살며시 날아왔어요.",
                    "발효요정이 오늘의 향을 기억한다며 반짝이는 가루를 털었어요.",
                    "오래 기다린 향처럼 깊어진 우정을 발효요정이 알아봤어요."
                },
                new[]
                {
                    new NpcVisitChoiceDefinition(
                        "share_aroma",
                        "향 함께 맡기",
                        "천천히 익어 가는 향을 함께 기억했어요.",
                        new StatEffect { maturation = 5, affection = 1 },
                        affinity: 2),
                    new NpcVisitChoiceDefinition(
                        "patient_wait",
                        "서두르지 않기",
                        "기다리는 시간도 돌봄이라는 걸 배웠어요.",
                        new StatEffect { mood = 3, affection = 3 },
                        affinity: 1)
                }),
            new NpcVisitDefinition(
                MilkCatId,
                "밀크냥",
                "밀크룸의 호기심 많은 길잡이",
                new[]
                {
                    "살금살금 들어온 밀크냥이 방 안의 작은 흔적을 가리켰어요.",
                    "밀크냥이 익숙한 발걸음으로 새로운 관찰 장소를 알려줬어요.",
                    "밀크냥이 비밀 친구에게만 보여 주는 반짝이는 지도를 펼쳤어요."
                },
                new[]
                {
                    new NpcVisitChoiceDefinition(
                        "search_together",
                        "같이 찾아보기",
                        "구석에서 작은 도감 조각을 발견했어요.",
                        new StatEffect { mood = 3 },
                        collectionFragments: 1,
                        affinity: 2),
                    new NpcVisitChoiceDefinition(
                        "play_together",
                        "잠깐 놀아주기",
                        "둘이 나란히 뛰어다니며 금세 가까워졌어요.",
                        new StatEffect { mood = 6, affection = 2 },
                        affinity: 1)
                })
        };

        public IReadOnlyList<NpcVisitDefinition> All => Definitions;

        public NpcVisitDefinition Find(string npcId)
        {
            foreach (var definition in Definitions)
            {
                if (string.Equals(definition.Id, npcId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        public bool TryGetPending(NpcVisitSaveData state, out NpcVisitOffer offer)
        {
            offer = null;
            if (state == null)
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            var visitor = Find(state.pending.npcId);
            if (!state.pending.HasValue || visitor == null)
            {
                return false;
            }

            offer = new NpcVisitOffer(
                state.pending.occurrenceId,
                visitor,
                state.pending.storyStep,
                false);
            return true;
        }

        public bool TryQueueVisit(
            NpcVisitSaveData state,
            CheeseTamaModel tama,
            CareHistorySaveData careHistory,
            DateTimeOffset now,
            double chanceRoll,
            double visitorRoll,
            string occurrenceId,
            bool force,
            out NpcVisitOffer offer)
        {
            offer = null;
            if (state == null || tama == null || string.IsNullOrWhiteSpace(occurrenceId))
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            if (TryGetPending(state, out offer))
            {
                return true;
            }

            EnsureDate(state, now);
            if (state.visitsToday >= MaximumVisitsPerDay
                || (!force && (careHistory?.totalCareActions ?? 0) < MinimumCareActionsBeforeVisit)
                || !CooldownElapsed(state.nextAllowedAtIso, now)
                || (!force && !PassesChance(chanceRoll)))
            {
                return false;
            }

            var visitor = SelectVisitor(state, tama, careHistory, visitorRoll);
            if (visitor == null)
            {
                return false;
            }

            var relationship = FindRelationship(state, visitor.Id);
            var storyStep = ResolveStoryStep(relationship?.visits ?? 0);
            state.pending.Set(
                occurrenceId.Trim(),
                visitor.Id,
                storyStep,
                now.ToString("O", CultureInfo.InvariantCulture));
            offer = new NpcVisitOffer(occurrenceId, visitor, storyStep, true);
            return true;
        }

        public bool TryResolve(
            NpcVisitSaveData state,
            CheeseTamaModel tama,
            EconomySaveData economy,
            string occurrenceId,
            string choiceId,
            DateTimeOffset now,
            out NpcVisitResolutionResult result)
        {
            result = new NpcVisitResolutionResult(false, occurrenceId, string.Empty, choiceId, string.Empty, 0, 0, 0);
            if (state == null || tama?.stats == null || economy == null)
            {
                return false;
            }

            state.EnsureRuntimeDefaults();
            if (!state.pending.HasValue
                || !string.Equals(state.pending.occurrenceId, occurrenceId, StringComparison.Ordinal)
                || HasReceipt(state, occurrenceId))
            {
                return false;
            }

            var visitor = Find(state.pending.npcId);
            var choice = FindChoice(visitor, choiceId);
            if (visitor == null || choice == null)
            {
                return false;
            }

            EnsureDate(state, now);

            tama.stats.Apply(choice.Effect);
            economy.milkCoins = SaturatingAdd(economy.milkCoins, choice.MilkCoins);
            economy.collectionFragments = SaturatingAdd(
                economy.collectionFragments,
                choice.CollectionFragments);
            var relationship = GetOrCreateRelationship(state, visitor.Id);
            relationship.visits = SaturatingAdd(relationship.visits, 1);
            relationship.affinity = Math.Min(99, SaturatingAdd(relationship.affinity, choice.Affinity));
            relationship.storyStep = ResolveStoryStep(relationship.visits);
            relationship.lastVisitedAtIso = now.ToString("O", CultureInfo.InvariantCulture);
            state.visitsToday = SaturatingAdd(state.visitsToday, 1);
            state.nextAllowedAtIso = now.AddHours(VisitCooldownHours).ToString("O", CultureInfo.InvariantCulture);
            state.receipts.Add(new NpcVisitReceiptSaveEntry
            {
                occurrenceId = occurrenceId,
                npcId = visitor.Id,
                choiceId = choice.Id,
                resolvedAtIso = now.ToString("O", CultureInfo.InvariantCulture)
            });
            while (state.receipts.Count > NpcVisitSaveData.MaximumReceipts)
            {
                state.receipts.RemoveAt(0);
            }

            state.pending.Clear();
            result = new NpcVisitResolutionResult(
                true,
                occurrenceId,
                visitor.Id,
                choice.Id,
                choice.ResultMessage,
                relationship.storyStep,
                choice.MilkCoins,
                choice.CollectionFragments);
            return true;
        }

        public static bool PassesChance(double roll)
        {
            return !double.IsNaN(roll) && roll >= 0d && roll < VisitChance;
        }

        private static NpcVisitDefinition SelectVisitor(
            NpcVisitSaveData state,
            CheeseTamaModel tama,
            CareHistorySaveData careHistory,
            double visitorRoll)
        {
            var stats = tama.stats;
            var scores = new int[Definitions.Length];
            scores[0] = (stats.health < 75 ? 4 : 0)
                + (stats.hunger < 45 ? 3 : 0)
                + (stats.overfullness > 0 ? 3 : 0);
            scores[1] = (stats.maturation >= 55 ? 4 : 0)
                + ((careHistory?.waitHours ?? 0) > 0 ? 2 : 0)
                + ((careHistory?.milkFeeds ?? 0) >= 3 ? 1 : 0);
            scores[2] = (stats.mood < 65 ? 4 : 0)
                + ((careHistory?.playSessions ?? 0) > 0 ? 2 : 0)
                + ((careHistory?.petSessions ?? 0) > 0 ? 1 : 0);

            var best = int.MinValue;
            var candidates = new List<int>();
            for (var index = 0; index < scores.Length; index += 1)
            {
                if (scores[index] > best)
                {
                    best = scores[index];
                    candidates.Clear();
                    candidates.Add(index);
                }
                else if (scores[index] == best)
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var normalized = double.IsNaN(visitorRoll)
                ? 0d
                : Math.Max(0d, Math.Min(0.999999d, visitorRoll));
            var selected = candidates[(int)(normalized * candidates.Count)];
            return Definitions[selected];
        }

        private static bool EnsureDate(NpcVisitSaveData state, DateTimeOffset now)
        {
            var key = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (string.Equals(state.dateKey, key, StringComparison.Ordinal))
            {
                return false;
            }

            state.dateKey = key;
            state.visitsToday = 0;
            return true;
        }

        private static bool CooldownElapsed(string nextAllowedAtIso, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(nextAllowedAtIso))
            {
                return true;
            }

            return DateTimeOffset.TryParse(
                    nextAllowedAtIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var nextAllowed)
                && now >= nextAllowed;
        }

        private static int ResolveStoryStep(int completedVisits)
        {
            return completedVisits >= 5 ? 2 : completedVisits >= 2 ? 1 : 0;
        }

        private static NpcVisitChoiceDefinition FindChoice(NpcVisitDefinition visitor, string choiceId)
        {
            if (visitor == null || string.IsNullOrWhiteSpace(choiceId))
            {
                return null;
            }

            foreach (var choice in visitor.Choices)
            {
                if (string.Equals(choice.Id, choiceId, StringComparison.Ordinal))
                {
                    return choice;
                }
            }

            return null;
        }

        private static NpcRelationshipSaveEntry FindRelationship(NpcVisitSaveData state, string npcId)
        {
            foreach (var relationship in state.relationships)
            {
                if (relationship != null && string.Equals(relationship.npcId, npcId, StringComparison.Ordinal))
                {
                    return relationship;
                }
            }

            return null;
        }

        private static NpcRelationshipSaveEntry GetOrCreateRelationship(NpcVisitSaveData state, string npcId)
        {
            var relationship = FindRelationship(state, npcId);
            if (relationship != null)
            {
                return relationship;
            }

            relationship = new NpcRelationshipSaveEntry { npcId = npcId };
            state.relationships.Add(relationship);
            return relationship;
        }

        private static bool HasReceipt(NpcVisitSaveData state, string occurrenceId)
        {
            foreach (var receipt in state.receipts)
            {
                if (receipt != null && string.Equals(receipt.occurrenceId, occurrenceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int SaturatingAdd(int current, int amount)
        {
            if (amount <= 0)
            {
                return current;
            }

            return current > int.MaxValue - amount ? int.MaxValue : current + amount;
        }
    }
}
