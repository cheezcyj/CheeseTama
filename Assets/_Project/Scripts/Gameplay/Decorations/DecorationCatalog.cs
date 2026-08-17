using System;

namespace CheeseTama.Gameplay.Decorations
{
    public static class DecorationCatalog
    {
        public const string CreamWallId = "wall_cream";
        public const string PeachWallId = "wall_peach_sunset";
        public const string StarlightWallId = "wall_starlight";
        public const string CreamRugId = "floor_cream_rug";
        public const string CheeseTileId = "floor_cheese_tile";
        public const string CloudMatId = "floor_cloud_mat";
        public const string MilkBottleId = "accent_milk_bottle";
        public const string StarLampId = "accent_star_lamp";
        public const string CreamCurtainId = "window_cream_curtain";
        public const string MoonCurtainId = "window_moon_curtain";
        public const string CheeseClockId = "shelf_cheese_clock";
        public const string MemoryFrameId = "shelf_memory_frame";
        public const string MilkCushionId = "bedside_milk_cushion";
        public const string StarPlushId = "bedside_star_plush";

        public static readonly DecorationDefinition CreamWall = new DecorationDefinition(
            CreamWallId,
            "크림 벽지",
            "밀크룸을 환하고 부드럽게 감싸는 기본 벽지예요.",
            DecorationSlot.Wall,
            0,
            0,
            true,
            "cream_wall");

        public static readonly DecorationDefinition PeachWall = new DecorationDefinition(
            PeachWallId,
            "복숭아 노을 벽지",
            "은은한 복숭아빛으로 방을 따뜻하게 바꿔요.",
            DecorationSlot.Wall,
            80,
            2,
            false,
            "peach_sunset_wall");

        public static readonly DecorationDefinition StarlightWall = new DecorationDefinition(
            StarlightWallId,
            "별빛 벽지",
            "작은 별무늬가 밤처럼 차분하게 반짝여요.",
            DecorationSlot.Wall,
            140,
            5,
            false,
            "starlight_wall");

        public static readonly DecorationDefinition CreamRug = new DecorationDefinition(
            CreamRugId,
            "포근한 크림 러그",
            "치즈타마가 편안히 머물 수 있는 기본 러그예요.",
            DecorationSlot.Floor,
            0,
            0,
            true,
            "cream_rug");

        public static readonly DecorationDefinition CheeseTile = new DecorationDefinition(
            CheeseTileId,
            "치즈 체크 타일",
            "노란 치즈 조각을 닮은 경쾌한 체크 바닥이에요.",
            DecorationSlot.Floor,
            100,
            3,
            false,
            "cheese_tile");

        public static readonly DecorationDefinition CloudMat = new DecorationDefinition(
            CloudMatId,
            "구름 쿠션 매트",
            "몽글몽글한 구름 모양이 발밑을 폭신하게 꾸며 줘요.",
            DecorationSlot.Floor,
            160,
            6,
            false,
            "cloud_mat");

        public static readonly DecorationDefinition MilkBottle = new DecorationDefinition(
            MilkBottleId,
            "작은 우유병",
            "밀크룸 한쪽을 채우는 소박한 기본 장식이에요.",
            DecorationSlot.Accent,
            0,
            0,
            true,
            "milk_bottle_prop");

        public static readonly DecorationDefinition StarLamp = new DecorationDefinition(
            StarLampId,
            "별방울 무드등",
            "별빛과 우유방울을 닮은 포인트 조명이에요.",
            DecorationSlot.Accent,
            180,
            8,
            false,
            "star_lamp_prop");

        public static readonly DecorationDefinition CreamCurtain = new DecorationDefinition(
            CreamCurtainId, "크림 커튼", "창가를 부드럽게 감싸는 기본 커튼이에요.",
            DecorationSlot.Window, 0, 0, true, "cream_curtain");
        public static readonly DecorationDefinition MoonCurtain = new DecorationDefinition(
            MoonCurtainId, "달빛 커튼", "밤하늘 색감의 차분한 창가 장식이에요.",
            DecorationSlot.Window, 130, 4, false, "moon_curtain");
        public static readonly DecorationDefinition CheeseClock = new DecorationDefinition(
            CheeseClockId, "치즈 시계", "선반 위에서 시간을 알려 주는 기본 소품이에요.",
            DecorationSlot.Shelf, 0, 0, true, "cheese_clock");
        public static readonly DecorationDefinition MemoryFrame = new DecorationDefinition(
            MemoryFrameId, "추억 액자", "함께한 기억을 담아 두는 따뜻한 액자예요.",
            DecorationSlot.Shelf, 150, 5, false, "memory_frame");
        public static readonly DecorationDefinition MilkCushion = new DecorationDefinition(
            MilkCushionId, "우유 쿠션", "침대 곁에 두는 포근한 기본 쿠션이에요.",
            DecorationSlot.Bedside, 0, 0, true, "milk_cushion");
        public static readonly DecorationDefinition StarPlush = new DecorationDefinition(
            StarPlushId, "별방울 인형", "반짝이는 별방울 모양의 작은 인형이에요.",
            DecorationSlot.Bedside, 170, 7, false, "star_plush");

        public static readonly DecorationDefinition[] All =
        {
            CreamWall,
            PeachWall,
            StarlightWall,
            CreamRug,
            CheeseTile,
            CloudMat,
            MilkBottle,
            StarLamp,
            CreamCurtain,
            MoonCurtain,
            CheeseClock,
            MemoryFrame,
            MilkCushion,
            StarPlush
        };

        public static DecorationDefinition Find(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return Array.Find(All, item => item != null && item.id == itemId);
        }

        public static DecorationDefinition GetDefault(DecorationSlot slot)
        {
            return Array.Find(All, item => item != null && item.slot == slot && item.defaultOwned);
        }
    }
}
