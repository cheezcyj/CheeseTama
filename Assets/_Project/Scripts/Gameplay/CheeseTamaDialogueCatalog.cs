using System.Collections.Generic;

namespace CheeseTama.Gameplay.Dialogue
{
    public static class CheeseTamaDialogueCatalog
    {
        private static readonly CheeseTamaDialogueLine[] Lines =
        {
            Line("ambient_room_1", "오늘 밀크룸은 포근한 우유 냄새가 나.", CheeseTamaDialogueContext.Ambient,
                CheeseTamaDialoguePriority.Ambient, 24f, 3.5f),
            Line("ambient_room_2", "조용히 같이 있어도 좋아.", CheeseTamaDialogueContext.Ambient,
                CheeseTamaDialoguePriority.Ambient, 24f, 3.5f),
            Line("ambient_room_3", "방울 소리가 들리는 것 같아.", CheeseTamaDialogueContext.Ambient,
                CheeseTamaDialoguePriority.Ambient, 24f, 3.5f),
            Line("ambient_room_4", "오늘은 어떤 추억을 만들까?", CheeseTamaDialogueContext.Ambient,
                CheeseTamaDialoguePriority.Ambient, 24f, 3.5f),

            State("state_normal_1", "기분도 몸도 말랑말랑해.", CheeseTamaDialogueState.Normal),
            State("state_normal_2", "지금은 아주 편안해.", CheeseTamaDialogueState.Normal),
            State("state_normal_3", "천천히 놀아도 좋아.", CheeseTamaDialogueState.Normal),
            State("state_hungry_1", "배 속에서 꼬르륵 소리가 났어...", CheeseTamaDialogueState.Hungry),
            State("state_hungry_2", "우유 한 모금이 생각나.", CheeseTamaDialogueState.Hungry),
            State("state_sleepy_1", "눈꺼풀이 치즈처럼 늘어지는 것 같아...", CheeseTamaDialogueState.Sleepy),
            State("state_sleepy_2", "조금만 쉬면 다시 말랑해질 거야.", CheeseTamaDialogueState.Sleepy),
            State("state_messy_1", "몸에 작은 먼지가 붙었어.", CheeseTamaDialogueState.Messy),
            State("state_messy_2", "밀크룸을 반짝이게 닦아 줄래?", CheeseTamaDialogueState.Messy),
            State("state_sick_1", "오늘은 조금 천천히 돌봐 줘.", CheeseTamaDialogueState.Sick),
            State("state_sick_2", "따뜻하고 조용한 시간이 필요해.", CheeseTamaDialogueState.Sick),
            State("state_happy_1", "지금이라면 통통 튀어 오를 수 있어!", CheeseTamaDialogueState.Happy),
            State("state_happy_2", "같이 있으니 볼이 더 반짝여.", CheeseTamaDialogueState.Happy),

            Line("feed_negative_1", "배가 조금 놀랐어. 다음엔 천천히 먹을래.", CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.State, 12f, 4f, tone: CheeseTamaDialogueTone.Negative),
            Line("feed_negative_2", "같은 맛이 계속되면 잠깐 쉬고 싶어.", CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.State, 12f, 4f, tone: CheeseTamaDialogueTone.Negative),
            Line("feed_generic_1", "꿀꺽! 우유 기운이 차오르는 것 같아.", CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.Feed, 8f, 3.5f),
            Line("feed_generic_2", "이 맛도 성장 기록에 꼭 기억해 둘게.", CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.Feed, 8f, 3.5f),
            Line("feed_generic_3", "우유를 마시니 몸이 더 말랑해졌어.", CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.Feed, 8f, 3.5f),
            Line("feed_generic_4", "고마워. 한 방울도 남기지 않았어!", CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.Feed, 8f, 3.5f),
            MilkMemory("feed_basic_memory", "기본 우유의 담백한 맛, 이제 알아볼 수 있어.", "basic_milk"),
            MilkMemory("feed_warm_memory", "따뜻한 우유를 마시면 포근했던 시간이 떠올라.", "warm_milk"),
            MilkMemory("feed_cold_memory", "차가운 우유의 상쾌한 느낌을 기억하고 있어.", "cold_milk"),
            MilkMemory("feed_nutty_memory", "고소한 향이 나면 신나게 놀았던 날이 생각나.", "nutty_milk"),
            MilkMemory("feed_rich_memory", "진한 우유는 천천히 음미해야 더 맛있어.", "rich_milk"),
            MilkMemory("feed_fermented_memory", "발효우유의 독특한 향도 이제 낯설지 않아.", "fermented_milk"),
            MilkMemory("feed_coffee_memory", "커피우유 향을 맡으면 조용한 밤이 떠올라.", "coffee_milk"),

            Line("pet_1", "손길이 따뜻해서 마음이 몽글몽글해.", CheeseTamaDialogueContext.Pet,
                CheeseTamaDialoguePriority.Pet, 10f, 3.5f),
            Line("pet_2", "조금만 더 쓰다듬어 줘도 좋아.", CheeseTamaDialogueContext.Pet,
                CheeseTamaDialoguePriority.Pet, 10f, 3.5f),
            Line("pet_3", "네 손길은 금방 알아볼 수 있어.", CheeseTamaDialogueContext.Pet,
                CheeseTamaDialoguePriority.Pet, 10f, 3.5f),
            Line("pet_4", "볼이 간질간질하지만 기분 좋아!", CheeseTamaDialogueContext.Pet,
                CheeseTamaDialoguePriority.Pet, 10f, 3.5f),

            Line("return_short", "금방 다시 왔네! 기다리고 있었어.", CheeseTamaDialogueContext.Return,
                CheeseTamaDialoguePriority.Return, 30f, 4f, subject: "short"),
            Line("return_long", "오랜만이야. 밀크룸 이야기가 잔뜩 쌓였어.", CheeseTamaDialogueContext.Return,
                CheeseTamaDialoguePriority.Return, 30f, 4.5f, subject: "long"),
            Line("return_1", "돌아왔구나. 오늘도 같이 지내자.", CheeseTamaDialogueContext.Return,
                CheeseTamaDialoguePriority.Return, 30f, 4f),
            Line("return_2", "네가 없는 동안에도 잘 기다렸어.", CheeseTamaDialogueContext.Return,
                CheeseTamaDialoguePriority.Return, 30f, 4f),

            Line("growth_1", "조금 전보다 몸이 더 단단하고 말랑해졌어!", CheeseTamaDialogueContext.Growth,
                CheeseTamaDialoguePriority.Growth, 30f, 4.5f),
            Line("growth_2", "함께한 돌봄이 새로운 모습으로 남았어.", CheeseTamaDialogueContext.Growth,
                CheeseTamaDialoguePriority.Growth, 30f, 4.5f),
            Line("growth_3", "다음 성장도 함께 지켜봐 줄 거지?", CheeseTamaDialogueContext.Growth,
                CheeseTamaDialoguePriority.Growth, 30f, 4.5f),
            Line("growth_final", "여기까지 함께 와 줘서 정말 고마워.", CheeseTamaDialogueContext.Growth,
                CheeseTamaDialoguePriority.Growth, 60f, 5f, subject: "growth_stage_final"),

            Line("evolution_cream", "따뜻한 추억이 모여 크림처럼 부드러워졌어.", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 60f, 5f, subject: "cream_cheesetama"),
            Line("evolution_cheddar", "함께 신나게 놀던 마음이 체다빛으로 빛나!", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 60f, 5f, subject: "cheddar_cheesetama"),
            Line("evolution_ricotta", "꾸준히 돌봐 준 시간이 포근하게 남았어.", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 60f, 5f, subject: "ricotta_cheesetama"),
            Line("evolution_mozzarella", "고르게 채운 하루들이 나를 말랑하게 만들었어.", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 60f, 5f, subject: "mozzarella_cheesetama"),
            Line("evolution_blue", "천천히 숙성된 특별한 향이 느껴져.", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 60f, 5f, subject: "blue_cheesetama"),
            Line("evolution_coffee", "조용한 밤의 기억이 커피빛으로 남았어.", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 60f, 5f, subject: "coffee_cheesetama"),
            Line("evolution_fallback", "우리의 추억이 새로운 모습으로 이어졌어.", CheeseTamaDialogueContext.Evolution,
                CheeseTamaDialoguePriority.Evolution, 45f, 5f),

            Line("event_fever", "조금 추웠는데, 따뜻한 빛이 곁에 와 줬어.", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f, subject: "small_fever"),
            Line("event_hungry", "방금 꼬르륵 소리, 들었어?", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f, subject: "hungry_peep"),
            Line("event_dust", "저 구석의 먼지도 같이 치워 보자.", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f, subject: "dusty_corner"),
            Line("event_sleepy", "하품이 자꾸 나와. 잠깐 쉬어 갈까?", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f, subject: "sleepy_yawn"),
            Line("event_happy", "몸이 저절로 살랑살랑 움직여!", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f, subject: "happy_wiggle"),
            Line("event_hum", "들었어? 밀크룸이 작게 노래했어.", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f, subject: "quiet_hum"),
            Line("event_1", "오늘 밀크룸에 작은 이야기가 하나 더 생겼어.", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f),
            Line("event_2", "이 순간도 잊지 않고 기억해 둘게.", CheeseTamaDialogueContext.Event,
                CheeseTamaDialoguePriority.Event, 30f, 4f)
        };

