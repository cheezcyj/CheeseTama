using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Input;
using CheeseTama.Platform.Accounts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTama.UI
{
    public enum AuthPanelScreen
    {
        Login,
        SignUp,
        Account,
        DeleteConfirmation
    }

    /// <summary>
    /// UI-safe result returned by an injected local-account authority adapter.
    /// Passwords and other credentials must never be placed in <see cref="Message"/>.
    /// </summary>
    public readonly struct AuthPanelOperationResult
    {
        public AuthPanelOperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string Message { get; }

        public static AuthPanelOperationResult Success(string message = "")
        {
            return new AuthPanelOperationResult(true, message);
        }

        public static AuthPanelOperationResult Failure(string message)
        {
            return new AuthPanelOperationResult(false, message);
        }
    }

    /// <summary>
    /// Presentation-only local-account modal. Authentication authority is supplied through
    /// delegates so this component never reads or mutates saves, account files, or GameManager.
    /// </summary>
    public sealed class AuthPanelController : MonoBehaviour
    {
        public const string OverlayObjectName = "Local Account Overlay";
        public const string DeleteConfirmationPhrase = "계정 지우기";
        public const float MaximumCompactCardScale = 1.75f;
        private const float CompactLayoutWidthThreshold = 1024f;
        private const float CompactLayoutHeightThreshold = 540f;
        private const float CompactLayoutPixelMargin = 12f;
        internal const float CompactCardHeight = 480f;
        internal const float FeedbackCardHeight = 580f;
        internal const float CompactCloseTop = 418f;
        internal const float FeedbackCloseTop = 518f;

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button entryButton;
        [SerializeField] private Button persistentEntryButton;
        [SerializeField] private Button startupStartButton;
        [SerializeField] private Button startupSignUpEntryButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private GameObject loginScreen;
        [SerializeField] private InputField loginEmailInput;
        [SerializeField] private InputField loginPasswordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button openSignUpButton;
        [SerializeField] private Button continueAsGuestButton;

        [SerializeField] private GameObject signUpScreen;
        [SerializeField] private InputField signUpEmailInput;
        [SerializeField] private InputField signUpPasswordInput;
        [SerializeField] private InputField signUpPasswordConfirmationInput;
        [SerializeField] private Button signUpButton;
        [SerializeField] private Button backToLoginButton;

        [SerializeField] private GameObject accountScreen;
        [SerializeField] private Text currentAccountLabel;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button openDeleteConfirmationButton;

        [SerializeField] private GameObject deleteConfirmationScreen;
        [SerializeField] private InputField deletePasswordInput;
        [SerializeField] private InputField deleteConfirmationInput;
        [SerializeField] private Button deleteAccountButton;
        [SerializeField] private Button cancelDeleteButton;

        [SerializeField] private Text statusLabel;
        [SerializeField] private Text errorLabel;

        private Func<string, string, string, AuthPanelOperationResult> registerAccountCommand;
        private Func<string, string, AuthPanelOperationResult> loginCommand;
        private Func<AuthPanelOperationResult> logoutCommand;
        private Func<string, string, AuthPanelOperationResult> deleteAccountCommand;
        private Func<bool> authenticatedProvider;
        private Func<string> currentAccountNameProvider;
        private Action<bool> blockingChanged;
        private Action beforeEntryOpen;
        private Action startupContinueCommand;

        private bool configured;
        private bool blockingNotified;
        private bool operationPending;
        private bool authenticationChoiceRequired;
        private GameObject previouslySelected;
        private RectTransform responsiveCard;
        private Vector3 responsiveCardBaseScale = Vector3.one;
        private readonly Dictionary<Text, ResponsiveTextBaseline> responsiveTextBaselines =
            new Dictionary<Text, ResponsiveTextBaseline>();

        private static bool startupGateResolved;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
        public bool IsBlockingGameplay => IsOpen;
        public bool IsAuthenticationChoiceRequired => authenticationChoiceRequired;
        public AuthPanelScreen CurrentScreen { get; private set; } = AuthPanelScreen.Login;
        public string RenderedStatus => statusLabel != null ? statusLabel.text : string.Empty;
        public string RenderedError => errorLabel != null ? errorLabel.text : string.Empty;
        public bool DeleteConfirmationMatches => MatchesDeleteConfirmation();
        public float CurrentResponsiveCardScale { get; private set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStartupGateState()
        {
            startupGateResolved = false;
        }

        public void Configure(
            GameObject root,
            Button profileEntryButton,
            Button dismissButton,
            GameObject loginView,
            InputField loginEmail,
            InputField loginPassword,
            Button submitLoginButton,
            Button showSignUpButton,
            GameObject signUpView,
            InputField signUpEmail,
            InputField signUpPassword,
            InputField signUpPasswordConfirmation,
            Button submitSignUpButton,
            Button showLoginButton,
            GameObject accountView,
            Text signedInAccountLabel,
            Button submitLogoutButton,
            Button showDeleteConfirmationButton,
            GameObject deleteView,
            InputField deletePassword,
            InputField deleteConfirmation,
            Button confirmDeleteButton,
            Button cancelDeleteConfirmationButton,
            Text status,
            Text error,
            Func<string, string, string, AuthPanelOperationResult> registerAccount,
            Func<string, string, AuthPanelOperationResult> authenticateAccount,
            Func<AuthPanelOperationResult> logoutAccount,
            Func<string, string, AuthPanelOperationResult> removeAccount,
            Func<bool> isAccountAuthenticated,
            Func<string> getCurrentAccountName,
            Action<bool> onBlockingChanged = null)
        {
            UnbindControls();
            SetOverlayActive(false);
            ClearSensitiveInputs();
            NotifyBlocking(false);

            overlayRoot = root;
            entryButton = profileEntryButton;
            persistentEntryButton = null;
            startupStartButton = null;
            startupSignUpEntryButton = null;
            startupContinueCommand = null;
            closeButton = dismissButton;

            loginScreen = loginView;
            loginEmailInput = loginEmail;
            loginPasswordInput = loginPassword;
            loginButton = submitLoginButton;
            openSignUpButton = showSignUpButton;
            continueAsGuestButton = null;

            signUpScreen = signUpView;
            signUpEmailInput = signUpEmail;
            signUpPasswordInput = signUpPassword;
            signUpPasswordConfirmationInput = signUpPasswordConfirmation;
            signUpButton = submitSignUpButton;
            backToLoginButton = showLoginButton;

            accountScreen = accountView;
            currentAccountLabel = signedInAccountLabel;
            logoutButton = submitLogoutButton;
            openDeleteConfirmationButton = showDeleteConfirmationButton;

            deleteConfirmationScreen = deleteView;
            deletePasswordInput = deletePassword;
            deleteConfirmationInput = deleteConfirmation;
            deleteAccountButton = confirmDeleteButton;
            cancelDeleteButton = cancelDeleteConfirmationButton;

            statusLabel = status;
            errorLabel = error;
            registerAccountCommand = registerAccount;
            loginCommand = authenticateAccount;
            logoutCommand = logoutAccount;
            deleteAccountCommand = removeAccount;
            authenticatedProvider = isAccountAuthenticated;
            currentAccountNameProvider = getCurrentAccountName;
            blockingChanged = onBlockingChanged;
            beforeEntryOpen = null;
            configured = overlayRoot != null;
            operationPending = false;
            authenticationChoiceRequired = false;
            previouslySelected = null;

            ConfigurePasswordMask(loginPasswordInput);
            ConfigurePasswordMask(signUpPasswordInput);
            ConfigurePasswordMask(signUpPasswordConfirmationInput);
            ConfigurePasswordMask(deletePasswordInput);
            ResolveResponsiveCard();
            ClearAllInputs();
            SetScreen(AuthPanelScreen.Login);
            SetStatus(string.Empty);
            SetError(string.Empty);
            SetOverlayActive(false);
            BindControls();
            RefreshControlStates();
        }

        /// <summary>
        /// Binds the always-visible login/account entry and the explicit guest choice.
        /// Reconfiguration remains listener-idempotent.
        /// </summary>
        public void ConfigureAdditionalNavigation(
            Button alwaysVisibleEntryButton,
            Button guestContinueButton)
        {
            UnbindControls();
            persistentEntryButton = alwaysVisibleEntryButton;
            continueAsGuestButton = guestContinueButton;
            BindControls();
            RefreshControlStates();
        }

        /// <summary>
        /// Supplies Boot controls that must remain unavailable while the first-run account
        /// choice is unresolved. The login entry is already owned through <see cref="entryButton"/>.
        /// </summary>
        public void ConfigureStartupGateControls(
            Button startButton,
            Button signUpEntryButton,
            Action continueToGame = null)
        {
            UnbindControls();
            startupStartButton = startButton;
            startupSignUpEntryButton = signUpEntryButton;
            startupContinueCommand = continueToGame;
            BindControls();
            RefreshControlStates();
        }

        /// <summary>
        /// Starts from the Boot home. A restored account or an already chosen guest session
        /// enters immediately; otherwise the explicit login/sign-up/guest choice opens.
        /// </summary>
        public void BeginFromHome()
        {
            if (operationPending || startupContinueCommand == null)
            {
                return;
            }

            if (ReadAuthenticatedWithoutError() || startupGateResolved)
            {
                startupContinueCommand.Invoke();
                return;
            }

            OpenInternal(true);
        }

        /// <summary>
        /// Returns from the Boot account modal to the intro without accepting a guest choice.
        /// The next Start press must therefore ask again.
        /// </summary>
        public void ReturnHome()
        {
            if (operationPending)
            {
                return;
            }

            authenticationChoiceRequired = false;
            SetStatus("홈으로 돌아왔어요. 준비되면 입장하기를 눌러 주세요.");
            RefreshControlStates();
            CloseInternal();
        }

        /// <summary>
        /// Supplies the parent-modal close action used by the profile entry. The entry listener
        /// remains owned by this controller, so repeated Builder configuration stays idempotent.
        /// </summary>
        public void ConfigureEntryNavigation(Action onBeforeOpen)
        {
            beforeEntryOpen = onBeforeOpen;
        }

        public void OpenFromEntry()
        {
            if (!IsOpen)
            {
                beforeEntryOpen?.Invoke();
            }

            OpenInternal(false);
        }

        public void Open()
        {
            OpenInternal(false);
        }

        /// <summary>
        /// Opens the one-time startup choice for a signed-out runtime session.
        /// It can only be resolved by authentication or the explicit guest button.
        /// </summary>
        public bool TryOpenStartupGate()
        {
            if (!configured || overlayRoot == null || startupGateResolved)
            {
                return false;
            }

            if (ReadAuthenticatedWithoutError())
            {
                startupGateResolved = true;
                authenticationChoiceRequired = false;
                RefreshControlStates();
                return false;
            }

            OpenInternal(true);
            return IsOpen;
        }

        private void OpenInternal(bool requireAuthenticationChoice)
        {
            if (!configured || overlayRoot == null)
            {
                SetError("계정 화면을 열지 못했어요. 다시 눌러 주세요.");
                return;
            }

            var currentlyAuthenticated = ReadAuthenticatedWithoutError();
            authenticationChoiceRequired = requireAuthenticationChoice
                || (authenticationChoiceRequired && !currentlyAuthenticated);

            if (!IsOpen)
            {
                previouslySelected = EventSystem.current?.currentSelectedGameObject;
            }

            ClearSensitiveInputs();
            SetError(string.Empty);
            SetOverlayActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (TryReadAuthenticated(out var authenticated))
            {
                SetScreen(authenticated ? AuthPanelScreen.Account : AuthPanelScreen.Login);
                SetStatus(authenticated
                    ? "지금 이 계정으로 로그인되어 있어요."
                    : string.Empty);
            }
            else
            {
                SetScreen(AuthPanelScreen.Login);
            }

            NotifyBlocking(true);
            ApplyResponsiveLayoutFromScreen();
            FocusCurrentScreen();
        }

        /// <summary>
        /// Enlarges the centered account card on compact screens while keeping it inside the
        /// available viewport. The canvas still owns global scaling; this is a local readability
        /// correction for the credential form only.
        /// </summary>
        public float ApplyResponsiveLayout(
            int availablePixelWidth,
            int availablePixelHeight,
            float canvasScaleFactor)
        {
            ResolveResponsiveCard();
            ApplyFeedbackLayoutGeometry();
            if (responsiveCard == null)
            {
                CurrentResponsiveCardScale = 1f;
                return CurrentResponsiveCardScale;
            }

            var pixelWidth = Mathf.Max(1f, availablePixelWidth);
            var pixelHeight = Mathf.Max(1f, availablePixelHeight);
            var canvasScale = Mathf.Max(0.0001f, canvasScaleFactor);
            var compact = pixelWidth <= CompactLayoutWidthThreshold
                || pixelHeight <= CompactLayoutHeightThreshold;
            var resolvedScale = 1f;
            if (compact)
            {
                var baseWidth = Mathf.Max(
                    1f,
                    responsiveCard.rect.width * canvasScale * Mathf.Abs(responsiveCardBaseScale.x));
                var baseHeight = Mathf.Max(
                    1f,
                    responsiveCard.rect.height * canvasScale * Mathf.Abs(responsiveCardBaseScale.y));
                var widthFit = Mathf.Max(1f, pixelWidth - CompactLayoutPixelMargin * 2f) / baseWidth;
                var heightFit = Mathf.Max(1f, pixelHeight - CompactLayoutPixelMargin) / baseHeight;
                resolvedScale = Mathf.Clamp(
                    Mathf.Min(MaximumCompactCardScale, widthFit, heightFit),
                    1f,
                    MaximumCompactCardScale);
            }

            CurrentResponsiveCardScale = resolvedScale;
            responsiveCard.localScale = Vector3.Scale(
                responsiveCardBaseScale,
                new Vector3(resolvedScale, resolvedScale, 1f));
            ApplyResponsiveTypography(compact, canvasScale, resolvedScale);
            return resolvedScale;
        }

        private void ApplyResponsiveTypography(bool compact, float canvasScale, float cardScale)
        {
            if (responsiveCard == null)
            {
                return;
            }

            var effectiveScale = Mathf.Max(0.0001f, canvasScale * cardScale);
            var compactMinimum = compact
                ? Mathf.CeilToInt(13f / effectiveScale)
                : 1;
            var textScale = CheeseTama.Save.GameSettingsSaveData.NormalizeTextScale(
                AccessibilityRuntime.TextScale);
            var labels = responsiveCard.GetComponentsInChildren<Text>(true);
            foreach (var label in labels)
            {
                if (label == null)
                {
                    continue;
                }

                if (!responsiveTextBaselines.TryGetValue(label, out var baseline))
                {
                    var accessibilityProfile = label.GetComponent<AccessibilityTextProfile>();
                    baseline = new ResponsiveTextBaseline(
                        accessibilityProfile != null
                            ? accessibilityProfile.BaseFontSize
                            : Mathf.Max(1, label.fontSize),
                        label.resizeTextForBestFit,
                        accessibilityProfile != null
                            ? accessibilityProfile.BaseBestFitMinSize
                            : Mathf.Max(1, label.resizeTextMinSize),
                        accessibilityProfile != null
                            ? accessibilityProfile.BaseBestFitMaxSize
                            : Mathf.Max(label.resizeTextMinSize, label.resizeTextMaxSize));
                    responsiveTextBaselines[label] = baseline;
                }

                var fontSize = Mathf.Max(
                    compactMinimum,
                    Mathf.RoundToInt(Mathf.Max(1, baseline.FontSize) * textScale));

                label.fontSize = fontSize;
                if (!baseline.BestFit)
                {
                    continue;
                }

                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(
                    compactMinimum,
                    Mathf.RoundToInt(Mathf.Max(1, baseline.MinimumSize) * textScale));
                label.resizeTextMaxSize = Mathf.Max(
                    label.resizeTextMinSize,
                    Mathf.RoundToInt(
                        Mathf.Max(baseline.MinimumSize, baseline.MaximumSize) * textScale));
            }
        }

        private readonly struct ResponsiveTextBaseline
        {
            public ResponsiveTextBaseline(
                int fontSize,
                bool bestFit,
                int minimumSize,
                int maximumSize)
            {
                FontSize = fontSize;
                BestFit = bestFit;
                MinimumSize = minimumSize;
                MaximumSize = maximumSize;
            }

            public int FontSize { get; }
            public bool BestFit { get; }
            public int MinimumSize { get; }
            public int MaximumSize { get; }
        }

        public void Close()
        {
            if (authenticationChoiceRequired && !ReadAuthenticatedWithoutError())
            {
                SetStatus("로그인, 새 계정 만들기, 게스트 중 하나를 골라 주세요.");
                RefreshControlStates();
                FocusCurrentScreen();
                return;
            }

            CloseInternal();
        }

        public void ContinueAsGuest()
        {
            if (operationPending)
            {
                return;
            }

            startupGateResolved = true;
            authenticationChoiceRequired = false;
            SetStatus("게스트로 시작해요. 게임 기록은 이 브라우저에 저장돼요.");
            RefreshControlStates();
            CloseInternal();
            startupContinueCommand?.Invoke();
        }

        private void CloseInternal()
        {
            var restoreSelection = IsOpen;
            SetOverlayActive(false);
            ClearSensitiveInputs();
            operationPending = false;
            NotifyBlocking(false);

            if (restoreSelection
                && EventSystem.current != null
                && previouslySelected != null
                && previouslySelected.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previouslySelected);
            }

            previouslySelected = null;
        }

        public void HandleCancel()
        {
            if (IsOpen)
            {
                if (authenticationChoiceRequired && !ReadAuthenticatedWithoutError())
                {
                    if (startupContinueCommand != null)
                    {
                        ReturnHome();
                        return;
                    }

                    SetStatus("로그인, 새 계정 만들기, 게스트 중 하나를 골라 주세요.");
                    RefreshControlStates();
                    FocusCurrentScreen();
                    return;
                }

                Close();
            }
        }

        public void ShowLogin()
        {
            ClearSensitiveInputs();
            SetError(string.Empty);
            SetStatus(string.Empty);
            SetScreen(AuthPanelScreen.Login);
            FocusCurrentScreen();
        }

        public void ShowSignUp()
        {
            if (ReadAuthenticatedWithoutError())
            {
                SetScreen(AuthPanelScreen.Account);
                SetStatus("이미 계정으로 로그인되어 있어요.");
                FocusCurrentScreen();
                return;
            }

            ClearSensitiveInputs();
            SetError(string.Empty);
            SetStatus(string.Empty);
            SetScreen(AuthPanelScreen.SignUp);
            FocusCurrentScreen();
        }

        public void ShowDeleteConfirmation()
        {
            if (!ReadAuthenticatedWithoutError())
            {
                SetScreen(AuthPanelScreen.Login);
                SetError("계정을 지우려면 먼저 로그인해 주세요.");
                FocusCurrentScreen();
                return;
            }

            ClearInput(deletePasswordInput);
            ClearDeleteConfirmation();
            SetError(string.Empty);
            SetStatus(
                $"현재 비밀번호를 쓰고, 확인 칸에 ‘{DeleteConfirmationPhrase}’라고 입력해 주세요.");
            SetScreen(AuthPanelScreen.DeleteConfirmation);
            FocusCurrentScreen();
        }

        public void CancelDeleteConfirmation()
        {
            ClearInput(deletePasswordInput);
            ClearDeleteConfirmation();
            SetError(string.Empty);
            SetStatus("계정 지우기를 취소했어요.");
            SetScreen(AuthPanelScreen.Account);
            FocusCurrentScreen();
        }

        public void SubmitLogin()
        {
            if (operationPending)
            {
                return;
            }

            var email = NormalizeEmail(loginEmailInput?.text);
            var password = loginPasswordInput?.text ?? string.Empty;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetError("아이디와 비밀번호를 모두 입력해 주세요.");
                RefreshControlStates();
                return;
            }

            if (!IsPasswordLengthAllowed(password))
            {
                SetError(LocalAccountPasswordPolicy.Description);
                RefreshControlStates();
                return;
            }

            if (loginCommand == null)
            {
                ClearInput(loginPasswordInput);
                SetError("지금은 로그인할 수 없어요. 잠시 뒤 다시 해 주세요.");
                RefreshControlStates();
                return;
            }

            ExecuteOperation(
                () => loginCommand(email, password),
                "로그인했어요.",
                "로그인하지 못했어요.",
                closeWhenAuthenticated: true);
        }

        public void SubmitSignUp()
        {
            if (operationPending)
            {
                return;
            }

            var email = NormalizeEmail(signUpEmailInput?.text);
            var password = signUpPasswordInput?.text ?? string.Empty;
            var confirmation = signUpPasswordConfirmationInput?.text ?? string.Empty;
            if (string.IsNullOrEmpty(email)
                || string.IsNullOrEmpty(password)
                || string.IsNullOrEmpty(confirmation))
            {
                SetError("아이디, 비밀번호, 비밀번호 확인을 모두 입력해 주세요.");
                RefreshControlStates();
                return;
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                SetError("두 비밀번호가 달라요. 똑같이 입력해 주세요.");
                RefreshControlStates();
                return;
            }

            if (!IsPasswordLengthAllowed(password))
            {
                SetError(LocalAccountPasswordPolicy.Description);
                RefreshControlStates();
                return;
            }

            if (registerAccountCommand == null)
            {
                ClearInput(signUpPasswordInput);
                ClearInput(signUpPasswordConfirmationInput);
                SetError("지금은 새 계정을 만들 수 없어요. 잠시 뒤 다시 해 주세요.");
                RefreshControlStates();
                return;
            }

            ExecuteOperation(
                () => registerAccountCommand(email, password, confirmation),
                "새 계정을 만들었어요.",
                "새 계정을 만들지 못했어요.",
                closeWhenAuthenticated: true);
        }

        public void SubmitLogout()
        {
            if (operationPending)
            {
                return;
            }

            if (logoutCommand == null)
            {
                SetError("지금은 로그아웃할 수 없어요. 잠시 뒤 다시 해 주세요.");
                return;
            }

            ExecuteOperation(
                logoutCommand,
                "로그아웃했어요.",
                "로그아웃하지 못했어요.",
                requireChoiceWhenSignedOut: true);
        }

        public void SubmitDeleteAccount()
        {
            if (operationPending)
            {
                return;
            }

            var password = deletePasswordInput?.text ?? string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                SetError("계정을 지우려면 현재 비밀번호를 입력해 주세요.");
                RefreshControlStates();
                return;
            }

            if (!IsPasswordLengthAllowed(password))
            {
                SetError(LocalAccountPasswordPolicy.Description);
                RefreshControlStates();
                return;
            }

            if (!MatchesDeleteConfirmation())
            {
                SetError($"확인 칸에 ‘{DeleteConfirmationPhrase}’라고 입력해 주세요.");
                RefreshControlStates();
                return;
            }

            if (deleteAccountCommand == null)
            {
                SetError("지금은 계정을 지울 수 없어요. 잠시 뒤 다시 해 주세요.");
                return;
            }

            ExecuteOperation(
                () => deleteAccountCommand(
                    password,
                    CheeseTama.Platform.Accounts.LocalAccountRuntime.DeleteConfirmationText),
                "계정과 계정의 게임 기록을 지웠어요.",
                "계정을 지우지 못했어요.",
                requireChoiceWhenSignedOut: true);
        }

        public void RefreshSession()
        {
            RefreshSessionScreen();
            RefreshControlStates();
        }

        private void OnEnable()
        {
            if (!configured)
            {
                return;
            }

            BindControls();
            RefreshControlStates();
            if (authenticationChoiceRequired && !ReadAuthenticatedWithoutError())
            {
                OpenInternal(true);
                return;
            }

            if (IsOpen)
            {
                NotifyBlocking(true);
            }
        }

        private void OnDisable()
        {
            UnbindControls();
            SetOverlayActive(false);
            ClearSensitiveInputs();
            operationPending = false;
            NotifyBlocking(false);
        }

        private void OnDestroy()
        {
            UnbindControls();
            NotifyBlocking(false);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            ApplyResponsiveLayoutFromScreen();
            if (GameInputRouter.WasPressed(GameInputActionIds.Cancel))
            {
                HandleCancel();
                return;
            }

            if ((UnityEngine.Input.GetKeyDown(KeyCode.Return)
                 || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                && IsCredentialInputFocused())
            {
                TrySubmitCurrentForm();
            }
        }

        public bool TrySubmitCurrentForm()
        {
            if (!IsOpen || operationPending)
            {
                return false;
            }

            if (CurrentScreen == AuthPanelScreen.Login && loginButton != null && loginButton.interactable)
            {
                SubmitLogin();
                return true;
            }

            if (CurrentScreen == AuthPanelScreen.SignUp && signUpButton != null && signUpButton.interactable)
            {
                SubmitSignUp();
                return true;
            }

            return false;
        }

        private void ExecuteOperation(
            Func<AuthPanelOperationResult> operation,
            string successFallback,
            string failureFallback,
            bool closeWhenAuthenticated = false,
            bool requireChoiceWhenSignedOut = false)
        {
            operationPending = true;
            SetError(string.Empty);
            RefreshControlStates();

            AuthPanelOperationResult result;
            try
            {
                result = operation();
            }
            catch
            {
                result = AuthPanelOperationResult.Failure(
                    "계정 작업을 끝내지 못했어요. 다시 시도해 주세요.");
            }
            finally
            {
                operationPending = false;
                ClearSensitiveInputs();
            }

            if (!result.Succeeded)
            {
                SetError(string.IsNullOrWhiteSpace(result.Message)
                    ? failureFallback
                    : result.Message);
                RefreshControlStates();
                FocusCurrentScreen();
                return;
            }

            SetError(string.Empty);
            SetStatus(string.IsNullOrWhiteSpace(result.Message)
                ? successFallback
                : result.Message);
            RefreshSessionScreen();
            var authenticated = ReadAuthenticatedWithoutError();
            var continueFromBoot = authenticated && startupContinueCommand != null;
            if (authenticated)
            {
                startupGateResolved = true;
                authenticationChoiceRequired = false;
            }
            else if (requireChoiceWhenSignedOut)
            {
                authenticationChoiceRequired = true;
            }

            RefreshControlStates();
            if (closeWhenAuthenticated && authenticated)
            {
                CloseInternal();
                if (continueFromBoot)
                {
                    startupContinueCommand.Invoke();
                }
                return;
            }

            FocusCurrentScreen();
        }

        private void RefreshSessionScreen()
        {
            if (!TryReadAuthenticated(out var authenticated))
            {
                SetScreen(AuthPanelScreen.Login);
                return;
            }

            SetScreen(authenticated ? AuthPanelScreen.Account : AuthPanelScreen.Login);
        }

        private bool TryReadAuthenticated(out bool authenticated)
        {
            authenticated = false;
            if (authenticatedProvider == null)
            {
                return true;
            }

            try
            {
                authenticated = authenticatedProvider();
                return true;
            }
            catch
            {
                SetError("어떤 계정으로 로그인했는지 확인하지 못했어요.");
                return false;
            }
        }

        private bool ReadAuthenticatedWithoutError()
        {
            if (authenticatedProvider == null)
            {
                return false;
            }

            try
            {
                return authenticatedProvider();
            }
            catch
            {
                return false;
            }
        }

        private void ResolveResponsiveCard()
        {
            var candidate = loginScreen != null ? loginScreen.transform.parent as RectTransform : null;
            if (candidate == null || (overlayRoot != null && candidate == overlayRoot.transform))
            {
                responsiveCard = null;
                responsiveCardBaseScale = Vector3.one;
                CurrentResponsiveCardScale = 1f;
                return;
            }

            if (responsiveCard == candidate)
            {
                return;
            }

            responsiveCard = candidate;
            responsiveCardBaseScale = candidate.localScale;
            CurrentResponsiveCardScale = 1f;
        }

        private void ApplyResponsiveLayoutFromScreen()
        {
            if (responsiveCard == null)
            {
                ResolveResponsiveCard();
            }

            var rootCanvas = overlayRoot != null ? overlayRoot.GetComponentInParent<Canvas>() : null;
            var safeArea = Screen.safeArea;
            ApplyResponsiveLayout(
                Mathf.RoundToInt(safeArea.width),
                Mathf.RoundToInt(safeArea.height),
                rootCanvas != null ? rootCanvas.scaleFactor : 1f);
        }

        private bool IsCredentialInputFocused()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null)
            {
                return false;
            }

            if (CurrentScreen == AuthPanelScreen.Login)
            {
                return selected == loginEmailInput?.gameObject
                    || selected == loginPasswordInput?.gameObject;
            }

            if (CurrentScreen == AuthPanelScreen.SignUp)
            {
                return selected == signUpEmailInput?.gameObject
                    || selected == signUpPasswordInput?.gameObject
                    || selected == signUpPasswordConfirmationInput?.gameObject;
            }

            return false;
        }

        private void SetScreen(AuthPanelScreen screen)
        {
            CurrentScreen = screen;
            SetActive(loginScreen, screen == AuthPanelScreen.Login);
            SetActive(signUpScreen, screen == AuthPanelScreen.SignUp);
            SetActive(accountScreen, screen == AuthPanelScreen.Account);
            SetActive(deleteConfirmationScreen, screen == AuthPanelScreen.DeleteConfirmation);
            RefreshAccountLabel();
            RefreshControlStates();
        }

        private void RefreshAccountLabel()
        {
            if (currentAccountLabel == null)
            {
                return;
            }

            var accountName = string.Empty;
            if (CurrentScreen == AuthPanelScreen.Account
                || CurrentScreen == AuthPanelScreen.DeleteConfirmation)
            {
                try
                {
                    accountName = currentAccountNameProvider?.Invoke() ?? string.Empty;
                }
                catch
                {
                    accountName = string.Empty;
                }
            }

            AccessibilityRuntime.SetTextAndApply(
                currentAccountLabel,
                string.IsNullOrWhiteSpace(accountName)
                    ? "로그인한 계정"
                    : $"로그인 중 · {accountName.Trim()}");
        }

        private void FocusCurrentScreen()
        {
            if (!IsOpen || EventSystem.current == null)
            {
                return;
            }

            Selectable target = CurrentScreen switch
            {
                AuthPanelScreen.Login => loginEmailInput,
                AuthPanelScreen.SignUp => signUpEmailInput,
                AuthPanelScreen.Account => logoutButton,
                AuthPanelScreen.DeleteConfirmation => deletePasswordInput,
                _ => closeButton
            };
            if (target != null && target.gameObject.activeInHierarchy && target.interactable)
            {
                EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

        private void BindControls()
        {
            UnbindControls();
            entryButton?.onClick.AddListener(OpenFromEntry);
            persistentEntryButton?.onClick.AddListener(OpenFromEntry);
            startupStartButton?.onClick.AddListener(BeginFromHome);
            closeButton?.onClick.AddListener(HandleCloseAction);
            continueAsGuestButton?.onClick.AddListener(ContinueAsGuest);
            loginButton?.onClick.AddListener(SubmitLogin);
            openSignUpButton?.onClick.AddListener(ShowSignUp);
            signUpButton?.onClick.AddListener(SubmitSignUp);
            backToLoginButton?.onClick.AddListener(ShowLogin);
            logoutButton?.onClick.AddListener(SubmitLogout);
            openDeleteConfirmationButton?.onClick.AddListener(ShowDeleteConfirmation);
            deleteAccountButton?.onClick.AddListener(SubmitDeleteAccount);
            cancelDeleteButton?.onClick.AddListener(CancelDeleteConfirmation);

            loginEmailInput?.onValueChanged.AddListener(HandleLoginInputChanged);
            loginPasswordInput?.onValueChanged.AddListener(HandleLoginInputChanged);
            signUpEmailInput?.onValueChanged.AddListener(HandleSignUpInputChanged);
            signUpPasswordInput?.onValueChanged.AddListener(HandleSignUpInputChanged);
            signUpPasswordConfirmationInput?.onValueChanged.AddListener(HandleSignUpInputChanged);
            deletePasswordInput?.onValueChanged.AddListener(HandleDeleteConfirmationChanged);
            deleteConfirmationInput?.onValueChanged.AddListener(HandleDeleteConfirmationChanged);
        }

        private void UnbindControls()
        {
            entryButton?.onClick.RemoveListener(OpenFromEntry);
            persistentEntryButton?.onClick.RemoveListener(OpenFromEntry);
            startupStartButton?.onClick.RemoveListener(BeginFromHome);
            closeButton?.onClick.RemoveListener(HandleCloseAction);
            continueAsGuestButton?.onClick.RemoveListener(ContinueAsGuest);
            loginButton?.onClick.RemoveListener(SubmitLogin);
            openSignUpButton?.onClick.RemoveListener(ShowSignUp);
            signUpButton?.onClick.RemoveListener(SubmitSignUp);
            backToLoginButton?.onClick.RemoveListener(ShowLogin);
            logoutButton?.onClick.RemoveListener(SubmitLogout);
            openDeleteConfirmationButton?.onClick.RemoveListener(ShowDeleteConfirmation);
            deleteAccountButton?.onClick.RemoveListener(SubmitDeleteAccount);
            cancelDeleteButton?.onClick.RemoveListener(CancelDeleteConfirmation);

            loginEmailInput?.onValueChanged.RemoveListener(HandleLoginInputChanged);
            loginPasswordInput?.onValueChanged.RemoveListener(HandleLoginInputChanged);
            signUpEmailInput?.onValueChanged.RemoveListener(HandleSignUpInputChanged);
            signUpPasswordInput?.onValueChanged.RemoveListener(HandleSignUpInputChanged);
            signUpPasswordConfirmationInput?.onValueChanged.RemoveListener(HandleSignUpInputChanged);
            deletePasswordInput?.onValueChanged.RemoveListener(HandleDeleteConfirmationChanged);
            deleteConfirmationInput?.onValueChanged.RemoveListener(HandleDeleteConfirmationChanged);
        }

        private void HandleLoginInputChanged(string _)
        {
            SetError(string.Empty);
            RefreshControlStates();
        }

        private void HandleCloseAction()
        {
            if (startupContinueCommand != null)
            {
                ReturnHome();
                return;
            }

            Close();
        }

        private void HandleSignUpInputChanged(string _)
        {
            var password = signUpPasswordInput?.text ?? string.Empty;
            var confirmation = signUpPasswordConfirmationInput?.text ?? string.Empty;
            SetError(!string.IsNullOrEmpty(password)
                && !string.IsNullOrEmpty(confirmation)
                && !string.Equals(password, confirmation, StringComparison.Ordinal)
                    ? "두 비밀번호가 달라요. 똑같이 입력해 주세요."
                    : string.Empty);
            RefreshControlStates();
        }

        private void HandleDeleteConfirmationChanged(string _)
        {
            SetError(string.Empty);
            RefreshControlStates();
        }

        private void RefreshControlStates()
        {
            var authenticated = ReadAuthenticatedWithoutError();
            var startupGateBlocksExternalControls = authenticationChoiceRequired && !authenticated;
            if (persistentEntryButton != null)
            {
                persistentEntryButton.interactable = !operationPending
                    && !startupGateBlocksExternalControls;
                SetButtonText(persistentEntryButton, authenticated ? "계정" : "로그인");
            }

            if (entryButton != null)
            {
                entryButton.interactable = !operationPending
                    && !startupGateBlocksExternalControls;
            }

            if (startupStartButton != null)
            {
                startupStartButton.interactable = !operationPending
                    && !startupGateBlocksExternalControls;
            }

            if (startupSignUpEntryButton != null)
            {
                startupSignUpEntryButton.interactable = !operationPending
                    && !startupGateBlocksExternalControls;
            }

            if (continueAsGuestButton != null)
            {
                SetActive(
                    continueAsGuestButton.gameObject,
                    !authenticated && CurrentScreen == AuthPanelScreen.Login);
                continueAsGuestButton.interactable = !operationPending;
            }

            if (closeButton != null)
            {
                var canClose = startupContinueCommand != null
                    || !authenticationChoiceRequired
                    || authenticated;
                SetActive(closeButton.gameObject, canClose);
                closeButton.interactable = !operationPending && canClose;
            }

            if (loginButton != null)
            {
                loginButton.interactable = !operationPending
                    && !string.IsNullOrEmpty(NormalizeEmail(loginEmailInput?.text))
                    && IsPasswordLengthAllowed(loginPasswordInput?.text);
            }

            if (signUpButton != null)
            {
                var password = signUpPasswordInput?.text ?? string.Empty;
                signUpButton.interactable = !operationPending
                    && !string.IsNullOrEmpty(NormalizeEmail(signUpEmailInput?.text))
                    && IsPasswordLengthAllowed(password)
                    && string.Equals(
                        password,
                        signUpPasswordConfirmationInput?.text,
                        StringComparison.Ordinal);
            }

            if (logoutButton != null)
            {
                logoutButton.interactable = !operationPending;
            }

            if (openDeleteConfirmationButton != null)
            {
                openDeleteConfirmationButton.interactable = !operationPending;
            }

            if (deleteAccountButton != null)
            {
                deleteAccountButton.interactable = !operationPending
                    && IsPasswordLengthAllowed(deletePasswordInput?.text)
                    && MatchesDeleteConfirmation();
            }
        }

        private bool MatchesDeleteConfirmation()
        {
            return deleteConfirmationInput != null
                && string.Equals(
                    deleteConfirmationInput.text,
                    DeleteConfirmationPhrase,
                    StringComparison.Ordinal);
        }

        private void ClearAllInputs()
        {
            ClearInput(loginEmailInput);
            ClearInput(signUpEmailInput);
            ClearSensitiveInputs();
        }

        private void ClearSensitiveInputs()
        {
            ClearInput(loginPasswordInput);
            ClearInput(signUpPasswordInput);
            ClearInput(signUpPasswordConfirmationInput);
            ClearInput(deletePasswordInput);
            ClearDeleteConfirmation();
        }

        private void ClearDeleteConfirmation()
        {
            ClearInput(deleteConfirmationInput);
        }

        private void SetOverlayActive(bool active)
        {
            if (overlayRoot != null && overlayRoot.activeSelf != active)
            {
                overlayRoot.SetActive(active);
            }
        }

        private void SetStatus(string value)
        {
            if (statusLabel != null)
            {
                AccessibilityRuntime.SetTextAndApply(statusLabel, value ?? string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(value) && errorLabel != null)
            {
                AccessibilityRuntime.SetTextAndApply(errorLabel, string.Empty);
            }

            RefreshFeedbackPresentation();
        }

        private void SetError(string value)
        {
            if (errorLabel != null)
            {
                AccessibilityRuntime.SetTextAndApply(errorLabel, value ?? string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(value) && statusLabel != null)
            {
                AccessibilityRuntime.SetTextAndApply(statusLabel, string.Empty);
            }

            RefreshFeedbackPresentation();
        }

        private void RefreshFeedbackPresentation()
        {
            ResolveResponsiveCard();
            ApplyFeedbackLayoutGeometry();
            if (IsOpen)
            {
                ApplyResponsiveLayoutFromScreen();
            }
        }

        private void ApplyFeedbackLayoutGeometry()
        {
            var hasStatus = statusLabel != null
                && !string.IsNullOrWhiteSpace(statusLabel.text);
            var hasError = errorLabel != null
                && !string.IsNullOrWhiteSpace(errorLabel.text);
            SetActive(statusLabel != null ? statusLabel.gameObject : null, hasStatus);
            SetActive(errorLabel != null ? errorLabel.gameObject : null, hasError);

            if (responsiveCard == null)
            {
                return;
            }

            var hasFeedback = hasStatus || hasError;
            responsiveCard.sizeDelta = new Vector2(
                responsiveCard.sizeDelta.x,
                hasFeedback ? FeedbackCardHeight : CompactCardHeight);
            var closeRect = closeButton != null ? closeButton.transform as RectTransform : null;
            if (closeRect != null)
            {
                closeRect.anchoredPosition = new Vector2(
                    closeRect.anchoredPosition.x,
                    -(hasFeedback ? FeedbackCloseTop : CompactCloseTop));
            }
        }

        private void NotifyBlocking(bool blocked)
        {
            if (blockingNotified == blocked)
            {
                return;
            }

            blockingNotified = blocked;
            blockingChanged?.Invoke(blocked);
        }

        private static void ConfigurePasswordMask(InputField input)
        {
            if (input == null)
            {
                return;
            }

            input.contentType = InputField.ContentType.Password;
            input.asteriskChar = '•';
            input.ForceLabelUpdate();
        }

        private static string NormalizeEmail(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static bool IsPasswordLengthAllowed(string value)
        {
            return value != null
                && value.Length >= LocalAccountPasswordPolicy.MinimumLength
                && value.Length <= LocalAccountPasswordPolicy.MaximumLength;
        }

        private static void ClearInput(InputField input)
        {
            if (input != null && !string.IsNullOrEmpty(input.text))
            {
                input.SetTextWithoutNotify(string.Empty);
                input.ForceLabelUpdate();
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            var label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label != null)
            {
                AccessibilityRuntime.SetTextAndApply(label, value ?? string.Empty);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
