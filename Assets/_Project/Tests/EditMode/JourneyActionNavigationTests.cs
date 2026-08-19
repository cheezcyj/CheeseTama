using System;
using System.Linq;
using System.Reflection;
using CheeseTama.Core;
using CheeseTama.Gameplay.Events;
using CheeseTama.Gameplay.Guidance;
using CheeseTama.Gameplay.NpcVisits;
using CheeseTama.Gameplay.Weekly;
using CheeseTama.Save;
using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class JourneyActionNavigationTests
    {
        [Test]
        public void CareEventFollowUpClosesResultBeforeOpeningMilkPanel()
        {
            using var core = IsolatedGameManagerFixture.Create("care_follow_up");
            var host = new GameObject("Care Follow Up Test Host", typeof(RectTransform));
            try
            {
                var overlay = CreateObject("Care Event Overlay", host.transform);
                var card = CreateObject("Care Event Card", overlay.transform)
                    .GetComponent<RectTransform>();
                var title = CreateText(card, "Title");
                var body = CreateText(card, "Body");
                var badge = CreateObject("Badge", card);
                var confirm = CreateButton(card, "Confirm");

                var milkRoot = CreateObject("Milk Panel", host.transform);
                var milkPanel = host.AddComponent<MilkPanelController>();
                milkPanel.Configure(
                    milkRoot,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<Button>(),
                    Array.Empty<Button>(),
                    null,
                    null);

                var controller = host.AddComponent<CareEventCardController>();
                controller.Configure(
                    overlay,
                    card,
                    title,
                    body,
                    badge,
                    confirm,
                    null,
                    null,
                    null,
                    null);
                InvokePrivate(controller, "EnsureChoiceButtons");
                InvokePrivate(controller, "EnsureChoiceButtons");
                InvokePrivate(controller, "BindButtons");

                var followUpButtons = card
                    .GetComponentsInChildren<Button>(true)
                    .Where(button => button.name == "Care Event Follow Up Button")
                    .ToArray();
                Assert.That(followUpButtons, Has.Length.EqualTo(1));

                overlay.SetActive(true);
                var result = new CareEventChoiceResult(
                    CareEventChoiceResolutionStatus.Applied,
                    "occurrence",
                    "event",
                    "choice",
                    "선택 결과",
                    "결과를 확인했어요.",
                    new CareEventChoiceEffect(
                        followUpAction: CareEventFollowUpAction.FeedMilk,
                        followUpHint: "우유를 챙겨 주세요."));
                InvokePrivate(controller, "ShowChoiceResult", result);

                var followUp = followUpButtons[0];
                Assert.That(followUp.gameObject.activeSelf, Is.True);
                Assert.That(
                    followUp.GetComponentInChildren<Text>(true)?.text,
                    Is.EqualTo("우유 챙기러 가기"));
                Assert.That(milkRoot.activeSelf, Is.False);

                followUp.onClick.Invoke();

                Assert.That(overlay.activeSelf, Is.False);
                Assert.That(followUp.gameObject.activeSelf, Is.False);
                Assert.That(milkRoot.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void JourneyHubDeepLinkSelectsRequestedTabAndKeepsOneBadge()
        {
            var host = new GameObject("Journey Deep Link Test Host", typeof(RectTransform));
            try
            {
                var overlay = CreateObject("Journey Hub Overlay", host.transform);
                var open = CreateButton(host.transform, JourneyHubPanelController.OpenButtonObjectName);
                var tabs = new Button[5];
                for (var index = 0; index < tabs.Length; index += 1)
                {
                    tabs[index] = CreateButton(overlay.transform, $"Tab {index}");
                }

                var title = CreateText(overlay.transform, "Title");
                var body = CreateText(overlay.transform, "Body");
                var status = CreateText(overlay.transform, "Status");
                var previous = CreateButton(overlay.transform, "Previous");
                var next = CreateButton(overlay.transform, "Next");
                var primary = CreateButton(overlay.transform, "Primary");
                var close = CreateButton(overlay.transform, "Close");
                var controller = host.AddComponent<JourneyHubPanelController>();

                ConfigureJourney(
                    controller,
                    overlay,
                    open,
                    tabs,
                    title,
                    body,
                    status,
                    previous,
                    next,
                    primary,
                    close);
                ConfigureJourney(
                    controller,
                    overlay,
                    open,
                    tabs,
                    title,
                    body,
                    status,
                    previous,
                    next,
                    primary,
                    close);

                var badges = open
                    .GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name == "Journey Hub Attention Badge")
                    .ToArray();
                Assert.That(badges, Has.Length.EqualTo(1));
                Assert.That(badges[0].gameObject.activeSelf, Is.False);

                controller.Open(JourneyHubTab.Album);

                Assert.That(overlay.activeSelf, Is.True);
                Assert.That(controller.SelectedTab, Is.EqualTo(JourneyHubTab.Album));
                Assert.That(title.text, Is.EqualTo("도감 세트 앨범"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void JourneyOpenBadgeCombinesClaimableAndExpiringAndDeepLinksToUrgentTab()
        {
            using var core = IsolatedGameManagerFixture.Create("journey_attention");
            core.Manager.LoadOrCreateGame();
            var now = DateTimeOffset.Now;
            var save = core.Manager.CurrentSave;
            var weekly = new WeeklyCareJourneySystem();
            Assert.That(
                weekly.RecordEvent(
                    save.weeklyCareJourney,
                    WeeklyCareEventIds.Feed,
                    6,
                    now,
                    "attention-feed").Applied,
                Is.True);
            Assert.That(
                weekly.RecordEvent(
                    save.weeklyCareJourney,
                    WeeklyCareEventIds.Play,
                    3,
                    now,
                    "attention-play").Applied,
                Is.True);
            Assert.That(
                weekly.RecordEvent(
                    save.weeklyCareJourney,
                    WeeklyCareEventIds.Discovery,
                    2,
                    now,
                    "attention-discovery").Applied,
                Is.True);
            save.npcRelationshipQuests.activeQuest.Set(
                "attention-offer",
                NpcVisitSystem.MilkyDoctorId,
                "doctor_warm_soup",
                now.AddDays(-2),
                now.AddMinutes(-10),
                now.AddDays(1));

            var host = new GameObject("Journey Attention Test Host", typeof(RectTransform));
            try
            {
                var overlay = CreateObject("Journey Hub Overlay", host.transform);
                var open = CreateButton(host.transform, JourneyHubPanelController.OpenButtonObjectName);
                var tabs = new Button[5];
                for (var index = 0; index < tabs.Length; index += 1)
                {
                    tabs[index] = CreateButton(overlay.transform, $"Tab {index}");
                }

                var controller = host.AddComponent<JourneyHubPanelController>();
                controller.Configure(
                    overlay,
                    open,
                    tabs,
                    CreateText(overlay.transform, "Title"),
                    CreateText(overlay.transform, "Body"),
                    CreateText(overlay.transform, "Status"),
                    CreateButton(overlay.transform, "Previous"),
                    CreateButton(overlay.transform, "Next"),
                    CreateButton(overlay.transform, "Primary"),
                    CreateButton(overlay.transform, "Close"),
                    core.Manager,
                    null,
                    null,
                    null);

                var badge = open.transform.Find("Journey Hub Attention Badge");
                Assert.That(badge, Is.Not.Null);
                Assert.That(badge.gameObject.activeSelf, Is.True);
                Assert.That(
                    badge.GetComponentInChildren<Text>(true)?.text,
                    Is.EqualTo("임박+받기"));

                open.onClick.Invoke();

                Assert.That(overlay.activeSelf, Is.True);
                Assert.That(controller.SelectedTab, Is.EqualTo(JourneyHubTab.Relationships));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GoalDestinationRouteFindsAndOpensMilkPanel()
        {
            var action = (NextAction)Activator.CreateInstance(
                typeof(NextAction),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[]
                {
                    "milk-goal",
                    NextActionUrgency.LongTerm,
                    "우유 성장 다양성 넓히기",
                    50,
                    "우유 성장 다양성 1/2",
                    NextActionRouteIds.MilkGrowth
                },
                null);
            var snapshot = (NextActionGoalBoardSnapshot)Activator.CreateInstance(
                typeof(NextActionGoalBoardSnapshot),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[]
                {
                    true,
                    30,
                    31,
                    50,
                    new[] { action },
                    new[] { "우유 성장 다양성 1/2" }
                },
                null);
            var routeArguments = new object[] { snapshot, string.Empty };
            var found = (bool)InvokePrivateStatic(
                typeof(JourneyHubPanelController),
                "TryFindGoalRoute",
                routeArguments);
            Assert.That(found, Is.True);
            Assert.That(routeArguments[1], Is.EqualTo(NextActionRouteIds.MilkGrowth));

            using var core = IsolatedGameManagerFixture.Create("goal_route");
            var host = new GameObject("Journey Goal Route Test Host", typeof(RectTransform));
            try
            {
                var overlay = CreateObject("Journey Hub Overlay", host.transform);
                var controller = host.AddComponent<JourneyHubPanelController>();
                var milkRoot = CreateObject("Milk Panel", host.transform);
                var milkPanel = host.AddComponent<MilkPanelController>();
                milkPanel.Configure(
                    milkRoot,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<Button>(),
                    Array.Empty<Button>(),
                    null,
                    null);
                SetPrivateField(controller, "overlay", overlay);
                SetPrivateField(controller, "selectedGoalRouteId", routeArguments[1]);
                overlay.SetActive(true);

                InvokePrivate(controller, "ExecuteGoalRoute");

                Assert.That(overlay.activeSelf, Is.False);
                Assert.That(milkRoot.activeSelf, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EligibleRelationshipEpisodeDeepLinksAndAppliesOneChoice()
        {
            using var core = IsolatedGameManagerFixture.Create("relationship_episode");
            core.Manager.LoadOrCreateGame();
            core.Manager.CurrentSave.npcVisits.relationships.Add(new NpcRelationshipSaveEntry
            {
                npcId = NpcVisitSystem.MilkyDoctorId,
                visits = 3,
                affinity = NpcRelationshipQuestSystem.FriendAffinityThreshold,
                storyStep = 1
            });

            var host = new GameObject("Relationship Episode Journey Host", typeof(RectTransform));
            try
            {
                var overlay = CreateObject("Journey Hub Overlay", host.transform);
                var open = CreateButton(host.transform, JourneyHubPanelController.OpenButtonObjectName);
                var tabs = new Button[5];
                for (var index = 0; index < tabs.Length; index += 1)
                {
                    tabs[index] = CreateButton(overlay.transform, $"Tab {index}");
                }

                var body = CreateText(overlay.transform, "Body");
                var previous = CreateButton(overlay.transform, "Previous");
                var next = CreateButton(overlay.transform, "Next");
                var controller = host.AddComponent<JourneyHubPanelController>();
                controller.Configure(
                    overlay,
                    open,
                    tabs,
                    CreateText(overlay.transform, "Title"),
                    body,
                    CreateText(overlay.transform, "Status"),
                    previous,
                    next,
                    CreateButton(overlay.transform, "Primary"),
                    CreateButton(overlay.transform, "Close"),
                    core.Manager,
                    null,
                    null,
                    null);

                var badge = open.transform.Find("Journey Hub Attention Badge");
                Assert.That(badge, Is.Not.Null);
                Assert.That(badge.gameObject.activeSelf, Is.True);
                Assert.That(badge.GetComponentInChildren<Text>(true)?.text, Is.EqualTo("이야기"));

                open.onClick.Invoke();
                Assert.That(controller.SelectedTab, Is.EqualTo(JourneyHubTab.Relationships));
                Assert.That(body.text, Does.Contain("친구가 된 날의 건강 수첩"));
                Assert.That(previous.gameObject.activeSelf, Is.True);
                Assert.That(next.gameObject.activeSelf, Is.True);
                Assert.That(previous.GetComponentInChildren<Text>(true)?.text, Is.EqualTo("선택 1"));

                previous.onClick.Invoke();

                Assert.That(core.Manager.CurrentSave.npcRelationshipEpisodes
                    .HasCompletedEpisode(NpcRelationshipEpisodeIds.DoctorFriend), Is.True);
                Assert.That(core.Manager.CurrentSave.npcRelationshipEpisodes
                    .HasKeepsake(NpcRelationshipKeepsakeIds.DoctorHealthNotebook), Is.True);
                Assert.That(body.text, Does.Contain("건강 수첩"));
                Assert.That(previous.gameObject.activeSelf, Is.False);
                Assert.That(next.gameObject.activeSelf, Is.False);
                Assert.That(badge.gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static void ConfigureJourney(
            JourneyHubPanelController controller,
            GameObject overlay,
            Button open,
            Button[] tabs,
            Text title,
            Text body,
            Text status,
            Button previous,
            Button next,
            Button primary,
            Button close)
        {
            controller.Configure(
                overlay,
                open,
                tabs,
                title,
                body,
                status,
                previous,
                next,
                primary,
                close,
                null,
                null,
                null,
                null);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static Text CreateText(Transform parent, string name)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            value.transform.SetParent(parent, false);
            return value.GetComponent<Text>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            value.transform.SetParent(parent, false);
            CreateText(value.transform, "Label");
            return value.GetComponent<Button>();
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
            return method.Invoke(target, arguments);
        }

        private static object InvokePrivateStatic(Type type, string methodName, object[] arguments)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private static method: {methodName}");
            return method.Invoke(null, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
            field.SetValue(target, value);
        }

        private sealed class IsolatedGameManagerFixture : IDisposable
        {
            private readonly GameObject root;
            private readonly SaveManager saveManager;

            private IsolatedGameManagerFixture(GameObject fixtureRoot, SaveManager manager)
            {
                root = fixtureRoot;
                saveManager = manager;
            }

            public GameManager Manager { get; private set; }

            public static IsolatedGameManagerFixture Create(string label)
            {
                var root = new GameObject($"{label} Navigation Fixture");
                root.SetActive(false);
                var saveManager = root.AddComponent<SaveManager>();
                var gameManager = root.AddComponent<GameManager>();
                SetPrivateField(
                    saveManager,
                    "saveFileName",
                    $"cheesetama_navigation_test_{label}_{Guid.NewGuid():N}.json");
                SetPrivateField(gameManager, "saveManager", saveManager);
                root.SetActive(true);
                return new IsolatedGameManagerFixture(root, saveManager)
                {
                    Manager = gameManager
                };
            }

            public void Dispose()
            {
                saveManager.DeleteSave();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
