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
        private static void EnsureBootLocalAccountFlow(
            Transform canvasTransform,
            GameManager manager)
        {
            if (canvasTransform == null)
            {
                return;
            }

            EnsureLocalAccountPanel(canvasTransform, manager, null, null);
            var controller = canvasTransform.GetComponent<AuthPanelController>();
            RemoveChildIfExists(canvasTransform, "Open Local Account Sign Up Button");

            RefreshLocalAccountEntryPresentation(canvasTransform, manager);
            controller?.ConfigureStartupGateControls(
                canvasTransform.Find("Start Button")?.GetComponent<Button>(),
                null,
                EnterMilkroomFromBoot);
        }

        private static void EnterMilkroomFromBoot()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneNames.Milkroom))
            {
                Debug.LogWarning(
                    $"'{SceneNames.Milkroom}' 씬이 빌드 설정에 없습니다. CheeseTama > 시작 씬 빌드를 실행하세요.");
                return;
            }

            SceneManager.LoadScene(SceneNames.Milkroom);
        }

        private static void EnsureCheeseTamaProfileMenuShell(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var topBar = canvasTransform.Find("Top Status Bar");
            if (topBar != null)
            {
                var profileButton = GetOrCreateTopLeftButton(
                    topBar,
                    "CheeseTama Profile Button",
                    string.Empty,
                    new Vector2(TopProfileLeft, -(TopHudHeight - TopProfileSize) * 0.5f),
                    new Vector2(TopProfileSize, TopProfileSize));
                ApplyCareButtonStyle(profileButton);
                var profileBackground = profileButton.targetGraphic as Image
                    ?? profileButton.GetComponent<Image>();
                if (profileBackground != null)
                {
                    ApplyRoundedImage(profileBackground);
                    profileBackground.color = new Color(1f, 0.67f, 0.12f, 1f);
                    profileBackground.raycastTarget = true;
                }

                var mask = profileButton.GetComponent<Mask>() ?? profileButton.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
                var portraitTransform = profileButton.transform.Find("Profile Portrait Image");
                Image portraitImage;
                if (portraitTransform == null)
                {
                    var portraitObject = new GameObject("Profile Portrait Image", typeof(RectTransform));
                    portraitObject.transform.SetParent(profileButton.transform, false);
                    portraitTransform = portraitObject.transform;
                    portraitImage = portraitObject.AddComponent<Image>();
                }
                else
                {
                    portraitImage = portraitTransform.GetComponent<Image>()
                        ?? portraitTransform.gameObject.AddComponent<Image>();
                }

                var portraitRect = portraitTransform.GetComponent<RectTransform>();
                portraitRect.anchorMin = Vector2.zero;
                portraitRect.anchorMax = Vector2.one;
                portraitRect.offsetMin = new Vector2(4f, 4f);
                portraitRect.offsetMax = new Vector2(-4f, -4f);
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
                portraitTransform.SetAsLastSibling();

                ConfigureTopBarIdentityLayout(topBar);
                var nameRect = topBar.Find("Name Text") as RectTransform;
                if (nameRect != null && nameRect.TryGetComponent(out Text nameText))
                {
                    nameText.resizeTextForBestFit = true;
                    nameText.resizeTextMinSize = 16;
                    nameText.resizeTextMaxSize = 28;
                    nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nameText.verticalOverflow = VerticalWrapMode.Truncate;
                }
            }

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                CheeseTamaProfileMenuController.OverlayObjectName,
                new Color(0.08f, 0.055f, 0.025f, 0.72f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Profile Card",
                Vector2.zero,
                new Vector2(600f, 610f));
            ConfigureCenteredRect(card.GetComponent<RectTransform>(), new Vector2(600f, 610f));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.84f, 1f);
                cardImage.raycastTarget = true;
            }

            var heading = GetOrCreateText(
                card.transform,
                "Profile Heading Text",
                "프로필",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(50f, -30f),
                new Vector2(500f, 48f));
            heading.fontStyle = FontStyle.Bold;
            var profileName = GetOrCreateText(
                card.transform,
                "Profile Name Text",
                "CheeseTama",
                26,
                TextAnchor.MiddleCenter,
                new Vector2(150f, -82f),
                new Vector2(300f, 42f));
            profileName.fontStyle = FontStyle.Bold;
            profileName.resizeTextForBestFit = true;
            profileName.resizeTextMinSize = 18;
            profileName.resizeTextMaxSize = 26;
            var profileDetail = GetOrCreateText(
                card.transform,
                "Profile Detail Text",
                "레벨 1 · 치즈타마 알",
                17,
                TextAnchor.MiddleCenter,
                new Vector2(50f, -124f),
                new Vector2(500f, 34f));
            profileDetail.color = new Color(0.55f, 0.34f, 0.14f, 1f);

            var entries = GetOrCreatePanel(
                card.transform,
                "Profile Entries",
                new Vector2(90f, -184f),
                new Vector2(420f, 346f));
            if (entries.TryGetComponent(out Image entriesImage))
            {
                entriesImage.color = Color.clear;
                entriesImage.raycastTarget = false;
            }

            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Profile Close Button",
                "닫기",
                new Vector2(212f, -530f),
                new Vector2(176f, 50f));
            ApplyCareButtonStyle(close);
            overlay.SetActive(false);
        }

        private static void EnsureCheeseTamaProfileMenu(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            EnsureCheeseTamaProfileMenuShell(canvasTransform);
            var overlay = canvasTransform.Find(CheeseTamaProfileMenuController.OverlayObjectName)?.gameObject;
            var card = overlay != null ? overlay.transform.Find("Profile Card") : null;
            var topBar = canvasTransform.Find("Top Status Bar");
            var profileButton = topBar != null
                ? topBar.Find("CheeseTama Profile Button")?.GetComponent<Button>()
                : null;
            var portraitImage = profileButton != null
                ? profileButton.transform.Find("Profile Portrait Image")?.GetComponent<Image>()
                : null;
            var entries = GetProfileMenuEntryParent(canvasTransform);
            var controller = canvasTransform.GetComponent<CheeseTamaProfileMenuController>()
                ?? canvasTransform.gameObject.AddComponent<CheeseTamaProfileMenuController>();
            controller.Configure(
                overlay,
                profileButton,
                portraitImage,
                card != null ? card.Find("Profile Name Text")?.GetComponent<Text>() : null,
                card != null ? card.Find("Profile Detail Text")?.GetComponent<Text>() : null,
                entries != null ? entries.Find("Open First Day Journey Button")?.GetComponent<Button>() : null,
                entries != null ? entries.Find("Open Growth Journey Button")?.GetComponent<Button>() : null,
                entries != null ? entries.Find("Open Memory Journal Button")?.GetComponent<Button>() : null,
                entries != null ? entries.Find("Open Bond Status Button")?.GetComponent<Button>() : null,
                card != null ? card.Find("Open Name Change Button")?.GetComponent<Button>() : null,
                card != null ? card.Find("Profile Close Button")?.GetComponent<Button>() : null,
                canvasTransform.GetComponent<TopMenuController>(),
                canvasTransform.Find("Bottom Action Bar")?.GetComponent<BottomActionBarController>(),
                canvasTransform.GetComponent<DevPanelController>(),
                entries != null
                    ? entries.Find("Replay First Meeting Button")?.GetComponent<Button>()
                    : null);
            overlay?.transform.SetAsLastSibling();
        }

        private static void EnsureLocalAccountPanel(
            Transform canvasTransform,
            GameManager manager,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var profileController = canvasTransform.GetComponent<CheeseTamaProfileMenuController>();
            var isBootCanvas = canvasTransform.Find("Start Button") != null;
            Button entry = null;
            if (isBootCanvas)
            {
                RemoveChildIfExists(canvasTransform, "Open Local Account Button");
                RemoveChildIfExists(canvasTransform, BootLocalAccountButtonName);
            }
            else
            {
                entry = GetOrMoveProfileEntryButton(
                    canvasTransform,
                    null,
                    "Open Local Account Button",
                    "계정",
                    3);
                ApplyCareButtonStyle(entry);
            }
            var persistentEntry = RemoveTopLocalAccountEntry(canvasTransform);

            var overlay = GetOrCreateFullScreenOverlay(
                canvasTransform,
                AuthPanelController.OverlayObjectName,
                new Color(0.06f, 0.045f, 0.025f, 0.8f));
            var card = GetOrCreatePanel(
                overlay.transform,
                "Local Account Card",
                Vector2.zero,
                new Vector2(680f, AuthPanelController.CompactCardHeight));
            ConfigureCenteredRect(
                card.GetComponent<RectTransform>(),
                new Vector2(680f, AuthPanelController.CompactCardHeight));
            if (card.TryGetComponent(out Image cardImage))
            {
                cardImage.color = new Color(1f, 0.97f, 0.84f, 1f);
                cardImage.raycastTarget = true;
            }

            var heading = GetOrCreateText(
                card.transform,
                "Local Account Heading",
                "계정",
                32,
                TextAnchor.MiddleCenter,
                new Vector2(60f, -18f),
                new Vector2(560f, 48f));
            heading.fontStyle = FontStyle.Bold;
            RemoveChildIfExists(card.transform, "Local Account Notice");

            var loginView = GetOrCreateAccountView(card.transform, "Local Account Login View");
            var loginEmail = GetOrCreateInputField(
                loginView.transform,
                "Login Email Input",
                "아이디",
                new Vector2(70f, 0f),
                new Vector2(460f, 72f));
            loginEmail.contentType = InputField.ContentType.Standard;
            loginEmail.lineType = InputField.LineType.SingleLine;
            loginEmail.characterLimit = LocalAccountEmail.MaximumLength;
            var loginPassword = GetOrCreateInputField(
                loginView.transform,
                "Login Password Input",
                "비밀번호",
                new Vector2(70f, -84f),
                new Vector2(460f, 72f));
            loginPassword.characterLimit = LocalAccountPasswordPolicy.MaximumLength;
            var loginButton = GetOrCreateTopLeftButton(
                loginView.transform,
                "Submit Login Button",
                "로그인",
                new Vector2(64f, -176f),
                new Vector2(170f, 56f));
            var continueAsGuest = GetOrCreateTopLeftButton(
                loginView.transform,
                ContinueAsGuestButtonName,
                isBootCanvas ? "게스트로 입장" : "게스트로 계속",
                new Vector2(250f, -176f),
                new Vector2(286f, 56f));
            var showSignUp = GetOrCreateTopLeftButton(
                loginView.transform,
                "Show Sign Up Button",
                "회원가입",
                new Vector2(170f, -252f),
                new Vector2(260f, 56f));

            var signUpView = GetOrCreateAccountView(card.transform, "Local Account Sign Up View");
            var signUpEmail = GetOrCreateInputField(
                signUpView.transform,
                "Sign Up Email Input",
                "새 아이디",
                new Vector2(70f, 0f),
                new Vector2(460f, 72f));
            signUpEmail.contentType = InputField.ContentType.Standard;
            signUpEmail.lineType = InputField.LineType.SingleLine;
            signUpEmail.characterLimit = LocalAccountEmail.MaximumLength;
            var signUpPassword = GetOrCreateInputField(
                signUpView.transform,
                "Sign Up Password Input",
                "비밀번호 (6자 이상이면 돼요)",
                new Vector2(70f, -84f),
                new Vector2(460f, 72f));
            signUpPassword.characterLimit = LocalAccountPasswordPolicy.MaximumLength;
            var signUpConfirmation = GetOrCreateInputField(
                signUpView.transform,
                "Sign Up Password Confirmation Input",
                "비밀번호 확인",
                new Vector2(70f, -168f),
                new Vector2(460f, 72f));
            signUpConfirmation.characterLimit = LocalAccountPasswordPolicy.MaximumLength;
            var signUpButton = GetOrCreateTopLeftButton(
                signUpView.transform,
                "Submit Sign Up Button",
                "계정 만들고 이어서 하기",
                new Vector2(72f, -260f),
                new Vector2(310f, 56f));
            var backToLogin = GetOrCreateTopLeftButton(
                signUpView.transform,
                "Back To Login Button",
                "로그인으로",
                new Vector2(398f, -260f),
                new Vector2(130f, 56f));

            var accountView = GetOrCreateAccountView(card.transform, "Local Account Signed In View");
            var currentAccount = GetOrCreateText(
                accountView.transform,
                "Current Local Account Text",
                "로그인한 계정",
                20,
                TextAnchor.MiddleCenter,
                new Vector2(70f, 0f),
                new Vector2(460f, 160f));
            currentAccount.fontStyle = FontStyle.Bold;
            currentAccount.horizontalOverflow = HorizontalWrapMode.Wrap;
            currentAccount.verticalOverflow = VerticalWrapMode.Truncate;
            currentAccount.resizeTextForBestFit = true;
            currentAccount.resizeTextMinSize = 12;
            currentAccount.resizeTextMaxSize = 20;
            var accountHelp = GetOrCreateText(
                accountView.transform,
                "Signed In Account Help",
                "로그아웃해도 게임 기록은 남아요.\n다시 로그인하면 이어서 할 수 있어요.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(75f, -172f),
                new Vector2(450f, 60f));
            accountHelp.color = new Color(0.49f, 0.31f, 0.12f, 1f);
            ConfigureMultiLineFeedbackText(accountHelp, 14, 18);
            var logout = GetOrCreateTopLeftButton(
                accountView.transform,
                "Submit Logout Button",
                "로그아웃",
                new Vector2(98f, -248f),
                new Vector2(190f, 56f));
            var showDelete = GetOrCreateTopLeftButton(
                accountView.transform,
                "Show Delete Account Button",
                "회원탈퇴",
                new Vector2(312f, -248f),
                new Vector2(190f, 56f));

            var deleteView = GetOrCreateAccountView(card.transform, "Local Account Delete View");
            var deleteWarning = GetOrCreateText(
                deleteView.transform,
                "Delete Account Warning",
                "이 계정의 게임 기록은 지워져요.\n게스트 기록과 받은 백업 파일은 남아요.",
                18,
                TextAnchor.MiddleCenter,
                new Vector2(70f, 0f),
                new Vector2(460f, 72f));
            deleteWarning.color = new Color(0.72f, 0.18f, 0.12f, 1f);
            ConfigureMultiLineFeedbackText(deleteWarning, 14, 18);
            var deletePassword = GetOrCreateInputField(
                deleteView.transform,
                "Delete Account Password Input",
                "현재 비밀번호",
                new Vector2(70f, -84f),
                new Vector2(460f, 72f));
            deletePassword.characterLimit = LocalAccountPasswordPolicy.MaximumLength;
            var deleteConfirmation = GetOrCreateInputField(
                deleteView.transform,
                "Delete Account Confirmation Input",
                "계정 지우기 입력",
                new Vector2(70f, -168f),
                new Vector2(460f, 72f));
            deleteConfirmation.contentType = InputField.ContentType.Standard;
            deleteConfirmation.lineType = InputField.LineType.SingleLine;
            deleteConfirmation.characterLimit = AuthPanelController.DeleteConfirmationPhrase.Length;
            var deleteAccount = GetOrCreateTopLeftButton(
                deleteView.transform,
                "Confirm Delete Account Button",
                "계정과 게임 기록 지우기",
                new Vector2(72f, -260f),
                new Vector2(310f, 56f));
            var cancelDelete = GetOrCreateTopLeftButton(
                deleteView.transform,
                "Cancel Delete Account Button",
                "취소",
                new Vector2(398f, -260f),
                new Vector2(130f, 56f));

            foreach (var input in new[]
                     {
                         loginEmail, loginPassword, signUpEmail, signUpPassword,
                         signUpConfirmation, deletePassword, deleteConfirmation
                     })
            {
                ApplyLocalAccountInputStyle(input);
            }

            foreach (var button in new[]
                     {
                         loginButton, showSignUp, signUpButton, backToLogin,
                         logout, showDelete, deleteAccount, cancelDelete, continueAsGuest
                     })
            {
                ApplyLocalAccountButtonStyle(button);
            }

            var status = GetOrCreateText(
                card.transform,
                "Local Account Status Text",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                new Vector2(55f, -410f),
                new Vector2(570f, 96f));
            ConfigureMultiLineFeedbackText(status, 15, 16);
            var error = GetOrCreateText(
                card.transform,
                "Local Account Error Text",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                new Vector2(55f, -410f),
                new Vector2(570f, 96f));
            ConfigureMultiLineFeedbackText(error, 15, 16);
            error.color = new Color(0.72f, 0.18f, 0.12f, 1f);
            var close = GetOrCreateTopLeftButton(
                card.transform,
                "Close Local Account Button",
                isBootCanvas ? "취소" : "닫기",
                new Vector2(260f, -AuthPanelController.CompactCloseTop),
                new Vector2(160f, 44f));
            ApplyLocalAccountButtonStyle(close);

            var controller = canvasTransform.GetComponent<AuthPanelController>()
                ?? canvasTransform.gameObject.AddComponent<AuthPanelController>();
            controller.Configure(
                overlay,
                entry,
                close,
                loginView,
                loginEmail,
                loginPassword,
                loginButton,
                showSignUp,
                signUpView,
                signUpEmail,
                signUpPassword,
                signUpConfirmation,
                signUpButton,
                backToLogin,
                accountView,
                currentAccount,
                logout,
                showDelete,
                deleteView,
                deletePassword,
                deleteConfirmation,
                deleteAccount,
                cancelDelete,
                status,
                error,
                (email, password, confirmation) => manager != null
                    ? HandleLocalAccountOperation(
                    manager.RegisterLocalAccount(email, password, confirmation),
                    "계정을 만들었어요. 지금까지 한 게임도 이어졌어요.",
                    canvasTransform,
                    milkroomUi,
                    visualController)
                    : AuthPanelOperationResult.Failure(
                        "지금은 계정 기능을 쓸 수 없어요. 게스트로 계속해 주세요."),
                (email, password) => manager != null
                    ? HandleLocalAccountOperation(
                    manager.LoginLocalAccount(email, password),
                    "로그인했어요. 저장한 게임을 이어서 할 수 있어요.",
                    canvasTransform,
                    milkroomUi,
                    visualController)
                    : AuthPanelOperationResult.Failure(
                        "지금은 계정 기능을 쓸 수 없어요. 게스트로 계속해 주세요."),
                () => manager != null
                    ? HandleLocalAccountOperation(
                    manager.LogoutLocalAccount(),
                    "로그아웃했어요. 게스트 게임으로 돌아왔어요.",
                    canvasTransform,
                    milkroomUi,
                    visualController)
                    : AuthPanelOperationResult.Failure(
                        "지금은 계정 기능을 쓸 수 없어요. 게스트로 계속해 주세요."),
                (password, confirmation) => manager != null
                    ? HandleLocalAccountOperation(
                    manager.DeleteLocalAccount(password, confirmation),
                    "계정과 이 계정의 게임 기록을 지웠어요.",
                    canvasTransform,
                    milkroomUi,
                    visualController)
                    : AuthPanelOperationResult.Failure(
                        "지금은 계정 기능을 쓸 수 없어요. 게스트로 계속해 주세요."),
                () => manager != null && manager.LocalAccount.IsSignedIn,
                () => manager != null ? manager.LocalAccount.NormalizedEmail : string.Empty,
                CreateControlBlockingCallback(canvasTransform));
            controller.ConfigureEntryNavigation(profileController != null
                ? profileController.CloseForChildNavigation
                : null);
            controller.ConfigureAdditionalNavigation(persistentEntry, continueAsGuest);
            controller.RefreshSession();
            RefreshLocalAccountEntryPresentation(canvasTransform, manager);
            overlay.transform.SetAsLastSibling();
        }

        private static Button RemoveTopLocalAccountEntry(Transform canvasTransform)
        {
            var topBar = canvasTransform != null
                ? canvasTransform.Find("Top Status Bar")
                : null;
            if (topBar == null)
            {
                return null;
            }

            RemoveChildIfExists(topBar, TopLocalAccountButtonName);
            ConfigureTopBarIdentityLayout(topBar);
            return null;
        }

        private static void RefreshLocalAccountEntryPresentation(
            Transform canvasTransform,
            GameManager manager)
        {
            ApplyLocalAccountEntryPresentation(
                canvasTransform,
                IsLocalAccountSignedIn(manager));
        }

        private static void ApplyLocalAccountEntryPresentation(
            Transform canvasTransform,
            bool signedIn)
        {
            if (canvasTransform == null)
            {
                return;
            }

            var topBar = canvasTransform.Find("Top Status Bar");
            if (topBar != null)
            {
                RemoveChildIfExists(topBar, TopLocalAccountButtonName);
            }

            var bootEntry = canvasTransform.Find(BootLocalAccountButtonName)?.GetComponent<Button>();
            if (bootEntry != null)
            {
                RemoveChildIfExists(canvasTransform, BootLocalAccountButtonName);
            }

            var bootStart = canvasTransform.Find("Start Button")?.GetComponent<Button>();
            if (bootStart != null)
            {
                SetButtonLabel(bootStart, "입장하기");
            }

            var bootSignUp = canvasTransform.Find("Open Local Account Sign Up Button");
            if (bootSignUp != null)
            {
                bootSignUp.gameObject.SetActive(false);
            }

            ApplyBootIntroSessionCopy(canvasTransform, signedIn);
        }

        private static bool IsLocalAccountSignedIn(GameManager manager)
        {
            return manager != null && manager.LocalAccount.IsSignedIn;
        }

        private static string ResolveLocalAccountEntryLabel(bool signedIn)
        {
            return signedIn ? "계정" : "로그인";
        }

        private static GameObject GetOrCreateAccountView(Transform card, string name)
        {
            var view = GetOrCreatePanel(
                card,
                name,
                new Vector2(40f, -82f),
                new Vector2(600f, 316f));
            if (view.TryGetComponent(out Image image))
            {
                image.color = Color.clear;
                image.raycastTarget = false;
            }

            return view;
        }

        private static void ApplyLocalAccountInputStyle(InputField input)
        {
            if (input == null)
            {
                return;
            }

            if (input.textComponent != null)
            {
                input.textComponent.fontSize = 22;
                input.textComponent.resizeTextForBestFit = false;
            }

            var inputImage = input.targetGraphic as Image ?? input.GetComponent<Image>();
            if (inputImage != null)
            {
                inputImage.color = new Color(1f, 0.995f, 0.94f, 1f);
                ApplyRoundedImage(inputImage);
            }

            var outline = input.GetComponent<Outline>() ?? input.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.48f, 0.29f, 0.09f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;

            if (input.placeholder is Text placeholder)
            {
                placeholder.fontSize = 22;
                placeholder.resizeTextForBestFit = true;
                placeholder.resizeTextMinSize = 18;
                placeholder.resizeTextMaxSize = 22;
            }
        }

        private static void ApplyLocalAccountButtonStyle(Button button)
        {
            ApplyCareButtonStyle(button);
            var label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label == null)
            {
                return;
            }

            label.fontSize = 20;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 20;
        }

        private static AuthPanelOperationResult HandleLocalAccountOperation(
            LocalAccountAttemptResult result,
            string successMessage,
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController)
        {
            if (!result.Succeeded)
            {
                if (!result.Snapshot.IsSignedIn && result.Snapshot.HasPendingDeletion)
                {
                    const string pendingMessage =
                        "계정은 지웠어요. 남은 게임 기록도 다음에 게임을 열 때 다시 지워 볼게요.";
                    RebindLocalAccountState(
                        GameManager.Instance,
                        canvasTransform,
                        milkroomUi,
                        visualController,
                        pendingMessage);
                    return AuthPanelOperationResult.Success(pendingMessage);
                }

                return AuthPanelOperationResult.Failure(ResolveLocalAccountFailure(result));
            }

            var manager = GameManager.Instance;
            RebindLocalAccountState(
                manager,
                canvasTransform,
                milkroomUi,
                visualController,
                successMessage);
            return AuthPanelOperationResult.Success(successMessage);
        }

        private static void RebindLocalAccountState(
            GameManager manager,
            Transform canvasTransform,
            MilkroomUIController milkroomUi,
            CheeseTamaVisualController visualController,
            string message)
        {
            RefreshLocalAccountEntryPresentation(canvasTransform, manager);
            if (manager?.CurrentSave == null)
            {
                return;
            }

            ApplySavedMilkroomTheme(manager);
            milkroomUi?.Bind(manager.CurrentSave);
            milkroomUi?.ShowMessage(message);
            if (visualController != null)
            {
                visualController.Bind(manager.CurrentTama);
                visualController.React(false);
            }

            Object.FindFirstObjectByType<GameSettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave(true);
            Object.FindFirstObjectByType<AccessibilitySettingsPanelController>(
                FindObjectsInactive.Include)?.RefreshFromSave("보기 편한 설정을 적용했어요.");
            canvasTransform?.GetComponent<CheeseTamaProfileMenuController>()?.RefreshProfile();
            if (canvasTransform != null)
            {
                AccessibilityRuntime.Apply(canvasTransform, manager.CurrentSave.settings);
            }
        }

        private static string ResolveLocalAccountFailure(LocalAccountAttemptResult result)
        {
            switch (result.Status)
            {
                case LocalAccountAttemptStatus.InvalidEmail:
                    return "아이디는 문자나 숫자로 시작하고, 문자·숫자·점·밑줄·빼기표만 써 주세요.";
                case LocalAccountAttemptStatus.WeakPassword:
                    return LocalAccountPasswordPolicy.Description;
                case LocalAccountAttemptStatus.ConfirmationMismatch:
                    return "비밀번호나 확인 말을 다시 확인해 주세요.";
                case LocalAccountAttemptStatus.AccountAlreadyExists:
                    return "이미 만든 계정이에요. 로그인해 주세요.";
                case LocalAccountAttemptStatus.InvalidCredentials:
                    return "아이디나 비밀번호를 다시 확인해 주세요.";
                case LocalAccountAttemptStatus.SessionRequired:
                    return "먼저 로그인해 주세요.";
                case LocalAccountAttemptStatus.AccountLimitReached:
                    return "이 브라우저에는 계정을 더 만들 수 없어요.";
                case LocalAccountAttemptStatus.DeletionPending:
                    return "계정은 지웠어요. 남은 게임 기록도 다음에 게임을 열 때 다시 지워 볼게요.";
                case LocalAccountAttemptStatus.AccountDataOperationFailed:
                    return "계정의 게임 기록을 지우거나 불러오지 못했어요. 다시 시도해 주세요.";
                case LocalAccountAttemptStatus.SecurityUnavailable:
                    return "이 브라우저에서는 계정을 저장할 수 없어요. 게스트로 계속할 수 있어요.";
                default:
                    return "이 브라우저에서는 계정을 저장할 수 없어요. 게스트로 계속할 수 있어요.";
            }
        }

        private static Transform GetProfileMenuEntryParent(Transform canvasTransform)
        {
            return canvasTransform?.Find(
                CheeseTamaProfileMenuController.OverlayObjectName + "/Profile Card/Profile Entries");
        }

        private static Button GetOrMoveProfileEntryButton(
            Transform canvasTransform,
            Transform legacyParent,
            string name,
            string label,
            int entryIndex)
        {
            var entryParent = GetProfileMenuEntryParent(canvasTransform);
            if (entryParent == null)
            {
                return GetOrCreateTopLeftButton(
                    legacyParent != null ? legacyParent : canvasTransform,
                    name,
                    label,
                    Vector2.zero,
                    new Vector2(420f, 48f));
            }

            var current = entryParent.Find(name);
            var legacy = legacyParent != null ? legacyParent.Find(name) : null;
            if (current == null && legacy != null)
            {
                legacy.SetParent(entryParent, false);
                current = legacy;
            }
            else if (current != null && legacy != null && legacy != current)
            {
                DestroyObjectSafely(legacy.gameObject);
            }

            var button = current != null ? current.GetComponent<Button>() : null;
            if (button == null)
            {
                button = GetOrCreateTopLeftButton(
                    entryParent,
                    name,
                    label,
                    Vector2.zero,
                    new Vector2(420f, 48f));
            }

            ConfigureTopLeftButton(
                button,
                label,
                new Vector2(0f, -entryIndex * 58f),
                new Vector2(420f, 48f));
            return button;
        }

        private static Button GetOrMoveProfileRenameButton(
            Transform canvasTransform,
            Transform legacyParent)
        {
            var card = canvasTransform?.Find(
                CheeseTamaProfileMenuController.OverlayObjectName + "/Profile Card");
            if (card == null)
            {
                return GetOrCreateTopLeftButton(
                    legacyParent != null ? legacyParent : canvasTransform,
                    "Open Name Change Button",
                    "이름 변경",
                    Vector2.zero,
                    new Vector2(100f, 36f));
            }

            var entries = GetProfileMenuEntryParent(canvasTransform);
            var current = card.Find("Open Name Change Button");
            var legacy = legacyParent != null
                ? legacyParent.Find("Open Name Change Button")
                : null;
            var oldEntry = entries != null
                ? entries.Find("Open Name Change Button")
                : null;
            if (current == null)
            {
                var source = legacy != null ? legacy : oldEntry;
                if (source != null)
                {
                    source.SetParent(card, false);
                    current = source;
                }
            }

            if (legacy != null && legacy != current)
            {
                DestroyObjectSafely(legacy.gameObject);
            }

            if (oldEntry != null && oldEntry != current)
            {
                DestroyObjectSafely(oldEntry.gameObject);
            }

            var button = current != null ? current.GetComponent<Button>() : null;
            if (button == null)
            {
                button = GetOrCreateTopLeftButton(
                    card,
                    "Open Name Change Button",
                    "이름 변경",
                    Vector2.zero,
                    new Vector2(100f, 36f));
            }

            ConfigureTopLeftButton(
                button,
                "이름 변경",
                new Vector2(456f, -85f),
                new Vector2(100f, 36f));
            return button;
        }
    }
}
