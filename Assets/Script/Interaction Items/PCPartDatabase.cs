// ============================================================
//  PCPartDatabase.cs — COMPATIBILITY FIX
//  
//  WHAT CHANGED:
//  1. Added FilterByCompatibility() helper method
//  2. GenerateRandomPC() now filters CPU/RAM/Cooler by motherboard tags
//  3. GeneratePCForPurpose() now filters CPU/RAM/Cooler by motherboard tags
//  4. AddRAMSticks() now accepts a filtered array parameter
//
//  HOW IT WORKS:
//  - After picking a motherboard, we extract its compatTags
//  - Socket tags (AM4, AM5, LGA1151, LGA1200, LGA1700, etc.)
//    are used to filter CPUs and Coolers
//  - Memory tags (DDR3, DDR4, DDR5) are used to filter RAM
//  - GPU, Storage, PSU, Fans have no socket/memory dependency
//
//  IMPORTANT — YOUR PART DATABASE MUST HAVE CORRECT TAGS:
//  Motherboard compatTags: ["AM5", "DDR5"] or ["LGA1700", "DDR5"] etc.
//  CPU compatTags:         ["AM5"] or ["LGA1700"] etc.
//  RAM compatTags:         ["DDR5"] or ["DDR4"] etc.
//  Cooler compatTags:      ["AM5"] or ["LGA1700"] etc.
// ============================================================

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PC Part Database", menuName = "EasyExpress/PC Part Database")]
public class PCPartDatabase : ScriptableObject
{
    [Header("PC Cases")]
    public GameObject[] cases;

    [Header("=== REQUIRED PARTS (Always Included) ===")]
    public StartingPCComponent[] motherboards;
    public StartingPCComponent[] psus;

    [Header("=== MOTHERBOARD-DEPENDENT PARTS ===")]
    public StartingPCComponent[] cpus;
    public StartingPCComponent[] gpus;
    public StartingPCComponent[] rams;
    public StartingPCComponent[] storage;

    [Header("=== CPU-DEPENDENT PARTS ===")]
    public StartingPCComponent[] coolers;

    [Header("=== OPTIONAL / EXTRAS ===")]
    public StartingPCComponent[] fans;

    [Header("=== CHANCE SETTINGS (0-100%) ===")]
    [Range(0, 100)] public float cpuChance = 90f;
    [Range(0, 100)] public float gpuChance = 75f;
    [Range(0, 100)] public float ramChance = 95f;
    [Range(0, 100)] public float storageChance = 70f;
    [Range(0, 100)] public float coolerChance = 80f;
    [Range(0, 100)] public float fanChance = 40f;

    [Header("=== JOB TYPE SETTINGS ===")]
    [Range(0, 100)]
    [Tooltip("Chance that a generated job is a Build instead of Repair")]
    public float buildJobChance = 35f;

    [Header("RAM Settings")]
    public int minRAMSticks = 1;
    public int maxRAMSticks = 4;

    [Header("Fallback Prices (if partPrice is 0)")]
    [Tooltip("Fallback price per category if a part has no price set")]
    public float fallbackMotherboardPrice = 4500f;
    public float fallbackCPUPrice = 6000f;
    public float fallbackGPUPrice = 12000f;
    public float fallbackRAMPrice = 1800f;
    public float fallbackStoragePrice = 2500f;
    public float fallbackPSUPrice = 3000f;
    public float fallbackCoolerPrice = 1500f;
    public float fallbackFanPrice = 500f;

    // =============================================
    //  COMPATIBILITY TAG SETS
    //  Used to identify which tags represent sockets
    //  vs memory types when filtering parts.
    // =============================================

    // Socket tags — must match between Motherboard ↔ CPU ↔ Cooler
    private static readonly HashSet<string> SOCKET_TAGS = new HashSet<string>(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "AM4", "AM5", "LGA1151", "LGA1200", "LGA1700", "LGA1851",
        "TR4", "TRX40", "TRX50", "sTRX4", "sTR5",
        "LGA2066", "LGA4677"
    };

