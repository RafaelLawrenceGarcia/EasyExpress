using UnityEngine;
using System.Collections.Generic;

public class PCCaseBuilder : MonoBehaviour
{
    public void BuildFromData(List<StartingPCComponent> partsToInstall)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        List<Transform> dummySlots = new List<Transform>();
        
        // 1. Gather all valid dummy slots
        foreach (Transform child in allChildren)
        {
            // We look for any object starting with "Slot_"
            if (child.name.StartsWith("Slot_"))
            {
                dummySlots.Add(child);
            }
        }

        // 2. Attempt to place each part
        foreach (StartingPCComponent part in partsToInstall)
        {
            Debug.Log($"[PC Builder] Trying to place: {part.partCategory} | prefab null? {part.partPrefab == null}");

            if (part.partPrefab == null) 
            {
                Debug.LogWarning($"[PC Builder] Skipping {part.partCategory} because its prefab is missing in the data file!");
                continue;
            }

            // Find a dummy that contains the EXACT category name (e.g., "RAM")
            Transform matchingDummy = dummySlots.Find(d => d.name.Contains(part.partCategory));

            Debug.Log($"[PC Builder]   → Slot found: {(matchingDummy != null ? matchingDummy.name : "NONE")}");

            if (matchingDummy != null)
            {
                GameObject realPart = Instantiate(part.partPrefab, matchingDummy.parent);
                
                realPart.transform.localPosition = matchingDummy.localPosition;
                realPart.transform.localRotation = matchingDummy.localRotation;
                
                // Put internal parts on the "Ignore Raycast" layer!
                // This makes them invisible to your interaction laser so they don't block clicks.
                SetLayerRecursively(realPart, LayerMask.NameToLayer("Ignore Raycast"));
                
                // Remove from our list and destroy the dummy so it isn't used again
                dummySlots.Remove(matchingDummy);
                Destroy(matchingDummy.gameObject);
                
                Debug.Log($"[PC Builder] Successfully placed {part.partCategory} into {matchingDummy.name}.");
            }
            else
            {
                Debug.LogWarning($"[PC Builder] Failed to place {part.partCategory}. No available slot starting with 'Slot_' containing that name was found.");
            }
        }

        // 3. Clean up any dummies that weren't requested in the email
        foreach (Transform leftoverDummy in dummySlots)
        {
            if (leftoverDummy != null)
            {
                Destroy(leftoverDummy.gameObject);
            }
        }
    }

    // Helper method to change the layer of the part and all its pieces
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}