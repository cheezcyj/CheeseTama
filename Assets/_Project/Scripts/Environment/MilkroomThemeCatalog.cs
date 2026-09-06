using System;
using System.Collections.Generic;

namespace CheeseTama.Environment
{
    public sealed class MilkroomThemeCatalogEntry
    {
        internal MilkroomThemeCatalogEntry(
            string id,
            string shortName,
            string displayName,
            string detail,
            string lightingDetail,
            string propsDetail,
            int starDropCost,
            bool requiresStarRoute)
        {
            Id = id ?? string.Empty;
            ShortName = shortName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Detail = detail ?? string.Empty;
            LightingDetail = lightingDetail ?? string.Empty;
            PropsDetail = propsDetail ?? string.Empty;
            StarDropCost = Math.Max(0, starDropCost);
            RequiresStarRoute = requiresStarRoute;
        }

        public string Id { get; }
        public string ShortName { get; }
        public string DisplayName { get; }
        public string Detail { get; }
        public string LightingDetail { get; }
        public string PropsDetail { get; }
        public int StarDropCost { get; }
        public bool RequiresStarRoute { get; }
        public bool IsOwnedByDefault => StarDropCost == 0;
    }

    public static class MilkroomThemeCatalog
    {
        private static readonly MilkroomThemeCatalogEntry[] Themes =
        {
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.MorningThemeId,
                "아침",
                "따뜻한 아침 밀크룸",
                "크림색 벽 / 정돈된 바닥 / 포근한 아침빛",
                "따뜻한 햇살 + 부드러운 림라이트",
                "기본 소품 배치 유지",
                0,
                false),
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.EveningThemeId,
                "오후",
                "따뜻한 오후 밀크룸",
                "노을빛 벽 / 따뜻한 그림자 / 창가의 주황빛",
                "노을빛 키라이트 + 낮은 림라이트",
                "오후 빛줄기 표시",
                0,
                false),
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.NightThemeId,
                "밤",
                "고요한 밤 밀크룸",
                "차분한 밤색 벽 / 푸른 창빛 / 달빛 포인트",
                "부드러운 푸른 주변광 + 낮은 조도",
                "밤하늘 별빛 표시",
                0,
                false),
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.RainyThemeId,
                "비",
                "비 오는 밀크룸",
                "흐린 벽색 / 차분한 바닥 / 창밖 빗방울 분위기",
                "흐린 하늘빛 필라이트 + 따뜻한 실내등",
                "빗줄기 표시",
                0,
                false),
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.StarlightThemeId,
                "별빛",
                "별빛 밀크룸",
                "남보라색 밤벽 / 깊은 별하늘 / 은은한 빛가루",
                "보랏빛 주변광 + 별빛 림라이트",
                "별무리와 반짝임 표시",
                3,
                true),
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.WinterThemeId,
                "겨울",
                "겨울 밀크룸",
                "차가운 창밖 / 눈 쌓인 색감 / 따뜻한 실내 대비",
                "푸른 창빛 + 포근한 금빛 키라이트",
                "창가 눈송이 표시",
                2,
                true),
            new MilkroomThemeCatalogEntry(
                MilkroomThemeController.VintageThemeId,
                "빈티지",
                "빈티지 밀크룸",
                "낮은 조도 / 오래된 목재 / 세피아색 기록 분위기",
                "낮은 금빛 조명 + 부드러운 갈색 주변광",
                "오래된 빛가루 표시",
                4,
                true)
        };

        public static IReadOnlyList<MilkroomThemeCatalogEntry> All => Themes;

        public static MilkroomThemeCatalogEntry Find(string themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId))
            {
                return null;
            }

            var normalized = themeId.Trim();
            for (var index = 0; index < Themes.Length; index += 1)
            {
                if (string.Equals(Themes[index].Id, normalized, StringComparison.Ordinal))
                {
                    return Themes[index];
                }
            }

            return null;
        }

        public static string Normalize(string themeId)
        {
            return Find(themeId)?.Id ?? MilkroomThemeController.MorningThemeId;
        }
    }
}
