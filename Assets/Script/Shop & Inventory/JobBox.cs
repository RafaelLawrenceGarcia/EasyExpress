using UnityEngine;
using System.Collections.Generic;

public class JobBox : MonoBehaviour
{
    [Header("Job Data")]
    public GameObject pcCasePrefabToSpawn;
    public List<StartingPCComponent> partsToBuild;

    // NEW: The email job this box was created from
    [HideInInspector] public EmailData sourceEmail;

    [Header("Saved State")]
    [Tooltip("If we packed up an already-built PC, it is stored secretly in here!")]
    public GameObject existingPC;

    // UPDATED: Now accepts the email reference
    public void SetupBox(GameObject casePrefab, List<StartingPCComponent> parts, EmailData email = null)
    {
        pcCasePrefabToSpawn = casePrefab;
        partsToBuild = parts;
        sourceEmail = email;
    }

    public void PackExistingPC(GameObject activePC)
    {
        existingPC = activePC;
        existingPC.transform.SetParent(this.transform);
        existingPC.transform.localPosition = Vector3.zero;
        existingPC.transform.localRotation = Quaternion.identity;
        existingPC.SetActive(false);
    }

    public GameObject UnpackPC(Transform workstationSpot)
    {
        // SCENARIO 1: Unpacking a PC we previously saved and boxed back up
        if (existingPC != null)
        {
            existingPC.SetActive(true);
            existingPC.transform.position = workstationSpot.position;
            existingPC.transform.rotation = workstationSpot.rotation;
            existingPC.transform.SetParent(null);

            Destroy(gameObject);
            return existingPC;
        }

        // SCENARIO 2: Normal unpacking (First time from the Email)
        if (pcCasePrefabToSpawn == null) return null;

        GameObject newPC = Instantiate(pcCasePrefabToSpawn, workstationSpot.position, workstationSpot.rotation);

        PCCaseBuilder builder = newPC.GetComponent<PCCaseBuilder>();
        if (builder != null)
        {
            builder.BuildFromData(partsToBuild);

            // NEW: Link the built PC back to its email job
            builder.linkedEmail = sourceEmail;
        }

        Destroy(gameObject);
        return newPC;
    }
}