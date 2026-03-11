using UnityEngine;
using System.Collections.Generic;

public class PCCaseBuilder : MonoBehaviour
{
    public void BuildFromData(List<StartingPCComponent> partsToInstall)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        List<Transform> dummySlots = new List<Transform>();
        
        foreach (Transform child in allChildren)
        {
            // We look for any object starting with "Slot_"
            if (child.name.StartsWith("Slot_"))
            {
                dummySlots.Add(child);
            }
        }

        foreach (StartingPCComponent part in partsToInstall)
        {
            if (part.partPrefab == null) continue;

            // Find a dummy that contains the EXACT category name (e.g., "RAM")
            Transform matchingDummy = dummySlots.Find(d => d.name.Contains(part.partCategory));

            if (matchingDummy != null)
            {
                GameObject realPart = Instantiate(part.partPrefab, matchingDummy.parent);
                
                realPart.transform.localPosition = matchingDummy.localPosition;
                realPart.transform.localRotation = matchingDummy.localRotation;
                
                // FIX: Put internal parts on the "Ignore Raycast" layer!
                // This makes them invisible to your interaction laser so they don't block clicks.
                SetLayerRecursively(realPart, LayerMask.NameToLayer("Ignore Raycast"));
                
                dummySlots.Remove(matchingDummy);
                Destroy(matchingDummy.gameObject);
            }
        }

        // Clean up any dummies that weren't requested in the email
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