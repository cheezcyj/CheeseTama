using System.Collections.Generic;
using CheeseTama.Audio;
using CheeseTama.Data;
using CheeseTama.Environment;
using CheeseTama.Gameplay.Autonomy;
using CheeseTama.Gameplay.Growth;
using CheeseTama.Gameplay.Input;
using CheeseTama.Gameplay.Decorations;
using CheeseTama.Gameplay.Milk;
using CheeseTama.Gameplay.MiniGames;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Records;
using CheeseTama.Gameplay.Reset;
using CheeseTama.Gameplay.Story;
using CheeseTama.Gameplay.Snacks;
using CheeseTama.Platform;
using CheeseTama.Platform.Accounts;
using CheeseTama.Save;
using CheeseTama.UI;
using CheeseTama.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTama.Core
{
    public static partial class StarterSceneBuilder
    {
        private static void EnsurePostgameResearchPanel(
            Transform canvasTransform,
            Transform journeyCard)
        {
            if (canvasTransform == null || journeyCard == null)
            {
                return;
            }

            var open = GetOrCreateTopLeftButton(
                journeyCard,
                "Postgame Research Open Button",
                "성장 연구",
                new Vector2(432f, -24f),
                new Vector2(204f, 52f));
            ApplyCareButtonStyle(open);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Postgame Research Overlay",
                new Color(0.045f, 0.04f, 0.08f, 0.86f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Postgame Research Card",
                Vector2.zero,
                new Vector2(820f, 620f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(820f, 620f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.98f, 0.95f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Postgame Research Title Text",
                "반복 성장 연구",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -32f),
                new Vector2(724f, 52f));
            title.fontStyle = FontStyle.Bold;
            var body = GetOrCreateText(
                card.transform,
                "Postgame Research Body Text",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                new Vector2(48f, -112f),
                new Vector2(724f, 326f));
            body.supportRichText = true;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            var status = GetOrCreateText(
                card.transform,
                "Postgame Research Status Text",
                string.Empty,
                17,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -454f),
                new Vector2(724f, 44f));
            status.color = new Color(0.48f, 0.25f, 0.63f);
            status.fontStyle = FontStyle.Bold;

            var previous = GetOrCreateTopLeftButton(
                card.transform,
                "Postgame Research Previous Button",
                "이전",
                new Vector2(48f, -526f),
                new Vector2(132f, 52f));
            var next = GetOrCreateTopLeftButton(
                card.transform,
                "Postgame Research Next Button",
                "다음",
                new Vector2(194f, -526f),
                new Vector2(132f, 52f));
            var unlock = GetOrCreateTopLeftButton(
                card.transform,
                "Postgame Research Unlock Button",
                "연구 열기",
                new Vector2(460f, -526f),
                new Vector2(168f, 52f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Postgame Research Close Button",
                "닫기",
                new Vector2(642f, -526f),
                new Vector2(130f, 52f));
            ApplyCareButtonStyle(previous);
            ApplyCareButtonStyle(next);
            ApplyCareButtonStyle(unlock);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<PostgameResearchPanelController>()
                ?? canvasTransform.gameObject.AddComponent<PostgameResearchPanelController>();
            controller.Configure(
                overlay,
                open,
                title,
                body,
                status,
                previous,
                next,
                unlock,
                close,
                () => GameManager.Instance?.GetPostgameResearchSnapshot(),
                nodeId => GameManager.Instance?.TryUnlockPostgameResearchNode(nodeId),
                CreateControlBlockingCallback(canvasTransform),
                GameManager.Instance,
                profileId => GameManager.Instance?.TrySelectPostgameResearchProfile(profileId) == true);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureMilkroomMysteryPanel(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var open = GetOrMoveUtilityButton(
                canvasTransform,
                null,
                "Open Milkroom Mystery Button",
                "비밀서랍",
                new Vector2(224f, 0f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                MilkroomMysteryPanelController.OverlayObjectName,
                new Color(0.04f, 0.03f, 0.06f, 0.86f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Milkroom Mystery Card",
                Vector2.zero,
                new Vector2(820f, 600f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(820f, 600f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.98f, 0.94f, 0.84f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Milkroom Mystery Title Text",
                string.Empty,
                30,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -34f),
                new Vector2(724f, 52f));
            title.fontStyle = FontStyle.Bold;
            var description = GetOrCreateText(
                card.transform,
                "Milkroom Mystery Description Text",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                new Vector2(48f, -112f),
                new Vector2(724f, 250f));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Overflow;
            var firstChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Milkroom Mystery First Choice Button",
                "첫 번째 선택",
                new Vector2(48f, -402f),
                new Vector2(350f, 72f));
            var secondChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Milkroom Mystery Second Choice Button",
                "두 번째 선택",
                new Vector2(422f, -402f),
                new Vector2(350f, 72f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Milkroom Mystery Close Button",
                "닫기",
                new Vector2(622f, -510f),
                new Vector2(150f, 52f));
            ApplyCareButtonStyle(firstChoice);
            ApplyCareButtonStyle(secondChoice);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<MilkroomMysteryPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MilkroomMysteryPanelController>();
            controller.Configure(
                overlay,
                title,
                description,
                firstChoice,
                firstChoice.GetComponentInChildren<Text>(true),
                secondChoice,
                secondChoice.GetComponentInChildren<Text>(true),
                close,
                () => GameManager.Instance?.GetMilkroomMysterySnapshot()
                    ?? MilkroomMysterySnapshot.CreateHidden(),
                (chapterId, choiceId) => GameManager.Instance?.TryChooseMilkroomMystery(
                    chapterId,
                    choiceId),
                CreateControlBlockingCallback(canvasTransform));
            open.onClick.RemoveAllListeners();
            open.onClick.AddListener(() => controller.Open());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureDreamStorySeasonPanel(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var open = GetOrMoveUtilityButton(
                canvasTransform,
                null,
                "Open Dream Story Season Button",
                "꿈이야기",
                new Vector2(336f, 0f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                DreamStorySeasonPanelController.OverlayObjectName,
                new Color(0.025f, 0.035f, 0.075f, 0.9f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Dream Story Season Card",
                Vector2.zero,
                new Vector2(900f, 660f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(900f, 660f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.92f, 0.94f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "Dream Story Season Title Text",
                string.Empty,
                30,
                TextAnchor.MiddleLeft,
                new Vector2(52f, -34f),
                new Vector2(796f, 54f));
            title.fontStyle = FontStyle.Bold;
            var body = GetOrCreateText(
                card.transform,
                "Dream Story Season Body Text",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                new Vector2(52f, -112f),
                new Vector2(796f, 300f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            var firstChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Dream Story Season First Choice Button",
                "첫 번째 선택",
                new Vector2(52f, -430f),
                new Vector2(382f, 76f));
            var secondChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Dream Story Season Second Choice Button",
                "두 번째 선택",
                new Vector2(466f, -430f),
                new Vector2(382f, 76f));
            var previousRecall = GetOrCreateTopLeftButton(
                card.transform,
                "Dream Story Season Previous Recall Button",
                "이전 회상",
                new Vector2(52f, -552f),
                new Vector2(142f, 52f));
            var nextRecall = GetOrCreateTopLeftButton(
                card.transform,
                "Dream Story Season Next Recall Button",
                "다음 회상",
                new Vector2(210f, -552f),
                new Vector2(142f, 52f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Dream Story Season Close Button",
                "닫기",
                new Vector2(698f, -552f),
                new Vector2(150f, 52f));
            ApplyCareButtonStyle(firstChoice);
            ApplyCareButtonStyle(secondChoice);
            ApplyCareButtonStyle(previousRecall);
            ApplyCareButtonStyle(nextRecall);
            ApplyCareButtonStyle(close);

            var controller = canvasTransform.GetComponent<DreamStorySeasonPanelController>()
                ?? canvasTransform.gameObject.AddComponent<DreamStorySeasonPanelController>();
            controller.Configure(
                overlay,
                title,
                body,
                firstChoice,
                firstChoice.GetComponentInChildren<Text>(true),
                secondChoice,
                secondChoice.GetComponentInChildren<Text>(true),
                close,
                () => GameManager.Instance?.GetDreamStoryPendingSnapshot()
                    ?? DreamStorySnapshot.CreateHidden(),
                episodeId => GameManager.Instance?.GetDreamStoryRecallSnapshot(episodeId)
                    ?? DreamStorySnapshot.CreateHidden(),
                (episodeId, choiceId) => GameManager.Instance?.TryChooseDreamStory(
                    episodeId,
                    choiceId),
                CreateControlBlockingCallback(canvasTransform),
                () => GameManager.Instance?.GetDreamStoryRecallIndex()
                    ?? System.Array.Empty<DreamStoryRecallEntry>(),
                previousRecall,
                nextRecall);
            open.onClick.RemoveAllListeners();
            open.onClick.AddListener(() => controller.Open());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureMilkroomInvestigation(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            const string modeName = "Milkroom Investigation Mode";
            var open = GetOrMoveUtilityButton(
                canvasTransform,
                null,
                "Open Milkroom Investigation Button",
                "방 조사",
                new Vector2(336f, -48f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var modeRoot = GetOrCreateFullScreenOverlay(
                canvasTransform,
                modeName,
                new Color(0.015f, 0.025f, 0.045f, 0.34f));
            var cancel = GetOrCreateTopLeftButton(
                modeRoot.transform,
                "Milkroom Investigation Cancel Button",
                "조사 끝내기",
                new Vector2(1710f, -44f),
                new Vector2(160f, 48f));
            ConfigureTopRightRect(cancel.GetComponent<RectTransform>(), 44f, 44f, 160f, 48f);
            var windowHotspot = GetOrCreateTopLeftButton(
                modeRoot.transform,
                "Milkroom Investigation Window Hotspot",
                "창문 흔적",
                new Vector2(284f, -180f),
                new Vector2(190f, 64f));
            var shelfHotspot = GetOrCreateTopLeftButton(
                modeRoot.transform,
                "Milkroom Investigation Shelf Hotspot",
                "선반 흔적",
                new Vector2(1360f, -280f),
                new Vector2(190f, 64f));
            var drawerHotspot = GetOrCreateTopLeftButton(
                modeRoot.transform,
                "Milkroom Investigation Drawer Hotspot",
                "서랍 흔적",
                new Vector2(850f, -472f),
                new Vector2(190f, 64f));
            ApplyCareButtonStyle(cancel);
            ApplyCareButtonStyle(windowHotspot);
            ApplyCareButtonStyle(shelfHotspot);
            ApplyCareButtonStyle(drawerHotspot);

            var panelRoot = GetOrCreateBottomPanel(
                modeRoot.transform,
                MilkroomInvestigationPanelController.PanelObjectName,
                new Vector2(0f, 24f),
                new Vector2(1020f, 346f));
            if (panelRoot.TryGetComponent(out Image panelImage))
            {
                panelImage.color = new Color(0.94f, 0.97f, 1f, 0.98f);
                panelImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                panelRoot.transform,
                "Milkroom Investigation Title Text",
                "밀크룸 조사",
                27,
                TextAnchor.MiddleLeft,
                new Vector2(34f, -22f),
                new Vector2(700f, 46f));
            title.fontStyle = FontStyle.Bold;
            var progress = GetOrCreateText(
                panelRoot.transform,
                "Milkroom Investigation Progress Text",
                string.Empty,
                17,
                TextAnchor.MiddleRight,
                new Vector2(744f, -24f),
                new Vector2(240f, 42f));
            var body = GetOrCreateText(
                panelRoot.transform,
                "Milkroom Investigation Body Text",
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                new Vector2(34f, -82f),
                new Vector2(952f, 112f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            var status = GetOrCreateText(
                panelRoot.transform,
                "Milkroom Investigation Status Text",
                string.Empty,
                16,
                TextAnchor.MiddleLeft,
                new Vector2(34f, -202f),
                new Vector2(952f, 38f));
            status.color = new Color(0.3f, 0.36f, 0.62f);
            var firstChoice = GetOrCreateTopLeftButton(
                panelRoot.transform,
                "Milkroom Investigation First Choice Button",
                "첫 번째 선택",
                new Vector2(34f, -252f),
                new Vector2(390f, 62f));
            var secondChoice = GetOrCreateTopLeftButton(
                panelRoot.transform,
                "Milkroom Investigation Second Choice Button",
                "두 번째 선택",
                new Vector2(442f, -252f),
                new Vector2(390f, 62f));
            var close = GetOrCreateTopLeftButton(
                panelRoot.transform,
                "Milkroom Investigation Close Button",
                "닫기",
                new Vector2(850f, -252f),
                new Vector2(136f, 62f));
            ApplyCareButtonStyle(firstChoice);
            ApplyCareButtonStyle(secondChoice);
            ApplyCareButtonStyle(close);

            var panelController = canvasTransform.GetComponent<MilkroomInvestigationPanelController>()
                ?? canvasTransform.gameObject.AddComponent<MilkroomInvestigationPanelController>();
            var inputController = canvasTransform.GetComponent<MilkroomInvestigationInputController>()
                ?? canvasTransform.gameObject.AddComponent<MilkroomInvestigationInputController>();
            panelController.Configure(
                panelRoot,
                title,
                body,
                progress,
                status,
                firstChoice,
                firstChoice.GetComponentInChildren<Text>(true),
                secondChoice,
                secondChoice.GetComponentInChildren<Text>(true),
                close,
                (episodeId, choiceId) => GameManager.Instance?.TryChooseMilkroomInvestigation(
                    episodeId,
                    choiceId),
                inputController.CancelMode);

            float ResolveZoomScale()
            {
                var navigation = Camera.main != null
                    ? Camera.main.GetComponent<MilkroomViewportNavigationController>()
                    : null;
                return navigation != null ? navigation.CurrentZoomScale : 1f;
            }

            inputController.Configure(
                modeRoot,
                open,
                cancel,
                windowHotspot,
                windowHotspot.GetComponentInChildren<Text>(true),
                shelfHotspot,
                shelfHotspot.GetComponentInChildren<Text>(true),
                drawerHotspot,
                drawerHotspot.GetComponentInChildren<Text>(true),
                panelController,
                () => GameManager.Instance?.GetMilkroomInvestigationSnapshot(ResolveZoomScale())
                    ?? MilkroomInvestigationSnapshot.Hidden,
                hotspotId => GameManager.Instance?.TryInspectMilkroomHotspot(
                    hotspotId,
                    ResolveZoomScale()),
                () => IsMilkroomPropInteractionBlockedExcept(canvasTransform, modeName),
                CreateControlBlockingCallback(canvasTransform));

            var availabilityBridge = canvasTransform.GetComponent<MilkroomInvestigationAvailabilityBridge>()
                ?? canvasTransform.gameObject.AddComponent<MilkroomInvestigationAvailabilityBridge>();
            availabilityBridge.Configure(inputController, GameManager.Instance);
            modeRoot.transform.SetAsLastSibling();
        }

        private static void EnsureNpcAfterstoryPanel(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var open = GetOrMoveUtilityButton(
                canvasTransform,
                null,
                "Open Npc Afterstory Button",
                "후일담",
                new Vector2(224f, -48f),
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Npc Afterstory Overlay",
                new Color(0.035f, 0.045f, 0.055f, 0.86f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Npc Afterstory Card",
                Vector2.zero,
                new Vector2(1120f, 760f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1120f, 760f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.95f, 0.98f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var header = GetOrCreateText(
                card.transform,
                "Npc Afterstory Header Text",
                "친구들의 다음 이야기",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(48f, -24f),
                new Vector2(720f, 52f));
            header.fontStyle = FontStyle.Bold;
            var previousNpc = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Afterstory Previous Npc Button",
                "이전 친구",
                new Vector2(792f, -24f),
                new Vector2(132f, 48f));
            var nextNpc = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Afterstory Next Npc Button",
                "다음 친구",
                new Vector2(940f, -24f),
                new Vector2(132f, 48f));

            var storyPanel = GetOrCreatePanel(
                card.transform,
                "Npc Afterstory Story Panel",
                new Vector2(48f, -96f),
                new Vector2(650f, 472f));
            var story = GetOrCreateText(
                storyPanel.transform,
                "Npc Afterstory Story Text",
                string.Empty,
                19,
                TextAnchor.UpperLeft,
                new Vector2(24f, -20f),
                new Vector2(602f, 300f));
            story.supportRichText = true;
            story.horizontalOverflow = HorizontalWrapMode.Wrap;
            story.verticalOverflow = VerticalWrapMode.Overflow;
            var choiceA = GetOrCreateTopLeftButton(
                storyPanel.transform,
                "Npc Afterstory Choice A Button",
                "첫 번째 선택",
                new Vector2(24f, -342f),
                new Vector2(288f, 84f));
            var choiceB = GetOrCreateTopLeftButton(
                storyPanel.transform,
                "Npc Afterstory Choice B Button",
                "두 번째 선택",
                new Vector2(338f, -342f),
                new Vector2(288f, 84f));

            var weeklyPanel = GetOrCreatePanel(
                card.transform,
                "Npc Afterstory Weekly Panel",
                new Vector2(722f, -96f),
                new Vector2(350f, 230f));
            var weekly = GetOrCreateText(
                weeklyPanel.transform,
                "Npc Afterstory Weekly Text",
                string.Empty,
                17,
                TextAnchor.UpperLeft,
                new Vector2(22f, -18f),
                new Vector2(306f, 136f));
            weekly.supportRichText = true;
            weekly.horizontalOverflow = HorizontalWrapMode.Wrap;
            var claimWeekly = GetOrCreateTopLeftButton(
                weeklyPanel.transform,
                "Npc Afterstory Weekly Claim Button",
                "주간 보상 받기",
                new Vector2(82f, -166f),
                new Vector2(186f, 46f));

            var keepsakePanel = GetOrCreatePanel(
                card.transform,
                "Npc Afterstory Keepsake Panel",
                new Vector2(722f, -350f),
                new Vector2(350f, 218f));
            var keepsake = GetOrCreateText(
                keepsakePanel.transform,
                "Npc Afterstory Keepsake Text",
                string.Empty,
                17,
                TextAnchor.UpperLeft,
                new Vector2(22f, -18f),
                new Vector2(306f, 92f));
            keepsake.supportRichText = true;
            var previousKeepsake = GetOrCreateTopLeftButton(
                keepsakePanel.transform,
                "Npc Afterstory Previous Keepsake Button",
                "이전",
                new Vector2(22f, -124f),
                new Vector2(84f, 44f));
            var nextKeepsake = GetOrCreateTopLeftButton(
                keepsakePanel.transform,
                "Npc Afterstory Next Keepsake Button",
                "다음",
                new Vector2(118f, -124f),
                new Vector2(84f, 44f));
            var displayKeepsake = GetOrCreateTopLeftButton(
                keepsakePanel.transform,
                "Npc Afterstory Display Keepsake Button",
                "진열",
                new Vector2(214f, -124f),
                new Vector2(114f, 44f));
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Afterstory Close Button",
                "닫기",
                new Vector2(922f, -650f),
                new Vector2(150f, 52f));
            foreach (var button in new[]
                     {
                         previousNpc, nextNpc, choiceA, choiceB, claimWeekly,
                         previousKeepsake, nextKeepsake, displayKeepsake, close
                     })
            {
                ApplyCareButtonStyle(button);
            }

            var displayRoot = GetOrCreatePanel(
                canvasTransform,
                "Npc Keepsake Display",
                new Vector2(410f, -716f),
                new Vector2(420f, 112f));
            var displayAccent = displayRoot.GetComponent<Image>();
            if (displayAccent != null)
            {
                displayAccent.raycastTarget = false;
            }

            var displayTitle = GetOrCreateText(
                displayRoot.transform,
                "Npc Keepsake Display Title Text",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                new Vector2(18f, -12f),
                new Vector2(384f, 32f));
            displayTitle.fontStyle = FontStyle.Bold;
            var displayAmbient = GetOrCreateText(
                displayRoot.transform,
                "Npc Keepsake Display Ambient Text",
                string.Empty,
                15,
                TextAnchor.UpperLeft,
                new Vector2(18f, -48f),
                new Vector2(384f, 50f));
            displayAmbient.horizontalOverflow = HorizontalWrapMode.Wrap;
            displayAmbient.verticalOverflow = VerticalWrapMode.Truncate;
            var displayController = canvasTransform.GetComponent<NpcKeepsakeDisplayController>()
                ?? canvasTransform.gameObject.AddComponent<NpcKeepsakeDisplayController>();
            displayController.Configure(
                displayRoot,
                displayAccent,
                displayTitle,
                displayAmbient,
                () => GameManager.Instance?.GetNpcKeepsakeDisplaySnapshot() ?? default,
                GameManager.Instance);

            var controller = canvasTransform.GetComponent<NpcAfterstoryPanelController>()
                ?? canvasTransform.gameObject.AddComponent<NpcAfterstoryPanelController>();
            controller.Configure(
                overlay,
                open,
                close,
                previousNpc,
                nextNpc,
                choiceA,
                choiceB,
                claimWeekly,
                previousKeepsake,
                nextKeepsake,
                displayKeepsake,
                header,
                story,
                choiceA.GetComponentInChildren<Text>(true),
                choiceB.GetComponentInChildren<Text>(true),
                weekly,
                keepsake,
                () => GameManager.Instance?.GetNpcAfterstoryPanelSnapshot()
                    ?? NpcAfterstoryPanelSnapshot.Empty,
                (afterstoryId, choiceId) =>
                {
                    GameManager.Instance?.TryApplyNpcAfterstoryChoice(afterstoryId, choiceId);
                    displayController.Apply(
                        GameManager.Instance?.GetNpcKeepsakeDisplaySnapshot() ?? default);
                },
                npcId => GameManager.Instance?.TryClaimNpcWeeklyQuest(npcId),
                keepsakeId =>
                {
                    GameManager.Instance?.TrySelectNpcKeepsake(keepsakeId);
                    displayController.Apply(
                        GameManager.Instance?.GetNpcKeepsakeDisplaySnapshot() ?? default);
                },
                null,
                CreateControlBlockingCallback(canvasTransform),
                GameManager.Instance);
            displayController.Apply(
                GameManager.Instance?.GetNpcKeepsakeDisplaySnapshot() ?? default);
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureFirstDayJourney(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                FirstDayJourneyController.OverlayObjectName,
                new Color(0.08f, 0.06f, 0.03f, 0.78f));
            var card = GetOrCreatePanel(
                overlay.transform,
                FirstDayJourneyController.CardObjectName,
                Vector2.zero,
                new Vector2(720f, 660f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(720f, 660f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.86f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(
                card.transform,
                "First Day Journey Title Text",
                "첫날 여정",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -30f),
                new Vector2(624f, 52f));
            title.fontStyle = FontStyle.Bold;
            var progress = GetOrCreateText(
                card.transform,
                "First Day Journey Progress Text",
                "첫날 여정  0/6",
                19,
                TextAnchor.MiddleCenter,
                new Vector2(48f, -84f),
                new Vector2(624f, 36f));
            progress.color = new Color(0.67f, 0.36f, 0.08f);

            var bodyScrollView = GetOrCreatePanel(
                card.transform,
                FirstDayJourneyController.BodyScrollViewObjectName,
                new Vector2(70f, -132f),
                new Vector2(580f, 382f));
            var bodyScrollViewImage = bodyScrollView.GetComponent<Image>();
            bodyScrollViewImage.color = Color.clear;
            bodyScrollViewImage.raycastTarget = false;

            var bodyViewport = GetOrCreateRect(
                bodyScrollView.transform,
                FirstDayJourneyController.BodyViewportObjectName);
            bodyViewport.anchorMin = Vector2.zero;
            bodyViewport.anchorMax = Vector2.one;
            bodyViewport.pivot = new Vector2(0.5f, 0.5f);
            bodyViewport.offsetMin = Vector2.zero;
            bodyViewport.offsetMax = new Vector2(-18f, 0f);
            var bodyViewportImage = bodyViewport.GetComponent<Image>()
                ?? bodyViewport.gameObject.AddComponent<Image>();
            bodyViewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            bodyViewportImage.raycastTarget = true;
            var bodyViewportMask = bodyViewport.GetComponent<RectMask2D>()
                ?? bodyViewport.gameObject.AddComponent<RectMask2D>();
            bodyViewportMask.padding = Vector4.zero;

            var bodyContent = GetOrCreateRect(
                bodyViewport,
                FirstDayJourneyController.BodyContentObjectName);
            bodyContent.anchorMin = new Vector2(0f, 1f);
            bodyContent.anchorMax = new Vector2(1f, 1f);
            bodyContent.pivot = new Vector2(0.5f, 1f);
            bodyContent.anchoredPosition = Vector2.zero;
            bodyContent.sizeDelta = new Vector2(0f, 378f);
            var bodyLayout = bodyContent.GetComponent<VerticalLayoutGroup>()
                ?? bodyContent.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(12, 12, 4, 4);
            bodyLayout.spacing = 8f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;
            var bodyFitter = bodyContent.GetComponent<ContentSizeFitter>()
                ?? bodyContent.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (var index = 0; index < Gameplay.Journey.FirstDayJourneySystem.Tasks.Count; index += 1)
            {
                var legacyTask = card.transform.Find($"First Day Journey Task Text {index}");
                if (legacyTask != null)
                {
                    legacyTask.SetParent(bodyContent, false);
                }
            }

            var legacyStatus = card.transform.Find("First Day Journey Status Text");
            if (legacyStatus != null)
            {
                legacyStatus.SetParent(bodyContent, false);
            }

            var taskTexts = new Text[Gameplay.Journey.FirstDayJourneySystem.Tasks.Count];
            for (var index = 0; index < taskTexts.Length; index += 1)
            {
                taskTexts[index] = GetOrCreateText(
                    bodyContent,
                    $"First Day Journey Task Text {index}",
                    $"○ {Gameplay.Journey.FirstDayJourneySystem.Tasks[index].DisplayName}",
                    20,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    new Vector2(0f, 44f));
                taskTexts[index].horizontalOverflow = HorizontalWrapMode.Wrap;
                taskTexts[index].verticalOverflow = VerticalWrapMode.Overflow;
                taskTexts[index].resizeTextForBestFit = false;
                var taskLayout = taskTexts[index].GetComponent<LayoutElement>()
                    ?? taskTexts[index].gameObject.AddComponent<LayoutElement>();
                taskLayout.minHeight = 44f;
                taskLayout.preferredHeight = -1f;
                taskLayout.flexibleHeight = 0f;
            }

            var status = GetOrCreateText(
                bodyContent,
                "First Day Journey Status Text",
                "정해진 순서 없이 천천히 경험해도 괜찮아요.",
                17,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(0f, 58f));
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.verticalOverflow = VerticalWrapMode.Overflow;
            status.resizeTextForBestFit = false;
            var statusLayout = status.GetComponent<LayoutElement>()
                ?? status.gameObject.AddComponent<LayoutElement>();
            statusLayout.minHeight = 58f;
            statusLayout.preferredHeight = -1f;
            statusLayout.flexibleHeight = 0f;

            var bodyScrollbarRect = GetOrCreateRect(
                bodyScrollView.transform,
                "First Day Journey Body Vertical Scrollbar");
            bodyScrollbarRect.anchorMin = new Vector2(1f, 0f);
            bodyScrollbarRect.anchorMax = new Vector2(1f, 1f);
            bodyScrollbarRect.pivot = new Vector2(1f, 0.5f);
            bodyScrollbarRect.offsetMin = new Vector2(-12f, 4f);
            bodyScrollbarRect.offsetMax = new Vector2(-4f, -4f);
            var bodyScrollbarImage = bodyScrollbarRect.GetComponent<Image>()
                ?? bodyScrollbarRect.gameObject.AddComponent<Image>();
            bodyScrollbarImage.color = new Color(0.56f, 0.36f, 0.12f, 0.20f);
            ApplyRoundedImage(bodyScrollbarImage);

            var bodySlidingArea = GetOrCreateRect(bodyScrollbarRect, "Sliding Area");
            bodySlidingArea.anchorMin = Vector2.zero;
            bodySlidingArea.anchorMax = Vector2.one;
            bodySlidingArea.offsetMin = new Vector2(2f, 2f);
            bodySlidingArea.offsetMax = new Vector2(-2f, -2f);
            var bodyHandleRect = GetOrCreateRect(bodySlidingArea, "Handle");
            bodyHandleRect.anchorMin = Vector2.zero;
            bodyHandleRect.anchorMax = Vector2.one;
            bodyHandleRect.offsetMin = Vector2.zero;
            bodyHandleRect.offsetMax = Vector2.zero;
            var bodyHandleImage = bodyHandleRect.GetComponent<Image>()
                ?? bodyHandleRect.gameObject.AddComponent<Image>();
            bodyHandleImage.color = new Color(0.94f, 0.54f, 0.12f, 0.94f);
            ApplyRoundedImage(bodyHandleImage);

            var bodyScrollbar = bodyScrollbarRect.GetComponent<Scrollbar>()
                ?? bodyScrollbarRect.gameObject.AddComponent<Scrollbar>();
            bodyScrollbar.direction = Scrollbar.Direction.BottomToTop;
            bodyScrollbar.handleRect = bodyHandleRect;
            bodyScrollbar.targetGraphic = bodyHandleImage;

            var bodyScrollRect = bodyScrollView.GetComponent<ScrollRect>()
                ?? bodyScrollView.AddComponent<ScrollRect>();
            bodyScrollRect.viewport = bodyViewport;
            bodyScrollRect.content = bodyContent;
            bodyScrollRect.horizontal = false;
            bodyScrollRect.vertical = true;
            bodyScrollRect.movementType = ScrollRect.MovementType.Clamped;
            bodyScrollRect.inertia = true;
            bodyScrollRect.decelerationRate = 0.12f;
            bodyScrollRect.scrollSensitivity = 34f;
            bodyScrollRect.verticalScrollbar = bodyScrollbar;
            bodyScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            bodyScrollRect.verticalScrollbarSpacing = 4f;
            bodyScrollRect.verticalNormalizedPosition = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);

            var close = GetOrCreateTopLeftButton(
                card.transform,
                "First Day Journey Close Button",
                "확인",
                new Vector2(275f, -590f),
                new Vector2(170f, 52f));
            var claim = GetOrCreateTopLeftButton(
                card.transform,
                "First Day Journey Claim Button",
                "첫날 선물 받기",
                new Vector2(275f, -532f),
                new Vector2(170f, 52f));
            ApplyCareButtonStyle(close);
            ApplyCareButtonStyle(claim);

            var profileEntries = GetProfileMenuEntryParent(canvasTransform);
            var open = GetOrMoveUtilityButton(
                canvasTransform,
                profileEntries,
                "Open First Day Journey Button",
                "첫날선물",
                Vector2.zero,
                new Vector2(104f, 40f));
            ApplyCareButtonStyle(open);

            var manager = Application.isPlaying ? GameManager.Instance : null;
            var controller = canvasTransform.GetComponent<FirstDayJourneyController>();
            if (controller == null)
            {
                controller = canvasTransform.gameObject.AddComponent<FirstDayJourneyController>();
            }

            controller.Configure(
                overlay,
                open,
                progress,
                status,
                taskTexts,
                claim,
                close,
                () => GameManager.Instance?.CurrentSave?.firstDayJourney,
                () => GameManager.Instance?.MarkFirstDayJourneyShown(),
                () => GameManager.Instance != null
                    ? GameManager.Instance.ClaimFirstDayJourneyReward()
                    : default,
                manager,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureEvolutionMilestone(
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                "Evolution Achievement Overlay",
                new Color(0.11f, 0.05f, 0.16f, 0.78f));
            var card = GetOrCreatePanel(overlay.transform, "Evolution Achievement Card", Vector2.zero, new Vector2(760f, 530f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(760f, 530f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(0.98f, 0.92f, 1f, 1f);
                cardImage.raycastTarget = true;
            }

            var title = GetOrCreateText(card.transform, "Evolution Achievement Title Text", "새로운 진화!", 34,
                TextAnchor.MiddleCenter, new Vector2(54f, -42f), new Vector2(652f, 58f));
            title.fontStyle = FontStyle.Bold;
            var emblem = GetOrCreateText(card.transform, "Evolution Achievement Emblem Text", "✦", 88,
                TextAnchor.MiddleCenter, new Vector2(250f, -112f), new Vector2(260f, 130f));
            emblem.color = new Color(0.66f, 0.4f, 0.85f, 1f);
            var level = GetOrCreateText(card.transform, "Evolution Achievement Level Text", "레벨 21 · 새로운 모습이 되었어요", 21,
                TextAnchor.MiddleCenter, new Vector2(64f, -258f), new Vector2(632f, 38f));
            level.fontStyle = FontStyle.Bold;
            var description = GetOrCreateText(card.transform, "Evolution Achievement Description Text", "돌봄의 추억이 새로운 모습으로 이어졌어요.", 19,
                TextAnchor.MiddleCenter, new Vector2(70f, -314f), new Vector2(620f, 118f));
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 14;
            description.resizeTextMaxSize = 19;
            var confirm = GetOrCreateTopLeftButton(card.transform, "Evolution Achievement Confirm Button", "새 모습 만나기",
                new Vector2(514f, -450f), new Vector2(192f, 54f));
            ApplyCareButtonStyle(confirm);

            var controller = canvasTransform.GetComponent<EvolutionMilestoneController>()
                ?? canvasTransform.gameObject.AddComponent<EvolutionMilestoneController>();
            controller.Configure(
                overlay,
                title,
                level,
                description,
                confirm,
                milkroomUi,
                visualController,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>());
            overlay.transform.SetAsLastSibling();
        }

        private static void EnsureNpcVisitCard(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                NpcVisitCardController.OverlayObjectName,
                new Color(0.055f, 0.04f, 0.025f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Npc Visit Card",
                Vector2.zero,
                new Vector2(720f, 590f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(720f, 590f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.965f, 0.82f, 1f);
                cardImage.raycastTarget = true;
            }

            var portrait = GetOrCreatePanel(
                card.transform,
                "Npc Portrait",
                new Vector2(48f, -42f),
                new Vector2(118f, 118f));
            if (portrait.TryGetComponent(out Image portraitImage))
            {
                portraitImage.color = new Color(1f, 0.79f, 0.34f, 1f);
                ApplyCircleImage(portraitImage);
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
            }

            var portraitText = GetOrCreateText(
                portrait.transform,
                "Npc Portrait Text",
                "손님",
                22,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(118f, 118f));
            portraitText.fontStyle = FontStyle.Bold;
            var title = GetOrCreateText(
                card.transform,
                "Npc Visit Title Text",
                "밀크룸의 손님",
                30,
                TextAnchor.MiddleLeft,
                new Vector2(194f, -42f),
                new Vector2(470f, 52f));
            title.fontStyle = FontStyle.Bold;
            var role = GetOrCreateText(
                card.transform,
                "Npc Visit Role Text",
                "새로운 방문자",
                17,
                TextAnchor.MiddleLeft,
                new Vector2(196f, -98f),
                new Vector2(430f, 34f));
            role.color = new Color(0.62f, 0.35f, 0.1f, 1f);
            var relationship = GetOrCreateText(
                card.transform,
                "Npc Visit Relationship Text",
                "이야기 1/3",
                15,
                TextAnchor.MiddleRight,
                new Vector2(500f, -132f),
                new Vector2(164f, 28f));
            var messagePanel = GetOrCreatePanel(
                card.transform,
                "Npc Visit Message Panel",
                new Vector2(48f, -184f),
                new Vector2(624f, 170f));
            if (messagePanel.TryGetComponent(out Image messageImage))
            {
                messageImage.color = new Color(1f, 0.99f, 0.93f, 1f);
            }

            var message = GetOrCreateText(
                messagePanel.transform,
                "Npc Visit Message Text",
                "밀크룸에 반가운 손님이 찾아왔어요.",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(28f, -18f),
                new Vector2(568f, 134f));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Truncate;

            var firstChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit First Choice Button",
                "첫 번째 선택",
                new Vector2(48f, -382f),
                new Vector2(296f, 58f));
            var secondChoice = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit Second Choice Button",
                "두 번째 선택",
                new Vector2(376f, -382f),
                new Vector2(296f, 58f));
            var later = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit Later Button",
                "나중에",
                new Vector2(288f, -494f),
                new Vector2(144f, 50f));
            var confirm = GetOrCreateTopLeftButton(
                card.transform,
                "Npc Visit Confirm Button",
                "확인",
                new Vector2(528f, -494f),
                new Vector2(144f, 50f));
            ApplyCareButtonStyle(firstChoice);
            ApplyCareButtonStyle(secondChoice);
            ApplyCareButtonStyle(later);
            ApplyCareButtonStyle(confirm);
            var firstLabel = firstChoice.transform.Find("Label")?.GetComponent<Text>();
            var secondLabel = secondChoice.transform.Find("Label")?.GetComponent<Text>();

            var controller = canvasTransform.GetComponent<NpcVisitCardController>()
                ?? canvasTransform.gameObject.AddComponent<NpcVisitCardController>();
            controller.Configure(
                overlay,
                portraitText,
                title,
                role,
                message,
                relationship,
                firstChoice,
                firstLabel,
                secondChoice,
                secondLabel,
                later,
                confirm,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                portraitImage);
            var bridge = canvasTransform.GetComponent<NpcVisitBridge>()
                ?? canvasTransform.gameObject.AddComponent<NpcVisitBridge>();
            bridge.Configure(
                controller,
                Application.isPlaying ? GameManager.Instance : null,
                canvasTransform);
            overlay.transform.SetAsLastSibling();
        }
    }
}
