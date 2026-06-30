using System.Text;
using CheeseTama.Gameplay;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class DebugUIController : MonoBehaviour
    {
        [SerializeField] private Text stateText;
        [SerializeField] private Text messageText;

        private CheeseTamaSaveData currentSave;
        private CheeseTamaModel current;

        public void Configure(Text stateLabel, Text messageLabel)
        {
            stateText = stateLabel;
            messageText = messageLabel;
        }

        public void Bind(CheeseTamaSaveData saveData)
        {
            saveData?.EnsureRuntimeDefaults();
            currentSave = saveData;
            current = saveData?.cheeseTama;
            Refresh();
        }

        public void Refresh()
        {
            if (current == null || current.stats == null)
            {
                SetText(stateText, "개발자 상태\n치즈타마 저장 데이터를 불러오지 못했습니다.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("개발자 상태");
            builder.AppendLine($"이름: {current.name}");
            builder.AppendLine($"형태: {FormatFormName(current.form)}");
            builder.AppendLine($"레벨: {current.level} ({current.levelProgress}%)");
            builder.AppendLine($"컨디션: {FormatCondition(current)}");
            builder.AppendLine($"포만감: {current.stats.hunger}");
            builder.AppendLine($"기분: {current.stats.mood}");
            builder.AppendLine($"청결: {current.stats.cleanliness}");
            builder.AppendLine($"졸림: {current.stats.sleepiness}");
            builder.AppendLine($"건강: {current.stats.health}");
            builder.AppendLine($"애정: {current.stats.affection}");
            builder.AppendLine(FormatMilkGrowthLine("basic_milk", "기본 우유"));
            builder.AppendLine(FormatStarMilkLine());
            builder.AppendLine(FormatUnlocks());
            builder.AppendLine(FormatCareHistory());
            builder.AppendLine(FormatDailyRoutine());
            builder.AppendLine(FormatSession());
            builder.AppendLine(FormatEconomy());
            builder.AppendLine(FormatHiddenRecords());
            SetText(stateText, builder.ToString());
        }

        public void ShowMessage(string message)
        {
            SetText(messageText, message);
        }

        private string FormatMilkGrowthLine(string milkId, string displayName)
        {
            var entry = FindMilkGrowthEntry(milkId);
            if (entry == null)
            {
                return $"{displayName}: 레벨 0 (0점)";
            }

            return $"{displayName}: 레벨 {entry.growthLevel} ({entry.growthPoints}점)";
        }

        private string FormatStarMilkLine()
        {
            if (currentSave == null || currentSave.unlocks == null || !currentSave.unlocks.starMilkUnlocked)
            {
                return "별빛 우유: 잠김";
            }

            return FormatMilkGrowthLine("star_milk", "별빛 우유");
        }

        private MilkGrowthSaveEntry FindMilkGrowthEntry(string milkId)
        {
            if (currentSave == null || currentSave.milkGrowth == null)
            {
                return null;
            }

            foreach (var entry in currentSave.milkGrowth)
            {
                if (entry != null && entry.milkId == milkId)
                {
                    return entry;
                }
            }

            return null;
        }

        private string FormatUnlocks()
        {
            var starMilkState = currentSave != null && currentSave.unlocks != null && currentSave.unlocks.starMilkUnlocked
                ? "별빛 우유 해금"
                : "별빛 우유 잠김";
            return $"해금: {starMilkState}";
        }

        private string FormatHiddenRecords()
        {
            var count = currentSave != null
                && currentSave.collections != null
                && currentSave.collections.hiddenUnlockedOnly != null
                ? currentSave.collections.hiddenUnlockedOnly.Count
                : 0;
            return $"숨겨진 기록: {count}";
        }

        private string FormatCareHistory()
        {
            var history = currentSave?.careHistory;
            if (history == null)
            {
                return "돌봄: 0회";
            }

            return $"돌봄: {history.totalCareActions}회, 시간 경과 {history.waitHours}시간";
        }

        private string FormatDailyRoutine()
        {
            var daily = currentSave?.dailyCare;
            if (daily == null)
            {
                return "오늘: 우유 0/1 놀이 0/1 청소 0/1 휴식 0/1";
            }

            return $"오늘: 우유 {ClampGoal(daily.milkFeeds)}/1 놀이 {ClampGoal(daily.playSessions)}/1 청소 {ClampGoal(daily.cleanings)}/1 휴식 {ClampGoal(daily.rests)}/1, 완료 {daily.completedRoutineCount}";
        }

        private string FormatSession()
        {
            var session = currentSave?.milkroomSession;
            if (session == null)
            {
                return "세션: 00:00, 오늘 00:00, 획득 0";
            }

            return $"세션: {FormatDuration(session.currentSessionSeconds)}, 오늘 {FormatDuration(session.todaySeconds)}, 획득 {session.totalMilkDropCatches}";
        }

        private string FormatEconomy()
        {
            var economy = currentSave?.economy;
            if (economy == null)
            {
                return "보유: 코인 0, 방울 0, 조각 0";
            }

            return $"보유: 코인 {economy.milkCoins}, 방울 {economy.milkDrops}, 조각 {economy.collectionFragments}";
        }

        private static int ClampGoal(int value)
        {
            return value > 0 ? 1 : 0;
        }

        private static string FormatDuration(int seconds)
        {
            var safeSeconds = Mathf.Max(0, seconds);
            var minutes = safeSeconds / 60;
            var remainingSeconds = safeSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static string FormatFormName(string form)
        {
            if (form == "egg")
            {
                return "알";
            }

            if (form == "soft_cheesetama")
            {
                return "말랑 치즈타마";
            }

            return string.IsNullOrWhiteSpace(form) ? "알 수 없음" : form;
        }

        private static string FormatCondition(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return "알 수 없음";
            }

            if (tama.stats.health < 35)
            {
                return "아픔";
            }

            if (tama.stats.hunger < 25)
            {
                return "배고픔";
            }

            if (tama.stats.cleanliness < 35)
            {
                return "지저분함";
            }

            if (tama.stats.sleepiness > 75)
            {
                return "졸림";
            }

            if (tama.stats.mood > 80)
            {
                return "신남";
            }

            return tama.isHatched ? "호기심" : "따뜻함";
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
