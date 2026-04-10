// ============================================================
//  PCPartDatabase.cs — COMPATIBILITY FIX + FAULT FIX
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
    // =============================================

    private static readonly HashSet<string> SOCKET_TAGS = new HashSet<string>(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "AM4", "AM5", "LGA1151", "LGA1200", "LGA1700", "LGA1851",
        "TR4", "TRX40", "TRX50", "sTRX4", "sTR5",
        "LGA2066", "LGA4677"
    };

    private static readonly HashSet<string> MEMORY_TAGS = new HashSet<string>(
        System.StringComparer.OrdinalIgnoreCase)
    {
        "DDR3", "DDR4", "DDR5"
    };

    // =============================================
    //  COMPATIBILITY FILTER
    // =============================================

    public StartingPCComponent[] FilterByCompatibility(
        StartingPCComponent[] pool,
        StartingPCComponent motherboard,
        string tagCategory)
    {
        if (pool == null || pool.Length == 0) return pool;
        if (motherboard == null || motherboard.compatTags == null) return pool;

        HashSet<string> relevantTagSet = (tagCategory == "memory") ? MEMORY_TAGS : SOCKET_TAGS;

        HashSet<string> moboRelevantTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string tag in motherboard.compatTags)
        {
            string trimmed = tag.Trim();
            if (relevantTagSet.Contains(trimmed))
                moboRelevantTags.Add(trimmed);
        }

        if (moboRelevantTags.Count == 0)
        {
            Debug.LogWarning($"[PCPartDB] Motherboard '{motherboard.partName}' has no {tagCategory} tags — skipping filter.");
            return pool;
        }

        List<StartingPCComponent> compatible = new List<StartingPCComponent>();
        foreach (StartingPCComponent part in pool)
        {
            if (part.compatTags == null || part.compatTags.Length == 0)
            {
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
    // =============================================
    public RandomPCResult GenerateRandomPC()
    {
        RandomPCResult result = new RandomPCResult();

        if (cases == null || cases.Length == 0) return result;
        result.casePrefab = cases[Random.Range(0, cases.Length)];

        if (motherboards == null || motherboards.Length == 0) return result;
        StartingPCComponent chosenMotherboard = motherboards[Random.Range(0, motherboards.Length)];
        AddCopiedPart(chosenMotherboard, result.parts);

        TryAddPart(psus, result.parts, true);

        StartingPCComponent[] compatCPUs = FilterByCompatibility(cpus, chosenMotherboard, "socket");
        bool hasCPU = TryAddPart(compatCPUs, result.parts, Roll(cpuChance));

        TryAddPart(gpus, result.parts, Roll(gpuChance));

        StartingPCComponent[] compatRAM = FilterByCompatibility(rams, chosenMotherboard, "memory");
        if (compatRAM != null && compatRAM.Length > 0 && Roll(ramChance))
        {
            int stickCount = Random.Range(minRAMSticks, maxRAMSticks + 1);
            StartingPCComponent chosenRam = compatRAM[Random.Range(0, compatRAM.Length)];
            for (int i = 0; i < stickCount; i++) AddCopiedPart(chosenRam, result.parts);
        }

        TryAddPart(storage, result.parts, Roll(storageChance));

        if (hasCPU)
        {
            StartingPCComponent[] compatCoolers = FilterByCompatibility(coolers, chosenMotherboard, "socket");
            TryAddPart(compatCoolers, result.parts, true);
        }

        if (fans != null && fans.Length > 0)
        {
            int fanCount = Random.Range(3, 5);
            for (int i = 0; i < fanCount; i++) AddCopiedPart(fans[Random.Range(0, fans.Length)], result.parts);
        }

        return result;
    }

    // =============================================
    //  PURPOSE-DRIVEN PC GENERATION (for Build jobs)
    // =============================================
    public RandomPCResult GeneratePCForPurpose(BuildPurpose purpose)
    {
        RandomPCResult result = new RandomPCResult();

        if (cases == null || cases.Length == 0) return result;
        result.casePrefab = cases[Random.Range(0, cases.Length)];

        if (motherboards == null || motherboards.Length == 0) return result;
        StartingPCComponent chosenMotherboard = motherboards[Random.Range(0, motherboards.Length)];
        AddCopiedPart(chosenMotherboard, result.parts);

        TryAddPart(psus, result.parts, true);

        StartingPCComponent[] compatCPUs = FilterByCompatibility(cpus, chosenMotherboard, "socket");
        StartingPCComponent[] compatRAM = FilterByCompatibility(rams, chosenMotherboard, "memory");
        StartingPCComponent[] compatCoolers = FilterByCompatibility(coolers, chosenMotherboard, "socket");

        switch (purpose)
        {
            case BuildPurpose.School:
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, Roll(30f));
                AddRAMSticks(result.parts, 1, 2, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 3, 4, 100f);
                break;

            case BuildPurpose.Office:
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, Roll(60f));
                AddRAMSticks(result.parts, 2, 2, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 3, 4, 100f);
                break;

            case BuildPurpose.Streaming:
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, true);
                AddRAMSticks(result.parts, 2, 4, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 3, 4, 100f);
                break;

            case BuildPurpose.Gaming:
                TryAddPart(compatCPUs, result.parts, true);
                TryAddPart(gpus, result.parts, true);
                AddRAMSticks(result.parts, 2, 4, compatRAM);
                TryAddPart(storage, result.parts, true);
                TryAddPart(compatCoolers, result.parts, true);
                TryAddFans(result.parts, 3, 4, 100f);
                break;
        }

        return result;
    }

    // =============================================
    //  RAM / FAN HELPERS
    // =============================================
    void AddRAMSticks(List<StartingPCComponent> parts, int min, int max, StartingPCComponent[] ramPool = null)
    {
        StartingPCComponent[] pool = (ramPool != null && ramPool.Length > 0) ? ramPool : rams;
        if (pool == null || pool.Length == 0) return;

        int count = Random.Range(min, max + 1);
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
    public float CalculateReward(List<StartingPCComponent> parts, bool isBuild)
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
    //  MASTER JOB GENERATOR
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

        // ═══ CLEAR ALL FAULTS — build jobs have no faults ═══
        foreach (var part in desiredPC.parts)
        {
            part.fault = PartFault.None;
            part.faultDescription = "";
            part.isDusty = false;
        }

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

        // ═══════════════════════════════════════════════════════════
        //  CRITICAL FIX: Clear ALL inherited faults from database
        //  entries BEFORE ApplyFaultToPC assigns the real ones.
        //  Without this, parts from the Inspector that have stale
        //  fault values (e.g. Corrupted on fans) bleed through.
        // ═══════════════════════════════════════════════════════════
        foreach (var part in pc.parts)
        {
            part.fault = PartFault.None;
            part.faultDescription = "";
            part.isDusty = false;
        }

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

        // ═══════════════════════════════════════════════════════
        //  PROGRESSIVE PROBLEM UNLOCKING
        //  Day 1-3:   Tier 1 — No Display, Boot Loop (GPU/RAM)
        //  Day 4-6:   Tier 2 — + Blue Screen, Slow Performance
        //  Day 7-9:   Tier 3 — + Overheating, Loud Fan
        //  Day 10-12: Tier 4 — + Won't Turn On, Random Shutdowns
        //  Day 13+:   Tier 5 — + Full of Dust
        // ═══════════════════════════════════════════════════════

        int currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        int tier = Mathf.Clamp(((currentDay - 1) / 3) + 1, 1, 5);

        List<ProblemRequirement> allProblems = new List<ProblemRequirement>();

        // Tier 1 (Day 1-3): GPU/RAM problems
        allProblems.Add(new ProblemRequirement("No Display on Monitor", new string[] { "GPU", "RAM" }));
        allProblems.Add(new ProblemRequirement("Boot Loop", new string[] { "RAM", "Motherboard", "GPU" }));

        // Tier 2 (Day 4+)
        if (tier >= 2)
        {
            allProblems.Add(new ProblemRequirement("Blue Screen of Death", new string[] { "RAM", "Storage" }));
            allProblems.Add(new ProblemRequirement("Slow Performance", new string[] { "Storage", "RAM" }));
        }

        // Tier 3 (Day 7+)
        if (tier >= 3)
        {
            allProblems.Add(new ProblemRequirement("PC Keeps Overheating", new string[] { "Cooler", "Fan" }));
            allProblems.Add(new ProblemRequirement("Loud Fan Noise", new string[] { "Fan", "Cooler" }));
        }

        // Tier 4 (Day 10+)
        if (tier >= 4)
        {
            allProblems.Add(new ProblemRequirement("Won't Turn On", new string[] { "PSU", "Motherboard" }));
            allProblems.Add(new ProblemRequirement("Random Shutdowns", new string[] { "PSU", "CPU" }));
        }

        // Tier 5 (Day 13+)
        if (tier >= 5)
        {
            allProblems.Add(new ProblemRequirement("Full of Dust", new string[] { }));
        }

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

        if (validProblems.Count == 0) return "No Display on Monitor";

        string chosen = validProblems[Random.Range(0, validProblems.Count)];
        Debug.Log($"[PCPartDB] Day {currentDay} (Tier {tier}): Picked '{chosen}' from {validProblems.Count} problems.");
        return chosen;
    }

    // =============================================
    //  FAULT ASSIGNMENT
    //
    //  RULES:
    //  - Only ONE primary fault per problem (the first TryFault
    //    that succeeds breaks out of the switch).
    //  - Fans NEVER get Corrupted — they're mechanical parts.
    //    Valid fan faults: Broken (bearing worn out), Dusty
    //    (blades clogged), LooseConnection (cable loose).
    //  - Overheating / thermal paste only assigned by Tier 3+
    //    problems (PC Keeps Overheating, Random Shutdowns).
    //  - GPU NotSeated can cause both No Display AND Boot Loop.
    // =============================================
    void ApplyFaultToPC(string problem, List<StartingPCComponent> parts)
    {
        if (parts == null || parts.Count == 0) return;

        switch (problem)
        {
            case "No Display on Monitor":
                if (Roll(60f))
                {
                    if (TryFaultByCategory(parts, "GPU", PartFault.NotSeated,
                        "GPU not fully inserted into PCIe slot")) break;
                }
                TryFaultByCategory(parts, "GPU", PartFault.Broken,
                    "GPU is dead — no display output");
                break;

            case "Boot Loop":
                if (Roll(35f))
                {
                    if (TryFaultByCategory(parts, "GPU", PartFault.NotSeated,
                        "GPU not seated properly — system keeps restarting")) break;
                }
                TryFaultByCategory(parts, "RAM", PartFault.NotSeated,
                    "RAM not seated correctly — system fails POST and restarts");
                break;

            case "Blue Screen of Death":
                if (TryFaultByCategory(parts, "RAM", PartFault.NotSeated,
                    "RAM stick not seated properly — push until it clicks")) break;
                TryFaultByCategory(parts, "Storage", PartFault.Corrupted,
                    "Storage drive has corrupted sectors");
                break;

            case "PC Keeps Overheating":
                if (TryFaultByCategory(parts, "Cooler", PartFault.Overheating,
                    "Thermal paste dried out — cooler not making proper contact")) break;
                TryFaultByCategory(parts, "Fan", PartFault.Dusty,
                    "Fan blades clogged with dust — barely spinning");
                break;

            case "Won't Turn On":
                if (TryFaultByCategory(parts, "PSU", PartFault.Broken,
                    "PSU is dead — no power output detected")) break;
                TryFaultByCategory(parts, "Motherboard", PartFault.LooseConnection,
                    "24-pin ATX power cable loose — reseat the connection");
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
                if (TryFaultByCategory(parts, "PSU", PartFault.Overloaded,
                    "PSU wattage too low for this build — causes random shutdowns")) break;
                TryFaultByCategory(parts, "CPU", PartFault.Overheating,
                    "CPU throttling due to heat — shuts down to protect itself");
                break;

            case "Loud Fan Noise":
                if (TryFaultByCategory(parts, "Fan", PartFault.Broken,
                    "Fan bearing worn out — grinding noise")) break;
                TryFaultByCategory(parts, "Cooler", PartFault.Dusty,
                    "Cooler fan clogged with dust — running at max RPM");
                break;

            case "Slow Performance":
                if (TryFaultByCategory(parts, "Storage", PartFault.Outdated,
                    "Old HDD failing — extremely slow read/write speeds")) break;
                TryFaultByCategory(parts, "RAM", PartFault.Incompatible,
                    "RAM running at wrong speed — not compatible with this motherboard");
                break;

            default:
                // Do NOT assign random faults — unknown problems get no fault
                Debug.LogWarning($"[PCPartDB] Unknown problem '{problem}' — no fault assigned.");
                break;
        }

        // ═══ SAFETY SWEEP: strip invalid faults ═══
        foreach (var p in parts)
        {
            if (p.fault == PartFault.None) continue;

            // Fans are mechanical — never Corrupted, Incompatible, Outdated, or Overheating
            if (p.partCategory == "Fan" &&
                (p.fault == PartFault.Corrupted ||
                 p.fault == PartFault.Incompatible ||
                 p.fault == PartFault.Outdated ||
                 p.fault == PartFault.Overheating))
            {
                Debug.LogWarning($"[PCPartDB] Stripped invalid fault '{p.fault}' from fan '{p.partName}'");
                p.fault = PartFault.None;
                p.faultDescription = "";
            }
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
        // Exclude fans from random fallback faults — only mechanical issues
        List<StartingPCComponent> candidates = new List<StartingPCComponent>();
        foreach (var p in parts)
        {
            if (p.partCategory != "Fan") candidates.Add(p);
        }
        if (candidates.Count == 0) candidates.AddRange(parts);

        StartingPCComponent target = candidates[Random.Range(0, candidates.Count)];
        PartFault[] possibleFaults = { PartFault.NotSeated, PartFault.Broken, PartFault.Dusty, PartFault.LooseConnection };
        string[] descriptions = {
            $"{target.partName}: not seated properly",
            $"{target.partName}: appears to be defective",
            $"{target.partName}: covered in dust",
            $"{target.partName}: has a loose connection"
        };
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
        // Always copy as clean — faults are applied later by ApplyFaultToPC
        copy.isDusty = false;
        copy.fault = PartFault.None;
        copy.faultDescription = "";
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
        string[] signoffs = { $"Thanks,\n{senderName}", $"Best regards,\n{senderName}",
                             $"Hoping to hear from you soon,\n{senderName}", $"Cheers,\n{senderName}" };

        string symptomParagraph;
        switch (problem)
        {
            case "No Display on Monitor":
                symptomParagraph = GetRandom(new[]
                {
                "My PC turns on — the fans spin and the lights come on — but nothing appears on my monitor. The screen stays completely black and just shows 'No Signal'. I've already checked the cable and the monitor works fine on another device.",
                "When I press the power button my computer seems to start up, but my display never gets a picture. The fans are running and there are lights on the motherboard, yet the screen stays dark the whole time.",
                "I'm getting no picture on my monitor even though the PC itself appears to be running. Everything powers up but no image ever shows. I've tried a different cable and a different monitor — same result."
            });
                break;

            case "Blue Screen of Death":
                symptomParagraph = GetRandom(new[]
                {
                "My computer keeps crashing with a blue screen a few minutes after I start using it. It restarts on its own and sometimes I can't even get to the desktop before it crashes again.",
                "I've been getting random blue screen errors. The PC reboots itself and I keep losing whatever I was working on. It seems to happen more often when I open a browser or run any program.",
                "My PC randomly crashes to a blue screen and then restarts. This has been happening several times a day and it's making the computer basically unusable for me."
            });
                break;

            case "PC Keeps Overheating":
                symptomParagraph = GetRandom(new[]
                {
                "My computer shuts itself off after running for a while, especially when I'm using it heavily. The case feels very warm and the fans seem to be working overtime before it switches off.",
                "My PC keeps turning off on its own. It usually lasts about ten to twenty minutes before it just cuts out. The fans are really loud too. I suspect it might be overheating.",
                "My computer has been randomly shutting down. The fans get very loud before it happens and the whole case feels hot. It boots back up fine but then shuts off again after a while."
            });
                break;

            case "Won't Turn On":
                symptomParagraph = GetRandom(new[]
                {
                "My PC does absolutely nothing when I press the power button. No fans, no lights, no beeps — just complete silence. It was working yesterday and now it won't respond at all.",
                "I press the power button and nothing happens. The computer is completely dead — no fans spinning, no LED lights, nothing. I've checked the power outlet and it's fine.",
                "My computer stopped turning on overnight. I haven't changed anything or dropped it. I press the power button and there's zero response from the machine whatsoever."
            });
                break;

            case "Full of Dust":
                symptomParagraph = GetRandom(new[]
                {
                "My PC is running much slower and hotter than usual and the fans are very loud. A friend told me it might need a good clean inside. I haven't opened the case in years so there's probably a lot of dust built up.",
                "My computer has been running hot and noisy lately. I think it just needs a thorough cleaning — I can see dust coming out of the vents. I'd rather have a professional do it properly.",
                "The PC runs warm all the time now and the fans never quiet down. I peeked inside and there's dust coating everything. It needs a proper clean but I don't want to accidentally break anything."
            });
                break;

            case "Random Shutdowns":
                symptomParagraph = GetRandom(new[]
                {
                "My PC turns off randomly while I'm using it — no warning, no blue screen, just instant power off. It usually happens when I'm doing something intensive like gaming or video editing.",
                "My computer has been shutting off by itself at random. It doesn't overheat as far as I can tell — it just cuts out suddenly, sometimes after five minutes, sometimes after an hour.",
                "I've been having random power-offs with no error messages. The computer just dies without warning while I'm in the middle of using it and I have to press the power button to restart."
            });
                break;

            case "Loud Fan Noise":
                symptomParagraph = GetRandom(new[]
                {
                "One of the fans inside my PC has started making a grinding or rattling noise. It's been getting louder over the past week and is now impossible to ignore while I'm working.",
                "My computer has developed a loud rattling noise that I think is coming from a fan. The PC still works, but the noise is really distracting. I'd like it checked and repaired.",
                "There's a persistent grinding sound coming from inside my case whenever the PC is running. It sounds like a fan bearing has gone bad. The noise is constant and getting worse."
            });
                break;

            case "Slow Performance":
                symptomParagraph = GetRandom(new[]
                {
                "My computer has become extremely slow. It takes a very long time to boot and programs take ages to open. It used to be fast but now even basic tasks feel sluggish.",
                "Everything on my PC has slowed to a crawl. Loading the desktop alone takes several minutes and apps hang or freeze regularly. This has been getting worse over the past few weeks.",
                "My PC is running far slower than it should. Opening files, launching programs, browsing the web — all of it is painfully slow. I haven't installed anything new but it just keeps getting worse."
            });
                break;

            case "Boot Loop":
                symptomParagraph = GetRandom(new[]
                {
                "My computer gets stuck in a restart loop. It shows the startup screen, sometimes gets partway through loading, and then restarts on its own — over and over without ever reaching the desktop.",
                "My PC keeps restarting itself automatically. It starts to boot, the logo appears, and then it just reboots. It's been cycling like this and I can't get into Windows at all.",
                "The computer restarts itself every time it tries to load. I see the startup logo briefly and then it reboots. It just keeps cycling and never finishes starting up."
            });
                break;

            default:
                symptomParagraph = $"My PC has been having a problem — specifically: {problem.ToLower()}. It's been happening consistently and I'm not able to fix it on my own.";
                break;
        }

        string[] closings =
        {
        $"I understand a repair like this may require replacement parts as well as labour, and I'm happy to cover both. My total budget for parts and labour is ₱{reward:N0}. Please let me know if you can help!",
        $"I'm willing to pay for any parts that need replacing on top of your labour fee. I've set aside ₱{reward:N0} in total to cover everything. Looking forward to hearing from you.",
        $"Please go ahead and replace whatever components need replacing — I just want it working again. The total I can pay for parts and labour combined is ₱{reward:N0}.",
        $"I know some parts might need to be swapped out and that's absolutely fine with me. As long as the total for parts and labour is around ₱{reward:N0}, I'm ready to proceed."
    };

        return greetings[Random.Range(0, greetings.Length)]
             + "\n\n"
             + symptomParagraph
             + "\n\n"
             + closings[Random.Range(0, closings.Length)]
             + "\n\n"
             + signoffs[Random.Range(0, signoffs.Length)];
    }

    static string GetRandom(string[] options)
    {
        return options[Random.Range(0, options.Length)];
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
            case "Boot Loop": objectives.Add("Reseat RAM and GPU"); objectives.Add("Check BIOS status"); break;
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