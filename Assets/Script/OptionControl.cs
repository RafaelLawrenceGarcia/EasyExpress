using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsController : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button audioBtn;
    public Button videoBtn;
    public Button controlsBtn;

    [Header("Content Panels")]
    public GameObject audioPanel;
    public GameObject videoPanel;
    public GameObject controlsPanel;

    [Header("Tab Colors")]
    public Color activeTabColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color inactiveTabColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color activeTextColor = Color.white;
    public Color inactiveTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private bool isWired = false;

    void OnEnable()
    {
        // Wire buttons once (not every enable)
        if (!isWired)
        {
            if (audioBtn != null) audioBtn.onClick.AddListener(() => OpenTab(0));
            if (videoBtn != null) videoBtn.onClick.AddListener(() => OpenTab(1));
            if (controlsBtn != null) controlsBtn.onClick.AddListener(() => OpenTab(2));
            isWired = true;
        }

        // Always start on Audio tab when opened
        OpenTab(0);
    }

    public void OpenTab(int tabIndex)
    {
        if (audioPanel != null) audioPanel.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        SetTabVisual(audioBtn, false);
        SetTabVisual(videoBtn, false);
        SetTabVisual(controlsBtn, false);

        switch (tabIndex)
        {
            case 0:
                if (audioPanel != null) audioPanel.SetActive(true);
                SetTabVisual(audioBtn, true);
                break;
            case 1:
                if (videoPanel != null) videoPanel.SetActive(true);
                SetTabVisual(videoBtn, true);
                break;
            case 2:
                if (controlsPanel != null) controlsPanel.SetActive(true);
                SetTabVisual(controlsBtn, true);
                break;
        }
    }

    void SetTabVisual(Button btn, bool isActive)
    {
        if (btn == null) return;

        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = isActive ? activeTabColor : inactiveTabColor;

        TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.color = isActive ? activeTextColor : inactiveTextColor;
    }
}