using UnityEngine;
using System.Collections.Generic;

public class RandomEmailGenerator : MonoBehaviour
{
    [Header("References")]
    public PCPartDatabase partDatabase;
    public EmailManager emailManager;

    [Header("Generation Settings")]
    public int minEmailsPerDay = 1;
    public int maxEmailsPerDay = 3;

    [Header("Scaling (Optional)")]
    public bool scaleWithDay = true;
    public int daysPerExtraEmail = 3;
    public int absoluteMaxEmails = 6;

    [Header("Profile Pictures (Optional)")]
    public Sprite[] profilePicturePool;

    [Header("Dust Settings")]
    [Range(0, 100)]
    public float dustChance = 30f;

    void OnEnable() { DayTransitionManager.OnNewDayStarted += OnNewDay; }
    void OnDisable() { DayTransitionManager.OnNewDayStarted -= OnNewDay; }

    void Start()
    {
        if (emailManager == null) emailManager = EmailManager.Instance;
        if (emailManager != null && partDatabase != null) GenerateEmails(1);
    }

    void OnNewDay(int newDayNumber)
    {
        if (emailManager == null || partDatabase == null) return;
        GenerateEmails(newDayNumber);
    }

    void GenerateEmails(int currentDay)
    {
        int maxToday = maxEmailsPerDay;

        if (scaleWithDay && daysPerExtraEmail > 0)
        {
            int bonusEmails = (currentDay - 1) / daysPerExtraEmail;
            maxToday = Mathf.Min(maxEmailsPerDay + bonusEmails, absoluteMaxEmails);
        }

        int emailCount = Random.Range(minEmailsPerDay, maxToday + 1);

        for (int i = 0; i < emailCount; i++)
        {
            // Now safely pulls either a Repair or Build job!
            EmailData newEmail = partDatabase.GenerateRandomJob();

            if (profilePicturePool != null && profilePicturePool.Length > 0)
            {
                newEmail.profilePic = profilePicturePool[Random.Range(0, profilePicturePool.Length)];
            }

            if (Random.Range(0f, 100f) < dustChance)
            {
                if (newEmail.startingParts != null && newEmail.startingParts.Count > 0)
                {
                    foreach (StartingPCComponent part in newEmail.startingParts) part.isDusty = true;
                }
            }

            emailManager.activeEmails.Add(newEmail);
        }

        emailManager.RefreshInboxUI();
    }
}