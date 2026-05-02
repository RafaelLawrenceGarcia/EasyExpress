// ============================================================
//  AuthManager.cs — REFACTORED (Self-Contained)
//  Easy Express — Authentication System
// ============================================================
//  Location: Assets/Script/Auth/AuthManager.cs
//
//  Manages PlayFab login, registration, and guest sessions.
//  Manages ONLY its own login/register UI overlay.
//  Publishes events — never touches other systems directly.
//
//  DEPENDENCIES: Core/GameEventBus, Core/AuthEvents, Core/GameSession
//  DEPENDS ON:   nothing else — fully independent
//
//  INSPECTOR:
//    - loginOverlay, loginPanel, registerPanel
//    - Input fields for login/register
//    - playOfflineButton
//    (No mainCanvas, no authManager cross-references)
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    [Header("Login Overlay (this system's own UI)")]
    public GameObject loginOverlay;

    [Header("Panel Switching")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Login Inputs")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginMessageText;

    [Header("Register Inputs")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;
    public TMP_Text registerMessageText;

    [Header("Show Password Toggles")]
    public Toggle loginShowPasswordToggle;
    public Toggle registerShowPasswordToggle;

    [Header("Guest / Offline Play")]
    public Button playOfflineButton;
    public string offlineSceneName = "";

    // ═══════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void OnEnable()
    {
        GameEventBus.Subscribe<LogoutRequestedEvent>(OnLogoutRequested);
    }

    void OnDisable()
    {
        GameEventBus.Unsubscribe<LogoutRequestedEvent>(OnLogoutRequested);
    }

    void Start()
    {
        PlayFabSettings.TitleId = "164227";
        PlayFabSettings.RequestType = PlayFab.WebRequestType.UnityWebRequest;

        SetPasswordHidden(loginPasswordInput);
        SetPasswordHidden(registerPasswordInput);
        SetPasswordHidden(registerConfirmPasswordInput);

        if (playOfflineButton != null)
            playOfflineButton.onClick.AddListener(PlayOffline);

        if (loginShowPasswordToggle != null)
        {
            loginShowPasswordToggle.isOn = false;
            loginShowPasswordToggle.onValueChanged.AddListener(OnLoginShowPasswordChanged);
        }
        if (registerShowPasswordToggle != null)
        {
            registerShowPasswordToggle.isOn = false;
            registerShowPasswordToggle.onValueChanged.AddListener(OnRegisterShowPasswordChanged);
        }

        if (GameSession.IsLoggedIn || GameSession.IsGuest)
        {
            if (loginOverlay != null) loginOverlay.SetActive(false);

            if (GameSession.IsLoggedIn)
                GameEventBus.Publish(new LoginSuccessEvent());
            else
                GameEventBus.Publish(new GuestLoginEvent());
        }
        else
        {
            ShowLoginOverlay();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  OWN UI MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    void ShowLoginOverlay()
    {
        if (loginOverlay != null) loginOverlay.SetActive(true);
        ShowLoginPanel();
    }

    void HideLoginOverlay()
    {
        if (loginOverlay != null) loginOverlay.SetActive(false);
    }

    public void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (loginMessageText != null)
        {
            loginMessageText.text = "Please Login";
            loginMessageText.color = Color.white;
        }
        if (loginShowPasswordToggle != null) loginShowPasswordToggle.isOn = false;
    }

    public void ShowRegisterPanel()
    {
        Application.OpenURL("https://easy-express-sites-rafcows-projects.vercel.app/");
    }

    public void BackToLogin() => ShowLoginPanel();

    public void ForgotPassword()
    {
        Application.OpenURL("https://easy-express-sites-rafcows-projects.vercel.app/#/reset-password");
    }

    // ═══════════════════════════════════════════════════════════
    //  LOGIN
    // ═══════════════════════════════════════════════════════════

    public void LoginButton()
    {
        if (loginEmailInput.text.Length < 4)
        {
            loginMessageText.text = "Invalid Email";
            loginMessageText.color = Color.red;
            return;
        }
        if (loginPasswordInput.text.Length < 6)
        {
            loginMessageText.text = "Password too short";
            loginMessageText.color = Color.red;
            return;
        }

        loginMessageText.text = "Authenticating...";
        loginMessageText.color = Color.white;

        var request = new LoginWithEmailAddressRequest
        {
            Email = loginEmailInput.text,
            Password = loginPasswordInput.text
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginError);
    }

    void OnLoginSuccess(LoginResult result)
    {
        GameSession.StartCloudSession();

        if (gameObject.activeInHierarchy)
            StartCoroutine(LoginSequence("Login Verified."));
        else
            FinalizeLogin();
    }

    void OnLoginError(PlayFabError error)
    {
        if (loginMessageText != null)
        {
            loginMessageText.color = Color.red;
            loginMessageText.text = "Error: " + error.ErrorMessage;
        }
        Debug.LogWarning("[AuthManager] Login failed: " + error.GenerateErrorReport());
    }

    IEnumerator LoginSequence(string startMessage)
    {
        loginMessageText.color = Color.green;
        loginMessageText.text = startMessage;
        yield return new WaitForSeconds(1.0f);

        loginMessageText.color = Color.white;
        loginMessageText.text = "Loading Player Profile...";
        yield return new WaitForSeconds(1.5f);

        loginMessageText.text = "Welcome!";
        yield return new WaitForSeconds(1.0f);

        FinalizeLogin();
    }

    void FinalizeLogin()
    {
        HideLoginOverlay();
        GameEventBus.Publish(new LoginSuccessEvent());
        Debug.Log("[AuthManager] Login finalized — LoginSuccessEvent published.");
    }

    // ═══════════════════════════════════════════════════════════
    //  REGISTRATION
    // ═══════════════════════════════════════════════════════════

    public void RegisterButton()
    {
        if (registerUsernameInput != null && registerUsernameInput.text.Length < 3)
        {
            registerMessageText.text = "Username must be at least 3 characters";
            registerMessageText.color = Color.red;
            return;
        }
        if (registerEmailInput.text.Length < 4)
        {
            registerMessageText.text = "Invalid Email";
            registerMessageText.color = Color.red;
            return;
        }
        if (registerPasswordInput.text.Length < 6)
        {
            registerMessageText.text = "Password must be at least 6 characters";
            registerMessageText.color = Color.red;
            return;
        }
        if (registerConfirmPasswordInput != null &&
            registerPasswordInput.text != registerConfirmPasswordInput.text)
        {
            registerMessageText.text = "Passwords don't match!";
            registerMessageText.color = Color.red;
            return;
        }

        registerMessageText.text = "Creating Account...";
        registerMessageText.color = Color.white;

        var request = new RegisterPlayFabUserRequest
        {
            Email = registerEmailInput.text,
            Password = registerPasswordInput.text,
            Username = registerUsernameInput != null ? registerUsernameInput.text : null,
            RequireBothUsernameAndEmail = registerUsernameInput != null
        };
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnRegisterError);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(RegisterSuccessSequence());
        else
        {
            ShowLoginPanel();
            if (loginMessageText != null)
            {
                loginMessageText.color = Color.green;
                loginMessageText.text = "Account ready! Please log in.";
            }
        }
    }

    void OnRegisterError(PlayFabError error)
    {
        if (registerMessageText != null)
        {
            registerMessageText.color = Color.red;

            string msg = error.ErrorMessage;
            if (error.Error == PlayFabErrorCode.UsernameNotAvailable)
                msg = "That username is already taken. Try a different one.";
            else if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable)
                msg = "That email is already registered. Try logging in instead.";
            else if (error.Error == PlayFabErrorCode.InvalidEmailAddress)
                msg = "Please enter a valid email address.";
            else if (error.Error == PlayFabErrorCode.InvalidPassword)
                msg = "Password is invalid. Use at least 6 characters.";

            registerMessageText.text = msg;
        }
        Debug.LogWarning("[AuthManager] Registration failed: " + error.GenerateErrorReport());
    }

    IEnumerator RegisterSuccessSequence()
    {
        if (registerMessageText != null)
        {
            registerMessageText.color = Color.green;
            registerMessageText.text = "Account Created!";
        }
        yield return new WaitForSeconds(1.5f);

        if (registerMessageText != null)
        {
            registerMessageText.text = "Redirecting to Login...";
            registerMessageText.color = Color.white;
        }
        yield return new WaitForSeconds(1.0f);

        ShowLoginPanel();
        if (loginMessageText != null)
        {
            loginMessageText.color = Color.green;
            loginMessageText.text = "Account ready! Please log in.";
        }

        if (loginEmailInput != null && registerEmailInput != null)
            loginEmailInput.text = registerEmailInput.text;
    }

    // ═══════════════════════════════════════════════════════════
    //  GUEST / OFFLINE
    // ═══════════════════════════════════════════════════════════

    public void PlayOffline()
    {
        GameSession.StartGuestSession();
        Debug.Log("[AuthManager] Starting guest session (offline / local save).");

        if (!string.IsNullOrEmpty(offlineSceneName))
        {
            PlayerPrefs.SetInt("IsLoadingGame", 0);
            SceneManager.LoadScene(offlineSceneName);
        }
        else
        {
            HideLoginOverlay();
            GameEventBus.Publish(new GuestLoginEvent());
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  LOGOUT HANDLER
    // ═══════════════════════════════════════════════════════════

    void OnLogoutRequested(LogoutRequestedEvent evt)
    {
        if (GameSession.IsLoggedIn)
            PlayFabClientAPI.ForgetAllCredentials();

        GameSession.Logout();
        ShowLoginOverlay();
        Debug.Log("[AuthManager] Logout complete — login overlay shown.");
    }

    // ═══════════════════════════════════════════════════════════
    //  PASSWORD HELPERS
    // ═══════════════════════════════════════════════════════════

    void SetPasswordHidden(TMP_InputField field)
    {
        if (field == null) return;
        field.contentType = TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }

    void OnLoginShowPasswordChanged(bool show) =>
        TogglePasswordVisibility(loginPasswordInput, show);

    void OnRegisterShowPasswordChanged(bool show)
    {
        TogglePasswordVisibility(registerPasswordInput, show);
        TogglePasswordVisibility(registerConfirmPasswordInput, show);
    }

    void TogglePasswordVisibility(TMP_InputField field, bool show)
    {
        if (field == null) return;
        field.contentType = show
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }
}