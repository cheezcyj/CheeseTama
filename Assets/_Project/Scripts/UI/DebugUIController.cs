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
                SetText(stateText, "Debug State\nNo CheeseTama save data loaded.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Debug State");
            builder.AppendLine($"Name: {current.name}");
            builder.AppendLine($"Form: {FormatFormName(current.form)}");
            builder.AppendLine($"Lv: {current.level} ({current.levelProgress}%)");
            builder.AppendLine($"Condition: {FormatCondition(current)}");
            builder.AppendLine($"Hunger: {current.stats.hunger}");
            builder.AppendLine($"Mood: {current.stats.mood}");
            builder.AppendLine($"Cleanliness: {current.stats.cleanliness}");
            builder.AppendLine($"Sleepiness: {current.stats.sleepiness}");
            builder.AppendLine($"Health: {current.stats.health}");
            builder.AppendLine($"Affection: {current.stats.affection}");
            builder.AppendLine(FormatMilkGrowthLine("basic_milk", "Basic Milk"));
            builder.AppendLine(FormatStarMilkLine());
            builder.AppendLine(FormatUnlocks());
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
                return $"{displayName}: Lv. 0 (0 pts)";
            }

            return $"{displayName}: Lv. {entry.growthLevel} ({entry.growthPoints} pts)";
        }

        private string FormatStarMilkLine()
        {
            if (currentSave == null || currentSave.unlocks == null || !currentSave.unlocks.starMilkUnlocked)
            {
                return "Star Milk: locked";
            }

            return FormatMilkGrowthLine("star_milk", "Star Milk");
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
                ? "Star Milk unlocked"
                : "Star Milk locked";
            return $"Unlocks: {starMilkState}";
        }

        private static string FormatFormName(string form)
        {
            if (form == "egg")
            {
                return "Egg";
            }

            if (form == "soft_cheesetama")
            {
                return "Soft CheeseTama";
            }

            return string.IsNullOrWhiteSpace(form) ? "Unknown" : form;
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

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
