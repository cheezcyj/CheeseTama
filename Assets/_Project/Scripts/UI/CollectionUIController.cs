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
        [SerializeField] private Button milkTabButton;
        [SerializeField] private Button evolutionTabButton;
        [SerializeField] private Button eventTabButton;
        [SerializeField] private Button hiddenTabButton;

        private readonly HiddenCollectionSystem hiddenCollectionSystem = new HiddenCollectionSystem();
        private CollectionRecordTab activeTab = CollectionRecordTab.Milk;
        private bool tabsEnabled;

        public void Configure(
            Text milkLabel,
            Text evolutionLabel,
            Text eventLabel,
            Text hiddenLabel,
            Text messageLabel)
        {
            Configure(milkLabel, evolutionLabel, eventLabel, hiddenLabel, messageLabel, null, null, null, null);
        }

        public void Configure(
            Text milkLabel,
            Text evolutionLabel,
            Text eventLabel,
            Text hiddenLabel,
            Text messageLabel,
            Button milkTab,
            Button evolutionTab,
            Button eventTab,
            Button hiddenTab)
        {
            milkText = milkLabel;
            evolutionText = evolutionLabel;
            eventText = eventLabel;
            hiddenText = hiddenLabel;
            messageText = messageLabel;
            milkTabButton = milkTab;
            evolutionTabButton = evolutionTab;
            eventTabButton = eventTab;
            hiddenTabButton = hiddenTab;
            tabsEnabled = milkTabButton != null
                && evolutionTabButton != null
                && eventTabButton != null
                && hiddenTabButton != null;

            ConfigureTabButtons();
            ShowTab(activeTab);
        }

        public void Bind(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                SetText(milkText, "우유 기록: 0");
                SetText(evolutionText, "진화 기록: 0");
                SetText(eventText, "이벤트 기록: 0");
                SetText(hiddenText, "특별 기록: 0\n- 아직 없음");
                SetText(messageText, "도감 데이터를 불러오지 못했습니다.");
                ShowTab(activeTab);
                return;
            }

            saveData.EnsureRuntimeDefaults();
            SetText(milkText, FormatRecordList("우유 기록", saveData.collections.milk, FormatKnownRecordName));
            SetText(evolutionText, FormatRecordList("진화 기록", saveData.collections.evolution, FormatKnownRecordName));
            SetText(eventText, FormatRecordList("이벤트 기록", saveData.collections.events, FormatKnownRecordName));
            SetText(hiddenText, FormatHiddenRecordList(saveData.collections.hiddenUnlockedOnly));
            SetText(messageText, "우유를 먹이고 부화시키면 이곳에 기록됩니다.");
            ShowTab(activeTab);
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
                return $"{title}: 0\n- 아직 없음";
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
                return "특별 기록: 0\n- 아직 없음";
            }

            var labels = new string[records.Count];
            for (var i = 0; i < records.Count; i++)
            {
                var entry = records[i];
                if (entry == null)
                {
                    labels[i] = "알 수 없음";
                    continue;
                }

                labels[i] = $"{FormatHiddenRecordName(entry.id)} ({FormatIso(entry.acquiredAtIso)})";
            }

            return $"특별 기록: {records.Count}\n- {string.Join("\n- ", labels)}";
        }

        private static string FormatKnownRecordName(string id)
        {
            if (id == "basic_milk")
            {
                return "기본 우유";
            }

            if (id == "star_milk")
            {
                return "별빛 우유";
            }

            if (id == "star_milk_unlocked")
            {
                return "별빛 우유 해금";
            }

            if (id == "cheese_snack_fed")
            {
                return "치즈 간식 맛봄";
            }

            if (id == "crumbly_snack")
            {
                return "부스러지는 간식";
            }

            if (id == "care_total_5")
            {
                return "돌봄 5회";
            }

            if (id == "care_total_15")
            {
                return "돌봄 15회";
            }

            if (id == "milk_feeds_5")
            {
                return "우유 5회";
            }

            if (id == "star_milk_feeds_3")
            {
                return "별빛 우유 3회";
            }

            if (id == "snacks_fed_3")
            {
                return "간식 3회";
            }

            if (id == "play_sessions_3")
            {
                return "놀이 3회";
            }

            if (id == "cleanings_3")
            {
                return "청소 3회";
            }

            if (id == "rests_3")
            {
                return "휴식 3회";
            }

            if (id == "wait_hours_3")
            {
                return "3시간 경과";
            }

            if (id == "daily_routine_complete")
            {
                return "일일 루틴 완료";
            }

            if (id == "daily_routine_3")
            {
                return "일일 루틴 3회";
            }

            if (id == "session_5m")
            {
                return "5분 체류";
            }

            if (id == "session_10m")
            {
                return "10분 체류";
            }

            if (id == "session_20m")
            {
                return "20분 체류";
            }

            if (id == "session_30m")
            {
                return "30분 체류";
            }

            if (id == "daily_presence_10m")
            {
                return "오늘 밀크룸 10분";
            }

            if (id == "daily_presence_30m")
            {
                return "오늘 밀크룸 30분";
            }

            if (id == "milk_drop_catch")
            {
                return "우유 방울 획득";
            }

            if (id == "milk_drop_catch_5")
            {
                return "우유 방울 5회 획득";
            }

            if (id == "milk_drop_catch_10")
            {
                return "우유 방울 10회 획득";
            }

            if (id == "quiet_hum")
            {
                return "조용한 밀크룸 울림";
            }

            if (id == "small_fever")
            {
                return "작은 열기";
            }

            if (id == "hungry_peep")
            {
                return "배고픈 소리";
            }

            if (id == "dusty_corner")
            {
                return "먼지 낀 구석";
            }

            if (id == "sleepy_yawn")
            {
                return "졸린 하품";
            }

            if (id == "happy_wiggle")
            {
                return "기쁜 흔들림";
            }

            if (id == "soft_cheesetama")
            {
                return "말랑 치즈타마";
            }

            const string BasicMilkGrowthPrefix = "basic_milk_growth_lv_";
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(BasicMilkGrowthPrefix))
            {
                return $"기본 우유 레벨 {id.Substring(BasicMilkGrowthPrefix.Length)} 달성";
            }

            const string StarMilkGrowthPrefix = "star_milk_growth_lv_";
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith(StarMilkGrowthPrefix))
            {
                return $"별빛 우유 레벨 {id.Substring(StarMilkGrowthPrefix.Length)} 달성";
            }

            return string.IsNullOrWhiteSpace(id) ? "알 수 없음" : id;
        }

        private static string FormatHiddenRecordName(string id)
        {
            if (id == "first_soft_hatch")
            {
                return "첫 말랑 부화";
            }

            if (id == "star_milk_keeper")
            {
                return "별빛 우유 지킴이";
            }

            if (id == "milkroom_listener")
            {
                return "밀크룸 청취자";
            }

            if (id == "first_snack_bite")
            {
                return "첫 간식 한입";
            }

            if (id == "gentle_caretaker")
            {
                return "다정한 돌봄이";
            }

            if (id == "tidy_keeper")
            {
                return "깔끔한 관리인";
            }

            if (id == "playful_friend")
            {
                return "장난스러운 친구";
            }

            if (id == "warm_balance")
            {
                return "따뜻한 균형";
            }

            if (id == "daily_regular")
            {
                return "꾸준한 일과";
            }

            if (id == "patient_guest")
            {
                return "느긋한 밀크룸 손님";
            }

            if (id == "drop_listener")
            {
                return "방울 청취자";
            }

            return string.IsNullOrWhiteSpace(id) ? "알 수 없음" : id;
        }

        private static string FormatIso(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso))
            {
                return "알 수 없음";
            }

            return iso.Length > 10 ? iso.Substring(0, 10) : iso;
        }

        private void ConfigureTabButtons()
        {
            ConfigureTabButton(milkTabButton, CollectionRecordTab.Milk);
            ConfigureTabButton(evolutionTabButton, CollectionRecordTab.Evolution);
            ConfigureTabButton(eventTabButton, CollectionRecordTab.Event);
            ConfigureTabButton(hiddenTabButton, CollectionRecordTab.Hidden);
        }

        private void ConfigureTabButton(Button button, CollectionRecordTab tab)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowTab(tab));
        }

        private void ShowTab(CollectionRecordTab tab)
        {
            activeTab = tab;
            if (!tabsEnabled)
            {
                SetTextVisible(milkText, true);
                SetTextVisible(evolutionText, true);
                SetTextVisible(eventText, true);
                SetTextVisible(hiddenText, true);
                return;
            }

            SetTextVisible(milkText, tab == CollectionRecordTab.Milk);
            SetTextVisible(evolutionText, tab == CollectionRecordTab.Evolution);
            SetTextVisible(eventText, tab == CollectionRecordTab.Event);
            SetTextVisible(hiddenText, tab == CollectionRecordTab.Hidden);
            UpdateTabVisuals();
        }

        private void UpdateTabVisuals()
        {
            UpdateTabVisual(milkTabButton, activeTab == CollectionRecordTab.Milk);
            UpdateTabVisual(evolutionTabButton, activeTab == CollectionRecordTab.Evolution);
            UpdateTabVisual(eventTabButton, activeTab == CollectionRecordTab.Event);
            UpdateTabVisual(hiddenTabButton, activeTab == CollectionRecordTab.Hidden);
        }

        private static void UpdateTabVisual(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.TryGetComponent(out Image image))
            {
                image.color = selected
                    ? new Color(1f, 0.74f, 0.24f, 1f)
                    : new Color(1f, 0.9f, 0.62f, 0.88f);
            }

            var colors = button.colors;
            colors.normalColor = selected
                ? new Color(1f, 0.74f, 0.24f, 1f)
                : new Color(1f, 0.9f, 0.62f, 0.88f);
            colors.highlightedColor = new Color(1f, 0.84f, 0.36f, 1f);
            colors.pressedColor = new Color(0.88f, 0.53f, 0.13f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            var labelTransform = button.transform.Find("Label");
            if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
            {
                label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                label.color = new Color(0.26f, 0.16f, 0.08f);
            }
        }

        private static void SetTextVisible(Text target, bool visible)
        {
            if (target != null)
            {
                target.gameObject.SetActive(visible);
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private enum CollectionRecordTab
        {
            Milk,
            Evolution,
            Event,
            Hidden
        }
    }
}
