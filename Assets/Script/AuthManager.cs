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
    public Toggle loginShowPasswordToggle;
    public Toggle registerShowPasswordToggle;

    [Header("Guest / Offline Play")]
    public Button playOfflineButton;
    public string offlineSceneName = "";

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
            if (mainCanvas != null)
            {
                mainCanvas.SetActive(true);
                MainMenu menuScript = mainCanvas.GetComponent<MainMenu>();
                if (menuScript != null) menuScript.ShowMainPanel();
            }

            if (GameSession.IsLoggedIn)
            {
                OnLoginSuccessEvent?.Invoke();

                // Load this account's settings from cloud
                if (PlayerSettingsCloud.Instance != null)
                    PlayerSettingsCloud.Instance.LoadSettings();
            }
        }
        else
        {
            ResetToLogin();
        }
    }

    void SetPasswordHidden(TMP_InputField field)
    {
        if (field == null) return;
        field.contentType = TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }

    void OnLoginShowPasswordChanged(bool showPassword)
    {
        TogglePasswordVisibility(loginPasswordInput, showPassword);
    }

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

    public void PlayOffline()
    {
        GameSession.StartGuestSession();
        Debug.Log("[Auth] Starting guest session (offline / local save).");

        if (!string.IsNullOrEmpty(offlineSceneName))
        {
            PlayerPrefs.SetInt("IsLoadingGame", 0);
            SceneManager.LoadScene(offlineSceneName);
        }
        else
        {
            if (loginOverlay != null) loginOverlay.SetActive(false);
            if (mainCanvas != null)
            {
                mainCanvas.SetActive(true);
                MainMenu menuScript = mainCanvas.GetComponent<MainMenu>();
                if (menuScript != null) menuScript.ShowMainPanel();
            }
        }
    }

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
        if (registerShowPasswordToggle != null) registerShowPasswordToggle.isOn = false;
    }

    public void BackToLogin()
    {
        ShowLoginPanel();
    }

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

        if (PlayerSettingsCloud.Instance != null)
            PlayerSettingsCloud.Instance.LoadSettings();

        if (loginOverlay != null) loginOverlay.SetActive(false);

        // Try the assigned reference first, then search the scene as fallback
        MainMenu menuScript = null;
        if (mainCanvas != null)
        {
            mainCanvas.SetActive(true);
            menuScript = mainCanvas.GetComponent<MainMenu>();
        }
        if (menuScript == null)
            menuScript = FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);

        if (menuScript != null)
        {
            menuScript.gameObject.SetActive(true);
            menuScript.ShowMainPanel();
        }
    }
}