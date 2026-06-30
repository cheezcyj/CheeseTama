using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkroomUIController : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text formText;
        [SerializeField] private Text conditionText;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text moodText;
        [SerializeField] private Text cleanlinessText;
        [SerializeField] private Text sleepinessText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text affectionText;
        [SerializeField] private Text maturationText;
        [SerializeField] private Text hatchProgressText;
        [SerializeField] private Text basicMilkGrowthText;
        [SerializeField] private Text starMilkGrowthText;
        [SerializeField] private Text unlockText;
        [SerializeField] private Text careSummaryText;
        [SerializeField] private Text dailyRoutineText;
        [SerializeField] private Text sessionText;
        [SerializeField] private Text economyText;
        [SerializeField] private Text careTipText;
        [SerializeField] private Text lastSavedText;
        [SerializeField] private Text messageText;

        private CheeseTamaModel current;
        private CheeseTamaSaveData currentSave;
        private float presenceTickAccumulator;

        public void Configure(
            Text nameLabel,
            Text levelLabel,
            Text formLabel,
            Text conditionLabel,
            Text hungerLabel,
            Text moodLabel,
            Text cleanlinessLabel,
            Text sleepinessLabel,
            Text healthLabel,
            Text affectionLabel,
            Text maturationLabel,
            Text hatchProgressLabel,
            Text basicMilkGrowthLabel,
            Text starMilkGrowthLabel,
            Text unlockLabel,
            Text careSummaryLabel,
            Text dailyRoutineLabel,
            Text sessionLabel,
            Text economyLabel,
            Text careTipLabel,
            Text lastSavedLabel,
            Text messageLabel)
        {
            nameText = nameLabel;
            levelText = levelLabel;
            formText = formLabel;
            conditionText = conditionLabel;
            hungerText = hungerLabel;
            moodText = moodLabel;
            cleanlinessText = cleanlinessLabel;
            sleepinessText = sleepinessLabel;
            healthText = healthLabel;
            affectionText = affectionLabel;
            maturationText = maturationLabel;
            hatchProgressText = hatchProgressLabel;
            basicMilkGrowthText = basicMilkGrowthLabel;
            starMilkGrowthText = starMilkGrowthLabel;
            unlockText = unlockLabel;
            careSummaryText = careSummaryLabel;
            dailyRoutineText = dailyRoutineLabel;
            sessionText = sessionLabel;
            economyText = economyLabel;
            careTipText = careTipLabel;
            lastSavedText = lastSavedLabel;
            messageText = messageLabel;
        }

        private void Update()
        {
            if (currentSave == null || GameManager.Instance == null)
            {
                return;
            }

            presenceTickAccumulator += Time.unscaledDeltaTime;
            if (presenceTickAccumulator < 1f)
            {
                return;
            }

            var seconds = Mathf.FloorToInt(presenceTickAccumulator);
            presenceTickAccumulator -= seconds;
            var rewardMessage = GameManager.Instance.TickMilkroomPresence(seconds);
            currentSave = GameManager.Instance.CurrentSave;
            current = currentSave?.cheeseTama;
            Refresh();

            if (!string.IsNullOrWhiteSpace(rewardMessage))
            {
                ShowMessage(rewardMessage);
            }
        }

        public void Bind(CheeseTamaModel tama)
        {
            current = tama;
            currentSave = null;
            Refresh();
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
                return;
            }

            SetText(nameText, current.name);
            SetText(levelText, $"레벨 {current.level} ({current.levelProgress}%)");
            SetText(formText, $"형태: {FormatFormName(current.form)}");
            SetText(conditionText, $"컨디션: {FormatCondition(current)}");
            SetText(hungerText, $"포만감: {current.stats.hunger}");
            SetText(moodText, $"기분: {current.stats.mood}");
            SetText(cleanlinessText, $"청결: {current.stats.cleanliness}");
            SetText(sleepinessText, $"졸림: {current.stats.sleepiness}");
            SetText(healthText, $"건강: {current.stats.health}");
            SetText(affectionText, $"애정: {current.stats.affection}");
            SetText(maturationText, $"성숙도: {current.stats.maturation}");
            SetText(hatchProgressText, FormatHatchProgress(current));
            SetText(basicMilkGrowthText, FormatMilkGrowthLine(currentSave, "basic_milk", "기본 우유"));
            SetText(starMilkGrowthText, FormatStarMilkGrowthLine(currentSave));
            SetText(unlockText, FormatUnlocks(currentSave));
            SetText(careSummaryText, FormatCareSummary(currentSave));
            SetText(dailyRoutineText, FormatDailyRoutine(currentSave));
            SetText(sessionText, FormatSession(currentSave));
            SetText(economyText, FormatEconomy(currentSave));
            SetText(careTipText, FormatCareTip(currentSave, current));
            SetText(lastSavedText, $"마지막 저장: {FormatIso(current.lastSavedAtIso)}");
        }

        public void ShowMessage(string message)
        {
            SetText(messageText, message);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static string FormatIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
            {
                return "없음";
            }

            return iso.Length > 19 ? iso.Substring(0, 19).Replace('T', ' ') : iso;
        }

        private static string FormatHatchProgress(CheeseTamaModel tama)
        {
            if (tama.isHatched)
            {
                return "부화: 깨어남";
            }

            return $"부화: {HatchingSystem.GetHatchProgressPercent(tama)}%";
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

        private static string FormatMilkGrowthLine(CheeseTamaSaveData saveData, string milkId, string displayName)
        {
            var entry = FindMilkGrowthEntry(saveData, milkId);
            if (entry == null)
            {
                return $"{displayName}: 레벨 0 (0점)";
            }

            return $"{displayName}: 레벨 {entry.growthLevel} ({entry.growthPoints}점)";
        }

        private static string FormatStarMilkGrowthLine(CheeseTamaSaveData saveData)
        {
            if (saveData == null || saveData.unlocks == null || !saveData.unlocks.starMilkUnlocked)
            {
                return "별빛 우유: 잠김";
            }

            return FormatMilkGrowthLine(saveData, "star_milk", "별빛 우유");
        }

        private static MilkGrowthSaveEntry FindMilkGrowthEntry(CheeseTamaSaveData saveData, string milkId)
        {
            if (saveData == null || saveData.milkGrowth == null)
            {
                return null;
            }

            foreach (var entry in saveData.milkGrowth)
            {
                if (entry != null && entry.milkId == milkId)
                {
                    return entry;
                }
            }

            return null;
        }

        private static string FormatUnlocks(CheeseTamaSaveData saveData)
        {
            var starMilkState = saveData != null && saveData.unlocks != null && saveData.unlocks.starMilkUnlocked
                ? "별빛 우유 해금"
                : "별빛 우유 잠김";
            return $"해금: {starMilkState}";
        }

        private static string FormatCareSummary(CheeseTamaSaveData saveData)
        {
            var history = saveData?.careHistory;
            if (history == null)
            {
                return "돌봄: 0 | 놀이 0 청소 0 휴식 0";
            }

            return $"돌봄: {history.totalCareActions} | 놀이 {history.playSessions} 청소 {history.cleanings} 휴식 {history.rests}";
        }

        private static string FormatDailyRoutine(CheeseTamaSaveData saveData)
        {
            var daily = saveData?.dailyCare;
            if (daily == null)
            {
                return "오늘: 우유 0/1 놀이 0/1 청소 0/1 휴식 0/1";
            }

            return $"오늘: 우유 {ClampGoal(daily.milkFeeds)}/1 놀이 {ClampGoal(daily.playSessions)}/1 청소 {ClampGoal(daily.cleanings)}/1 휴식 {ClampGoal(daily.rests)}/1";
        }

        private static string FormatSession(CheeseTamaSaveData saveData)
        {
            var session = saveData?.milkroomSession;
            if (session == null)
            {
                return "세션: 00:00 | 오늘 00:00";
            }

            return $"세션: {FormatDuration(session.currentSessionSeconds)} | 오늘 {FormatDuration(session.todaySeconds)}";
        }

        private static string FormatEconomy(CheeseTamaSaveData saveData)
        {
            var economy = saveData?.economy;
            if (economy == null)
            {
                return "보유: 코인 0 방울 0 조각 0";
            }

            return $"보유: 코인 {economy.milkCoins} 방울 {economy.milkDrops} 조각 {economy.collectionFragments}";
        }

        private static string FormatCareTip(CheeseTamaSaveData saveData, CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return "돌봄 팁: 치즈타마 데이터를 불러오세요.";
            }

            if (tama.stats.health < 35)
            {
                return "돌봄 팁: 먼저 쉬게 하고 방을 청소하세요.";
            }

            if (tama.stats.hunger < 30)
            {
                return "돌봄 팁: 우유나 간식을 주세요.";
            }

            if (tama.stats.cleanliness < 35)
            {
                return "돌봄 팁: 밀크룸을 청소하세요.";
            }

            if (tama.stats.sleepiness > 75)
            {
                return "돌봄 팁: 따뜻한 빛 아래에서 쉬게 하세요.";
            }

            if (tama.stats.mood < 45)
            {
                return "돌봄 팁: 놀아주거나 간식을 주세요.";
            }

            if (!tama.isHatched)
            {
                var hatchProgress = HatchingSystem.GetHatchProgressPercent(tama);
                return hatchProgress >= 75
                    ? "돌봄 팁: 부화가 가까워졌습니다."
                    : "돌봄 팁: 우유를 먹이면 성장합니다.";
            }

            if (saveData != null
                && saveData.unlocks != null
                && saveData.unlocks.starMilkUnlocked
                && FindMilkGrowthEntry(saveData, "star_milk") == null)
            {
                return "돌봄 팁: 별빛 우유를 시도해 보세요.";
            }

            if (saveData != null
                && saveData.dailyCare != null
                && !IsDailyRoutineComplete(saveData.dailyCare))
            {
                return $"돌봄 팁: {FormatNextDailyRoutineStep(saveData.dailyCare)}";
            }

            if (saveData != null
                && saveData.milkroomSession != null
                && saveData.milkroomSession.currentSessionSeconds < 300)
            {
                return "돌봄 팁: 우유 방울 보상을 위해 5분까지 머물러 보세요.";
            }

            if (tama.stats.hunger >= 70
                && tama.stats.mood >= 70
                && tama.stats.cleanliness >= 70
                && tama.stats.sleepiness <= 35
                && tama.stats.health >= 80)
            {
                return "돌봄 팁: 안정적인 상태입니다.";
            }

            return "돌봄 팁: 천천히 리듬을 유지하세요.";
        }

        private static string FormatDuration(int seconds)
        {
            var safeSeconds = Mathf.Max(0, seconds);
            var minutes = safeSeconds / 60;
            var remainingSeconds = safeSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static int ClampGoal(int value)
        {
            return value > 0 ? 1 : 0;
        }

        private static bool IsDailyRoutineComplete(DailyCareSaveData daily)
        {
            return daily != null
                && daily.milkFeeds >= 1
                && daily.playSessions >= 1
                && daily.cleanings >= 1
                && daily.rests >= 1;
        }

        private static string FormatNextDailyRoutineStep(DailyCareSaveData daily)
        {
            if (daily.milkFeeds < 1)
            {
                return "오늘 루틴: 우유 주기.";
            }

            if (daily.playSessions < 1)
            {
                return "오늘 루틴: 한 번 놀아주기.";
            }

            if (daily.cleanings < 1)
            {
                return "오늘 루틴: 한 번 청소하기.";
            }

            if (daily.rests < 1)
            {
                return "오늘 루틴: 한 번 쉬게 하기.";
            }

            return "오늘 루틴 완료.";
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
    }
}
