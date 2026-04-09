using UnityEngine;
using System.Collections.Generic;

public class PCCaseBuilder : MonoBehaviour
{
    // Link this PC back to the email job it came from
    [HideInInspector] public EmailData linkedEmail;

    public void BuildFromData(List<StartingPCComponent> partsToInstall)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        List<Transform> dummySlots = new List<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child.name.StartsWith("Slot_"))
                dummySlots.Add(child);
        }

        // =============================================
        //  TRACK INSTALLED PARTS FOR BLOCKING SYSTEM
        // =============================================
        InspectableItem motherboardScript = null;
        List<InspectableItem> allInstalledParts = new List<InspectableItem>();

        foreach (StartingPCComponent part in partsToInstall)
        {
            if (part.partPrefab == null)
            {
                Debug.LogWarning($"[PC Builder] Skipping {part.partCategory} - prefab missing!");
                continue;
            }

            Transform matchingDummy = dummySlots.Find(d => d.name.Contains(part.partCategory));
            if (matchingDummy != null)
            {
                GameObject realPart = Instantiate(part.partPrefab, matchingDummy.parent);
                realPart.transform.localPosition = matchingDummy.localPosition;
                realPart.transform.localRotation = matchingDummy.localRotation;

                // Set category on the root part
                InspectableItem partScript = realPart.GetComponent<InspectableItem>();
                if (partScript != null)
                {
                    string prefabDefaultName = partScript.itemName;

                    partScript.partCategory = part.partCategory;
                    partScript.itemName = part.partName;
                    partScript.compatTags = part.compatTags;
                    partScript.powerDraw = part.powerDraw;
                    partScript.maxWattage = part.maxWattage;

                    // Transfer fault data
                    partScript.fault = part.fault;
                    partScript.faultDescription = part.faultDescription;

                    // Rename direct children that still have the prefab's default name
                    foreach (InspectableItem child in realPart.GetComponentsInChildren<InspectableItem>(true))
                    {
                        if (child == partScript) continue;

                        // Skip children placed by earlier iterations
                        if (child.isRemovable && child.partCategory != part.partCategory) continue;

                        // Only update children with the same default name
                        if (child.itemName == prefabDefaultName)
                        {
                            child.itemName = part.partName;
                        }
                    }

                    // Transfer the blocking rules to the real part
                    InspectableItem dummyScript = matchingDummy.GetComponent<InspectableItem>();
                    if (dummyScript != null && dummyScript.blockingParts != null)
                    {
                        partScript.blockingParts = new List<InspectableItem>(dummyScript.blockingParts);
                    }

                    // =============================================
                    //  MAKE MOTHERBOARD REMOVABLE
                    // =============================================
                    if (part.partCategory == "Motherboard")
                    {
                        partScript.isRemovable = true;
                        motherboardScript = partScript;

                        if (partScript.blockingParts == null)
                            partScript.blockingParts = new List<InspectableItem>();
                    }

                    // Track all non-motherboard parts for blocking
                    allInstalledParts.Add(partScript);
                }

                // Also set category on ALL child InspectableItems
                foreach (InspectableItem childScript in realPart.GetComponentsInChildren<InspectableItem>(true))
                {
                    if (string.IsNullOrEmpty(childScript.partCategory) || childScript.partCategory == "Generic")
                    {
                        childScript.partCategory = part.partCategory;
                    }
                }

                SetLayerRecursively(realPart, LayerMask.NameToLayer("Ignore Raycast"));

                // =============================================
                //  REFRESH SLOT LIST
                // =============================================
                dummySlots.Remove(matchingDummy);

                // Remove old slots that were children of the dummy being destroyed
                dummySlots.RemoveAll(s => s != null && s.IsChildOf(matchingDummy));

                // Add new Slot_ children from the placed part into the pool
                foreach (Transform newChild in realPart.GetComponentsInChildren<Transform>(true))
                {
                    if (newChild.name.StartsWith("Slot_") && !dummySlots.Contains(newChild))
                        dummySlots.Add(newChild);
                }

                Destroy(matchingDummy.gameObject);

                Debug.Log($"[PC Builder] Placed {part.partCategory} into {matchingDummy.name}.");
            }
            else
            {
                Debug.LogWarning($"[PC Builder] No slot found for {part.partCategory}.");
            }
        }

        // =============================================
        //  SET UP MOTHERBOARD BLOCKING
        // =============================================
        if (motherboardScript != null)
        {
            foreach (InspectableItem installedPart in allInstalledParts)
            {
                if (installedPart == motherboardScript) continue;
                if (installedPart.partCategory == "PSU") continue;

                if (!motherboardScript.blockingParts.Contains(installedPart))
                {
                    motherboardScript.blockingParts.Add(installedPart);
                }
            }

            Debug.Log($"[PC Builder] Motherboard is removable. Blocked by {motherboardScript.blockingParts.Count} parts.");
        }

        // Turn leftover Slot_ dummies into proper ghost slots
        foreach (Transform leftoverDummy in dummySlots)
        {
            if (leftoverDummy == null) continue;
            string category = leftoverDummy.name.Replace("Slot_", "").Trim();
            InspectableItem ghostScript = leftoverDummy.gameObject.GetComponent<InspectableItem>();
            if (ghostScript == null)
                ghostScript = leftoverDummy.gameObject.AddComponent<InspectableItem>();
            ghostScript.partCategory = category;
            ghostScript.itemName = category + " Slot";
            ghostScript.itemDescription = "Install a " + category + " here.";
            ghostScript.isInventorySlot = true;
            ghostScript.isRemovable = false;

            foreach (Renderer r in leftoverDummy.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            foreach (Collider col in leftoverDummy.GetComponentsInChildren<Collider>())
                col.enabled = false;
            Debug.Log($"[PC Builder] Created ghost slot for: {category}");
        }

        // Apply dust if any part flagged it
        bool shouldBeDusty = false;
        foreach (StartingPCComponent part in partsToInstall)
        {
            if (part.isDusty) { shouldBeDusty = true; break; }
        }

        if (shouldBeDusty)
        {
            DustSystem dust = GetComponent<DustSystem>();
            if (dust == null) dust = gameObject.AddComponent<DustSystem>();
            dust.isDusty = true;
        }

        // =============================================
        //  AUTO-ADD PCPowerSystem
        // =============================================
        PCPowerSystem powerSystem = GetComponent<PCPowerSystem>();
        if (powerSystem == null)
            powerSystem = gameObject.AddComponent<PCPowerSystem>();

        powerSystem.isPoweredOn = false;
        powerSystem.isPowerCordConnected = false;

        Transform cordSlot = transform.Find("PowerCordSlot");
        if (cordSlot != null)
            powerSystem.powerCordSnapPoint = cordSlot;

        Debug.Log($"[PC Builder] PCPowerSystem added — PC starts OFF, fans disabled.");

        // =============================================
        //  AUTO-CONNECT PRE-BUILT WIRES
        // =============================================
        AutoConnectPrebuiltWires();
    }

    /// <summary>
    /// Scans all IPrebuiltWire components on this case and auto-connects
    /// internal wires (24-pin, 8-pin, SATA, fan wires) if their required
    /// component is installed. The power cord is skipped — the player
    /// must plug that in themselves during inspection.
    /// </summary>
    void AutoConnectPrebuiltWires()
    {
        int connectedCount = 0;

        foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            IPrebuiltWire wire = mb as IPrebuiltWire;
            if (wire == null) continue;

            // Power cord is always player-connected manually
            if (wire.IsPowerCord)
            {
                Debug.Log($"[PC Builder] Skipping power cord '{wire.WireName}'.");
                continue;
            }

            // ── Guard: skip wires whose required category was never set ──
            if (string.IsNullOrEmpty(wire.RequiredPartCategory))
            {
                Debug.LogWarning($"[PC Builder] Wire '{wire.WireName}' has no " +
                                 "requiredPartCategory assigned — skipping auto-connect. " +
                                 "Set the category on its PrebuiltWire component.");
                continue;
            }

            if (wire.IsRequiredComponentInstalled(transform))
            {
                wire.Connect(transform);
                connectedCount++;
                Debug.Log($"[PC Builder] Auto-connected '{wire.WireName}'.");
            }
            else
            {
                Debug.Log($"[PC Builder] Skipping '{wire.WireName}' — " +
                          $"{wire.RequiredPartCategory} not installed.");
            }
        }

        if (connectedCount > 0)
            Debug.Log($"[PC Builder] Auto-connected {connectedCount} wire(s).");
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}