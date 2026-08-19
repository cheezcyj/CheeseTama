using System.Collections.Generic;
using System.Text;
using CheeseTama.Collections.HiddenCareers;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Independent hidden-card view. With zero unlocked cards it hides its entry
    /// point and panel, so it never creates placeholder slots or reveals a total.
    /// </summary>
    public sealed class HiddenCareerCardPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text cardListText;
        [SerializeField] private Button entryButton;
        [SerializeField] private Button closeButton;

        private readonly List<HiddenCareerCardViewData> visibleCards =
            new List<HiddenCareerCardViewData>();

        public bool HasVisibleCards => visibleCards.Count > 0;
        public int VisibleCardCount => visibleCards.Count;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public string RenderedText => cardListText != null ? cardListText.text : string.Empty;

        public void Configure(
            GameObject root,
            Text titleLabel,
            Text cardsLabel,
            Button openButton,
            Button panelCloseButton)
        {
            UnbindButtons();
            panelRoot = root;
            titleText = titleLabel;
            cardListText = cardsLabel;
            entryButton = openButton;
            closeButton = panelCloseButton;
            BindButtons();
            Render();
            Close();
        }

        public void Bind(IReadOnlyList<HiddenCareerCardViewData> cards)
        {
            visibleCards.Clear();
            if (cards != null)
            {
                for (var index = 0; index < cards.Count; index += 1)
                {
                    var card = cards[index];
                    if (card != null)
                    {
                        visibleCards.Add(card);
                    }
                }
            }

            Render();
            if (!HasVisibleCards)
            {
                Close();
            }
        }

        public bool Open()
        {
            if (!HasVisibleCards || panelRoot == null)
            {
                return false;
            }

            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            Render();
            return true;
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public static string FormatVisibleCards(IReadOnlyList<HiddenCareerCardViewData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var appended = 0;
            for (var index = 0; index < cards.Count; index += 1)
            {
                var card = cards[index];
                if (card == null)
                {
                    continue;
                }

                if (appended > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("────────────");
                    builder.AppendLine();
                }

                builder.Append(card.DisplayName);
                builder.Append("  ·  ");
                builder.AppendLine(HiddenCareerCardCatalog.GetRarityLabel(card.Rarity));
                builder.Append('“');
                builder.Append(card.Quote);
                builder.AppendLine("”");
                builder.AppendLine(card.DeepText);
                if (!string.IsNullOrWhiteSpace(card.EffectDescription))
                {
                    builder.Append("효과 · ");
                    builder.AppendLine(card.EffectDescription);
                }
                builder.Append("획득  ");
                builder.Append(card.AcquiredDateText);
                appended += 1;
            }

            return builder.ToString();
        }

        private void Awake()
        {
            BindButtons();
            Render();
            Close();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void BindButtons()
        {
            entryButton?.onClick.RemoveListener(HandleOpenClicked);
            entryButton?.onClick.AddListener(HandleOpenClicked);
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
        }

        private void UnbindButtons()
        {
            entryButton?.onClick.RemoveListener(HandleOpenClicked);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void HandleOpenClicked()
        {
            Open();
        }

        private void Render()
        {
            if (entryButton != null)
            {
                entryButton.gameObject.SetActive(HasVisibleCards);
            }

            if (titleText != null)
            {
                titleText.text = HasVisibleCards ? "발견한 특별 기록" : string.Empty;
            }

            if (cardListText != null)
            {
                cardListText.text = FormatVisibleCards(visibleCards);
            }
        }
    }
}
