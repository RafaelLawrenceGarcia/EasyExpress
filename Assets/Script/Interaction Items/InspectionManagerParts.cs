// ============================================================
//  InspectionManager.Parts.cs  (partial class 3/6)
//  Part removal, installation, compatibility, ghost slots
// ============================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class InspectionManager
{
    // ─── Compatibility ───────────────────────────────────────────

    public bool IsPartCompatible(InspectableItem slot, InspectableItem part)
    {
        if (slot.partCategory != part.partCategory) return false;

        if (slot.requiredTags != null && slot.requiredTags.Length > 0)
        {
            if (part.compatTags == null || part.compatTags.Length == 0) return false;
            foreach (string reqTag in slot.requiredTags)
            {
                bool hasTag = false;
                foreach (string partTag in part.compatTags)
                    if (reqTag.Trim().ToLower() == partTag.Trim().ToLower()) { hasTag = true; break; }
                if (!hasTag) return false;
            }
        }
        return true;
    }

    // ─── Ghost Slot Management ───────────────────────────────────

    public void PrepareInstallationFromUI(GameObject partObj, InspectableItem partData)
    {
        if (currentClone == null) return;
        partPendingInstallation = partObj;
        isPlacingFromInventory = true;

        foreach (Collider col in currentClone.GetComponentsInChildren<Collider>(true)) col.enabled = false;

        foreach (var item in currentClone.GetComponentsInChildren<InspectableItem>(true))
        {
            if (!item.isInventorySlot) continue;

            if (IsPartCompatible(item, partData))
            {
                foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true))
                {
                    if (ghostMaterial != null)
                    {
                        Material[] ghostMats = new Material[rend.sharedMaterials.Length];
                        for (int i = 0; i < ghostMats.Length; i++) ghostMats[i] = ghostMaterial;
                        rend.sharedMaterials = ghostMats;
                    }
                    rend.enabled = true;
                }
                foreach (Collider col in item.GetComponentsInChildren<Collider>(true)) col.enabled = true;
            }
            else
            {
                foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true)) rend.enabled = false;
            }
        }
    }

    public void HideAllGhostSlots()
    {
        if (currentClone == null) return;
        partPendingInstallation = null;
        isPlacingFromInventory = false;

        InspectableItem[] allItems = currentClone.GetComponentsInChildren<InspectableItem>(true);

        foreach (var item in allItems)
        {
            if (!item.isInventorySlot)
            {
                if (item.gameObject == currentClone || IsCaseShellObject(item.gameObject)) continue;
                foreach (Collider col in item.GetComponentsInChildren<Collider>(true)) col.enabled = true;
            }
        }

        foreach (var item in allItems)
        {
            if (item.isInventorySlot)
            {
                foreach (Renderer rend in item.GetComponentsInChildren<Renderer>(true)) rend.enabled = false;
                foreach (Collider col in item.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }
        }
    }

    // ─── Removal ─────────────────────────────────────────────────

    void BeginRemovalConfirmation(InspectableItem part)
    {
        if (!RequireCorrectToolForRemoval(part)) return;

        foreach (InspectableItem blocker in part.blockingParts)
        {
            if (blocker != null && !blocker.isInventorySlot)
            {
                ShowTooltipMessage("Blocked!", $"Remove {blocker.itemName} first.");
                return;
            }
        }

        if (screwdriverSystem != null)
        {
            isConfirmingRemoval = true;
            screwdriverSystem.onCancelled = () => { isConfirmingRemoval = false; };
            screwdriverSystem.BeginUnscrewing(part.transform, () =>
            {
                isConfirmingRemoval = false;
                TryRemovePart(part);
            });
        }
        else TryRemovePart(part);
    }

    void TryRemovePart(InspectableItem part)
    {
        foreach (InspectableItem blocker in part.blockingParts)
            if (blocker != null && !blocker.isInventorySlot) return;

        Debug.Log($"Removing {part.itemName}");
        AutoDisconnectPrebuiltWires(part);
        if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteRemoveTask(part);
        ClearHighlight();

        // Detach children that should stay behind (e.g. cooler on CPU)
        if (part.childPartsToDetach != null && part.childPartsToDetach.Count > 0)
        {
            Transform newParent = part.transform.parent;
            foreach (GameObject childObj in part.childPartsToDetach)
            {
                if (childObj == null) continue;
                Vector3 worldPos = childObj.transform.position;
                Quaternion worldRot = childObj.transform.rotation;
                childObj.transform.SetParent(newParent, true);
                childObj.transform.position = worldPos;
                childObj.transform.rotation = worldRot;

                InspectableItem childItem = childObj.GetComponent<InspectableItem>();
                if (childItem != null)
                {
                    childItem.isRemovable = true;
                    foreach (Renderer rend in childObj.GetComponentsInChildren<Renderer>(true)) rend.enabled = true;
                    foreach (Collider col in childObj.GetComponentsInChildren<Collider>(true)) col.enabled = true;
                }
            }
            part.childPartsToDetach.Clear();
        }

        // Store copy in player inventory
        GameObject storedPart = Instantiate(part.gameObject, voidAnchor.transform);
        storedPart.SetActive(false);
        SetLayerRecursively(storedPart, LayerMask.NameToLayer("Default"));

        InspectableItem storedScript = storedPart.GetComponent<InspectableItem>();
        if (storedScript != null)
        {
            if (storedScript.fault == PartFault.NotSeated ||
                storedScript.fault == PartFault.LooseConnection ||
                storedScript.fault == PartFault.WrongSlot)
            {
                storedScript.fault = PartFault.None;
                storedScript.faultDescription = "";
            }
        }
        playerStorage.Add(storedPart);

        // Animate fly-away
        GameObject flyingCopy = Instantiate(part.gameObject, part.transform.position, part.transform.rotation, voidAnchor.transform);
        Destroy(flyingCopy.GetComponent<InspectableItem>());
        foreach (Light l in flyingCopy.GetComponentsInChildren<Light>()) Destroy(l);
        foreach (TrailRenderer t in flyingCopy.GetComponentsInChildren<TrailRenderer>()) Destroy(t);
        foreach (ParticleSystem p in flyingCopy.GetComponentsInChildren<ParticleSystem>()) Destroy(p);
        StartCoroutine(AnimateRemovalAndDestroy(flyingCopy));

        // Convert removed part into ghost slot
        part.isRemovable = false;
        part.isInventorySlot = true;
        foreach (InspectableItem childItem in part.GetComponentsInChildren<InspectableItem>(true))
        {
            childItem.isRemovable = false;
            childItem.isInventorySlot = true;
        }
        foreach (Renderer rend in part.GetComponentsInChildren<Renderer>())
        {
            Material[] ghostMats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++) ghostMats[i] = ghostMaterial;
            rend.sharedMaterials = ghostMats;
            rend.enabled = false;
        }
        foreach (Collider col in part.GetComponentsInChildren<Collider>()) col.enabled = false;

        // Walk up: auto-mark empty parents as ghost slots
        Transform walkUp = part.transform.parent;
        while (walkUp != null && walkUp.gameObject != currentClone)
        {
            InspectableItem parentItem = walkUp.GetComponent<InspectableItem>();
            if (parentItem != null && !parentItem.isMainObject && !parentItem.isInventorySlot)
            {
                bool hasActiveChildren = false;
                foreach (InspectableItem child in parentItem.GetComponentsInChildren<InspectableItem>(true))
                {
                    if (child == parentItem) continue;
                    if (!child.isInventorySlot) { hasActiveChildren = true; break; }
                }
                if (!hasActiveChildren)
                {
                    parentItem.isInventorySlot = true;
                    parentItem.isRemovable = false;
                    foreach (Renderer rend in walkUp.GetComponents<Renderer>()) rend.enabled = false;
                    foreach (Collider col in walkUp.GetComponents<Collider>()) col.enabled = false;
                }
                else break;
            }
            walkUp = walkUp.parent;
        }
    }

    // ─── Installation ────────────────────────────────────────────

    void BeginInstallConfirmation(InspectableItem slot)
    {
        if (!RequireHand()) return;

        GameObject partToInstall = FindPartForSlot(slot);
        if (partToInstall == null) return;

        if (screwdriverSystem != null)
        {
            isConfirmingRemoval = true;
            screwdriverSystem.onCancelled = () => { isConfirmingRemoval = false; };
            screwdriverSystem.BeginUnscrewing(slot.transform, () =>
            {
                isConfirmingRemoval = false;
                TryInstallPart(slot);
            });
        }
        else TryInstallPart(slot);
    }

    GameObject FindPartForSlot(InspectableItem slot)
    {
        if (partPendingInstallation != null)
        {
            InspectableItem pending = partPendingInstallation.GetComponent<InspectableItem>();
            if (pending == null || !IsPartCompatible(slot, pending))
            {
                ShowTooltipMessage("Incompatible Part!", $"This {slot.partCategory} requires different specs.");
                return null;
            }
            return partPendingInstallation;
        }

        foreach (GameObject item in playerStorage)
        {
            InspectableItem stored = item.GetComponent<InspectableItem>();
            if (stored != null && IsPartCompatible(slot, stored)) return item;
        }
        return null;
    }

    void TryInstallPart(InspectableItem slot)
    {
        GameObject partToInstall = FindPartForSlot(slot);
        if (partToInstall == null) return;

        ClearHighlight();
        playerStorage.Remove(partToInstall);

        partToInstall.transform.SetParent(slot.transform.parent, false);
        partToInstall.transform.localPosition = slot.transform.localPosition;
        partToInstall.transform.localRotation = slot.transform.localRotation;
        partToInstall.transform.localScale = slot.transform.localScale;

        SetLayerRecursively(partToInstall, currentClone.layer);
        partToInstall.SetActive(true);
        foreach (Renderer rend in partToInstall.GetComponentsInChildren<Renderer>(true)) rend.enabled = true;

        InspectableItem newPartScript = partToInstall.GetComponent<InspectableItem>();
        newPartScript.isRemovable = true;
        newPartScript.isInventorySlot = false;

        if (slot.blockingParts != null)
            newPartScript.blockingParts = new List<InspectableItem>(slot.blockingParts);

        // Update blocking references
        foreach (var item in currentClone.GetComponentsInChildren<InspectableItem>(true))
        {
            if (item.blockingParts != null && item.blockingParts.Contains(slot))
            {
                item.blockingParts.Remove(slot);
                item.blockingParts.Add(newPartScript);
            }
        }

        // Update wire ports
        allPorts.RemoveAll(p => p == null || p.transform.IsChildOf(slot.transform));
        foreach (var p in partToInstall.GetComponentsInChildren<InspectableItem>())
            if (p.isWirePort) allPorts.Add(p);

        Destroy(slot.gameObject);
        HideAllGhostSlots();

        // Initialize Slot_ children inside new part as ghost slots
        foreach (Transform child in partToInstall.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.StartsWith("Slot_"))
            {
                InspectableItem ghostScript = child.GetComponent<InspectableItem>();
                if (ghostScript == null) ghostScript = child.gameObject.AddComponent<InspectableItem>();
                if (!ghostScript.isInventorySlot && !ghostScript.isRemovable)
                {
                    string category = child.name.Replace("Slot_", "").Trim();
                    ghostScript.partCategory = category;
                    ghostScript.itemName = category + " Slot";
                    ghostScript.itemDescription = "Install a " + category + " here.";
                    ghostScript.isInventorySlot = true;
                    ghostScript.isRemovable = false;
                    foreach (Renderer r in child.GetComponentsInChildren<Renderer>()) r.enabled = false;
                    foreach (Collider col in child.GetComponentsInChildren<Collider>()) col.enabled = false;
                }
            }
        }

        // Disable fans if PC is off
        PCPowerSystem powerSystem = currentClone.GetComponent<PCPowerSystem>();
        if (powerSystem == null || !powerSystem.isPoweredOn)
        {
            foreach (PCFanController fan in partToInstall.GetComponentsInChildren<PCFanController>())
            {
                fan.enabled = false;
                Renderer fanRend = fan.GetComponentInChildren<Renderer>();
                if (fanRend != null && fanRend.material.HasProperty("_EmissionColor"))
                    fanRend.material.SetColor("_EmissionColor", Color.black);
            }
        }

        StartCoroutine(AnimateInstall(partToInstall));

        // Refresh pre-built wire connector ports
        foreach (MonoBehaviour mb in currentClone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            IPrebuiltWire w = mb as IPrebuiltWire;
            if (w != null && w.ConnectorPort != null && w.ConnectorPort.isWirePort)
                if (!allPorts.Contains(w.ConnectorPort)) allPorts.Add(w.ConnectorPort);
        }

        if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteInstallComponentTask();
    }

    // ─── Helper: Require Screwdriver ─────────────────────────────

    bool RequireCorrectToolForRemoval(InspectableItem part)
    {
        if (part.requiresScrewdriver) return RequireScrewdriver();
        else return RequireHand();
    }

    bool RequireScrewdriver()
    {
        if (InspectionToolbarUI.Instance != null && !InspectionToolbarUI.Instance.IsScrewdriverSelected())
        {
            ShowTooltipMessage("Tool Required", "Equip the Screwdriver first.\nPress 1 to equip.");
            return false;
        }
        return true;
    }

    bool RequireHand()
    {
        if (InspectionToolbarUI.Instance != null && !InspectionToolbarUI.Instance.IsHandSelected())
        {
            ShowTooltipMessage("Wrong Tool", "Use your Hand to grab this part.\nPress 4 to equip.");
            return false;
        }
        return true;
    }

    void ShowTooltipMessage(string title, string body)
    {
        if (!tooltipPanel) return;
        tooltipPanel.SetActive(true);
        if (tooltipTitle) tooltipTitle.text = title;
        if (tooltipBody) tooltipBody.text = body;
    }
}