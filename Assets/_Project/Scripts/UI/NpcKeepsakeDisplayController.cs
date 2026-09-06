using System;
using CheeseTama.Core;
using CheeseTama.Gameplay.NpcVisits;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    /// <summary>
    /// Small Milkroom presentation adapter for the selected afterstory keepsake.
    /// </summary>
    public sealed class NpcKeepsakeDisplayController : MonoBehaviour
    {
        [SerializeField] private GameObject displayRoot;
        [SerializeField] private Image accentImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text ambientDialogueText;

        private Vector3 baseScale = Vector3.one;
        private GameObject baseScaleRoot;
        private bool baseScaleCaptured;
        private NpcKeepsakeDisplaySnapshot snapshot;
        private Func<NpcKeepsakeDisplaySnapshot> snapshotProvider;
        private GameManager eventSource;
        private bool eventsBound;

        public NpcKeepsakeDisplaySnapshot Snapshot => snapshot;
        public bool HasDisplay => snapshot.HasDisplay;
        public string AmbientDialogue => snapshot.AmbientDialogue;

        public void Configure(
            GameObject root,
            Image accent,
            Text titleLabel,
            Text ambientLabel,
            Func<NpcKeepsakeDisplaySnapshot> getSnapshot = null,
            GameManager manager = null)
        {
            UnbindEvents();
            if (!baseScaleCaptured || baseScaleRoot != root)
            {
                baseScale = root != null ? root.transform.localScale : Vector3.one;
                baseScaleRoot = root;
                baseScaleCaptured = true;
            }

            displayRoot = root;
            accentImage = accent;
            titleText = titleLabel;
            ambientDialogueText = ambientLabel;
            snapshotProvider = getSnapshot;
            eventSource = manager;
            BindEvents();
            Refresh();
        }

        private void OnEnable()
        {
            BindEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        public void Refresh()
        {
            Apply(snapshotProvider?.Invoke() ?? new NpcKeepsakeDisplaySnapshot(null));
        }

        public void Apply(NpcKeepsakeDisplaySnapshot value)
        {
            snapshot = value;
            if (displayRoot != null)
            {
                displayRoot.SetActive(value.HasDisplay);
            }

            if (!value.HasDisplay)
            {
                if (displayRoot != null)
                {
                    displayRoot.transform.localScale = baseScale;
                }

                if (titleText != null)
                {
                    titleText.text = string.Empty;
                }

                if (ambientDialogueText != null)
                {
                    ambientDialogueText.text = string.Empty;
                }

                return;
            }

            var profile = value.Profile;
            if (displayRoot != null)
            {
                displayRoot.transform.localScale = baseScale * profile.DisplayScale;
            }

            if (accentImage != null
                && ColorUtility.TryParseHtmlString(profile.AccentHex, out var color))
            {
                accentImage.color = color;
            }

            if (titleText != null)
            {
                titleText.text = profile.Title;
                AccessibilityRuntime.ApplyCurrent(titleText);
            }

            if (ambientDialogueText != null)
            {
                ambientDialogueText.text = profile.AmbientDialogue;
                AccessibilityRuntime.ApplyCurrent(ambientDialogueText);
            }
        }

        private void BindEvents()
        {
            if (eventSource == null || eventsBound)
            {
                return;
            }

            eventSource.SaveDataReplaced += Refresh;
            eventSource.JourneyHubChanged += Refresh;
            eventSource.NpcKeepsakeDisplayChanged += Apply;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (eventSource == null || !eventsBound)
            {
                eventsBound = false;
                return;
            }

            eventSource.SaveDataReplaced -= Refresh;
            eventSource.JourneyHubChanged -= Refresh;
            eventSource.NpcKeepsakeDisplayChanged -= Apply;
            eventsBound = false;
        }
    }
}
