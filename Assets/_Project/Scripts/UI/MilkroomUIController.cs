using System.Collections.Generic;
using CheeseTama.Core;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Feeding;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkroomUIController : MonoBehaviour
    {
        private const string RecordDetailVerticalGap = "\n";
        private const string RecordTextViewportName = "Record Text Viewport";
        private const string RecordTextContentName = "Record Text Content";
        private const float RecordPanelWidth = 360f;
        private const float RecordPanelMinHeight = 556f;
        private const float RecordPanelBottomPadding = 12f;
        private const float RecordSectionLeft = 12f;
        private const float RecordSectionWidth = 336f;
        private const float RecordSectionGap = 12f;
        private const float RecordTextLeft = 10f;
        private const float RecordFixedTextTop = 10f;
        private const float RecordTextViewportInset = 14f;
        private const float RecordTextContentInset = 4f;
        private const float RecordTextWidth = 316f;
        private const float RecordTextVerticalPadding = (RecordTextViewportInset + RecordTextContentInset) * 2f;
        private const int RecordMultilineFontSize = 16;
        private const float RecordFixedLineHeight = 30f;
        private const float RecordFixedLineGap = 8f;
        private const float RecordScrollableLineLimit = 3f;
        private const float RecordScrollGapAllowance = 20f;
        private const float RecordCareSummaryMinHeight = 96f;
        private const float RecordDailyRoutineMinHeight = 140f;
        private const int TopBarNameMaxCharacters = 12;
        private const int CenteredStatusExplanationLineLimit = 4;
        private const int HungerWarningThreshold = 30;
        private const int MoodWarningThreshold = 45;
        private const int CleanlinessWarningThreshold = 35;
        private const int SleepinessWarningThreshold = 75;
        private const int HealthWarningThreshold = 35;

        private static readonly Color StatTextColor = new Color(0.22f, 0.17f, 0.12f, 1f);
        private static readonly Color StatWarningTextColor = new Color(0.64f, 0.12f, 0.08f, 1f);
        private static readonly Color HungerGaugeColor = new Color(0.96f, 0.62f, 0.18f, 1f);
        private static readonly Color MoodGaugeColor = new Color(0.92f, 0.42f, 0.54f, 1f);
        private static readonly Color CleanlinessGaugeColor = new Color(0.26f, 0.68f, 0.82f, 1f);
        private static readonly Color SleepinessGaugeColor = new Color(0.50f, 0.48f, 0.86f, 1f);
        private static readonly Color HealthGaugeColor = new Color(0.30f, 0.70f, 0.38f, 1f);
        private static readonly Color LevelProgressGaugeColor = new Color(0.98f, 0.64f, 0.16f, 1f);
        private static readonly Color StatWarningGaugeColor = new Color(0.88f, 0.28f, 0.18f, 1f);

        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Image levelProgressGaugeFill;
        [SerializeField] private Text formText;
        [SerializeField] private Text conditionText;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text moodText;
        [SerializeField] private Text cleanlinessText;
        [SerializeField] private Text sleepinessText;
        [SerializeField] private Text healthText;
        [SerializeField] private Image hungerGaugeFill;
        [SerializeField] private Image moodGaugeFill;
        [SerializeField] private Image cleanlinessGaugeFill;
        [SerializeField] private Image sleepinessGaugeFill;
        [SerializeField] private Image healthGaugeFill;
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
        private FirstMeetingOnboardingController onboardingController;
        private ReturnSummaryController returnSummaryController;
        private GrowthMilestoneController growthMilestoneController;
        private MilkDropMiniGameController milkDropMiniGameController;
        private CleaningMiniGameController cleaningMiniGameController;
        private EvolutionMilestoneController evolutionMilestoneController;
        private CareEventCardController careEventCardController;
        private NewGameSetupController newGameSetupController;
        private PlayChoicePanelController playChoicePanelController;
        private CookingChoicePanelController cookingChoicePanelController;
        private MilkBlendingPanelController milkBlendingPanelController;
        private GrowthJourneyController growthJourneyController;
        private BouncyJumpMiniGameController bouncyJumpMiniGameController;
        private FirstDayJourneyController firstDayJourneyController;
        private CheeseStarDeliveryBridge cheeseStarDeliveryBridge;
        private MemoryJournalPanelController memoryJournalPanelController;
        private FantasyPowderHiddenRecipePanelController fantasyPowderPanelController;
        private CheeseTamaProfileMenuController profileMenuController;
        private SleepSchedulePanelController sleepSchedulePanelController;

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
            if (messageText != null)
            {
                SetStatusExplanationText(messageText, messageText.text);
            }
        }

        public void ConfigureStatGauges(
            Image hungerFill,
            Image moodFill,
            Image cleanlinessFill,
            Image sleepinessFill,
            Image healthFill)
        {
            hungerGaugeFill = hungerFill;
            moodGaugeFill = moodFill;
            cleanlinessGaugeFill = cleanlinessFill;
            sleepinessGaugeFill = sleepinessFill;
            healthGaugeFill = healthFill;

            ConfigureStatGauge(hungerGaugeFill, HungerGaugeColor);
            ConfigureStatGauge(moodGaugeFill, MoodGaugeColor);
            ConfigureStatGauge(cleanlinessGaugeFill, CleanlinessGaugeColor);
            ConfigureStatGauge(sleepinessGaugeFill, SleepinessGaugeColor);
            ConfigureStatGauge(healthGaugeFill, HealthGaugeColor);

            if (current != null && current.stats != null)
            {
                RefreshStatGauges();
            }
        }

        public void ConfigureLevelProgressGauge(Image fill)
        {
            levelProgressGaugeFill = fill;
            ConfigureStatGauge(levelProgressGaugeFill, LevelProgressGaugeColor);
            RefreshLevelProgressVisualization();
        }

        private void OnEnable()
        {
            AccessibilityRuntime.SettingsChanged -= RefreshRecordPanelLayout;
            AccessibilityRuntime.SettingsChanged += RefreshRecordPanelLayout;
        }

        private void OnDisable()
        {
            AccessibilityRuntime.SettingsChanged -= RefreshRecordPanelLayout;
        }

        private void Update()
        {
            UpdateEventMessageFade();

            onboardingController ??= GetComponent<FirstMeetingOnboardingController>();
            returnSummaryController ??= GetComponent<ReturnSummaryController>();
            growthMilestoneController ??= GetComponent<GrowthMilestoneController>();
            milkDropMiniGameController ??= GetComponent<MilkDropMiniGameController>();
            cleaningMiniGameController ??= GetComponent<CleaningMiniGameController>();
            evolutionMilestoneController ??= GetComponent<EvolutionMilestoneController>();
            careEventCardController ??= GetComponent<CareEventCardController>();
            newGameSetupController ??= GetComponent<NewGameSetupController>();
            playChoicePanelController ??= GetComponent<PlayChoicePanelController>();
            cookingChoicePanelController ??= GetComponent<CookingChoicePanelController>();
            milkBlendingPanelController ??= GetComponent<MilkBlendingPanelController>();
            growthJourneyController ??= GetComponent<GrowthJourneyController>();
            bouncyJumpMiniGameController ??= GetComponent<BouncyJumpMiniGameController>();
            firstDayJourneyController ??= GetComponent<FirstDayJourneyController>();
            cheeseStarDeliveryBridge ??= GetComponent<CheeseStarDeliveryBridge>();
            memoryJournalPanelController ??= GetComponent<MemoryJournalPanelController>();
            fantasyPowderPanelController ??= GetComponent<FantasyPowderHiddenRecipePanelController>();
            profileMenuController ??= GetComponent<CheeseTamaProfileMenuController>();
            sleepSchedulePanelController ??= GetComponent<SleepSchedulePanelController>();
            if ((onboardingController != null && onboardingController.IsBlockingGameplay)
                || (returnSummaryController != null && returnSummaryController.IsBlockingGameplay)
                || (growthMilestoneController != null && growthMilestoneController.IsBlockingGameplay)
                || (milkDropMiniGameController != null && milkDropMiniGameController.IsBlockingGameplay)
                || (cleaningMiniGameController != null && cleaningMiniGameController.IsBlockingGameplay)
                || (evolutionMilestoneController != null && evolutionMilestoneController.IsBlockingGameplay)
                || (careEventCardController != null && careEventCardController.IsBlockingGameplay)
                || (newGameSetupController != null && newGameSetupController.IsBlockingGameplay)
                || (playChoicePanelController != null && playChoicePanelController.IsBlockingGameplay)
                || (cookingChoicePanelController != null && cookingChoicePanelController.IsBlockingGameplay)
                || (milkBlendingPanelController != null && milkBlendingPanelController.IsBlockingGameplay)
                || (growthJourneyController != null && growthJourneyController.IsBlockingGameplay)
                || (bouncyJumpMiniGameController != null && bouncyJumpMiniGameController.IsBlockingGameplay)
                || (firstDayJourneyController != null && firstDayJourneyController.IsBlockingGameplay)
                || (cheeseStarDeliveryBridge != null && cheeseStarDeliveryBridge.IsBlockingGameplay)
                || (memoryJournalPanelController != null && memoryJournalPanelController.IsBlockingGameplay)
                || (fantasyPowderPanelController != null && fantasyPowderPanelController.IsBlockingGameplay)
                || (profileMenuController != null && profileMenuController.IsBlockingGameplay)
                || (sleepSchedulePanelController != null && sleepSchedulePanelController.BlocksGameplayInput)
                || GameManager.Instance?.IsSleepScheduleActive == true)
            {
                return;
            }

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

            SetText(nameText, FormatTopBarName(current.name));
            RefreshLevelProgressVisualization();
            SetText(formText, FormatRecordLine("형태", FormatFormName(current.form)));
            SetText(conditionText, FormatRecordLine("상태", FormatCondition(currentSave, current)));
            RefreshStatGauges();
            SetText(affectionText, FormatRecordLine("애정", current.stats.affection.ToString()));
            SetText(maturationText, FormatRecordLine("성장", current.stats.maturation.ToString()));
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
            SetStatusExplanationText(messageText, message);
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

        private static void SetStatusExplanationText(Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = FormatStatusExplanation(value);
            var scrollRect = target.GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(target.rectTransform);
            Canvas.ForceUpdateCanvases();
            // The rendered generator includes explicit newlines and width-driven wrapping.
            var renderedLineCount = target.cachedTextGenerator != null
                ? target.cachedTextGenerator.lineCount
                : 0;
            target.alignment = renderedLineCount <= CenteredStatusExplanationLineLimit
                ? TextAnchor.MiddleLeft
                : TextAnchor.UpperLeft;
            LayoutRebuilder.ForceRebuildLayoutImmediate(target.rectTransform);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private static string FormatStatusExplanation(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var source = value.Trim();
            var builder = new System.Text.StringBuilder(source.Length);
            var pendingWhitespace = false;
            var sentenceBoundary = false;
            for (var index = 0; index < source.Length; index += 1)
            {
                var character = source[index];
                if (character == '\r')
                {
                    continue;
                }

                if (character == '\n')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
                    {
                        builder.Append('\n');
                    }

                    pendingWhitespace = false;
                    sentenceBoundary = false;
                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    pendingWhitespace = true;
                    continue;
                }

                if (pendingWhitespace && builder.Length > 0 && builder[builder.Length - 1] != '\n')
                {
                    builder.Append(sentenceBoundary ? '\n' : ' ');
                }

                pendingWhitespace = false;
                builder.Append(character);
                if (IsSentenceTerminator(character))
                {
                    sentenceBoundary = true;
                }
                else if (!IsSentenceCloser(character))
                {
                    sentenceBoundary = false;
                }
            }

            return builder.ToString().Trim();
        }

        private static bool IsSentenceTerminator(char character)
        {
            return character == '.'
                || character == '!'
                || character == '?'
                || character == '。'
                || character == '！'
                || character == '？'
                || character == '…';
        }

        private static bool IsSentenceCloser(char character)
        {
            return character == '\''
                || character == '"'
                || character == '’'
                || character == '”'
                || character == ')'
                || character == ']'
                || character == '}';
        }

        private static string FormatTopBarName(string value)
        {
            var safeValue = string.IsNullOrWhiteSpace(value) ? CheeseTamaModel.DefaultName : value.Trim();
            if (safeValue.Length <= TopBarNameMaxCharacters)
            {
                return safeValue;
            }

            return safeValue.Substring(0, TopBarNameMaxCharacters - 1) + "…";
        }

        private void RefreshStatGauges()
        {
            var stats = current.stats;
            ApplyStatGauge(
                hungerText,
                hungerGaugeFill,
                "배부름",
                stats.hunger,
                stats.hunger < HungerWarningThreshold,
                HungerGaugeColor);
            ApplyStatGauge(
                moodText,
                moodGaugeFill,
                "기분",
                stats.mood,
                stats.mood < MoodWarningThreshold,
                MoodGaugeColor);
            ApplyStatGauge(
                cleanlinessText,
                cleanlinessGaugeFill,
                "깨끗함",
                stats.cleanliness,
                stats.cleanliness < CleanlinessWarningThreshold,
                CleanlinessGaugeColor);
            ApplyStatGauge(
                sleepinessText,
                sleepinessGaugeFill,
                "졸림",
                stats.sleepiness,
                stats.sleepiness > SleepinessWarningThreshold,
                SleepinessGaugeColor);
            ApplyStatGauge(
                healthText,
                healthGaugeFill,
                "건강",
                stats.health,
                stats.health < HealthWarningThreshold,
                HealthGaugeColor);
        }

        private void RefreshLevelProgressVisualization()
        {
            var displayPercent = ResolveLevelDisplayPercent();
            if (current != null)
            {
                SetText(levelText, $"레벨 {current.level} · 성장 {displayPercent}%");
            }

            if (levelProgressGaugeFill != null)
            {
                levelProgressGaugeFill.fillAmount = displayPercent * 0.01f;
                levelProgressGaugeFill.color = LevelProgressGaugeColor;
            }
        }

        private int ResolveLevelDisplayPercent()
        {
            if (current == null)
            {
                return 0;
            }

            var lateLevelState = currentSave?.lateLevelGrowth;
            if (lateLevelState != null
                && lateLevelState.initialized
                && lateLevelState.trackedLevel == current.level
                && LateLevelGrowthCatalog.TryGetForCurrentLevel(current.level, out var requirement))
            {
                return LateLevelProgressMigration.GetDisplayPercent(
                    lateLevelState.progressUnits,
                    requirement.RequiredProgressUnits);
            }

            return Mathf.Clamp(current.levelProgress, 0, 100);
        }

        private static void ConfigureStatGauge(Image fill, Color normalColor)
        {
            if (fill == null)
            {
                return;
            }

            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.color = normalColor;
            fill.preserveAspect = false;
            fill.raycastTarget = false;
        }

        private static void ApplyStatGauge(
            Text label,
            Image fill,
            string statName,
            int rawValue,
            bool warning,
            Color normalColor)
        {
            var value = Mathf.Clamp(rawValue, 0, 100);
            if (label != null)
            {
                label.text = FormatStatLine(statName, value, warning);
                label.color = warning ? StatWarningTextColor : StatTextColor;
            }

            if (fill != null)
            {
                fill.fillAmount = value * 0.01f;
                fill.color = warning ? StatWarningGaugeColor : normalColor;
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
                false);
            var dailyY = careY - careHeight - RecordSectionGap;
            var dailyHeight = RefreshScrollableRecordSection(
                dailyRoutineText,
                dailyY,
                RecordDailyRoutineMinHeight,
                true);

            var requiredPanelHeight = -dailyY + dailyHeight + RecordPanelBottomPadding;
            panel.sizeDelta = new Vector2(
                RecordPanelWidth,
                Mathf.Ceil(Mathf.Max(RecordPanelMinHeight, requiredPanelHeight)));
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
            if (label == null)
            {
                return null;
            }

            var directParent = label.transform.parent as RectTransform;
            var current = directParent;
            for (var depth = 0; current != null && depth < 3; depth += 1)
            {
                if (current.name == RecordTextViewportName)
                {
                    return current.parent as RectTransform;
                }

                current = current.parent as RectTransform;
            }

            return directParent;
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
            var maxVisibleTextHeight = Mathf.Ceil(
                label.fontSize * Mathf.Max(1f, label.lineSpacing) * RecordScrollableLineLimit
                + RecordScrollGapAllowance);
            var visibleTextHeight = Mathf.Min(preferredTextHeight, maxVisibleTextHeight);
            var sectionHeight = fixedHeight
                ? Mathf.Ceil(minHeight)
                : Mathf.Ceil(Mathf.Max(minHeight, visibleTextHeight + RecordTextVerticalPadding));

            var verticalPadding = RecordTextVerticalPadding;
            var availableTextHeight = Mathf.Ceil(Mathf.Max(1f, sectionHeight - verticalPadding));
            var scrollable = preferredTextHeight > availableTextHeight + 1f;
            var contentHeight = scrollable
                ? Mathf.Ceil(Mathf.Max(availableTextHeight, preferredTextHeight))
                : availableTextHeight;
            var existingScrollRect = section.GetComponent<ScrollRect>();
            var preserveScrollPosition = scrollable
                && existingScrollRect != null
                && existingScrollRect.enabled
                && existingScrollRect.vertical;
            var previousScrollPosition = preserveScrollPosition
                ? existingScrollRect.verticalNormalizedPosition
                : 1f;

            ConfigureRecordSection(section, topY, sectionHeight);
            label.alignment = scrollable ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            var viewport = ConfigureRecordTextViewport(
                section,
                label.rectTransform,
                availableTextHeight,
                contentHeight,
                out var scrollContent);
            ConfigureTextRect(
                label,
                new Vector2(0f, -RecordTextContentInset),
                new Vector2(RecordTextWidth, contentHeight));
            var scrollRect = ConfigureRecordScroll(section, viewport, scrollContent, scrollable);
            if (preserveScrollPosition && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = previousScrollPosition;
            }

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
            label.lineSpacing = 1.04f;
            AccessibilityRuntime.ApplyCurrent(label);
        }

        private static void ConfigureFixedRecordText(Text label, int lineIndex)
        {
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleLeft;
            }

            var y = -RecordFixedTextTop - ((RecordFixedLineHeight + RecordFixedLineGap) * Mathf.Max(0, lineIndex));
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

        private static RectTransform ConfigureRecordTextViewport(
            RectTransform section,
            RectTransform label,
            float availableTextHeight,
            float contentTextHeight,
            out RectTransform scrollContent)
        {
            scrollContent = null;
            if (section == null || label == null)
            {
                return null;
            }

            var viewport = section.Find(RecordTextViewportName) as RectTransform;
            if (viewport == null)
            {
                var viewportObject = new GameObject(RecordTextViewportName, typeof(RectTransform), typeof(RectMask2D));
                viewportObject.transform.SetParent(section, false);
                viewport = viewportObject.GetComponent<RectTransform>();
            }
            else if (viewport.GetComponent<RectMask2D>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(0f, 1f);
            viewport.pivot = new Vector2(0f, 1f);
            viewport.anchoredPosition = new Vector2(RecordTextLeft, -RecordTextViewportInset);
            viewport.sizeDelta = new Vector2(
                RecordTextWidth,
                availableTextHeight + (RecordTextContentInset * 2f));

            scrollContent = viewport.Find(RecordTextContentName) as RectTransform;
            if (scrollContent == null)
            {
                var contentObject = new GameObject(RecordTextContentName, typeof(RectTransform));
                contentObject.transform.SetParent(viewport, false);
                scrollContent = contentObject.GetComponent<RectTransform>();
            }

            scrollContent.anchorMin = new Vector2(0f, 1f);
            scrollContent.anchorMax = new Vector2(0f, 1f);
            scrollContent.pivot = new Vector2(0f, 1f);
            scrollContent.anchoredPosition = Vector2.zero;
            scrollContent.sizeDelta = new Vector2(
                RecordTextWidth,
                contentTextHeight + (RecordTextContentInset * 2f));

            if (label.parent != scrollContent)
            {
                label.SetParent(scrollContent, false);
            }

            label.SetAsLastSibling();
            var legacySectionMask = section.GetComponent<RectMask2D>();
            if (legacySectionMask != null)
            {
                if (Application.isPlaying)
                {
                    legacySectionMask.enabled = false;
                }
                else
                {
                    DestroyImmediate(legacySectionMask);
                }
            }

            return viewport;
        }

        private static ScrollRect ConfigureRecordScroll(
            RectTransform section,
            RectTransform viewport,
            RectTransform content,
            bool scrollable)
        {
            if (section == null || viewport == null || content == null)
            {
                return null;
            }

            var scrollRect = section.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = section.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.viewport = viewport;
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

            return scrollRect;
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

        private static string FormatStatLine(string label, int value, bool warning)
        {
            var valueText = $"{label}  {Mathf.Clamp(value, 0, 100)}/100";
            return warning ? $"{valueText} · 주의!" : $"{valueText} · 좋아요";
        }

        private static string FormatFormName(string form)
        {
            var normalEvolution = EvolutionSystem.FindNormalEvolution(form);
            if (normalEvolution != null)
            {
                return normalEvolution.DisplayName;
            }

            if (form == "egg")
            {
                return "알";
            }

            if (form == "soft_cheesetama")
            {
                return "말랑 치즈타마";
            }

            return "알 수 없는 모습";
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
                return FormatRecordLine(milk.displayName, unlocked ? "레벨 0 / 0점" : $"아직 잠김 · {FormatUnlockRequirement(milk)}");
            }

            return FormatRecordLine(milk.displayName, unlocked
                ? $"레벨 {entry.growthLevel} / {entry.growthPoints}점"
                : $"아직 잠김 · {FormatUnlockRequirement(milk)}");
        }

        private static string FormatStarMilkGrowthLine(CheeseTamaSaveData saveData)
        {
            if (saveData == null || saveData.unlocks == null || !saveData.unlocks.starMilkUnlocked)
            {
                return FormatRecordLine("숨겨진 기록", "아직 발견되지 않음");
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
                : $"{requiredMilk.displayName} 레벨 {milk.requiredMilkLevel}";
        }

        private static string FormatUnlocks(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                return FormatRecordLine("다음 성장 목표", "저장 데이터 없음");
            }

            saveData.EnsureRuntimeDefaults();
            if (saveData.unlocks != null && saveData.unlocks.starMilkUnlocked)
            {
                return FormatRecordLine("별빛 목표", "별빛 알과 별빛 우유 열기");
            }

            return FormatRecordLine("다음 성장 목표", "새로운 성장 길은 조건 달성 후 발견");
        }

        private static string FormatCareSummary(CheeseTamaSaveData saveData)
        {
            var history = saveData?.careHistory;
            if (history == null)
            {
                return "<b>돌봄 누적</b>  0회" + RecordDetailVerticalGap + "쓰다듬기 0  놀이 0  청소 0  휴식 0";
            }

            return $"<b>돌봄 누적</b>  {history.totalCareActions}회{RecordDetailVerticalGap}쓰다듬기 {history.petSessions}  놀이 {history.playSessions}  청소 {history.cleanings}  휴식 {history.rests}";
        }

        private static string FormatDailyRoutine(CheeseTamaSaveData saveData)
        {
            var daily = saveData?.dailyCare;
            if (daily == null)
            {
                return "<b>오늘 루틴</b>" + RecordDetailVerticalGap + "먹기 0/3  요리 0/2\n놀이 0/3  청소 0/2  휴식 0/2\n<size=14>완료 보상  코인 20 · 우유방울 5 · 도감조각 1</size>";
            }

            var eatingCount = daily.milkFeeds + daily.snacksFed;
            return $"<b>오늘 루틴</b>{RecordDetailVerticalGap}먹기 {ClampGoal(eatingCount, DailyCareSaveData.EatingGoal)}/{DailyCareSaveData.EatingGoal}  요리 {ClampGoal(daily.cookings, DailyCareSaveData.CookingGoal)}/{DailyCareSaveData.CookingGoal}\n놀이 {ClampGoal(daily.playSessions, DailyCareSaveData.PlayGoal)}/{DailyCareSaveData.PlayGoal}  청소 {ClampGoal(daily.cleanings, DailyCareSaveData.CleanGoal)}/{DailyCareSaveData.CleanGoal}  휴식 {ClampGoal(daily.rests, DailyCareSaveData.RestGoal)}/{DailyCareSaveData.RestGoal}\n<size=14>완료 보상  코인 20 · 우유방울 5 · 도감조각 1</size>";
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
                return "지금 함께한 시간 00:00\n오늘 함께한 시간 00:00";
            }

            return $"지금 함께한 시간 {FormatDuration(session.currentSessionSeconds)}\n오늘 함께한 시간 {FormatDuration(session.todaySeconds)}";
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
            if (tama.growthHistory != null
                && tama.growthHistory.sameMilkFeedStreak >= FeedingStatusSystem.MilkAversionStreakThreshold)
            {
                tips.Add("같은 우유가 지겨운가 봐요.\n→ [우유주기]에서 다른 우유를 골라 주세요.");
            }

            if (tama.stats.overfullness > 0)
            {
                tips.Add($"너무 배부름 {tama.stats.overfullness}/100\n→ [놀아주기]를 하거나 잠깐 기다려 주세요.");
            }

            if (tama.stats.bodyChillIntensity > 0)
            {
                tips.Add($"몸 떨림 {tama.stats.bodyChillIntensity}/100 · 약 {tama.stats.bodyChillHoursRemaining}시간 남았어요.\n→ 따뜻한 우유를 주거나 [휴식하기]를 눌러 주세요.");
            }

            if (tama.stats.fermentedAftertasteIntensity > 0)
            {
                tips.Add($"발효 뒷맛 {tama.stats.fermentedAftertasteIntensity}/100 · 약 {tama.stats.fermentedAftertasteHoursRemaining}시간 남았어요.\n→ [청소하기]를 하거나 잠깐 기다려 주세요.");
            }

            if (tama.stats.sleepRhythmDisruptionIntensity > 0)
            {
                tips.Add($"잠자는 시간이 흐트러졌어요 ({tama.stats.sleepRhythmDisruptionIntensity}/100).\n→ 약 {tama.stats.sleepRhythmDisruptionHoursRemaining}시간 동안 [휴식하기]로 돌봐 주세요.");
            }

            if (tama.stats.heavinessIntensity > 0)
            {
                tips.Add($"몸이 무거워요 ({tama.stats.heavinessIntensity}/100) · 약 {tama.stats.heavinessHoursRemaining}시간 남았어요.\n→ [휴식하기]나 따뜻한 우유가 좋아요.");
            }

            if (tama.stats.sugarOverloadIntensity > 0)
            {
                tips.Add($"단것을 너무 많이 먹었어요 ({tama.stats.sugarOverloadIntensity}/100).\n→ 약 {tama.stats.sugarOverloadHoursRemaining}시간 동안 [청소하기]나 [휴식하기]로 돌봐 주세요.");
            }

            if (tama.stats.fantasyEchoIntensity > 0)
            {
                tips.Add($"환상 잔향 {tama.stats.fantasyEchoIntensity}/100 · 약 {tama.stats.fantasyEchoHoursRemaining}시간 남았어요.\n→ 따뜻한 우유나 [휴식하기]로 가라앉혀 주세요.");
            }

            if (tama.stats.lethargyIntensity > 0)
            {
                tips.Add($"나른함 {tama.stats.lethargyIntensity}/100 · 약 {tama.stats.lethargyHoursRemaining}시간 남았어요.\n→ [놀아주기]나 커피 향으로 깨워 주세요.");
            }

            if (tama.stats.stomachRiskIntensity > 0)
            {
                tips.Add($"배탈 위험 {tama.stats.stomachRiskIntensity}/100 · 약 {tama.stats.stomachRiskHoursRemaining}시간 남았어요.\n→ 따뜻한 우유나 [휴식하기]로 속을 달래 주세요.");
            }

            if (tama.stats.traitDistortionIntensity > 0)
            {
                tips.Add($"성격 흔들림 {tama.stats.traitDistortionIntensity}/100 · 약 {tama.stats.traitDistortionHoursRemaining}시간 남았어요.\n→ 평소 하던 돌봄과 [휴식하기]로 되돌려 주세요.");
            }

            if (tama.stats.health < HealthWarningThreshold)
            {
                tips.Add("건강이 낮아요.\n→ [휴식하기]와 [청소하기]를 먼저 해 주세요.");
            }

            if (tama.stats.hunger < HungerWarningThreshold)
            {
                tips.Add("배가 고파요.\n→ [우유주기] 또는 [간식주기]를 눌러 주세요.");
            }

            if (tama.stats.cleanliness < CleanlinessWarningThreshold)
            {
                tips.Add("몸과 방이 지저분해요.\n→ [청소하기]를 눌러 깨끗하게 해 주세요.");
            }

            if (tama.stats.sleepiness > SleepinessWarningThreshold)
            {
                tips.Add("많이 졸려 해요.\n→ [휴식하기]를 눌러 푹 쉬게 해 주세요.");
            }

            if (tama.stats.mood < MoodWarningThreshold)
            {
                tips.Add("기분이 좋지 않아요.\n→ [놀아주기]를 하거나 간식을 주세요.");
            }

            if (!tama.isHatched)
            {
                var hatchProgress = HatchingSystem.GetHatchProgressPercent(tama);
                tips.Add(hatchProgress >= 75
                    ? "부화가 가까워졌어요. 상태를 안정시켜 주세요."
                    : "우유주기는 부화 진행을 올리는 기본 돌봄이에요.");
                tips.Add("알 상태에서는 배부름과 건강을 골고루 챙겨 주세요.");
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
                tips.Add("밀크룸에서 5분 함께 있으면 우유방울을 받아요.");
            }

            if (tama.stats.hunger >= 70
                && tama.stats.mood >= 70
                && tama.stats.cleanliness >= 70
                && tama.stats.sleepiness <= 35
                && tama.stats.health >= 80)
            {
                tips.Add("모든 상태가 좋아요!\n→ 오늘 할 일을 천천히 이어 가세요.");
            }

            tips.Add("우유 종류를 바꾸면 성장 기록이 채워져요.");
            tips.Add("요리한 음식은 간식가방에서 먹일 수 있어요.");
            tips.Add("놀아주기는 기분을 빠르게 올려줘요.");
            tips.Add("청소하기는 건강 관리에도 도움이 돼요.");
            tips.Add("휴식하기는 졸림을 낮추고 건강을 지켜줘요.");
            tips.Add("오늘 루틴을 채우면 코인·우유방울·도감조각을 받아요.");
            tips.Add("새 기능을 여는 방법은 [여정]과 [도감]에서 볼 수 있어요.");
            tips.Add("배부름·기분·깨끗함·건강을 골고루 챙겨 주세요.");

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
                return "오늘 할 일 → 우유나 간식을 한 번 주세요.";
            }

            if (daily.cookings < DailyCareSaveData.CookingGoal)
            {
                return "오늘 할 일 → 음식을 한 번 요리해 주세요.";
            }

            if (daily.playSessions < DailyCareSaveData.PlayGoal)
            {
                return "오늘 할 일 → [놀아주기]를 눌러 주세요.";
            }

            if (daily.cleanings < DailyCareSaveData.CleanGoal)
            {
                return "오늘 할 일 → [청소하기]를 눌러 주세요.";
            }

            if (daily.rests < DailyCareSaveData.RestGoal)
            {
                return "오늘 할 일 → [휴식하기]를 눌러 주세요.";
            }

            return "오늘 할 일을 모두 마쳤어요!";
        }

        private static string FormatCondition(CheeseTamaSaveData saveData, CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return "알 수 없음";
            }

            var milkAverse = tama.growthHistory != null
                && tama.growthHistory.sameMilkFeedStreak >= FeedingStatusSystem.MilkAversionStreakThreshold;
            var overfull = tama.stats.overfullness > 0;
            var feedingConditions = new List<string>();
            if (milkAverse)
            {
                feedingConditions.Add("우유 질림");
            }

            if (overfull)
            {
                feedingConditions.Add($"너무 배부름 {tama.stats.overfullness}");
            }

            if (tama.stats.bodyChillIntensity > 0)
            {
                feedingConditions.Add($"몸 떨림 {tama.stats.bodyChillIntensity}");
            }

            if (tama.stats.fermentedAftertasteIntensity > 0)
            {
                feedingConditions.Add($"발효 뒷맛 {tama.stats.fermentedAftertasteIntensity}");
            }

            if (tama.stats.sleepRhythmDisruptionIntensity > 0)
            {
                feedingConditions.Add($"수면 리듬 {tama.stats.sleepRhythmDisruptionIntensity}");
            }

            if (tama.stats.heavinessIntensity > 0)
            {
                feedingConditions.Add($"무거움 {tama.stats.heavinessIntensity}");
            }

            if (tama.stats.sugarOverloadIntensity > 0)
            {
                feedingConditions.Add($"단것 과다 {tama.stats.sugarOverloadIntensity}");
            }

            if (tama.stats.fantasyEchoIntensity > 0)
            {
                feedingConditions.Add($"환상 잔향 {tama.stats.fantasyEchoIntensity}");
            }

            if (tama.stats.lethargyIntensity > 0)
            {
                feedingConditions.Add($"나른함 {tama.stats.lethargyIntensity}");
            }

            if (tama.stats.stomachRiskIntensity > 0)
            {
                feedingConditions.Add($"배탈 위험 {tama.stats.stomachRiskIntensity}");
            }

            if (tama.stats.traitDistortionIntensity > 0)
            {
                feedingConditions.Add($"성격 흔들림 {tama.stats.traitDistortionIntensity}");
            }

            if (feedingConditions.Count > 0)
            {
                return string.Join(" · ", feedingConditions);
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
