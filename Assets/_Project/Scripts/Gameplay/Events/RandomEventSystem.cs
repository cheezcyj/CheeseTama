using CheeseTama.Data;
using CheeseTama.Gameplay;
using UnityEngine;

namespace CheeseTama.Gameplay.Events
{
    public readonly struct CareEventResult
    {
        public readonly bool occurred;
        public readonly string eventId;
        public readonly string message;

        public CareEventResult(bool occurred, string eventId, string message)
        {
            this.occurred = occurred;
            this.eventId = eventId;
            this.message = message;
        }

        public static CareEventResult None()
        {
            return new CareEventResult(false, string.Empty, string.Empty);
        }
    }

    public sealed class RandomEventSystem
    {
        public GameEventDefinition Roll(GameEventDefinition[] candidates)
        {
            if (candidates == null)
            {
                return null;
            }

            foreach (var candidate in candidates)
            {
                if (candidate == null || candidate.isHiddenUntilUnlocked)
                {
                    continue;
                }

                if (Random.value <= candidate.baseChance)
                {
                    return candidate;
                }
            }

            return null;
        }

        public CareEventResult RollCareEvent(CheeseTamaModel tama, bool force = false)
        {
            if (tama == null || tama.stats == null)
            {
                return CareEventResult.None();
            }

            var candidate = PickConditionEvent(tama);
            if (candidate.occurred && (force || Random.value <= GetEventChance(candidate.eventId)))
            {
                return candidate;
            }

            if (force || Random.value <= 0.06f)
            {
                return new CareEventResult(true, "quiet_hum", "이벤트: 밀크룸이 치즈타마에게 부드럽게 울렸습니다.");
            }

            return CareEventResult.None();
        }

        private static CareEventResult PickConditionEvent(CheeseTamaModel tama)
        {
            if (tama.stats.health < 35)
            {
                return new CareEventResult(true, "small_fever", "이벤트: 치즈타마가 추워해서 조명이 더 따뜻해졌습니다.");
            }

            if (tama.stats.hunger < 25)
            {
                return new CareEventResult(true, "hungry_peep", "이벤트: 치즈타마가 배고픈 소리를 작게 냈습니다.");
            }

            if (tama.stats.cleanliness < 35)
            {
                return new CareEventResult(true, "dusty_corner", "이벤트: 먼지 낀 구석이 치즈타마의 눈에 들어왔습니다.");
            }

            if (tama.stats.sleepiness > 75)
            {
                return new CareEventResult(true, "sleepy_yawn", "이벤트: 치즈타마가 졸린 하품을 했습니다.");
            }

            if (tama.stats.mood > 80)
            {
                return new CareEventResult(true, "happy_wiggle", "이벤트: 치즈타마가 기분 좋게 살짝 흔들렸습니다.");
            }

            return CareEventResult.None();
        }

        private static float GetEventChance(string eventId)
        {
            return eventId switch
            {
                "happy_wiggle" => 0.22f,
                "small_fever" => 0.36f,
                "hungry_peep" => 0.34f,
                "dusty_corner" => 0.32f,
                "sleepy_yawn" => 0.32f,
                _ => 0.08f
            };
        }
    }
}
