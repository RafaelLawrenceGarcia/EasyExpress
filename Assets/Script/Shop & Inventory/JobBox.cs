using UnityEngine;
using System.Collections.Generic;

public class JobBox : MonoBehaviour
{
    [Header("Job Data")]
    public GameObject pcCasePrefabToSpawn;
    public List<StartingPCComponent> partsToBuild;

    [Header("Saved State")]
    [Tooltip("If we packed up an already-built PC, it is stored secretly in here!")]
    public GameObject existingPC; 

    // EmailManager calls this to inject the email data into the box (First Time)
    public void SetupBox(GameObject casePrefab, List<StartingPCComponent> parts)
    {
        pcCasePrefabToSpawn = casePrefab;
        partsToBuild = parts;
    }

    // --- NEW: This hides an active, half-built PC inside the box! ---
    public void PackExistingPC(GameObject activePC)
    {
        existingPC = activePC;
        
        // Parent the PC to the box so it moves with the box!
        existingPC.transform.SetParent(this.transform); 
        existingPC.transform.localPosition = Vector3.zero; 
        existingPC.transform.localRotation = Quaternion.identity;
        
        // Hide the 3D PC so we only see the cardboard box exterior
        existingPC.SetActive(false); 
    }

    // PlacementManager calls this when you put the box on the desk
    public GameObject UnpackPC(Transform workstationSpot)
    {
        // SCENARIO 1: Unpacking a PC we previously saved and boxed back up!
        if (existingPC != null)
        {
            existingPC.SetActive(true); // Unhide the PC
            existingPC.transform.position = workstationSpot.position;
            existingPC.transform.rotation = workstationSpot.rotation;
            existingPC.transform.SetParent(null); // Detach it from the box
            
            Destroy(gameObject); // Destroy the cardboard box
            return existingPC;   // Return the exact same PC with all saved progress!
        }

        // SCENARIO 2: Normal unpacking (First time from the Email)
        if (pcCasePrefabToSpawn == null) return null;
        
        // 1. Spawn the real PC case on the desk
        GameObject newPC = Instantiate(pcCasePrefabToSpawn, workstationSpot.position, workstationSpot.rotation);
        
        // 2. Build the customer's parts inside it
        PCCaseBuilder builder = newPC.GetComponent<PCCaseBuilder>();
        if (builder != null)
        {
            builder.BuildFromData(partsToBuild);
        }

        // 3. Destroy this cardboard box
        Destroy(gameObject);

        // 4. Return the new PC so the desk knows what's sitting on it
        return newPC;
    }
}