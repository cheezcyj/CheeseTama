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
            SetText(milkText, FormatRecordList("Milk Records", saveData.collections.milk));
            SetText(evolutionText, FormatRecordList("Evolution Records", saveData.collections.evolution));
            SetText(eventText, FormatRecordList("Event Records", saveData.collections.events));
            SetText(hiddenText, $"Hidden Records: {saveData.collections.hiddenUnlockedOnly.Count}");
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

        private static string FormatRecordList(string title, System.Collections.Generic.List<string> records)
        {
            if (records == null || records.Count == 0)
            {
                return $"{title}: 0\n- none yet";
            }

            return $"{title}: {records.Count}\n- {string.Join("\n- ", records)}";
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
