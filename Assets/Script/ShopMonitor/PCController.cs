using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PCController : MonoBehaviour
{
    [Header("Main System")]
    public GameObject screenContainer;
    public GameObject desktopPanel;
    public GameObject biosPanel;

    [Header("Boot Screen")]
    public GameObject bootScreenPanel;
    public TextMeshProUGUI bootLogoText;
    public Slider bootLoadingBar;
    public TextMeshProUGUI bootStatusText;

    [Header("No Signal Screen")]
    public GameObject noSignalPanel;

    [Header("BSOD Screen")]
    public GameObject bsodPanel;
    public TextMeshProUGUI bsodErrorCodeText;
    public TextMeshProUGUI bsodMessageText;

    [Header("Applications")]
    public GameObject storeAppPanel;
    public GameObject storeAppPanel2;
    public GameObject furnitureShopPanel;
    public GameObject emailAppPanel;
    public GameObject programsAppPanel;
    public GameObject wallpaperAppPanel;

    [Header("Search System")]
    public GameObject searchPanel;
    public TMP_InputField searchInput;
    public GameObject[] searchableIcons;

    private bool isPoweredOn = true;
    private Coroutine bootCoroutine;

    [Header("Save System")]
    public string pcID = "DefaultPC";

    private void Start()
    {
        // Hide everything on start — OpenWorkstationMonitor triggers the boot
        HideAllScreens();

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(FilterApps);
    }

    // ─────────────────────────────────────────────────────────────────
    //  SCREEN STATES
    // ─────────────────────────────────────────────────────────────────

    void HideAllScreens()
    {
        if (screenContainer != null) screenContainer.SetActive(false);
        if (biosPanel != null) biosPanel.SetActive(false);
        if (bootScreenPanel != null) bootScreenPanel.SetActive(false);
        if (noSignalPanel != null) noSignalPanel.SetActive(false);
        if (bsodPanel != null) bsodPanel.SetActive(false);
    }

    /// <summary>
    /// Called by PlayerInteract when the monitor is opened and PC is healthy.
    /// Plays boot animation then shows desktop.
    /// </summary>
    public void StartBoot()
    {
        HideAllScreens();
        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        bootCoroutine = StartCoroutine(BootSequence());
    }
    /// <summary>
    /// Called when PC has a crash fault (NotSeated RAM, Overheating).
    /// Plays the full boot animation, shows desktop briefly, then crashes to BSOD.
    /// </summary>
    public void StartBootThenCrash(string errorCode, string message)
    {
        HideAllScreens();
        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        bootCoroutine = StartCoroutine(BootThenCrashSequence(errorCode, message));
    }

    IEnumerator BootThenCrashSequence(string errorCode, string message)
    {
        // ── Phase 1: Normal boot screen ──────────────────────────
        if (bootScreenPanel != null)
        {
            bootScreenPanel.SetActive(true);
            if (bootLogoText != null) bootLogoText.text = "EasyExpress";
            if (bootStatusText != null) bootStatusText.text = "Starting up...";

            if (bootLoadingBar != null)
            {
                bootLoadingBar.value = 0f;
                float elapsed = 0f;
                float duration = 2f;
                string[] statusMessages =
                {
                    "Initializing hardware...",
                    "Loading system files...",
                    "Starting services...",
                    "Almost ready..."
                };
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    bootLoadingBar.value = t;
                    int msgIndex = Mathf.Clamp(Mathf.FloorToInt(t * statusMessages.Length), 0, statusMessages.Length - 1);
                    if (bootStatusText != null) bootStatusText.text = statusMessages[msgIndex];
                    yield return null;
                }
                bootLoadingBar.value = 1f;
                if (bootStatusText != null) bootStatusText.text = "Welcome!";
            }

            yield return new WaitForSeconds(0.5f);
            bootScreenPanel.SetActive(false);
        }

        // ── Phase 2: Show desktop briefly before the crash ───────
        BootToOS();
        yield return new WaitForSeconds(2.5f);

        // ── Phase 3: Crash to BSOD ────────────────────────────────
        ShowBSOD(errorCode, message);
    }
    /// <summary>
    /// Called when PC has no display output (GPU fault).
    /// </summary>
    public void ShowNoSignal()
    {
        HideAllScreens();
        if (noSignalPanel != null) noSignalPanel.SetActive(true);
    }

    /// <summary>
    /// Called when PC fails POST or has critical fault.
    /// </summary>
    public void ShowBSOD(string errorCode, string message)
    {
        HideAllScreens();
        if (bsodPanel != null) bsodPanel.SetActive(true);
        if (bsodErrorCodeText != null)
            bsodErrorCodeText.text = errorCode;
        if (bsodMessageText != null)
            bsodMessageText.text = message;
    }

    IEnumerator BootSequence()
    {
        // ── Phase 1: Boot screen ──────────────────────────────────
        if (bootScreenPanel != null)
        {
            bootScreenPanel.SetActive(true);

            if (bootLogoText != null) bootLogoText.text = "EasyExpress";
            if (bootStatusText != null) bootStatusText.text = "Starting up...";

            // Fill loading bar over 2 seconds
            if (bootLoadingBar != null)
            {
                bootLoadingBar.value = 0f;
                float elapsed = 0f;
                float duration = 2f;

                string[] statusMessages =
                {
                    "Initializing hardware...",
                    "Loading system files...",
                    "Starting services...",
                    "Almost ready..."
                };

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    bootLoadingBar.value = t;

                    // Update status text at each quarter
                    int msgIndex = Mathf.FloorToInt(t * statusMessages.Length);
                    msgIndex = Mathf.Clamp(msgIndex, 0, statusMessages.Length - 1);
                    if (bootStatusText != null)
                        bootStatusText.text = statusMessages[msgIndex];

                    yield return null;
                }

                bootLoadingBar.value = 1f;
                if (bootStatusText != null) bootStatusText.text = "Welcome!";
            }

            yield return new WaitForSeconds(0.5f);
            bootScreenPanel.SetActive(false);
        }

        // ── Phase 2: Show desktop ─────────────────────────────────
        BootToOS();
    }

    // ─────────────────────────────────────────────────────────────────
    //  POWER & BIOS
    // ─────────────────────────────────────────────────────────────────

    public void PowerOff()
    {
        isPoweredOn = false;
        if (bootCoroutine != null) { StopCoroutine(bootCoroutine); bootCoroutine = null; }
        HideAllScreens();
    }

    public void RestartToBIOS()
    {
        isPoweredOn = true;
        HideAllScreens();
        if (biosPanel != null) biosPanel.SetActive(true);
    }

    public void BootToOS()
    {
        isPoweredOn = true;
        if (biosPanel != null) biosPanel.SetActive(false);
        if (bootScreenPanel != null) bootScreenPanel.SetActive(false);
        if (noSignalPanel != null) noSignalPanel.SetActive(false);
        if (bsodPanel != null) bsodPanel.SetActive(false);
        if (screenContainer != null) screenContainer.SetActive(true);
        ShowDesktop();
    }

    // ─────────────────────────────────────────────────────────────────
    //  DESKTOP & APPS  (unchanged from original)
    // ─────────────────────────────────────────────────────────────────

    public void ShowDesktop()
    {
        if (desktopPanel) desktopPanel.SetActive(true);
        if (storeAppPanel) storeAppPanel.SetActive(false);
        if (storeAppPanel2) storeAppPanel2.SetActive(false);
        if (furnitureShopPanel) furnitureShopPanel.SetActive(false);
        if (emailAppPanel) emailAppPanel.SetActive(false);
        if (programsAppPanel) programsAppPanel.SetActive(false);
        if (wallpaperAppPanel) wallpaperAppPanel.SetActive(false);
        if (searchPanel) searchPanel.SetActive(false);
    }

    public void CloseCurrentApp() { ShowDesktop(); }

    public void OpenStoreApp()
    {
        ShowDesktop();
        if (storeAppPanel) storeAppPanel.SetActive(true);
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.OpenShopApp();
    }

    public void OpenFurnitureShop() { ShowDesktop(); if (furnitureShopPanel) furnitureShopPanel.SetActive(true); }

    public void OpenEmailApp()
    {
        ShowDesktop();
        if (emailAppPanel) emailAppPanel.SetActive(true);
        EmailManager email = EmailManager.Instance;
        if (email != null) email.OpenEmailApp();
    }

    public void OpenProgramsApp() { ShowDesktop(); if (programsAppPanel) programsAppPanel.SetActive(true); }
    public void OpenWallpaperApp() { ShowDesktop(); if (wallpaperAppPanel) wallpaperAppPanel.SetActive(true); }

    public void ToggleSearchPanel()
    {
        if (searchPanel == null) return;
        bool isOpening = !searchPanel.activeSelf;
        searchPanel.SetActive(isOpening);
        if (isOpening && searchInput != null)
        {
            searchInput.text = "";
            searchInput.Select();
        }
    }

    public void FilterApps(string searchTerm)
    {
        string lowerSearchTerm = searchTerm.ToLower();
        foreach (GameObject icon in searchableIcons)
        {
            if (icon == null) continue;
            TextMeshProUGUI label = icon.GetComponentInChildren<TextMeshProUGUI>();
            string appName = (label != null) ? label.text.ToLower() : icon.name.ToLower();
            bool isMatch = string.IsNullOrEmpty(lowerSearchTerm) || appName.Contains(lowerSearchTerm);
            icon.SetActive(isMatch);
        }
    }

    public bool HandleEscapeInput()
    {
        if (!isPoweredOn || (biosPanel != null && biosPanel.activeSelf)) return true;

        bool isAnyAppOpen =
            (storeAppPanel != null && storeAppPanel.activeSelf) ||
            (furnitureShopPanel != null && furnitureShopPanel.activeSelf) ||
            (emailAppPanel != null && emailAppPanel.activeSelf) ||
            (programsAppPanel != null && programsAppPanel.activeSelf) ||
            (wallpaperAppPanel != null && wallpaperAppPanel.activeSelf) ||
            (searchPanel != null && searchPanel.activeSelf);

        if (isAnyAppOpen) { ShowDesktop(); return false; }
        return true;
    }

    public void UnlockApp(string appName)
    {
        PlayerPrefs.SetInt(pcID + "_" + appName + "Unlocked", 1);
        PlayerPrefs.Save();
    }

    public bool IsAppUnlocked(string appName)
    {
        return PlayerPrefs.GetInt(pcID + "_" + appName + "Unlocked", 0) == 1;
    }
}