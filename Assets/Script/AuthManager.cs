using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    public static System.Action OnLoginSuccessEvent;

    [Header("UI References")]
    public GameObject loginOverlay;
    public GameObject mainCanvas;

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
    [Tooltip("Toggle on the Login panel to show/hide the password.")]
    public Toggle loginShowPasswordToggle;

    [Tooltip("Toggle on the Register panel to show/hide both password fields.")]
    public Toggle registerShowPasswordToggle;

    [Header("Guest / Offline Play")]
    [Tooltip("Optional — a button on the Login panel that lets the player skip login entirely.")]
    public Button playOfflineButton;

    [Tooltip("Scene to load directly when playing offline. Leave empty to show the main menu instead.")]
    public string offlineSceneName = "";

    void Start()
    {
        PlayFabSettings.TitleId = "164227";
        PlayFabSettings.RequestType = PlayFab.WebRequestType.UnityWebRequest;

        // ── MASK ALL PASSWORD FIELDS ──
        SetPasswordHidden(loginPasswordInput);
        SetPasswordHidden(registerPasswordInput);
        SetPasswordHidden(registerConfirmPasswordInput);

        // ── WIRE UP OFFLINE BUTTON ──
        if (playOfflineButton != null)
            playOfflineButton.onClick.AddListener(PlayOffline);

        // ── WIRE UP SHOW PASSWORD TOGGLES ──
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

        // ── SKIP LOGIN IF ALREADY AUTHENTICATED ──
        // (e.g. returning from gameplay via Quit, session is still active)
        if (GameSession.IsLoggedIn || GameSession.IsGuest)
        {
            // Go straight to the main menu — no need to log in again
            if (loginOverlay != null) loginOverlay.SetActive(false);
            if (mainCanvas != null)
            {
                mainCanvas.SetActive(true);
                MainMenu menuScript = mainCanvas.GetComponent<MainMenu>();
                if (menuScript != null) menuScript.ShowMainPanel();
            }

            // Re-fire the event so CheckForSaveData runs
            if (GameSession.IsLoggedIn)
                OnLoginSuccessEvent?.Invoke();
        }
        else
        {
            ResetToLogin();
        }
    }

    /// <summary>
    /// Forces a TMP_InputField to hide characters (shows ● instead of plain text).
    /// </summary>
    void SetPasswordHidden(TMP_InputField field)
    {
        if (field == null) return;
        field.contentType = TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }

    // =============================================
    //  SHOW / HIDE PASSWORD
    // =============================================

    /// <summary>
    /// Toggles the login password field between hidden (●●●) and visible (plain text).
    /// </summary>
    void OnLoginShowPasswordChanged(bool showPassword)
    {
        TogglePasswordVisibility(loginPasswordInput, showPassword);
    }

    /// <summary>
    /// Toggles both register password fields between hidden and visible.
    /// </summary>
    void OnRegisterShowPasswordChanged(bool showPassword)
    {
        TogglePasswordVisibility(registerPasswordInput, showPassword);
        TogglePasswordVisibility(registerConfirmPasswordInput, showPassword);
    }

    void TogglePasswordVisibility(TMP_InputField field, bool showPassword)
    {
        if (field == null) return;
        field.contentType = showPassword
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }

    // =============================================
    //  GUEST / OFFLINE PLAY
    // =============================================

    /// <summary>
    /// Called by the "Play Offline" button.
    /// Skips PlayFab authentication and either loads the gameplay scene directly
    /// or shows the main menu in guest mode.
    /// </summary>
    public void PlayOffline()
    {
        GameSession.StartGuestSession();
        Debug.Log("[Auth] Starting guest session (offline / local save).");

        if (!string.IsNullOrEmpty(offlineSceneName))
        {
            // Go straight to gameplay with local data
            PlayerPrefs.SetInt("IsLoadingGame", 0);
            SceneManager.LoadScene(offlineSceneName);
        }
        else
        {
            // Show the main menu in guest mode (no cloud features)
            if (loginOverlay != null) loginOverlay.SetActive(false);

            if (mainCanvas != null)
            {
                mainCanvas.SetActive(true);

                MainMenu menuScript = mainCanvas.GetComponent<MainMenu>();
                if (menuScript != null) menuScript.ShowMainPanel();
            }
        }
    }

    // =============================================
    //  PANEL SWITCHING
    // =============================================

    public void ResetToLogin()
    {
        if (loginOverlay != null) loginOverlay.SetActive(true);
        if (mainCanvas != null) mainCanvas.SetActive(false);
        ShowLoginPanel();
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
        // Reset toggle so password is hidden when returning to this panel
        if (loginShowPasswordToggle != null) loginShowPasswordToggle.isOn = false;
    }

    public void ShowRegisterPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (registerMessageText != null)
        {
            registerMessageText.text = "Create an Account";
            registerMessageText.color = Color.white;
        }
        if (registerUsernameInput != null) registerUsernameInput.text = "";
        if (registerEmailInput != null) registerEmailInput.text = "";
        if (registerPasswordInput != null) registerPasswordInput.text = "";
        if (registerConfirmPasswordInput != null) registerConfirmPasswordInput.text = "";
        // Reset toggle so passwords are hidden when entering this panel
        if (registerShowPasswordToggle != null) registerShowPasswordToggle.isOn = false;
    }

    public void BackToLogin()
    {
        ShowLoginPanel();
    }

    // =============================================
    //  LOGIN
    // =============================================

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
        // ── MARK THIS AS A CLOUD SESSION ──
        GameSession.StartCloudSession();

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(LoginSequence("Login Verified."));
        }
        else
        {
            FinalizeLogin();
        }
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

    // =============================================
    //  REGISTER
    // =============================================

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
        {
            StartCoroutine(RegisterSuccessSequence());
        }
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

    // =============================================
    //  LOGIN SEQUENCE (after successful login)
    // =============================================

    IEnumerator LoginSequence(string startMessage)
    {
        loginMessageText.color = Color.green;
        loginMessageText.text = startMessage;
        yield return new WaitForSeconds(1.0f);

        loginMessageText.color = Color.white;
        loginMessageText.text = "Loading Player Profile...";
        yield return new WaitForSeconds(1.5f);

        loginMessageText.text = "Welcome Back!";
        yield return new WaitForSeconds(1.0f);

        FinalizeLogin();
    }

    void FinalizeLogin()
    {
        OnLoginSuccessEvent?.Invoke();

        if (loginOverlay != null) loginOverlay.SetActive(false);

        if (mainCanvas != null)
        {
            mainCanvas.SetActive(true);

            MainMenu menuScript = mainCanvas.GetComponent<MainMenu>();
            if (menuScript != null) menuScript.ShowMainPanel();
        }
    }
}