    // Memory tags — must match between Motherboard ↔ RAM
    private static readonly HashSet<string> MEMORY_TAGS = new HashSet<string>(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "DDR3", "DDR4", "DDR5"
    };

    // =============================================
    //  COMPATIBILITY FILTER
    //  Returns only parts whose compatTags share at
    //  least one tag from the given filter set.
    // =============================================

    /// <summary>
    /// Filters a parts array to only those compatible with the given motherboard.
    /// tagCategory: "socket" filters by socket tags, "memory" filters by memory tags.
    /// Returns the filtered array. If no matches, returns the original array as fallback.
    /// </summary>
    StartingPCComponent[] FilterByCompatibility(
        StartingPCComponent[] pool,
        StartingPCComponent motherboard,
        string tagCategory)
    {
        if (pool == null || pool.Length == 0) return pool;
        if (motherboard == null || motherboard.compatTags == null) return pool;

        // Extract the relevant tags from the motherboard
        HashSet<string> relevantTagSet = (tagCategory == "memory") ? MEMORY_TAGS : SOCKET_TAGS;

        // Get motherboard's tags that belong to the relevant category
        HashSet<string> moboRelevantTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string tag in motherboard.compatTags)
        {
            string trimmed = tag.Trim();
            if (relevantTagSet.Contains(trimmed))
                moboRelevantTags.Add(trimmed);
        }

        // If motherboard has no tags in this category, skip filtering
        if (moboRelevantTags.Count == 0)
        {
            Debug.LogWarning($"[PCPartDB] Motherboard '{motherboard.partName}' has no {tagCategory} tags — skipping filter.");
            return pool;
        }

        // Filter: keep parts that share at least one relevant tag with the motherboard
        List<StartingPCComponent> compatible = new List<StartingPCComponent>();
        foreach (StartingPCComponent part in pool)
        {
            if (part.compatTags == null || part.compatTags.Length == 0)
            {
                // Parts with no tags are treated as universal (backward compat)
                compatible.Add(part);
                continue;
            }

            foreach (string partTag in part.compatTags)
            {
                if (moboRelevantTags.Contains(partTag.Trim()))
                {
                    compatible.Add(part);
                    break;
                }
            }
        }

        if (compatible.Count == 0)
        {
            // No compatible parts found — fall back to full pool to avoid empty builds
            Debug.LogWarning($"[PCPartDB] No {tagCategory}-compatible parts found for " +
                             $"motherboard '{motherboard.partName}' (tags: {string.Join(",", motherboard.compatTags)}). " +
                             $"Using full pool as fallback.");
            return pool;
        }

        Debug.Log($"[PCPartDB] {tagCategory} filter: {compatible.Count}/{pool.Length} parts " +
                  $"compatible with '{motherboard.partName}' " +
                  $"(mobo tags: {string.Join(",", moboRelevantTags)})");

