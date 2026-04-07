using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class JobBox : MonoBehaviour
{
    [Header("Job Data")]
    public GameObject pcCasePrefabToSpawn;
    public List<StartingPCComponent> partsToBuild;

    [HideInInspector] public EmailData sourceEmail;

    [Header("Saved State")]
    [Tooltip("If we packed up an already-built PC, it is stored secretly in here!")]
    public GameObject existingPC;

    [Header("Owner Label Settings")]
    public float labelHeight = 0.4f;
    public float labelCharSize = 0.04f;
    public int labelFontSize = 80;
    public float labelViewRange = 6f;
    public float labelViewDot = 0.75f;

    [Header("Bad News UI")]
    [Tooltip("Drag your bad-news panel (a UI Panel GameObject) here.")]
    public GameObject badNewsPanel;
    [Tooltip("Title text inside the bad news panel.")]
    public TextMeshProUGUI badNewsTitleText;
    [Tooltip("Body text inside the bad news panel.")]
    public TextMeshProUGUI badNewsBodyText;
    [Tooltip("How many seconds the bad news stays on screen.")]
    public float badNewsDisplayTime = 5f;

    // Internal
    private GameObject ownerLabelObj;
    private Camera playerCam;
    private bool labelCreated = false;

    public void SetupBox(GameObject casePrefab, List<StartingPCComponent> parts, EmailData email = null)
    {
        pcCasePrefabToSpawn = casePrefab;
        partsToBuild = parts;
        sourceEmail = email;

        if (email != null && !string.IsNullOrEmpty(email.senderName))
            CreateOwnerLabel(email.senderName, email.jobType);
    }

    Camera FindPlayerCamera()
    {
        Camera cam = Camera.main;
        if (cam != null) return cam;
        OrbitCamera orbit = FindFirstObjectByType<OrbitCamera>();
        if (orbit != null) { cam = orbit.GetComponent<Camera>(); if (cam != null) return cam; }
        return FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        if (!labelCreated || ownerLabelObj == null) return;
        if (playerCam == null) { playerCam = FindPlayerCamera(); if (playerCam == null) return; }

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, labelViewRange);

        JobBox closestBox = null;
        float closestDist = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            JobBox hitBox = hit.collider.GetComponentInParent<JobBox>();
            if (hitBox != null && hit.distance < closestDist)
            { closestBox = hitBox; closestDist = hit.distance; }
        }

        if (closestBox == this)
        {
            ownerLabelObj.SetActive(true);
            ownerLabelObj.transform.rotation = Quaternion.LookRotation(
                ownerLabelObj.transform.position - playerCam.transform.position, Vector3.up);
        }
        else ownerLabelObj.SetActive(false);
    }

    void CreateOwnerLabel(string ownerName, JobType jobType)
    {
        if (ownerLabelObj != null) Destroy(ownerLabelObj);

        ownerLabelObj = new GameObject("OwnerLabel");
        ownerLabelObj.transform.SetParent(transform);
        ownerLabelObj.transform.localPosition = new Vector3(0f, labelHeight, 0f);
        ownerLabelObj.transform.localRotation = Quaternion.identity;
        ownerLabelObj.transform.localScale = Vector3.one;

        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.name = "LabelBackground";
        bgQuad.transform.SetParent(ownerLabelObj.transform, false);
        bgQuad.transform.localPosition = new Vector3(0f, -0.01f, 0.001f);
        bgQuad.transform.localScale = new Vector3(0.35f, 0.12f, 1f);
        Collider bgCol = bgQuad.GetComponent<Collider>();
        if (bgCol != null) Destroy(bgCol);

        Renderer bgRend = bgQuad.GetComponent<Renderer>();
        if (bgRend != null)
        {
            Color bgColor = (jobType == JobType.Build)
                ? new Color(0.1f, 0.2f, 0.4f, 1f)
                : new Color(0.35f, 0.1f, 0.1f, 1f);

            Shader unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material bgMat = new Material(unlitShader);
                bgMat.color = bgColor;
                bgMat.renderQueue = 3000;
                bgRend.material = bgMat;
            }
            else bgRend.material.color = bgColor;
        }

        GameObject textObj = new GameObject("NameText");
        textObj.transform.SetParent(ownerLabelObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0.005f, -0.01f);
        textObj.transform.localScale = Vector3.one;
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = ownerName;
        textMesh.characterSize = labelCharSize;
        textMesh.fontSize = labelFontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyle.Bold;
        MeshRenderer textRend = textObj.GetComponent<MeshRenderer>();
        if (textRend != null) { textRend.sortingOrder = 1; if (textRend.material != null) textRend.material.renderQueue = 3100; }

        GameObject typeObj = new GameObject("JobTypeText");
        typeObj.transform.SetParent(ownerLabelObj.transform, false);
        typeObj.transform.localPosition = new Vector3(0f, -0.035f, -0.01f);
        typeObj.transform.localScale = Vector3.one;
        TextMesh typeMesh = typeObj.AddComponent<TextMesh>();
        typeMesh.text = (jobType == JobType.Build) ? "[BUILD]" : "[REPAIR]";
        typeMesh.characterSize = labelCharSize * 0.55f;
        typeMesh.fontSize = labelFontSize;
        typeMesh.anchor = TextAnchor.MiddleCenter;
        typeMesh.alignment = TextAlignment.Center;
        typeMesh.fontStyle = FontStyle.Normal;
        typeMesh.color = (jobType == JobType.Build) ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.6f, 0.6f);
        MeshRenderer typeRend = typeObj.GetComponent<MeshRenderer>();
        if (typeRend != null) { typeRend.sortingOrder = 1; if (typeRend.material != null) typeRend.material.renderQueue = 3100; }

        ownerLabelObj.SetActive(false);
        labelCreated = true;
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
        // SCENARIO 1: Unpacking a previously packed PC
        if (existingPC != null)
        {
            existingPC.SetActive(true);
            existingPC.transform.position = workstationSpot.position;
            existingPC.transform.rotation = workstationSpot.rotation;
            existingPC.transform.SetParent(null);
            Destroy(gameObject);
            return existingPC;
        }

        // SCENARIO 2: First-time unbox from email job
        if (pcCasePrefabToSpawn == null) return null;

        GameObject newPC = Instantiate(pcCasePrefabToSpawn, workstationSpot.position, workstationSpot.rotation);

        PCCaseBuilder builder = newPC.GetComponent<PCCaseBuilder>();
        if (builder != null)
        {
            builder.BuildFromData(partsToBuild);
            builder.linkedEmail = sourceEmail;
        }

        // ── BAD NEWS: apply random faults for Repair jobs ──────────
        if (sourceEmail != null && sourceEmail.jobType == JobType.Repair)
        {
            string badNewsReport = ApplyBadNews(newPC);
            if (!string.IsNullOrEmpty(badNewsReport))
                ShowBadNews(sourceEmail.senderName, badNewsReport);
        }
        // ────────────────────────────────────────────────────────────

        Destroy(gameObject);
        return newPC;
    }

    // ================================================================
    //  BAD NEWS SYSTEM
    //  Randomly picks 1-2 faults and applies them to the PC's parts.
    //  Returns a summary string to display to the player.
    // ================================================================
    string ApplyBadNews(GameObject pc)
    {
        if (partsToBuild == null || partsToBuild.Count == 0) return "";

        // Possible faults that can be randomly assigned
        var faultPool = new List<(PartFault fault, string description)>
        {
            (PartFault.Broken,          "completely dead — needs replacement"),
            (PartFault.Dusty,           "clogged with dust — needs cleaning"),
            (PartFault.NotSeated,       "not seated properly — needs reseating"),
            (PartFault.LooseConnection, "has a loose connection"),
            (PartFault.Overheating,     "thermal paste dried out — overheating risk"),
            (PartFault.Corrupted,       "has corrupted data"),
        };

        // How many parts get a fault (1 or 2)
        int faultCount = Random.Range(1, 3);
        faultCount = Mathf.Min(faultCount, partsToBuild.Count);

        // Pick random parts to break (no duplicates)
        List<int> indices = new List<int>();
        for (int i = 0; i < partsToBuild.Count; i++) indices.Add(i);
        // Shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = indices[i]; indices[i] = indices[j]; indices[j] = tmp;
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < faultCount; i++)
        {
            int idx = indices[i];
            var (fault, desc) = faultPool[Random.Range(0, faultPool.Count)];
            partsToBuild[idx].fault = fault;
            partsToBuild[idx].faultDescription = partsToBuild[idx].partName + " " + desc + ".";
            lines.Add("• " + partsToBuild[idx].partName + ": " + desc);
        }

        // Also apply to the actual built components in the PC
        PCCaseBuilder builder = pc.GetComponent<PCCaseBuilder>();
        if (builder != null) builder.BuildFromData(partsToBuild);

        return string.Join("\n", lines);
    }

    void ShowBadNews(string customerName, string faultSummary)
    {
        // Try the directly assigned panel first
        if (badNewsPanel != null)
        {
            badNewsPanel.SetActive(true);
            if (badNewsTitleText != null)
                badNewsTitleText.text = "⚠ Bad News — " + customerName + "'s PC";
            if (badNewsBodyText != null)
                badNewsBodyText.text = faultSummary;
            // Auto-hide after delay
            StartCoroutine(HideBadNewsAfterDelay());
            return;
        }

        // Fallback: try to find a BadNewsUI anywhere in the scene
        BadNewsUI ui = FindFirstObjectByType<BadNewsUI>();
        if (ui != null)
        {
            ui.Show("⚠ Bad News — " + customerName + "'s PC", faultSummary);
            return;
        }

        // Last resort: just log it
        Debug.Log("[JobBox] Bad News for " + customerName + ":\n" + faultSummary);
    }

    System.Collections.IEnumerator HideBadNewsAfterDelay()
    {
        yield return new WaitForSeconds(badNewsDisplayTime);
        if (badNewsPanel != null) badNewsPanel.SetActive(false);
    }
}