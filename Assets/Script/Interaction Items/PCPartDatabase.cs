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

    [Header("Job Generation")]
    public float minLabourCost = 100f;
    public float maxLabourCost = 500f;
    public float minPartsBudget = 500f;
    public float maxPartsBudget = 5000f;

    // =============================================
    //  MASTER API: Generate Random PC
    // =============================================
    public RandomPCResult GenerateRandomPC()
    {
        RandomPCResult result = new RandomPCResult();

        if (cases == null || cases.Length == 0) return result;
        result.casePrefab = cases[Random.Range(0, cases.Length)];

        bool hasMotherboard = TryAddPart(motherboards, result.parts, true);
        if (!hasMotherboard) return result;

        TryAddPart(psus, result.parts, true);
        bool hasCPU = TryAddPart(cpus, result.parts, Roll(cpuChance));
        TryAddPart(gpus, result.parts, Roll(gpuChance));

        if (rams != null && rams.Length > 0 && Roll(ramChance))
        {
            int stickCount = Random.Range(minRAMSticks, maxRAMSticks + 1);
            for (int i = 0; i < stickCount; i++) AddCopiedPart(rams[Random.Range(0, rams.Length)], result.parts);
        }

        TryAddPart(storage, result.parts, Roll(storageChance));
        if (hasCPU) TryAddPart(coolers, result.parts, Roll(coolerChance));

        if (fans != null && fans.Length > 0 && Roll(fanChance))
        {
            int fanCount = Random.Range(1, 5);
            for (int i = 0; i < fanCount; i++) AddCopiedPart(fans[Random.Range(0, fans.Length)], result.parts);
        }
        return result;
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
        RandomPCResult desiredPC = GenerateRandomPC();

        EmailData job = ScriptableObject.CreateInstance<EmailData>();
        job.jobType = JobType.Build;
        job.basePCCasePrefab = desiredPC.casePrefab;
        job.startingParts = new List<StartingPCComponent>(); // Empty case
        job.requestedParts = desiredPC.parts;

        job.labourCost = Mathf.Round(Random.Range(minLabourCost * 1.5f, maxLabourCost * 2f));
        job.partsBudget = Mathf.Round(Random.Range(minPartsBudget, maxPartsBudget));

        job.pcProblems = new string[] { "New PC Build" };
        job.originalFaultCount = 0;
        job.objectives = GenerateBuildObjectives(desiredPC.parts);
        job.senderName = GenerateRandomName();
        job.subjectLine = GenerateBuildSubjectLine();
        job.bodyText = GenerateBuildEmailBody(job.senderName, desiredPC.parts, job.partsBudget);

        return job;
    }

    public EmailData GenerateRandomEmailJob()
    {
        RandomPCResult pc = GenerateRandomPC();

        EmailData job = ScriptableObject.CreateInstance<EmailData>();
        job.jobType = JobType.Repair;
        job.basePCCasePrefab = pc.casePrefab;
        job.startingParts = pc.parts;

        job.labourCost = Mathf.Round(Random.Range(minLabourCost, maxLabourCost));
        job.partsBudget = Mathf.Round(Random.Range(minPartsBudget, maxPartsBudget));

        string[] problems = new string[]
        {
            "Blue Screen of Death", "No Display on Monitor", "PC Keeps Overheating",
            "Won't Turn On", "Full of Dust", "Random Shutdowns", "Loud Fan Noise",
            "Slow Performance", "Boot Loop", "No Internet Connection"
        };
        job.pcProblems = new string[] { problems[Random.Range(0, problems.Length)] };

        ApplyFaultToPC(job.pcProblems[0], pc.parts);

        int faultCount = 0;
        foreach (var part in pc.parts)
        {
            if (part.fault != PartFault.None) faultCount++;
        }
        job.originalFaultCount = faultCount;

        job.objectives = GenerateObjectives(job.pcProblems[0]);
        job.senderName = GenerateRandomName();
        job.subjectLine = GenerateSubjectLine(job.pcProblems[0]);
        job.bodyText = GenerateEmailBody(job.senderName, job.pcProblems[0], job.partsBudget);

        return job;
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
    string[] GenerateBuildObjectives(List<StartingPCComponent> requestedParts)
    {
        List<string> objectives = new List<string>();
        objectives.Add("Build the PC from scratch");
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

    string GenerateBuildSubjectLine()
    {
        string[] templates = { "Custom PC Build Request", "Need a new PC built", "PC Build Order", "Build me a computer!", "New PC — Can you build it?", "Custom Build Request" };
        return templates[Random.Range(0, templates.Length)];
    }

    string GenerateBuildEmailBody(string senderName, List<StartingPCComponent> parts, float budget)
    {
        string[] greetings = { "Hi there,", "Hello,", "Hey,", "Good day," };
        string[] intros = { "I'd like to have a custom PC built. Here's what I need:", "Can you build me a new PC? I have a specific parts list:", "I'm looking for someone to assemble a PC for me. Here are the specs:", "I need a brand new PC built from scratch. My requirements:" };

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

        string[] closings = { $"\nMy budget is around ₱{budget:N0}. Thanks!", $"\nI can spend up to ₱{budget:N0} on this build. Let me know!", $"\nBudget: ₱{budget:N0}. Looking forward to the build!", $"\nI've set aside ₱{budget:N0} for this. Hope you can do it!" };
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

    string GenerateEmailBody(string senderName, string problem, float budget)
    {
        string[] greetings = { "Hi there,", "Hello,", "Hey,", "Good day," };
        string[] descriptions = { $"My PC has been giving me trouble lately. The issue is: {problem}.", $"Something's wrong with my PC. I think the problem is {problem}." };
        string[] closings = { $"My budget is around ₱{budget:N0}. Let me know if you can help!", $"I can spend up to ₱{budget:N0} on this. Thanks in advance!" };
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
            default: objectives.Add("Replace broken part"); break;
        }
        objectives.Add("Boot to Desktop");
        return objectives.ToArray();
    }
}

[System.Serializable]
public class RandomPCResult
{
    public GameObject casePrefab;
    public List<StartingPCComponent> parts = new List<StartingPCComponent>();
}