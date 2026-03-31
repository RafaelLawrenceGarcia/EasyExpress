using UnityEngine;
using System.Collections.Generic;

public class AutoBuildWorkstationPC : MonoBehaviour
{
    [Header("Workstation Links")]
    [Tooltip("The placement slot where the PC will sit.")]
    public SlotData targetSlot;

    [Header("Database Setup")]
    [Tooltip("Drag your PC Part Database here!")]
    public PCPartDatabase partDatabase;

    [Header("Auto-Boot Settings")]
    public bool autoPlugInPower = true;
    public bool autoTurnOn = true;

    void Start()
    {
        // 1. Double check we aren't overriding an existing PC

        if (targetSlot != null && targetSlot.isOccupied) return;
        if (partDatabase == null)
        {
            Debug.LogError("[AutoBuild] No PC Part Database assigned!");
            return;
        }

        // 2. Pick a random Case from the database and spawn it
        GameObject casePrefab = partDatabase.cases[Random.Range(0, partDatabase.cases.Length)];
        GameObject newPC = Instantiate(casePrefab, targetSlot.transform);
        newPC.transform.localPosition = Vector3.zero;
        newPC.transform.localRotation = Quaternion.identity;

        // 3. Assemble a guaranteed working parts list from the database
        List<StartingPCComponent> partsToInstall = new List<StartingPCComponent>();

        // We forcefully grab one of every required part so it always turns on
        AddRandomPart(partDatabase.motherboards, partsToInstall);
        AddRandomPart(partDatabase.cpus, partsToInstall);
        AddRandomPart(partDatabase.coolers, partsToInstall);
        AddRandomPart(partDatabase.rams, partsToInstall);
        AddRandomPart(partDatabase.gpus, partsToInstall);
        AddRandomPart(partDatabase.storage, partsToInstall);
        AddRandomPart(partDatabase.psus, partsToInstall);

        // 4. Command the builder to install all the generated parts
        PCCaseBuilder builder = newPC.GetComponent<PCCaseBuilder>();
        if (builder != null)
        {
            builder.BuildFromData(partsToInstall);
        }

        // 5. Lock it into the workstation slot so the Monitor can find it!
        if (targetSlot != null)
        {
            targetSlot.isOccupied = true;
            targetSlot.currentItem = newPC;
        }

        // 6. Handle the Power & Boot sequence        // 6. Handle the Power & Boot sequence
        PCPowerSystem powerSystem = newPC.GetComponent<PCPowerSystem>();
        if (powerSystem != null)
        {
            powerSystem.skipStartReset = true;

            if (autoPlugInPower)
                powerSystem.isPowerCordConnected = true;

            if (autoTurnOn)
            {
                powerSystem.TryTogglePower(out string reason);
                Debug.Log($"[AutoBuild] Workstation PC Boot: {reason}");
            }
        }
    }

    // Helper function to safely grab a random part and ensure it is fault-free
    void AddRandomPart(StartingPCComponent[] pool, List<StartingPCComponent> list)
    {
        if (pool == null || pool.Length == 0) return;

        StartingPCComponent original = pool[Random.Range(0, pool.Length)];

        // Create a copy so we don't accidentally edit the database
        StartingPCComponent copy = new StartingPCComponent();
        copy.partCategory = original.partCategory;
        copy.partName = original.partName;
        copy.partPrefab = original.partPrefab;
        // copy.partIcon = original.partIcon; 
        // copy.compatTags = original.compatTags;
        // copy.powerDraw = original.powerDraw;
        // copy.maxWattage = original.maxWattage;

        // Force the part to be perfectly clean and working!
        copy.isDusty = false;
        copy.fault = PartFault.None;
        copy.faultDescription = "";

        list.Add(copy);
    }
}