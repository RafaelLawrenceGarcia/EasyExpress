using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResolutionSelector : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resolutionText;

    private Resolution[] options;
    private int currentIndex = 0;

    void Start()
    {
        // 1. Get all resolutions your monitor supports
        options = Screen.resolutions;
        
        // 2. Find current resolution to set the starting index
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i].width == Screen.currentResolution.width &&
                options[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        UpdateUI();
    }

    public void NextOption()
    {
        currentIndex++;
        // If we go past the end, loop back to start (or stop)
        if (currentIndex >= options.Length) currentIndex = 0; 
        
        UpdateUI();
        ApplyResolution();
    }

    public void PreviousOption()
    {
        currentIndex--;
        // If we go below zero, loop to the end
        if (currentIndex < 0) currentIndex = options.Length - 1;

        UpdateUI();
        ApplyResolution();
    }

    void UpdateUI()
    {
        // Format the text like "1920 x 1080"
        resolutionText.text = options[currentIndex].width + " x " + options[currentIndex].height;
    }

    void ApplyResolution()
    {
        // Actually change the screen size
        Screen.SetResolution(options[currentIndex].width, options[currentIndex].height, Screen.fullScreen);
    }
}