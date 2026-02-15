using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using System.Collections; // <--- REQUIRED FOR TIMERS (Coroutines)

public class AuthManager : MonoBehaviour
{
    public static System.Action OnLoginSuccessEvent; 

    [Header("UI References")]
    public GameObject loginOverlay;  
    public GameObject mainPanel;     
    
    [Header("Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;

    void Start()
    {
        PlayFabSettings.TitleId = "164227"; 
        PlayFabSettings.RequestType = PlayFab.WebRequestType.UnityWebRequest;

        if (loginOverlay != null) loginOverlay.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false); 
    }

    // --- BUTTONS ---
    
    public void RegisterButton() 
    {
        // Simple input validation
        if (emailInput.text.Length < 4) { messageText.text = "Invalid Email"; return; }
        if (passwordInput.text.Length < 6) { messageText.text = "Password too short"; return; }

        messageText.text = "Registering...";
        var request = new RegisterPlayFabUserRequest
        {
            Email = emailInput.text,
            Password = passwordInput.text,
            RequireBothUsernameAndEmail = false
        };
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    public void LoginButton() 
    {
        messageText.text = "Authenticating..."; // Step 1: Feedback
        var request = new LoginWithEmailAddressRequest 
        { 
            Email = emailInput.text, 
            Password = passwordInput.text 
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    // --- RESPONSES ---

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        // Reuse the professional login sequence after registering
        StartCoroutine(LoginSequence("Account Created!"));
    }

    void OnLoginSuccess(LoginResult result)
    {
        // Start the timer sequence instead of swapping instantly
        StartCoroutine(LoginSequence("Login Verified."));
    }

    // --- THE PROFESSIONAL SEQUENCE ---
    
    IEnumerator LoginSequence(string startMessage)
    {
        // Phase 1: Success Message
        messageText.color = Color.green; // Make it look positive
        messageText.text = startMessage;
        yield return new WaitForSeconds(1.0f); // Wait 1 second so they can read it

        // Phase 2: "Fake" Loading (Makes game feel bigger/connected)
        messageText.color = Color.white;
        messageText.text = "Loading Player Profile...";
        yield return new WaitForSeconds(1.5f); // Wait 1.5 seconds

        // Phase 3: Welcome
        messageText.text = "Welcome Back!";
        yield return new WaitForSeconds(1.0f); // One last second of glory

        // Phase 4: NOW we swap
        Debug.Log("Transitioning to Main Menu...");
        
        OnLoginSuccessEvent?.Invoke(); 

        if (loginOverlay != null) loginOverlay.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    void OnError(PlayFabError error)
    {
        messageText.color = Color.red; // Set text to red

        // We check the specific "Code" from PlayFab and give it a custom name
        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidEmailOrPassword:
                messageText.text = "Incorrect Email or Password.";
                break;

            case PlayFabErrorCode.AccountNotFound:
                messageText.text = "Account not found. Please Register.";
                break;

            case PlayFabErrorCode.EmailAddressNotAvailable:
                messageText.text = "That email is already taken!";
                break;

            case PlayFabErrorCode.InvalidParams:
                // It's usually the email format being wrong!9  ``
                messageText.text = "Invalid Email or Password format.";
                break;

            case PlayFabErrorCode.ConnectionError:
                messageText.text = "No Internet Connection.";
                break;

            default:
                // If it's a weird error, show the original one but shorter
                messageText.text = "Error: " + error.ErrorMessage;
                break;
        }
        
        // Optional: Shake the camera or input field here for juice!
        Debug.LogError(error.GenerateErrorReport());
    }
}