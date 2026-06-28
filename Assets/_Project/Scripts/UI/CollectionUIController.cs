using CheeseTama.Collections;
using CheeseTama.Data;
using CheeseTama.Save;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public sealed class CollectionUIController : MonoBehaviour
    {
        [SerializeField] private Text milkText;
        [SerializeField] private Text evolutionText;
        [SerializeField] private Text eventText;
        [SerializeField] private Text hiddenText;
        [SerializeField] private Text messageText;

        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();

        public void Configure(
            Text milkLabel,
            Text evolutionLabel,
            Text eventLabel,
            Text hiddenLabel,
            Text messageLabel)
        {
            milkText = milkLabel;
            evolutionText = evolutionLabel;
            eventText = eventLabel;
            hiddenText = hiddenLabel;
            messageText = messageLabel;
        }

        public void Bind(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                SetText(milkText, "Milk Records: 0");
                SetText(evolutionText, "Evolution Records: 0");
                SetText(eventText, "Event Records: 0");
                SetText(hiddenText, "Hidden Records: 0");
                SetText(messageText, "No collection data loaded.");
                return;
            }

            saveData.EnsureRuntimeDefaults();
            SetText(milkText, FormatRecordList("Milk Records", saveData.collections.milk, FormatKnownRecordName));
            SetText(evolutionText, FormatRecordList("Evolution Records", saveData.collections.evolution, FormatKnownRecordName));
            SetText(eventText, FormatRecordList("Event Records", saveData.collections.events, FormatKnownRecordName));
            SetText(hiddenText, FormatHiddenRecordList(saveData.collections.hiddenUnlockedOnly));
            SetText(messageText, "Feed milk and hatch CheeseTama to add records here.");
        }

        public HiddenCollectionDefinition[] GetVisibleHiddenCards(
            HiddenCollectionDefinition[] definitions,
            CollectionSaveData collections)
        {
            var visible = hiddenCollectionSystem.GetVisibleUnlockedCards(definitions, collections);
            var result = new HiddenCollectionDefinition[visible.Count];
            for (var i = 0; i < visible.Count; i++)
            {
                result[i] = visible[i];
            }

            return result;
        }

        private static string FormatRecordList(
            string title,
            System.Collections.Generic.List<string> records,
            System.Func<string, string> formatter)
        {
            if (records == null || records.Count == 0)
            {
                return $"{title}: 0\n- none yet";
            }

            var labels = new string[records.Count];
            for (var i = 0; i < records.Count; i++)
            {
                labels[i] = formatter != null ? formatter(records[i]) : records[i];
            }

            return $"{title}: {records.Count}\n- {string.Join("\n- ", labels)}";
        }

        private static string FormatHiddenRecordList(System.Collections.Generic.List<HiddenCollectionSaveEntry> records)
        {
            if (records == null || records.Count == 0)
            {
                return "Hidden Records: 0\n- none yet";
            }

            var labels = new string[records.Count];
            for (var i = 0; i < records.Count; i++)
            {
                var entry = records[i];
                if (entry == null)
                {
                    labels[i] = "unknown";
                    continue;
                }

                labels[i] = $"{FormatHiddenRecordName(entry.id)} ({FormatIso(entry.acquiredAtIso)})";
            }

            return $"Hidden Records: {records.Count}\n- {string.Join("\n- ", labels)}";
        }

        private static string FormatKnownRecordName(string id)
        {
            if (id == "basic_milk")
            {
                return "Basic Milk";
            }

            if (id == "star_milk")
            {
                return "Star Milk";
            }

            if (id == "star_milk_unlocked")
            {
                return "Star Milk unlocked";
            }

            if (id == "cheese_snack_fed")
            {
                return "Cheese Snack tasted";
            }

            if (id == "crumbly_snack")
            {
                return "Crumbly snack";
            }

            if (id == "quiet_hum")
            {
                return "Quiet milkroom hum";
            }

            if (id == "small_fever")
            {
                return "Small fever";
            }

            if (id == "hungry_peep")
            {
                return "Hungry peep";
            }

            if (id == "dusty_corner")
            {
                return "Dusty corner";
            }

            if (id == "sleepy_yawn")
            {
                return "Sleepy yawn";
            }

            if (id == "happy_wiggle")
            {
                return "Happy wiggle";
            }

            if (id == "soft_cheesetama")
            {
                return "Soft CheeseTama";
            }

            const string BasicMilkGrowthPrefix = "basic_milk_growth_lv_";
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(BasicMilkGrowthPrefix))
            {
                return $"Basic Milk reached Lv. {id.Substring(BasicMilkGrowthPrefix.Length)}";
            }

            const string StarMilkGrowthPrefix = "star_milk_growth_lv_";
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(StarMilkGrowthPrefix))
            {
                return $"Star Milk reached Lv. {id.Substring(StarMilkGrowthPrefix.Length)}";
            }

            return string.IsNullOrWhiteSpace(id) ? "unknown" : id;
        }

        private static string FormatHiddenRecordName(string id)
        {
            if (id == "first_soft_hatch")
            {
                return "First Soft Hatch";
            }

            if (id == "star_milk_keeper")
            {
                return "Star Milk Keeper";
            }

            if (id == "milkroom_listener")
            {
                return "Milkroom Listener";
            }

            if (id == "first_snack_bite")
            {
                return "First Snack Bite";
            }

            if (id == "warm_balance")
            {
                return "Warm Balance";
            }

            return string.IsNullOrWhiteSpace(id) ? "unknown" : id;
        }

        private static string FormatIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
            {
                return "unknown";
            }

            return iso.Length > 10 ? iso.Substring(0, 10) : iso;
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
