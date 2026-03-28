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
        // We need to remember which real parts were placed
        // so we can set up the motherboard's blocking list.
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
                    partScript.partCategory = part.partCategory;
                    partScript.itemName = part.partName;
                    partScript.compatTags = part.compatTags;
                    partScript.powerDraw = part.powerDraw;
                    partScript.maxWattage = part.maxWattage;

                    // Transfer fault data
                    partScript.fault = part.fault;
                    partScript.faultDescription = part.faultDescription;

                    // Transfer the blocking rules to the real part
                    InspectableItem dummyScript = matchingDummy.GetComponent<InspectableItem>();
                    if (dummyScript != null && dummyScript.blockingParts != null)
                    {
                        partScript.blockingParts = new List<InspectableItem>(dummyScript.blockingParts);
                    }

                    // =============================================
                    //  NEW: MAKE MOTHERBOARD REMOVABLE
                    // =============================================
                    if (part.partCategory == "Motherboard")
                    {
                        partScript.isRemovable = true;
                        motherboardScript = partScript;

                        // Initialize blocking list (will be filled after all parts are placed)
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
                dummySlots.Remove(matchingDummy);
                Destroy(matchingDummy.gameObject);

                Debug.Log($"[PC Builder] Placed {part.partCategory} into {matchingDummy.name}.");
            }
            else
            {
                Debug.LogWarning($"[PC Builder] No slot found for {part.partCategory}.");
            }
        }

        // =============================================
        //  NEW: SET UP MOTHERBOARD BLOCKING
        //  Every installed part blocks the motherboard
        //  so you MUST remove all parts before you can
        //  take out the motherboard.
        // =============================================
        if (motherboardScript != null)
        {
            foreach (InspectableItem installedPart in allInstalledParts)
            {
                // Don't add the motherboard as blocking itself!
                if (installedPart == motherboardScript) continue;

                // Don't add PSU (it's not ON the motherboard)
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
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}