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
        [SerializeField] private Text milkGrowthText;
        [SerializeField] private Text unlockText;
        [SerializeField] private Text lastSavedText;
        [SerializeField] private Text messageText;

        private CheeseTamaModel current;
        private CheeseTamaSaveData currentSave;

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
            Text milkGrowthLabel,
            Text unlockLabel,
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
            milkGrowthText = milkGrowthLabel;
            unlockText = unlockLabel;
            lastSavedText = lastSavedLabel;
            messageText = messageLabel;
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
            SetText(levelText, $"Lv. {current.level} ({current.levelProgress}%)");
            SetText(formText, $"Form: {current.form}");
            SetText(conditionText, $"Condition: {FormatCondition(current)}");
            SetText(hungerText, $"Hunger: {current.stats.hunger}");
            SetText(moodText, $"Mood: {current.stats.mood}");
            SetText(cleanlinessText, $"Cleanliness: {current.stats.cleanliness}");
            SetText(sleepinessText, $"Sleepiness: {current.stats.sleepiness}");
            SetText(healthText, $"Health: {current.stats.health}");
            SetText(affectionText, $"Affection: {current.stats.affection}");
            SetText(maturationText, $"Maturation: {current.stats.maturation}");
            SetText(hatchProgressText, FormatHatchProgress(current));
            SetText(milkGrowthText, FormatMilkGrowth(currentSave, current));
            SetText(unlockText, FormatUnlocks(currentSave));
            SetText(lastSavedText, $"Last Saved: {FormatIso(current.lastSavedAtIso)}");
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
                return "Never";
            }

            return iso.Length > 19 ? iso.Substring(0, 19).Replace('T', ' ') : iso;
        }

        private static string FormatHatchProgress(CheeseTamaModel tama)
        {
            if (tama.isHatched)
            {
                return "Hatch: awake";
            }

            return $"Hatch: {HatchingSystem.GetHatchProgressPercent(tama)}%";
        }

        private static string FormatMilkGrowth(CheeseTamaSaveData saveData, CheeseTamaModel tama)
        {
            var milkId = tama?.growthHistory?.lastFedMilkId;
            if (string.IsNullOrWhiteSpace(milkId))
            {
                milkId = "basic_milk";
            }

            var entry = FindMilkGrowthEntry(saveData, milkId);
            if (entry == null)
            {
                return "Milk Growth: Basic Milk Lv. 0";
            }

            return $"Milk Growth: {FormatMilkName(entry.milkId)} Lv. {entry.growthLevel} ({entry.growthPoints} pts)";
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

        private static string FormatMilkName(string milkId)
        {
            if (milkId == "basic_milk")
            {
                return "Basic Milk";
            }

            if (milkId == "star_milk")
            {
                return "Star Milk";
            }

            return string.IsNullOrWhiteSpace(milkId) ? "Milk" : milkId;
        }

        private static string FormatUnlocks(CheeseTamaSaveData saveData)
        {
            var starMilkState = saveData != null && saveData.unlocks != null && saveData.unlocks.starMilkUnlocked
                ? "Star Milk unlocked"
                : "Star Milk locked";
            return $"Unlocks: {starMilkState}";
        }

        private static string FormatCondition(CheeseTamaModel tama)
        {
            if (tama == null || tama.stats == null)
            {
                return "unknown";
            }

            if (tama.stats.health < 35)
            {
                return "unwell";
            }

            if (tama.stats.hunger < 25)
            {
                return "hungry";
            }

            if (tama.stats.cleanliness < 35)
            {
                return "messy";
            }

            if (tama.stats.sleepiness > 75)
            {
                return "sleepy";
            }

            if (tama.stats.mood > 80)
            {
                return "cheerful";
            }

            return tama.isHatched ? "curious" : "warm";
        }
    }
}
