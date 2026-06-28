using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Growth;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkroomUIController : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text formText;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text moodText;
        [SerializeField] private Text cleanlinessText;
        [SerializeField] private Text sleepinessText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text affectionText;
        [SerializeField] private Text maturationText;
        [SerializeField] private Text hatchProgressText;
        [SerializeField] private Text lastSavedText;
        [SerializeField] private Text messageText;

        private CheeseTamaModel current;

        public void Configure(
            Text nameLabel,
            Text levelLabel,
            Text formLabel,
            Text hungerLabel,
            Text moodLabel,
            Text cleanlinessLabel,
            Text sleepinessLabel,
            Text healthLabel,
            Text affectionLabel,
            Text maturationLabel,
            Text hatchProgressLabel,
            Text lastSavedLabel,
            Text messageLabel)
        {
            nameText = nameLabel;
            levelText = levelLabel;
            formText = formLabel;
            hungerText = hungerLabel;
            moodText = moodLabel;
            cleanlinessText = cleanlinessLabel;
            sleepinessText = sleepinessLabel;
            healthText = healthLabel;
            affectionText = affectionLabel;
            maturationText = maturationLabel;
            hatchProgressText = hatchProgressLabel;
            lastSavedText = lastSavedLabel;
            messageText = messageLabel;
        }

        public void Bind(CheeseTamaModel tama)
        {
            current = tama;
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
            SetText(hungerText, $"Hunger: {current.stats.hunger}");
            SetText(moodText, $"Mood: {current.stats.mood}");
            SetText(cleanlinessText, $"Cleanliness: {current.stats.cleanliness}");
            SetText(sleepinessText, $"Sleepiness: {current.stats.sleepiness}");
            SetText(healthText, $"Health: {current.stats.health}");
            SetText(affectionText, $"Affection: {current.stats.affection}");
            SetText(maturationText, $"Maturation: {current.stats.maturation}");
            SetText(hatchProgressText, FormatHatchProgress(current));
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
    }
}
