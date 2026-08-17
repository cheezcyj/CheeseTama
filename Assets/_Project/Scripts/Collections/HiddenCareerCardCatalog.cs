using System;
using System.Collections.Generic;
using System.Globalization;
using CheeseTama.Data;

namespace CheeseTama.Collections.HiddenCareers
{
    public enum HiddenCareerBenefitKind
    {
        RecipeHintProgress = 0,
        CollectionInterpretation = 1,
        RecoveryEffectPercent = 2,
        RandomEventWeightPercent = 3,
        NegativeEffectMitigationPercent = 4,
        RareByproductWeightPercent = 5,
        DeepLoreSignal = 6
    }

    public sealed class HiddenCareerBenefit
    {
        internal HiddenCareerBenefit(HiddenCareerBenefitKind kind, int magnitude)
        {
            Kind = kind;
            Magnitude = Math.Max(0, magnitude);
        }

        public HiddenCareerBenefitKind Kind { get; }
        public int Magnitude { get; }
    }

    public sealed class HiddenCareerCardDefinition
    {
        internal HiddenCareerCardDefinition(
            string id,
            string internalCategory,
            string displayName,
            Rarity rarity,
            string quote,
            string deepText,
            string imageResourceKey,
            HiddenCareerBenefit benefit)
        {
            Id = id ?? string.Empty;
            InternalCategory = internalCategory ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Rarity = rarity;
            Quote = quote ?? string.Empty;
            DeepText = deepText ?? string.Empty;
            ImageResourceKey = imageResourceKey ?? string.Empty;
            Benefit = benefit;
        }

        public string Id { get; }
        internal string InternalCategory { get; }
        public string DisplayName { get; }
        public Rarity Rarity { get; }
        public string Quote { get; }
        public string DeepText { get; }
        public string ImageResourceKey { get; }
        public HiddenCareerBenefit Benefit { get; }
    }

    /// <summary>
    /// Presentation-safe data. It intentionally contains no unlock condition,
    /// category name, undiscovered count, or rarity of undiscovered cards.
    /// </summary>
    public sealed class HiddenCareerCardViewData
    {
        internal HiddenCareerCardViewData(
            HiddenCareerCardDefinition definition,
            string acquiredAtIso)
        {
            Id = definition?.Id ?? string.Empty;
            DisplayName = definition?.DisplayName ?? string.Empty;
            Rarity = definition?.Rarity ?? Rarity.Common;
            Quote = definition?.Quote ?? string.Empty;
            DeepText = definition?.DeepText ?? string.Empty;
            ImageResourceKey = definition?.ImageResourceKey ?? string.Empty;
            AcquiredAtIso = acquiredAtIso ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public Rarity Rarity { get; }
        public string Quote { get; }
        public string DeepText { get; }
        public string ImageResourceKey { get; }
        public string AcquiredAtIso { get; }

        public string AcquiredDateText
        {
            get
            {
                return DateTimeOffset.TryParse(AcquiredAtIso, out var acquiredAt)
                    ? acquiredAt.LocalDateTime.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)
                    : "기록 없음";
            }
        }
    }

    public static class HiddenCareerCardCatalog
    {
        public const string ScientistId = "scientist_cheesetama";
        public const string TeacherId = "teacher_cheesetama";
        public const string DoctorId = "doctor_cheesetama";
        public const string ExplorerId = "explorer_cheesetama";
        public const string GuardianId = "guardian_cheesetama";
        public const string RiftArchitectId = "rift_architect_cheesetama";
        public const string BlackStarObserverId = "black_star_observer_cheesetama";

        private static readonly HiddenCareerCardDefinition[] Cards =
        {
            new HiddenCareerCardDefinition(
                ScientistId,
                "hero",
                "과학자치즈타마",
                Rarity.Epic,
                "맛은 데이터야.",
                "실패한 조합도 기록된다.",
                "HiddenCareers/scientist_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.RecipeHintProgress, 1)),
            new HiddenCareerCardDefinition(
                TeacherId,
                "hero",
                "선생님치즈타마",
                Rarity.Rare,
                "천천히 알게 된 건 오래 남아.",
                "읽지 못했던 문장은 돌봄의 순서에서 다시 열린다.",
                "HiddenCareers/teacher_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.CollectionInterpretation, 1)),
            new HiddenCareerCardDefinition(
                DoctorId,
                "hero",
                "의사치즈타마",
                Rarity.Epic,
                "아픈 마음도 쉬어 갈 자리가 필요해.",
                "회복은 지워지는 일이 아니라 다시 말랑해지는 일이다.",
                "HiddenCareers/doctor_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.RecoveryEffectPercent, 10)),
            new HiddenCareerCardDefinition(
                ExplorerId,
                "hero",
                "탐험가치즈타마",
                Rarity.Rare,
                "모르는 방울 소리를 따라가 보자!",
                "익숙한 밀크룸에도 아직 밟지 않은 길이 남아 있다.",
                "HiddenCareers/explorer_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.RandomEventWeightPercent, 10)),
            new HiddenCareerCardDefinition(
                GuardianId,
                "hero",
                "수호자치즈타마",
                Rarity.Unique,
                "네가 돌아올 자리까지 지켜 둘게.",
                "오래 이어진 돌봄은 약한 순간을 대신 받아 내는 빛이 된다.",
                "HiddenCareers/guardian_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.NegativeEffectMitigationPercent, 15)),
            new HiddenCareerCardDefinition(
                RiftArchitectId,
                "shadow",
                "균열 설계자치즈타마",
                Rarity.Unique,
                "금이 간 곳에는 다른 길이 보여.",
                "같은 조합을 반복한 끝에서 결과를 나누는 가느다란 선이 생겼다.",
                "HiddenCareers/rift_architect_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.RareByproductWeightPercent, 7)),
            new HiddenCareerCardDefinition(
                BlackStarObserverId,
                "shadow",
                "검은 별 관측자치즈타마",
                Rarity.Legendary,
                "빛나지 않는 별도 우리를 보고 있어.",
                "모든 기록의 바깥에서 일곱 번째 시선이 조용히 되돌아본다.",
                "HiddenCareers/black_star_observer_cheesetama",
                new HiddenCareerBenefit(HiddenCareerBenefitKind.DeepLoreSignal, 1))
        };

        public static IReadOnlyList<HiddenCareerCardDefinition> All => Cards;

        public static HiddenCareerCardDefinition Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var normalized = id.Trim();
            for (var index = 0; index < Cards.Length; index += 1)
            {
                if (string.Equals(Cards[index].Id, normalized, StringComparison.Ordinal))
                {
                    return Cards[index];
                }
            }

            return null;
        }

        public static string GetRarityLabel(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Rare => "Rare",
                Rarity.Epic => "Epic",
                Rarity.Unique => "unique",
                Rarity.Legendary => "Legendary",
                _ => "common"
            };
        }
    }
}
