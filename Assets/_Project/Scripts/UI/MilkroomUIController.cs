using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkroomUIController : MonoBehaviour
    {
        private const string RecordDetailVerticalGap = "\n<size=3> </size>\n";
        private const float RecordPanelWidth = 360f;
        private const float RecordPanelMinHeight = 510f;
        private const float RecordSectionLeft = 12f;
        private const float RecordSectionWidth = 336f;
        private const float RecordSectionGap = 12f;
        private const float RecordTextLeft = 10f;
        private const float RecordTextTop = 10f;
        private const float RecordTextWidth = 316f;
        private const float RecordTextVerticalPadding = 26f;
        private const int RecordMultilineFontSize = 16;
        private const float RecordFixedLineHeight = 30f;
        private const float RecordFixedLineGap = 8f;
        private const float RecordScrollableLineLimit = 3f;
        private const float RecordScrollGapAllowance = 20f;
        private const float RecordCareSummaryMinHeight = 80f;
        private const float RecordDailyRoutineMinHeight = 100f;

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
        [SerializeField] private Text coinEconomyText;
        [SerializeField] private Text milkDropEconomyText;
        [SerializeField] private Text collectionFragmentEconomyText;
        [SerializeField] private Text careTipText;
        [SerializeField] private Text lastSavedText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text eventMessageText;

        private CheeseTamaModel current;
        private CheeseTamaSaveData currentSave;
        private float presenceTickAccumulator;
        private CanvasGroup eventMessageCanvasGroup;
        private float eventMessageFadeTarget;
        private int careTipRotationIndex;

        private const float EventMessageFadeSeconds = 0.45f;

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
            Text messageLabel,
            Text eventMessageLabel = null,
            Text coinEconomyLabel = null,
            Text milkDropEconomyLabel = null,
            Text collectionFragmentEconomyLabel = null)
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
            coinEconomyText = coinEconomyLabel;
            milkDropEconomyText = milkDropEconomyLabel;
            collectionFragmentEconomyText = collectionFragmentEconomyLabel;
            careTipText = careTipLabel;
            lastSavedText = lastSavedLabel;
            messageText = messageLabel;
            eventMessageText = eventMessageLabel;
            EnsureEventMessageCanvasGroup();
        }

        private void Update()
        {
            UpdateEventMessageFade();

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
                RefreshRecordPanelLayout();
                return;
            }

            SetText(nameText, current.name);
            SetText(levelText, $"레벨 {current.level} ({current.levelProgress}%)");
            SetText(formText, FormatRecordLine("형태", FormatFormName(current.form)));
            SetText(conditionText, FormatRecordLine("상태", FormatCondition(current)));
            SetText(hungerText, FormatStatLine("포만감", current.stats.hunger));
            SetText(moodText, FormatStatLine("기분", current.stats.mood));
            SetText(cleanlinessText, FormatStatLine("청결", current.stats.cleanliness));
            SetText(sleepinessText, FormatStatLine("졸림", current.stats.sleepiness));
            SetText(healthText, FormatStatLine("건강", current.stats.health));
            SetText(affectionText, FormatRecordLine("애정", current.stats.affection.ToString()));
            SetText(maturationText, FormatRecordLine("성숙도", current.stats.maturation.ToString()));
            SetText(hatchProgressText, FormatHatchProgress(current));
            SetText(basicMilkGrowthText, FormatMainMilkGrowthLines(currentSave));
            SetText(starMilkGrowthText, FormatStarMilkGrowthLine(currentSave));
            SetText(unlockText, FormatUnlocks(currentSave));
            SetText(careSummaryText, FormatCareSummary(currentSave));
            SetText(dailyRoutineText, FormatDailyRoutine(currentSave));
            SetText(sessionText, FormatSession(currentSave));
            SetText(economyText, FormatEconomy(currentSave));
            RefreshEconomyResourceTexts();
            RefreshCareTip();
            RefreshLastSavedText();
            RefreshRecordPanelLayout();
        }

        public void ShowMessage(string message)
        {
            SetText(messageText, message);
            AdvanceCareTip();
        }

        public void AdvanceCareTip()
        {
            careTipRotationIndex += 1;
            RefreshCareTip();
        }

        public void SetLastSavedText(Text lastSavedLabel)
        {
            lastSavedText = lastSavedLabel;
            RefreshLastSavedText();
        }

        private void RefreshCareTip()
        {
            SetText(careTipText, FormatCareTip(currentSave, current, careTipRotationIndex));
        }

        private void RefreshLastSavedText()
        {
            if (current == null)
            {
                SetText(lastSavedText, FormatRecordLine("마지막 저장", "없음"));
                return;
            }

            SetText(lastSavedText, FormatRecordLine("마지막 저장", FormatIso(current.lastSavedAtIso)));
        }

        public void ShowEventMessage(string message)
        {
            if (eventMessageText == null)
            {
                return;
            }

            var hasMessage = !string.IsNullOrWhiteSpace(message);
            var bar = eventMessageText.transform.parent;
            EnsureEventMessageCanvasGroup();

            if (hasMessage)
            {
                if (bar != null)
                {
                    bar.gameObject.SetActive(true);
                }

                eventMessageFadeTarget = 1f;
                if (eventMessageCanvasGroup != null)
                {
                    eventMessageCanvasGroup.alpha = 1f;
                    eventMessageCanvasGroup.interactable = false;
                    eventMessageCanvasGroup.blocksRaycasts = false;
                }

                eventMessageText.text = message;
                return;
            }

            eventMessageFadeTarget = 0f;
            if (bar != null && !bar.gameObject.activeSelf)
            {
                bar.gameObject.SetActive(false);
            }
        }

        private void EnsureEventMessageCanvasGroup()
        {
            if (eventMessageText == null)
            {
                eventMessageCanvasGroup = null;
                eventMessageFadeTarget = 0f;
                return;
            }

            var bar = eventMessageText.transform.parent;
            if (bar == null)
            {
                eventMessageCanvasGroup = null;
                eventMessageFadeTarget = 0f;
                return;
            }

            if (!bar.TryGetComponent(out eventMessageCanvasGroup))
            {
                eventMessageCanvasGroup = bar.gameObject.AddComponent<CanvasGroup>();
            }

            var active = bar.gameObject.activeSelf;
            eventMessageFadeTarget = active ? 1f : 0f;
            eventMessageCanvasGroup.alpha = active ? 1f : 0f;
            eventMessageCanvasGroup.interactable = false;
            eventMessageCanvasGroup.blocksRaycasts = false;
        }

        private void UpdateEventMessageFade()
        {
            if (eventMessageCanvasGroup == null)
            {
                return;
            }

            var currentAlpha = eventMessageCanvasGroup.alpha;
            if (!Mathf.Approximately(currentAlpha, eventMessageFadeTarget))
            {
                var step = Time.unscaledDeltaTime / Mathf.Max(0.01f, EventMessageFadeSeconds);
                eventMessageCanvasGroup.alpha = Mathf.MoveTowards(currentAlpha, eventMessageFadeTarget, step);
            }

            if (eventMessageFadeTarget <= 0f && eventMessageCanvasGroup.alpha <= 0.001f)
            {
                var bar = eventMessageCanvasGroup.transform;
                if (bar != null && bar.gameObject.activeSelf)
                {
                    bar.gameObject.SetActive(false);
                }
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        public void RefreshRecordPanelLayout()
        {
            var careSection = GetSection(careSummaryText);
            var dailySection = GetSection(dailyRoutineText);
            var panel = careSection != null ? careSection.parent as RectTransform : null;
            if (careSection == null || dailySection == null || panel == null)
            {
                return;
            }

            var identitySection = FindRecordSection(panel, "Record Identity Section");
            var growthSection = FindRecordSection(panel, "Record Growth Section");
            if (identitySection == null || growthSection == null)
            {
                return;
            }

            var identityY = identitySection.anchoredPosition.y;
            var identityHeight = identitySection.sizeDelta.y;
            var growthHeight = growthSection.sizeDelta.y;
            var growthY = identityY - identityHeight - RecordSectionGap;
            var careY = growthY - growthHeight - RecordSectionGap;

            ConfigureRecordSection(identitySection, identityY, identityHeight);
            ConfigureRecordSection(growthSection, growthY, growthHeight);
            ConfigureFixedRecordText(formText, 0);
            ConfigureFixedRecordText(conditionText, 1);
            ConfigureFixedRecordText(affectionText, 0);
            ConfigureFixedRecordText(maturationText, 1);
            ConfigureFixedRecordText(hatchProgressText, 2);

            var careHeight = RefreshScrollableRecordSection(
                careSummaryText,
                careY,
                RecordCareSummaryMinHeight,
                true,
                true);
            var dailyY = careY - careHeight - RecordSectionGap;
            RefreshScrollableRecordSection(
                dailyRoutineText,
                dailyY,
                RecordDailyRoutineMinHeight,
                true,
                true);

            panel.sizeDelta = new Vector2(RecordPanelWidth, RecordPanelMinHeight);
        }

        private static RectTransform FindRecordSection(RectTransform panel, string sectionName)
        {
            if (panel == null)
            {
                return null;
            }

            var section = panel.Find(sectionName);
            return section != null ? section as RectTransform : null;
        }

        private static RectTransform GetSection(Text label)
        {
            return label != null ? label.transform.parent as RectTransform : null;
        }

        private static void ConfigureRecordSection(RectTransform section, float topY, float height)
        {
            if (section == null)
            {
                return;
            }

            section.anchorMin = new Vector2(0f, 1f);
            section.anchorMax = new Vector2(0f, 1f);
            section.pivot = new Vector2(0f, 1f);
            section.anchoredPosition = new Vector2(RecordSectionLeft, topY);
            section.sizeDelta = new Vector2(RecordSectionWidth, height);
        }

        private static float RefreshScrollableRecordSection(
            Text label,
            float topY,
            float minHeight,
            bool centerContent = false,
            bool fixedHeight = false)
        {
            var section = GetSection(label);
            if (section == null)
            {
                return minHeight;
            }

            PrepareRecordText(label);
            Canvas.ForceUpdateCanvases();

            var preferredTextHeight = Mathf.Ceil(Mathf.Max(1f, label.preferredHeight));
            var maxVisibleTextHeight = Mathf.Ceil(label.fontSize * Mathf.Max(1f, label.lineSpacing) * RecordScrollableLineLimit + RecordScrollGapAllowance);
            var visibleTextHeight = Mathf.Min(preferredTextHeight, maxVisibleTextHeight);
            var sectionHeight = fixedHeight
                ? Mathf.Ceil(minHeight)
                : Mathf.Ceil(Mathf.Max(minHeight, visibleTextHeight + RecordTextVerticalPadding));
            if (centerContent && !fixedHeight)
            {
                sectionHeight = Mathf.Ceil(Mathf.Max(minHeight, preferredTextHeight + RecordTextVerticalPadding));
            }

            var availableTextHeight = Mathf.Ceil(Mathf.Max(1f, sectionHeight - RecordTextVerticalPadding));
            var scrollable = !centerContent && preferredTextHeight > availableTextHeight + 1f;
            var contentHeight = centerContent
                ? availableTextHeight
                : scrollable
                ? Mathf.Ceil(Mathf.Max(availableTextHeight, preferredTextHeight))
                : availableTextHeight;
            var contentTop = centerContent
                ? (sectionHeight - contentHeight) * 0.5f
                : RecordTextTop;

            ConfigureRecordSection(section, topY, sectionHeight);
            label.alignment = scrollable ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            ConfigureTextRect(label, new Vector2(RecordTextLeft, -contentTop), new Vector2(RecordTextWidth, contentHeight));
            ConfigureRecordScroll(section, label.rectTransform, scrollable);
            return sectionHeight;
        }

        private static void PrepareRecordText(Text label)
        {
            if (label == null)
            {
                return;
            }

            label.supportRichText = true;
            label.fontSize = RecordMultilineFontSize;
            label.resizeTextForBestFit = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.12f;
        }

        private static void ConfigureFixedRecordText(Text label, int lineIndex)
        {
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleLeft;
            }

            var y = -RecordTextTop - ((RecordFixedLineHeight + RecordFixedLineGap) * Mathf.Max(0, lineIndex));
            ConfigureTextRect(label, new Vector2(RecordTextLeft, y), new Vector2(RecordTextWidth, RecordFixedLineHeight));
        }

        private static void ConfigureTextRect(Text label, Vector2 anchoredPosition, Vector2 size)
        {
            if (label == null || label.rectTransform == null)
            {
                return;
            }

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void ConfigureRecordScroll(RectTransform section, RectTransform content, bool scrollable)
        {
            if (section == null || content == null)
            {
                return;
            }

            var scrollRect = section.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = section.gameObject.AddComponent<ScrollRect>();
            }

            if (section.GetComponent<RectMask2D>() == null)
            {
                section.gameObject.AddComponent<RectMask2D>();
            }

            scrollRect.viewport = section;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = scrollable;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 18f;
            scrollRect.inertia = true;
            scrollRect.enabled = scrollable;

            if (section.TryGetComponent(out Image image))
            {
                image.raycastTarget = scrollable;
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
                return FormatRecordLine("부화 상태", "깨어남");
            }

            return FormatRecordLine("부화 진행", $"{HatchingSystem.GetHatchProgressPercent(tama)}%");
        }

        private static string FormatStatLine(string label, int value)
        {
            return $"{label}  {Mathf.Clamp(value, 0, 100)}/100";
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

        private static string FormatMainMilkGrowthLines(CheeseTamaSaveData saveData)
        {
            var lines = new System.Text.StringBuilder();
            for (var i = 0; i < MilkCatalog.MainMilks.Length; i++)
            {
                var milk = MilkCatalog.MainMilks[i];
                if (milk == null)
                {
                    continue;
                }

                if (lines.Length > 0)
                {
                    lines.AppendLine();
                }

                lines.Append(FormatMilkGrowthLine(saveData, milk));
            }

            return lines.ToString();
        }

        private static string FormatMilkGrowthLine(CheeseTamaSaveData saveData, MilkDefinition milk)
        {
            var entry = FindMilkGrowthEntry(saveData, milk.id);
            var unlocked = IsMilkUnlocked(saveData, milk);
            if (entry == null)
            {
                return FormatRecordLine(milk.displayName, unlocked ? "Lv.0 / 0점" : $"잠김 · {FormatUnlockRequirement(milk)}");
            }

            return FormatRecordLine(milk.displayName, unlocked
                ? $"Lv.{entry.growthLevel} / {entry.growthPoints}점"
                : $"잠김 · {FormatUnlockRequirement(milk)}");
        }

        private static string FormatStarMilkGrowthLine(CheeseTamaSaveData saveData)
        {
            if (saveData == null || saveData.unlocks == null || !saveData.unlocks.starMilkUnlocked)
            {
                return FormatRecordLine("별빛 우유", "조건 충족 후 표시");
            }

            return FormatMilkGrowthLine(saveData, MilkCatalog.StarMilk);
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

        private static int GetMilkGrowthLevel(CheeseTamaSaveData saveData, string milkId)
        {
            return FindMilkGrowthEntry(saveData, milkId)?.growthLevel ?? 0;
        }

        private static bool IsMilkUnlocked(CheeseTamaSaveData saveData, MilkDefinition milk)
        {
            if (milk == null)
            {
                return false;
            }

            if (milk.id == MilkCatalog.BasicMilkId)
            {
                return true;
            }

            if (saveData == null)
            {
                return false;
            }

            saveData.EnsureRuntimeDefaults();
            if (milk.id == MilkCatalog.StarMilkId)
            {
                return saveData.unlocks != null && saveData.unlocks.starMilkUnlocked;
            }

            return milk.IsUnlocked(GetMilkGrowthLevel(saveData, milk.requiredMilkId));
        }

        private static string FormatUnlockRequirement(MilkDefinition milk)
        {
            var requiredMilk = MilkCatalog.Find(milk.requiredMilkId);
            return requiredMilk == null
                ? "처음부터 사용 가능"
                : $"{requiredMilk.displayName} Lv.{milk.requiredMilkLevel}";
        }

        private static string FormatUnlocks(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                return FormatRecordLine("별빛 조건", "저장 데이터 없음");
            }

            saveData.EnsureRuntimeDefaults();
            if (saveData.unlocks != null && saveData.unlocks.starMilkUnlocked)
            {
                return FormatRecordLine("별빛 조건", "별빛 알 / 별빛 우유 해금");
            }

            var completedMainMilks = 0;
            foreach (var milk in MilkCatalog.MainMilks)
            {
                if (milk != null && GetMilkGrowthLevel(saveData, milk.id) >= MilkCatalog.MainMilkMaxGrowthLevel)
                {
                    completedMainMilks += 1;
                }
            }

            var level = saveData.cheeseTama != null ? saveData.cheeseTama.level : 0;
            var starMilkState = $"주요 우유 {completedMainMilks}/{MilkCatalog.MainMilks.Length}개 Lv.5 · 치즈타마 Lv.{level}/33";
            return FormatRecordLine("별빛 조건", starMilkState);
        }

        private static string FormatCareSummary(CheeseTamaSaveData saveData)
        {
            var history = saveData?.careHistory;
            if (history == null)
            {
                return "<b>돌봄 누적</b>  0회" + RecordDetailVerticalGap + "놀이 0  청소 0  휴식 0";
            }

            return $"<b>돌봄 누적</b>  {history.totalCareActions}회{RecordDetailVerticalGap}놀이 {history.playSessions}  청소 {history.cleanings}  휴식 {history.rests}";
        }

        private static string FormatDailyRoutine(CheeseTamaSaveData saveData)
        {
            var daily = saveData?.dailyCare;
            if (daily == null)
            {
                return "<b>오늘 루틴</b>" + RecordDetailVerticalGap + "먹기 0/3  요리 0/2\n놀이 0/3  청소 0/2  휴식 0/2";
            }

            var eatingCount = daily.milkFeeds + daily.snacksFed;
            return $"<b>오늘 루틴</b>{RecordDetailVerticalGap}먹기 {ClampGoal(eatingCount, DailyCareSaveData.EatingGoal)}/{DailyCareSaveData.EatingGoal}  요리 {ClampGoal(daily.cookings, DailyCareSaveData.CookingGoal)}/{DailyCareSaveData.CookingGoal}\n놀이 {ClampGoal(daily.playSessions, DailyCareSaveData.PlayGoal)}/{DailyCareSaveData.PlayGoal}  청소 {ClampGoal(daily.cleanings, DailyCareSaveData.CleanGoal)}/{DailyCareSaveData.CleanGoal}  휴식 {ClampGoal(daily.rests, DailyCareSaveData.RestGoal)}/{DailyCareSaveData.RestGoal}";
        }

        private static string FormatRecordLine(string title, string value)
        {
            return $"<b>{title}</b>  {value}";
        }

        private static string FormatSession(CheeseTamaSaveData saveData)
        {
            var session = saveData?.milkroomSession;
            if (session == null)
            {
                return "밀크룸에 머문 시간 00:00\n오늘 총 플레이 시간 00:00";
            }

            return $"밀크룸에 머문 시간 {FormatDuration(session.currentSessionSeconds)}\n오늘 총 플레이 시간 {FormatDuration(session.todaySeconds)}";
        }

        private static string FormatEconomy(CheeseTamaSaveData saveData)
        {
            var economy = saveData?.economy;
            if (economy == null)
            {
                return "코인 0   우유방울 0   도감조각 0";
            }

            return $"코인 {economy.milkCoins}   우유방울 {economy.milkDrops}   도감조각 {economy.collectionFragments}";
        }

        private void RefreshEconomyResourceTexts()
        {
            var economy = currentSave?.economy;
            var milkCoins = economy != null ? economy.milkCoins : 0;
            var milkDrops = economy != null ? economy.milkDrops : 0;
            var collectionFragments = economy != null ? economy.collectionFragments : 0;

            SetText(coinEconomyText, $"코인 {milkCoins}");
            SetText(milkDropEconomyText, $"우유방울 {milkDrops}");
            SetText(collectionFragmentEconomyText, $"도감조각 {collectionFragments}");
        }

        private static string FormatCareTip(CheeseTamaSaveData saveData, CheeseTamaModel tama, int rotationIndex)
        {
            if (tama == null || tama.stats == null)
            {
                return "치즈타마 데이터를 불러오세요.";
            }

            var tips = new List<string>();
            if (tama.stats.health < 35)
            {
                tips.Add("건강이 낮아요. 휴식과 청소를 먼저 해주세요.");
            }

            if (tama.stats.hunger < 30)
            {
                tips.Add("포만감이 낮아요. 우유주기나 간식을 챙겨주세요.");
            }

            if (tama.stats.cleanliness < 35)
            {
                tips.Add("청결이 낮아요. 청소하기로 방을 정리하세요.");
            }

            if (tama.stats.sleepiness > 75)
            {
                tips.Add("졸림이 높아요. 휴식하기로 쉬게 해주세요.");
            }

            if (tama.stats.mood < 45)
            {
                tips.Add("기분이 낮아요. 놀아주기나 간식이 좋아요.");
            }

            if (!tama.isHatched)
            {
                var hatchProgress = HatchingSystem.GetHatchProgressPercent(tama);
                tips.Add(hatchProgress >= 75
                    ? "부화가 가까워졌어요. 상태를 안정시켜 주세요."
                    : "우유주기는 부화 진행을 올리는 기본 돌봄이에요.");
                tips.Add("알 상태에서는 포만감과 건강을 고르게 챙겨주세요.");
            }

            if (saveData != null
                && saveData.unlocks != null
                && saveData.unlocks.starMilkUnlocked
                && FindMilkGrowthEntry(saveData, MilkCatalog.StarMilkId) == null)
            {
                tips.Add("별빛 우유가 열렸어요. 우유주기에서 확인하세요.");
            }

            if (saveData != null
                && saveData.dailyCare != null
                && !IsDailyRoutineComplete(saveData.dailyCare))
            {
                tips.Add(FormatNextDailyRoutineStep(saveData.dailyCare));
            }

            if (saveData != null
                && saveData.milkroomSession != null
                && saveData.milkroomSession.currentSessionSeconds < 300)
            {
                tips.Add("우유 방울 보상은 5분 머무르면 얻어요");
            }

            if (tama.stats.hunger >= 70
                && tama.stats.mood >= 70
                && tama.stats.cleanliness >= 70
                && tama.stats.sleepiness <= 35
                && tama.stats.health >= 80)
            {
                tips.Add("상태가 안정적이에요. 루틴을 이어가세요.");
            }

            tips.Add("우유 종류를 바꾸면 성장 기록이 채워져요.");
            tips.Add("요리한 음식은 간식가방에서 먹일 수 있어요.");
            tips.Add("놀아주기는 기분을 빠르게 올려줘요.");
            tips.Add("청소하기는 건강 관리에도 도움이 돼요.");
            tips.Add("휴식하기는 졸림을 낮추고 건강을 지켜줘요.");
            tips.Add("오늘 루틴을 채우면 돌봄 기록이 쌓여요.");
            tips.Add("해금 조건은 밀크룸 기록과 도감에서 확인하세요.");
            tips.Add("상태 수치가 고르게 높으면 안정적이에요.");

            var index = rotationIndex % tips.Count;
            if (index < 0)
            {
                index += tips.Count;
            }

            return tips[index];
        }

        private static string FormatDuration(int seconds)
        {
            var safeSeconds = Mathf.Max(0, seconds);
            var minutes = safeSeconds / 60;
            var remainingSeconds = safeSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        private static int ClampGoal(int value, int goal)
        {
            return Mathf.Clamp(value, 0, Mathf.Max(1, goal));
        }

        private static bool IsDailyRoutineComplete(DailyCareSaveData daily)
        {
            return daily != null
                && daily.milkFeeds + daily.snacksFed >= DailyCareSaveData.EatingGoal
                && daily.cookings >= DailyCareSaveData.CookingGoal
                && daily.playSessions >= DailyCareSaveData.PlayGoal
                && daily.cleanings >= DailyCareSaveData.CleanGoal
                && daily.rests >= DailyCareSaveData.RestGoal;
        }

        private static string FormatNextDailyRoutineStep(DailyCareSaveData daily)
        {
            if (daily.milkFeeds + daily.snacksFed < DailyCareSaveData.EatingGoal)
            {
                return "오늘 루틴: 먹기 채우기.";
            }

            if (daily.cookings < DailyCareSaveData.CookingGoal)
            {
                return "오늘 루틴: 요리하기.";
            }

            if (daily.playSessions < DailyCareSaveData.PlayGoal)
            {
                return "오늘 루틴: 놀아주기.";
            }

            if (daily.cleanings < DailyCareSaveData.CleanGoal)
            {
                return "오늘 루틴: 청소하기.";
            }

            if (daily.rests < DailyCareSaveData.RestGoal)
            {
                return "오늘 루틴: 휴식하기.";
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
