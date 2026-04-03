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

    [Header("Daily Email Limit")]
    [Tooltip("Maximum total emails that can exist at once (pending + accepted).")]
    public int absoluteEmailLimit = 5;

    [Header("Scaling (Optional)")]
    public bool scaleWithDay = true;
    public int daysPerExtraEmail = 3;
    public int absoluteMaxEmails = 6;

    [Header("Profile Pictures (Optional)")]
    public Sprite[] profilePicturePool;

    [Header("Dust Settings")]
    [Range(0, 100)]
    public float dustChance = 30f;

    [Header("Part Variety")]
    [Tooltip("Always include a CPU Cooler in generated repair builds.")]
    public bool alwaysIncludeCooler = true;

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

        // Enforce hard limit: count existing pending + accepted emails
        int existingCount = emailManager.activeEmails.Count + emailManager.acceptedJobs.Count;
        int slotsAvailable = absoluteEmailLimit - existingCount;

        if (slotsAvailable <= 0)
        {
            Debug.Log($"[EmailGen] Email limit reached ({existingCount}/{absoluteEmailLimit}). No new emails today.");
            return;
        }

        int emailCount = Random.Range(minEmailsPerDay, maxToday + 1);

        // Cap to available slots
        emailCount = Mathf.Min(emailCount, slotsAvailable);

        for (int i = 0; i < emailCount; i++)
        {
            EmailData newEmail = partDatabase.GenerateRandomJob();

            if (profilePicturePool != null && profilePicturePool.Length > 0)
            {
                newEmail.profilePic = profilePicturePool[Random.Range(0, profilePicturePool.Length)];
            }

            // =============================================
            //  ENSURE COOLER IS INCLUDED (Repair jobs)
            //  Customer PCs should always have a CPU cooler.
            //  The database already has a coolers[] array —
            //  this just makes sure one is always present.
            // =============================================
            if (alwaysIncludeCooler && newEmail.startingParts != null)
            {
                EnsurePartIncluded(newEmail.startingParts, "Cooler", partDatabase.coolers);
            }

            // =============================================
            //  DUST — apply to all parts if rolled
            // =============================================
            if (Random.Range(0f, 100f) < dustChance)
            {
                if (newEmail.startingParts != null && newEmail.startingParts.Count > 0)
                {
                    foreach (StartingPCComponent part in newEmail.startingParts)
                        part.isDusty = true;
                }
            }

            emailManager.activeEmails.Add(newEmail);
        }

        Debug.Log($"[EmailGen] Generated {emailCount} new emails. Total: {emailManager.activeEmails.Count} pending, {emailManager.acceptedJobs.Count} accepted.");
        emailManager.RefreshInboxUI();
    }

    /// <summary>
    /// Checks if a part category already exists in the parts list.
    /// If not, picks a random one from the pool and adds it.
    /// </summary>
    void EnsurePartIncluded(List<StartingPCComponent> parts, string category, StartingPCComponent[] pool)
    {
        if (pool == null || pool.Length == 0) return;

        // Check if this category is already present
        foreach (StartingPCComponent existing in parts)
        {
            if (existing.partCategory == category)
                return; // Already has one
        }

        // Pick a random part from the pool and add a clean copy
        StartingPCComponent original = pool[Random.Range(0, pool.Length)];

        StartingPCComponent copy = new StartingPCComponent();
        copy.partCategory = category;
        copy.partName = original.partName;
        copy.partPrefab = original.partPrefab;
        copy.partIcon = original.partIcon;
        copy.partPrice = original.partPrice;
        copy.compatTags = original.compatTags;
        copy.powerDraw = original.powerDraw;
        copy.maxWattage = original.maxWattage;
        copy.isDusty = false;
        copy.fault = PartFault.None;
        copy.faultDescription = "";

        parts.Add(copy);
        Debug.Log($"[EmailGen] Added missing {category}: {copy.partName}");
    }
}