        public static IReadOnlyList<CheeseTamaDialogueLine> All => Lines;

        private static CheeseTamaDialogueLine State(
            string id,
            string text,
            CheeseTamaDialogueState state)
        {
            return Line(
                id,
                text,
                CheeseTamaDialogueContext.State,
                CheeseTamaDialoguePriority.State,
                15f,
                3.75f,
                state: state);
        }

        private static CheeseTamaDialogueLine MilkMemory(string id, string text, string milkId)
        {
            return Line(
                id,
                text,
                CheeseTamaDialogueContext.Feed,
                CheeseTamaDialoguePriority.FeedMemory,
                20f,
                4f,
                subject: milkId,
                minimumGrowthLevel: 2);
        }

        private static CheeseTamaDialogueLine Line(
            string id,
            string text,
            CheeseTamaDialogueContext context,
            CheeseTamaDialoguePriority priority,
            float cooldown,
            float duration,
            string subject = "",
            CheeseTamaDialogueState state = CheeseTamaDialogueState.Any,
            CheeseTamaDialogueTone tone = CheeseTamaDialogueTone.Any,
            int minimumGrowthLevel = 0)
        {
            return new CheeseTamaDialogueLine(
                id,
                text,
                context,
                priority,
                cooldown,
                duration,
                subject,
                state,
                tone,
                minimumGrowthLevel);
        }
    }
}
