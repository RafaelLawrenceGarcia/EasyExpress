using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PartCompatibility — Checks if PC components are compatible with each other.
/// 
/// HOW IT WORKS:
/// Each InspectableItem now has a "compatTags" field (e.g., "DDR4", "LGA1700", "ATX").
/// When you try to install a part, this system checks if the part's tags
/// match the slot's required tags.
/// 
/// EXAMPLES:
/// - RAM slot requires "DDR4" → only DDR4 RAM can go in
/// - Motherboard slot requires "LGA1700" → only LGA1700 CPUs fit
/// - PSU must provide enough wattage for all installed parts
/// 
/// SETUP:
/// 1. On each InspectableItem (RAM, GPU, etc.), fill in the new fields:
///    - compatTags: ["DDR4"] or ["DDR5", "DIMM"] 
///    - requiredTags: [] (empty for parts, filled for SLOTS)
///    - powerDraw: 65 (watts this part uses)
///    
/// 2. On ghost SLOTS (the Slot_ objects in your PC case):
///    - requiredTags: ["DDR4"] means only DDR4 parts can go here
///    
/// 3. On PSU parts:
///    - maxWattage: 650 (total watts this PSU provides)
/// </summary>
public static class PartCompatibility
{
    /// <summary>
    /// Check if a part can be installed in a slot.
    /// Returns true if compatible, false if not.
    /// errorMessage will contain the reason if incompatible.
    /// </summary>
    public static bool IsCompatible(InspectableItem part, InspectableItem slot, out string errorMessage)
    {
        errorMessage = "";

        // If the slot has no required tags, anything fits (generic slot)
        if (slot.requiredTags == null || slot.requiredTags.Length == 0)
            return true;

        // If the part has no compat tags, it can't match anything specific
        if (part.compatTags == null || part.compatTags.Length == 0)
        {
            // But if categories match, allow it (backward compatibility with old system)
            if (part.partCategory == slot.partCategory)
                return true;

            errorMessage = $"{part.itemName} has no compatibility info.";
            return false;
        }

        // Check: does the part have ALL the tags the slot requires?
        foreach (string required in slot.requiredTags)
        {
            if (string.IsNullOrEmpty(required)) continue;

            bool found = false;
            foreach (string partTag in part.compatTags)
            {
                if (partTag.ToUpper().Trim() == required.ToUpper().Trim())
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                errorMessage = $"{part.itemName} is not compatible.\nRequires: {required}";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if a PSU can handle the total power draw of all installed parts.
    /// Call this after installing a part to warn the player.
    /// </summary>
    public static bool CheckPSUCapacity(GameObject pcRoot, out float totalDraw, out float psuCapacity)
    {
        totalDraw = 0f;
        psuCapacity = 0f;

        InspectableItem[] allParts = pcRoot.GetComponentsInChildren<InspectableItem>(true);

        foreach (InspectableItem item in allParts)
        {
            if (item.isInventorySlot) continue; // Skip empty ghost slots

            // Sum up power draw
            totalDraw += item.powerDraw;

            // Find the PSU
            if (item.partCategory == "PSU" && item.maxWattage > 0)
            {
                psuCapacity = item.maxWattage;
            }
        }

        // If no PSU found, can't check
        if (psuCapacity <= 0) return true;

        return totalDraw <= psuCapacity;
    }

    /// <summary>
    /// Get a formatted compatibility summary for a part.
    /// Shows tags, power draw, etc. in the tooltip.
    /// </summary>
    public static string GetCompatInfo(InspectableItem part)
    {
        string info = "";

        if (part.compatTags != null && part.compatTags.Length > 0)
        {
            info += "Type: " + string.Join(", ", part.compatTags);
        }

        if (part.powerDraw > 0)
        {
            if (info.Length > 0) info += "\n";
            info += $"Power: {part.powerDraw}W";
        }

        if (part.maxWattage > 0)
        {
            if (info.Length > 0) info += "\n";
            info += $"Max Output: {part.maxWattage}W";
        }

        return info;
    }
}