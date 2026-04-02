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

    [Header("Owner Label Settings")]
    [Tooltip("Height offset above the box for the owner name label")]
    public float labelHeight = 0.4f;
    [Tooltip("Character size for the 3D text")]
    public float labelCharSize = 0.04f;
    [Tooltip("Font size for sharpness")]
    public int labelFontSize = 80;
    [Tooltip("How far the player can be to see the label")]
    public float labelViewRange = 6f;
    [Tooltip("How closely the player must aim at the box (0.9 = very precise, 0.7 = wider)")]
    public float labelViewDot = 0.75f;

    // Internal references
    private GameObject ownerLabelObj;
    private Camera playerCam;
    private bool labelCreated = false;

    // UPDATED: Now accepts the email reference
    public void SetupBox(GameObject casePrefab, List<StartingPCComponent> parts, EmailData email = null)
    {
        pcCasePrefabToSpawn = casePrefab;
        partsToBuild = parts;
        sourceEmail = email;

        // Create the owner name label on the box (starts hidden)
        if (email != null && !string.IsNullOrEmpty(email.senderName))
        {
            CreateOwnerLabel(email.senderName, email.jobType);
        }
    }

    /// <summary>
    /// Finds the player camera reliably, even if it isn't tagged "MainCamera".
    /// Tries: Camera.main → OrbitCamera's Camera → first active Camera in scene.
    /// </summary>
    Camera FindPlayerCamera()
    {
        // 1. Standard Unity shortcut (requires the "MainCamera" tag)
        Camera cam = Camera.main;
        if (cam != null) return cam;

        // 2. Find the OrbitCamera (your player camera script) and grab its Camera
        OrbitCamera orbit = FindFirstObjectByType<OrbitCamera>();
        if (orbit != null)
        {
            cam = orbit.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        // 3. Last resort: any active camera in the scene
        cam = FindFirstObjectByType<Camera>();
        return cam;
    }

    void Update()
    {
        if (!labelCreated || ownerLabelObj == null) return;

        if (playerCam == null)
        {
            playerCam = FindPlayerCamera();
            if (playerCam == null) return;
        }

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, labelViewRange);

        // Find the closest JobBox along the ray
        JobBox closestBox = null;
        float closestDist = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            JobBox hitBox = hit.collider.GetComponentInParent<JobBox>();
            if (hitBox != null && hit.distance < closestDist)
            {
                closestBox = hitBox;
                closestDist = hit.distance;
            }
        }

        // Only show label if THIS box is the closest one
        if (closestBox == this)
        {
            ownerLabelObj.SetActive(true);
            ownerLabelObj.transform.rotation = Quaternion.LookRotation(
                ownerLabelObj.transform.position - playerCam.transform.position, Vector3.up);
        }
        else
        {
            ownerLabelObj.SetActive(false);
        }
    }

    void CreateOwnerLabel(string ownerName, JobType jobType)
    {
        // Destroy any existing label first
        if (ownerLabelObj != null) Destroy(ownerLabelObj);

        // === PARENT CONTAINER ===
        ownerLabelObj = new GameObject("OwnerLabel");
        ownerLabelObj.transform.SetParent(transform);
        ownerLabelObj.transform.localPosition = new Vector3(0f, labelHeight, 0f);
        ownerLabelObj.transform.localRotation = Quaternion.identity;
        // Ensure label is not affected by box scale
        ownerLabelObj.transform.localScale = Vector3.one;

        // === BACKGROUND QUAD ===
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "LabelBackground";
        bgQuad.transform.SetParent(ownerLabelObj.transform, false);
        bgQuad.transform.localPosition = new Vector3(0f, -0.01f, 0.001f); // slightly behind text
        bgQuad.transform.localScale = new Vector3(0.35f, 0.12f, 1f);

        // Remove collider so it doesn't block raycasts / picks
        Collider bgCol = bgQuad.GetComponent<Collider>();
        if (bgCol != null) Destroy(bgCol);

        // Unlit material so it's always visible regardless of lighting
        Renderer bgRend = bgQuad.GetComponent<Renderer>();
        if (bgRend != null)
        {
            Color bgColor;
            if (jobType == JobType.Build)
                bgColor = new Color(0.1f, 0.2f, 0.4f, 1f);  // Blue for builds
            else
                bgColor = new Color(0.35f, 0.1f, 0.1f, 1f);  // Red for repairs

            // Try Unlit/Color first; fall back to a basic colored material
            Shader unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader == null) unlitShader = Shader.Find("UI/Default");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");

            if (unlitShader != null)
            {
                Material bgMat = new Material(unlitShader);
                bgMat.color = bgColor;
                // Render queue 3000 = renders BEFORE text overlay
                bgMat.renderQueue = 3000;
                bgRend.material = bgMat;
            }
            else
            {
                bgRend.material.color = bgColor;
                bgRend.material.renderQueue = 3000;
                Debug.LogWarning("[JobBox] No unlit shader found — label bg may look wrong.");
            }
        }

        // === OWNER NAME (3D TextMesh) ===
        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(ownerLabelObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.005f, -0.01f); // in front of bg
        textObj.transform.localScale = Vector3.one;

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = ownerName;
        textMesh.characterSize = labelCharSize;
        textMesh.fontSize = labelFontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyle.Bold;

        // Force text to render ON TOP of the background quad
        MeshRenderer textRend = textObj.GetComponent<MeshRenderer>();
        if (textRend != null)
        {
            textRend.sortingOrder = 1;
            if (textRend.material != null)
                textRend.material.renderQueue = 3100;
        }

        // === JOB TYPE TAG (smaller, below the name) ===
        GameObject typeObj = new GameObject("JobTypeText");
        typeObj.transform.SetParent(ownerLabelObj.transform, false);
        typeObj.transform.localPosition = new Vector3(0f, -0.035f, -0.01f); // in front of bg
        typeObj.transform.localScale = Vector3.one;

        TextMesh typeMesh = typeObj.AddComponent<TextMesh>();
        typeMesh.text = (jobType == JobType.Build) ? "[BUILD]" : "[REPAIR]";
        typeMesh.characterSize = labelCharSize * 0.55f;
        typeMesh.fontSize = labelFontSize;
        typeMesh.anchor = TextAnchor.MiddleCenter;
        typeMesh.alignment = TextAlignment.Center;
        typeMesh.fontStyle = FontStyle.Normal;

        if (jobType == JobType.Build)
            typeMesh.color = new Color(0.5f, 0.85f, 1f);  // Light blue
        else
            typeMesh.color = new Color(1f, 0.6f, 0.6f);   // Light red

        // Force job type text to render ON TOP as well
        MeshRenderer typeRend = typeObj.GetComponent<MeshRenderer>();
        if (typeRend != null)
        {
            typeRend.sortingOrder = 1;
            if (typeRend.material != null)
                typeRend.material.renderQueue = 3100;
        }

        // Start hidden — only shows when player looks at it
        ownerLabelObj.SetActive(false);
        labelCreated = true;

        Debug.Log("[JobBox] Owner label created: " + ownerName + " (" + jobType + ")");
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

            // Link the built PC back to its email job
            builder.linkedEmail = sourceEmail;
        }

        Destroy(gameObject);
        return newPC;
    }
}