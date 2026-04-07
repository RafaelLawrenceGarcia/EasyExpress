using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Place this on a UI Panel in your scene (e.g. inside Player HUD).
/// JobBox will find it automatically when a repair box is opened.
/// </summary>
public class BadNewsUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Settings")]
    public float displayTime = 5f;

    private Coroutine hideRoutine;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string title, string body)
    {
        if (panel == null) return;

        if (titleText != null) titleText.text = title;
        if (bodyText  != null) bodyText.text  = body;
        panel.SetActive(true);

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        if (panel != null) panel.SetActive(false);
    }

    public void HideNow()
    {
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        if (panel != null) panel.SetActive(false);
    }
}