using CheeseTama.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class MilkroomUIController : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text moodText;
        [SerializeField] private Text cleanlinessText;
        [SerializeField] private Text sleepinessText;
        [SerializeField] private Text healthText;

        private CheeseTamaModel current;

        public void Configure(
            Text nameLabel,
            Text levelLabel,
            Text hungerLabel,
            Text moodLabel,
            Text cleanlinessLabel,
            Text sleepinessLabel,
            Text healthLabel)
        {
            nameText = nameLabel;
            levelText = levelLabel;
            hungerText = hungerLabel;
            moodText = moodLabel;
            cleanlinessText = cleanlinessLabel;
            sleepinessText = sleepinessLabel;
            healthText = healthLabel;
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
            SetText(levelText, $"Lv. {current.level}");
            SetText(hungerText, $"Hunger: {current.stats.hunger}");
            SetText(moodText, $"Mood: {current.stats.mood}");
            SetText(cleanlinessText, $"Cleanliness: {current.stats.cleanliness}");
            SetText(sleepinessText, $"Sleepiness: {current.stats.sleepiness}");
            SetText(healthText, $"Health: {current.stats.health}");
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
