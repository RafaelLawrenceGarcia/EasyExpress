using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    public static System.Action OnLoginSuccessEvent; 

    [Header("UI References")]
    public GameObject loginOverlay;  // This should be your "Login" object
    public GameObject mainCanvas;   // This should be "MainMenuUI" (the parent of all menus)
    
    [Header("Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;

    void Start()
    {
        PlayFabSettings.TitleId = "164227"; 
        PlayFabSettings.RequestType = PlayFab.WebRequestType.UnityWebRequest;
        ResetToLogin();
    }

    public void ResetToLogin()
    {
        // Force the correct visibility
        if (loginOverlay != null) loginOverlay.SetActive(true);
        if (mainCanvas != null) mainCanvas.SetActive(false); 
        
        messageText.text = "Please Login or Register";
        messageText.color = Color.white;
    }

    public void RegisterButton() 
    {
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
        messageText.text = "Authenticating...";
        var request = new LoginWithEmailAddressRequest 
        { 
            Email = emailInput.text, 
            Password = passwordInput.text 
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result) => StartCoroutine(LoginSequence("Account Created!"));
    void OnLoginSuccess(LoginResult result) => StartCoroutine(LoginSequence("Login Verified."));

    IEnumerator LoginSequence(string startMessage)
    {
        messageText.color = Color.green;
        messageText.text = startMessage;
        yield return new WaitForSeconds(1.0f);

        messageText.color = Color.white;
        messageText.text = "Loading Player Profile...";
        yield return new WaitForSeconds(1.5f);

        messageText.text = "Welcome Back!";
        yield return new WaitForSeconds(1.0f);

        // Notify MainMenu.cs to check for Gold/Save data
        OnLoginSuccessEvent?.Invoke(); 

        // CRITICAL FIX: Hide the login and show the new menu
        if (loginOverlay != null) loginOverlay.SetActive(false);
        
        if (mainCanvas != null) 
        {
            mainCanvas.SetActive(true);
            
            // Ensure the script on the MainMenuUI forces the right panel (Play/Options/etc)
            MainMenu menuScript = mainCanvas.GetComponent<MainMenu>();
            if(menuScript != null) menuScript.ShowMainPanel();
        }
    }

    void OnError(PlayFabError error)
    {
        messageText.color = Color.red;
        messageText.text = "Error: " + error.ErrorMessage;
        Debug.LogError(error.GenerateErrorReport());
    }
}