        return compatible.ToArray();
    }

    // =============================================
    //  MASTER API: Generate Random PC (generic)
    //  NOW WITH COMPATIBILITY FILTERING
    // =============================================
    public RandomPCResult GenerateRandomPC()
    {
        RandomPCResult result = new RandomPCResult();

        if (cases == null || cases.Length == 0) return result;
        result.casePrefab = cases[Random.Range(0, cases.Length)];

        // ── Step 1: Pick motherboard FIRST (everything depends on it) ──
        if (motherboards == null || motherboards.Length == 0) return result;
        StartingPCComponent chosenMotherboard = motherboards[Random.Range(0, motherboards.Length)];
        AddCopiedPart(chosenMotherboard, result.parts);

        // ── Step 2: PSU (no compatibility constraint) ──
        TryAddPart(psus, result.parts, true);

        // ── Step 3: CPU (must match motherboard SOCKET) ──
        StartingPCComponent[] compatCPUs = FilterByCompatibility(cpus, chosenMotherboard, "socket");
        bool hasCPU = TryAddPart(compatCPUs, result.parts, Roll(cpuChance));

        // ── Step 4: GPU (no socket/memory constraint) ──
        TryAddPart(gpus, result.parts, Roll(gpuChance));

        // ── Step 5: RAM (must match motherboard MEMORY type) ──
        StartingPCComponent[] compatRAM = FilterByCompatibility(rams, chosenMotherboard, "memory");
        if (compatRAM != null && compatRAM.Length > 0 && Roll(ramChance))
        {
            int stickCount = Random.Range(minRAMSticks, maxRAMSticks + 1);
            // Pick ONE ram type, then duplicate — no mixed DDR4/DDR5
            StartingPCComponent chosenRam = compatRAM[Random.Range(0, compatRAM.Length)];
            for (int i = 0; i < stickCount; i++) AddCopiedPart(chosenRam, result.parts);
        }

        // ── Step 6: Storage (no constraint) ──
        TryAddPart(storage, result.parts, Roll(storageChance));

        // ── Step 7: Cooler (must match motherboard SOCKET for mounting) ──
        if (hasCPU)
        {
            StartingPCComponent[] compatCoolers = FilterByCompatibility(coolers, chosenMotherboard, "socket");
            TryAddPart(compatCoolers, result.parts, Roll(coolerChance));
        }

        // ── Step 8: Fans (no constraint) ──
        if (fans != null && fans.Length > 0 && Roll(fanChance))
        {
            int fanCount = Random.Range(1, 5);
            for (int i = 0; i < fanCount; i++) AddCopiedPart(fans[Random.Range(0, fans.Length)], result.parts);
        }

        return result;
    }

    // =============================================
    //  PURPOSE-DRIVEN PC GENERATION (for Build jobs)
    //  NOW WITH COMPATIBILITY FILTERING
    // =============================================
    public RandomPCResult GeneratePCForPurpose(BuildPurpose purpose)
    {
        RandomPCResult result = new RandomPCResult();

        if (cases == null || cases.Length == 0) return result;
        result.casePrefab = cases[Random.Range(0, cases.Length)];

        // ── Pick motherboard FIRST ──
        if (motherboards == null || motherboards.Length == 0) return result;
        StartingPCComponent chosenMotherboard = motherboards[Random.Range(0, motherboards.Length)];
        AddCopiedPart(chosenMotherboard, result.parts);

        // PSU is always required (no compatibility constraint)
        TryAddPart(psus, result.parts, true);

        // ── Filter parts by motherboard compatibility ──
        StartingPCComponent[] compatCPUs = FilterByCompatibility(cpus, chosenMotherboard, "socket");
        StartingPCComponent[] compatRAM = FilterByCompatibility(rams, chosenMotherboard, "memory");
        StartingPCComponent[] compatCoolers = FilterByCompatibility(coolers, chosenMotherboard, "socket");

        switch (purpose)
        {
            case BuildPurpose.School:
                // Basic: CPU always, GPU optional (30%), 1-2 RAM, storage always, cooler likely
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, Roll(30f));
                AddRAMSticks(result.parts, 1, 2, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, Roll(80f));
                TryAddFans(result.parts, 1, 2, 50f);
                break;

            case BuildPurpose.Office:
                // Reliable mid-range: CPU always, GPU likely (60%), 2 RAM, storage always, cooler always
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, Roll(60f));
                AddRAMSticks(result.parts, 2, 2, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 1, 3, 60f);
                break;

            case BuildPurpose.Streaming:
                // Mid-high: CPU always, GPU always, 2-4 RAM, storage always, cooler always, fans
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, true);
                AddRAMSticks(result.parts, 2, 4, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 2, 4, 80f);
                break;

            case BuildPurpose.Gaming:
                // High-end: Everything maxed
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, true);
                AddRAMSticks(result.parts, 2, 4, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 2, 5, 90f);
                break;
        }

        return result;
    }

    // =============================================
    //  UPDATED: AddRAMSticks now takes a filtered array
    // =============================================
    void AddRAMSticks(List<StartingPCComponent> parts, int min, int max, StartingPCComponent[] ramPool = null)
    {
        // Use filtered pool if provided, otherwise fall back to master array
        StartingPCComponent[] pool = (ramPool != null && ramPool.Length > 0) ? ramPool : rams;
        if (pool == null || pool.Length == 0) return;

        int count = Random.Range(min, max + 1);
        // Pick ONE type — all sticks match
        StartingPCComponent chosenRam = pool[Random.Range(0, pool.Length)];
        for (int i = 0; i < count; i++)
            AddCopiedPart(chosenRam, parts);
    }

    void TryAddFans(List<StartingPCComponent> parts, int min, int max, float chance)
    {
        if (fans == null || fans.Length == 0 || !Roll(chance)) return;
        int count = Random.Range(min, max + 1);
        for (int i = 0; i < count; i++)
            AddCopiedPart(fans[Random.Range(0, fans.Length)], parts);
    }

    // =============================================
    //  REWARD CALCULATION
    // =============================================
    float CalculateReward(List<StartingPCComponent> parts, bool isBuild)
    {
        float totalComponentCost = 0f;
        int faultCount = 0;

        foreach (var part in parts)
        {
            float price = part.partPrice > 0f ? part.partPrice : GetFallbackPrice(part.partCategory);

            if (isBuild)
            {
                totalComponentCost += price;
            }
            else
            {
                if (part.fault != PartFault.None && part.fault != PartFault.Dusty
                    && part.fault != PartFault.NotSeated && part.fault != PartFault.LooseConnection
                    && part.fault != PartFault.WrongSlot)
                {
                    totalComponentCost += price;
                }

                if (part.fault != PartFault.None)
                    faultCount++;
            }
        }

        float markedUp = totalComponentCost * 1.10f;

        float labour;
        if (isBuild)
            labour = 1000f;
        else
            labour = faultCount >= 2 ? 1000f : 500f;

        return Mathf.Round(markedUp + labour);
    }

    float GetFallbackPrice(string category)
    {
        switch (category)
        {
            case "Motherboard": return fallbackMotherboardPrice;
            case "CPU": return fallbackCPUPrice;
            case "GPU": return fallbackGPUPrice;
            case "RAM": return fallbackRAMPrice;
            case "Storage": return fallbackStoragePrice;
            case "PSU": return fallbackPSUPrice;
            case "Cooler": return fallbackCoolerPrice;
            case "Fan": return fallbackFanPrice;
            default: return 1000f;
        }
    }

    // =============================================
    //  MASTER JOB GENERATOR (Calls Build or Repair)
    // =============================================
    public EmailData GenerateRandomJob()
    {
        if (Roll(buildJobChance)) return GenerateRandomBuildJob();
        else return GenerateRandomEmailJob();
    }

    public EmailData GenerateRandomBuildJob()
    {
        BuildPurpose[] purposes = { BuildPurpose.School, BuildPurpose.Streaming, BuildPurpose.Gaming, BuildPurpose.Office };
        BuildPurpose purpose = purposes[Random.Range(0, purposes.Length)];

        RandomPCResult desiredPC = GeneratePCForPurpose(purpose);

        EmailData job = ScriptableObject.CreateInstance<EmailData>();
        job.jobType = JobType.Build;
        job.buildPurpose = purpose;
        job.basePCCasePrefab = desiredPC.casePrefab;
        job.startingParts = new List<StartingPCComponent>();
        job.requestedParts = desiredPC.parts;

        job.reward = CalculateReward(desiredPC.parts, true);

        job.pcProblems = new string[] { "New PC Build" };
        job.originalFaultCount = 0;
        job.objectives = GenerateBuildObjectives(desiredPC.parts, purpose);
        job.senderName = GenerateRandomName();
        job.subjectLine = GenerateBuildSubjectLine(purpose);
        job.bodyText = GenerateBuildEmailBody(job.senderName, desiredPC.parts, job.reward, purpose);

        return job;
    }

    public EmailData GenerateRandomEmailJob()
    {
        RandomPCResult pc = GenerateRandomPC();

        EmailData job = ScriptableObject.CreateInstance<EmailData>();
        job.jobType = JobType.Repair;
        job.basePCCasePrefab = pc.casePrefab;
        job.startingParts = pc.parts;

        string chosenProblem = PickValidProblem(pc.parts);
        job.pcProblems = new string[] { chosenProblem };

        ApplyFaultToPC(chosenProblem, pc.parts);

        int faultCount = 0;
        foreach (var part in pc.parts)
        {
            if (part.fault != PartFault.None) faultCount++;
        }
        job.originalFaultCount = faultCount;

        job.reward = CalculateReward(pc.parts, false);

        job.objectives = GenerateObjectives(chosenProblem);
        job.senderName = GenerateRandomName();
        job.subjectLine = GenerateSubjectLine(chosenProblem);
        job.bodyText = GenerateEmailBody(job.senderName, chosenProblem, job.reward);

        return job;
    }

    // =============================================
    //  SMART PROBLEM PICKER
    // =============================================
    string PickValidProblem(List<StartingPCComponent> parts)
    {
        HashSet<string> categories = new HashSet<string>();
        foreach (var p in parts) categories.Add(p.partCategory);

        List<ProblemRequirement> allProblems = new List<ProblemRequirement>
        {
            new ProblemRequirement("Blue Screen of Death",  new string[] { "RAM", "Storage" }),
            new ProblemRequirement("No Display on Monitor", new string[] { "GPU", "RAM" }),
            new ProblemRequirement("PC Keeps Overheating",  new string[] { "Cooler", "Fan" }),
            new ProblemRequirement("Won't Turn On",         new string[] { "PSU", "Motherboard" }),
            new ProblemRequirement("Full of Dust",          new string[] { }),
            new ProblemRequirement("Random Shutdowns",      new string[] { "PSU", "CPU" }),
            new ProblemRequirement("Loud Fan Noise",        new string[] { "Fan", "Cooler" }),
            new ProblemRequirement("Slow Performance",      new string[] { "Storage", "RAM" }),
            new ProblemRequirement("Boot Loop",             new string[] { "RAM", "Motherboard" }),
            new ProblemRequirement("No Internet Connection", new string[] { "Motherboard" }),
        };

        List<string> validProblems = new List<string>();
        foreach (var prob in allProblems)
        {
            if (prob.requiredCategories.Length == 0)
            {
                validProblems.Add(prob.problemName);
                continue;
            }

            foreach (string cat in prob.requiredCategories)
            {
                if (categories.Contains(cat))
                {
                    validProblems.Add(prob.problemName);
                    break;
                }
            }
        }

        if (validProblems.Count == 0) return "Full of Dust";

        return validProblems[Random.Range(0, validProblems.Count)];
    }

    // =============================================
    //  FAULT ASSIGNMENT
    // =============================================
    void ApplyFaultToPC(string problem, List<StartingPCComponent> parts)
    {
        if (parts == null || parts.Count == 0) return;

        switch (problem)
        {
            case "Blue Screen of Death":
                if (TryFaultByCategory(parts, "RAM", PartFault.NotSeated, "RAM stick not seated properly — push until it clicks")) break;
                TryFaultByCategory(parts, "Storage", PartFault.Corrupted, "Storage drive has corrupted sectors");
                break;
            case "No Display on Monitor":
                if (TryFaultByCategory(parts, "GPU", PartFault.NotSeated, "GPU not fully inserted into PCIe slot")) break;
                TryFaultByCategory(parts, "RAM", PartFault.Broken, "Faulty RAM stick preventing POST");
                break;
            case "PC Keeps Overheating":
                if (TryFaultByCategory(parts, "Cooler", PartFault.Overheating, "Thermal paste dried out — cooler not making proper contact")) break;
                TryFaultByCategory(parts, "Fan", PartFault.Dusty, "Fan blades clogged with dust — not spinning properly");
                break;
            case "Won't Turn On":
                if (TryFaultByCategory(parts, "PSU", PartFault.Broken, "PSU is dead — no power output detected")) break;
                TryFaultByCategory(parts, "Motherboard", PartFault.LooseConnection, "24-pin ATX power cable loose — reseat the connection");
                break;
            case "Full of Dust":
                foreach (var p in parts)
                {
                    p.isDusty = true;
                    p.fault = PartFault.Dusty;
                    p.faultDescription = "Covered in dust — needs cleaning";
                }
                break;
            case "Random Shutdowns":
                if (TryFaultByCategory(parts, "PSU", PartFault.Overloaded, "PSU wattage too low for this build — causes random shutdowns")) break;
                TryFaultByCategory(parts, "CPU", PartFault.Overheating, "CPU throttling due to heat — shuts down to protect itself");
                break;
            case "Loud Fan Noise":
                if (TryFaultByCategory(parts, "Fan", PartFault.Broken, "Fan bearing worn out — grinding noise")) break;
                TryFaultByCategory(parts, "Cooler", PartFault.Dusty, "Cooler fan clogged with dust — running at max RPM");
                break;
            case "Slow Performance":
                if (TryFaultByCategory(parts, "Storage", PartFault.Outdated, "Old HDD failing — extremely slow read/write speeds")) break;
                TryFaultByCategory(parts, "RAM", PartFault.Incompatible, "RAM running at wrong speed — not compatible with this motherboard");
                break;
            case "Boot Loop":
                if (TryFaultByCategory(parts, "RAM", PartFault.NotSeated, "RAM not seated correctly — system fails POST and restarts")) break;
                TryFaultByCategory(parts, "Motherboard", PartFault.Corrupted, "BIOS corrupted — needs reset or reflash");
                break;
            case "No Internet Connection":
                TryFaultByCategory(parts, "Motherboard", PartFault.Broken, "Onboard network adapter failed — may need a PCIe network card");
                break;
            default:
                ApplyRandomFallbackFault(parts);
                break;
        }
    }

    bool TryFaultByCategory(List<StartingPCComponent> parts, string category, PartFault fault, string description)
    {
        List<StartingPCComponent> matches = new List<StartingPCComponent>();
        foreach (var p in parts) if (p.partCategory == category) matches.Add(p);
        if (matches.Count == 0) return false;
        StartingPCComponent target = matches[Random.Range(0, matches.Count)];
        target.fault = fault;
        target.faultDescription = description;
        return true;
    }

    void ApplyRandomFallbackFault(List<StartingPCComponent> parts)
    {
        if (parts.Count == 0) return;
        StartingPCComponent target = parts[Random.Range(0, parts.Count)];
        PartFault[] possibleFaults = { PartFault.NotSeated, PartFault.Broken, PartFault.Dusty, PartFault.LooseConnection };
        string[] descriptions = { $"{target.partCategory} not seated properly", $"{target.partCategory} appears to be defective", $"{target.partCategory} is covered in dust", $"{target.partCategory} has a loose connection" };
        int pick = Random.Range(0, possibleFaults.Length);
        target.fault = possibleFaults[pick];
        target.faultDescription = descriptions[pick];
    }

    // =============================================
    //  INTERNAL HELPERS
    // =============================================
    bool TryAddPart(StartingPCComponent[] pool, List<StartingPCComponent> targetList, bool shouldAdd)
    {
        if (!shouldAdd || pool == null || pool.Length == 0) return false;
        AddCopiedPart(pool[Random.Range(0, pool.Length)], targetList);
        return true;
    }

    void AddCopiedPart(StartingPCComponent original, List<StartingPCComponent> targetList)
    {
        StartingPCComponent copy = new StartingPCComponent();
        copy.partCategory = original.partCategory;
        copy.partName = original.partName;
        copy.partPrefab = original.partPrefab;
        copy.partIcon = original.partIcon;
        copy.partPrice = original.partPrice;
        copy.compatTags = original.compatTags;
        copy.powerDraw = original.powerDraw;
        copy.maxWattage = original.maxWattage;
        copy.isDusty = original.isDusty;
        copy.fault = original.fault;
        copy.faultDescription = original.faultDescription;
        targetList.Add(copy);
    }

    bool Roll(float chance) { return Random.Range(0f, 100f) <= chance; }

    // =============================================
    //  BUILD JOB — TEXT GENERATION
    // =============================================
    string[] GenerateBuildObjectives(List<StartingPCComponent> requestedParts, BuildPurpose purpose)
    {
        List<string> objectives = new List<string>();
        objectives.Add($"Build a PC for {PurposeLabel(purpose)}");
        Dictionary<string, int> partCounts = new Dictionary<string, int>();
        foreach (var part in requestedParts)
        {
            if (partCounts.ContainsKey(part.partCategory)) partCounts[part.partCategory]++;
            else partCounts[part.partCategory] = 1;
        }
        foreach (var kvp in partCounts)
        {
            if (kvp.Value > 1) objectives.Add($"Install {kvp.Key} x{kvp.Value}");
            else objectives.Add($"Install {kvp.Key}");
        }
        objectives.Add("Deliver the finished PC");
        return objectives.ToArray();
    }

    string GenerateBuildSubjectLine(BuildPurpose purpose)
    {
        switch (purpose)
        {
            case BuildPurpose.School:
                return new string[] { "Need a PC for school", "Student PC Build Request", "Build me a study PC!", "School laptop won't cut it — need a desktop" }[Random.Range(0, 4)];
            case BuildPurpose.Streaming:
                return new string[] { "Streaming PC Build Request", "Need a PC for streaming", "Build me a streaming rig!", "Time to start streaming — need a PC" }[Random.Range(0, 4)];
            case BuildPurpose.Gaming:
                return new string[] { "Gaming PC Build Request", "Need a beast gaming rig!", "Build me a gaming PC!", "Custom Gaming PC — Can you build it?" }[Random.Range(0, 4)];
            case BuildPurpose.Office:
                return new string[] { "Office PC Build Request", "Need a reliable work PC", "Build me an office computer", "New office setup — PC needed" }[Random.Range(0, 4)];
            default:
                return "Custom PC Build Request";
        }
    }

    string GenerateBuildEmailBody(string senderName, List<StartingPCComponent> parts, float reward, BuildPurpose purpose)
    {
        string[] greetings = { "Hi there,", "Hello,", "Hey,", "Good day," };

        string purposeDesc = "";
        switch (purpose)
        {
            case BuildPurpose.School:
                purposeDesc = "I need a PC for school — mostly for documents, browsing, and video calls.";
                break;
            case BuildPurpose.Streaming:
                purposeDesc = "I'm getting into streaming and need a solid rig that can handle OBS, games, and a webcam all at once.";
                break;
            case BuildPurpose.Gaming:
                purposeDesc = "I want a gaming PC that can handle the latest AAA titles at high settings. No compromises!";
                break;
            case BuildPurpose.Office:
                purposeDesc = "I need a reliable office PC for spreadsheets, email, and general productivity.";
                break;
        }

        string[] intros = {
            $"{purposeDesc} Here's what I need:",
            $"Can you build me a new PC? {purposeDesc} I have a specific parts list:",
            $"I'm looking for someone to assemble a PC for me. {purposeDesc} Here are the specs:"
        };

        string partsList = "\n\n";
        Dictionary<string, List<string>> grouped = new Dictionary<string, List<string>>();
        foreach (var part in parts)
        {
            if (!grouped.ContainsKey(part.partCategory)) grouped[part.partCategory] = new List<string>();
            grouped[part.partCategory].Add(part.partName);
        }
        foreach (var kvp in grouped)
        {
            foreach (string name in kvp.Value) partsList += $"  • {kvp.Key}: {name}\n";
        }

        string[] closings = {
            $"\nI'm willing to pay ₱{reward:N0} for the whole build. Thanks!",
            $"\nMy budget for this build is ₱{reward:N0} including parts and labour. Let me know!",
            $"\nI've set aside ₱{reward:N0} total for parts and your service fee. Hope you can do it!"
        };
        string[] signoffs = { $"Thanks,\n{senderName}", $"Best regards,\n{senderName}", $"Cheers,\n{senderName}", $"— {senderName}" };

        return greetings[Random.Range(0, greetings.Length)] + "\n\n" + intros[Random.Range(0, intros.Length)] + partsList + "\n" + closings[Random.Range(0, closings.Length)] + "\n\n" + signoffs[Random.Range(0, signoffs.Length)];
    }

    // =============================================
    //  REPAIR JOB — TEXT GENERATION
    // =============================================
    string GenerateRandomName()
    {
        string[] firstNames = { "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda" };
        string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis" };
        return firstNames[Random.Range(0, firstNames.Length)] + " " + lastNames[Random.Range(0, lastNames.Length)];
    }

    string GenerateSubjectLine(string problem)
    {
        string[] templates = { "Help! My PC has a problem", "PC Issue — " + problem, "Need repair ASAP", "Computer won't work" };
        return templates[Random.Range(0, templates.Length)];
    }

    string GenerateEmailBody(string senderName, string problem, float reward)
    {
        string[] greetings = { "Hi there,", "Hello,", "Hey,", "Good day," };
        string[] descriptions = { $"My PC has been giving me trouble lately. The issue is: {problem}.", $"Something's wrong with my PC. I think the problem is {problem}." };
        string[] closings = { $"I can pay ₱{reward:N0} for the repair. Let me know if you can help!", $"I'll pay up to ₱{reward:N0} to get this fixed. Thanks in advance!" };
        string[] signoffs = { $"Thanks,\n{senderName}", $"Best regards,\n{senderName}" };
        return greetings[Random.Range(0, greetings.Length)] + "\n\n" + descriptions[Random.Range(0, descriptions.Length)] + "\n\n" + closings[Random.Range(0, closings.Length)] + "\n\n" + signoffs[Random.Range(0, signoffs.Length)];
    }

    string[] GenerateObjectives(string problem)
    {
        List<string> objectives = new List<string>();
        objectives.Add("Diagnose the issue");
        switch (problem)
        {
            case "Blue Screen of Death": objectives.Add("Check RAM and storage"); objectives.Add("Replace faulty component"); break;
            case "No Display on Monitor": objectives.Add("Check GPU seating"); objectives.Add("Test with a different GPU"); break;
            case "PC Keeps Overheating": objectives.Add("Clean dust from components"); objectives.Add("Check CPU cooler"); break;
            case "Won't Turn On": objectives.Add("Check PSU connections"); objectives.Add("Test power supply"); break;
            case "Full of Dust": objectives.Add("Clean all components thoroughly"); objectives.Add("Check for heat damage"); break;
            case "Random Shutdowns": objectives.Add("Check PSU wattage capacity"); objectives.Add("Monitor CPU temperatures"); break;
            case "Loud Fan Noise": objectives.Add("Inspect fan bearings"); objectives.Add("Clean or replace noisy fans"); break;
            case "Slow Performance": objectives.Add("Check storage health"); objectives.Add("Verify RAM compatibility"); break;
            case "Boot Loop": objectives.Add("Reseat RAM sticks"); objectives.Add("Check BIOS status"); break;
            case "No Internet Connection": objectives.Add("Test onboard network adapter"); objectives.Add("Consider PCIe network card"); break;
            default: objectives.Add("Replace broken part"); break;
        }
        objectives.Add("Boot to Desktop");
        return objectives.ToArray();
    }

    // =============================================
    //  UTILITY
    // =============================================
    string PurposeLabel(BuildPurpose purpose)
    {
        switch (purpose)
        {
            case BuildPurpose.School: return "School";
            case BuildPurpose.Streaming: return "Streaming";
            case BuildPurpose.Gaming: return "Gaming";
            case BuildPurpose.Office: return "Office";
            default: return "General Use";
        }
    }
}

// =============================================
//  HELPER CLASSES
// =============================================
[System.Serializable]
public class RandomPCResult
{
    public GameObject casePrefab;
    public List<StartingPCComponent> parts = new List<StartingPCComponent>();
}

public class ProblemRequirement
{
    public string problemName;
    public string[] requiredCategories;

    public ProblemRequirement(string name, string[] categories)
    {
        problemName = name;
        requiredCategories = categories;
    }
}