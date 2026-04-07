// ============================================================
//  InspectionManager.UI.cs  (partial class 4/6)
//  Click dispatch, hover, tooltips, highlights, power, dust
// ============================================================
using UnityEngine;
using System.Collections.Generic;

public partial class InspectionManager
{
    // ─── Click Dispatch ──────────────────────────────────────────

    void HandleClickInteractions()
    {
        if (viewOnlyMode) return;

        // Dust blocks all interactions
        DustSystem dust = currentClone != null ? currentClone.GetComponent<DustSystem>() : null;
        if (dust != null && dust.isDusty)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (InspectionToolbarUI.Instance == null || !InspectionToolbarUI.Instance.IsAirCanSelected())
                    ShowTooltipMessage("Too Dusty!", "Clean the PC with compressed air first.\nPress 2 to equip air can.");
            }
            return;
        }

        if (inventoryUI != null && inventoryUI.inventoryPanel.activeSelf) return;
        if (isWiring && Input.GetMouseButtonDown(1)) { CancelWiring(); return; }
        if (!Input.GetMouseButtonDown(0)) return;

        // Raycast to find clicked part
        Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        InspectableItem part = null;
        foreach (RaycastHit hit in hits)
        {
            InspectableItem candidate = hit.collider.gameObject.GetComponent<InspectableItem>();
            if (candidate == null) continue;
            if (candidate.isMainObject) continue;
            if (isPlacingFromInventory && !candidate.isInventorySlot) continue;
            part = candidate;
            break;
        }

        if (part == null) return;
        if (isPlacingFromInventory && !part.isInventorySlot) return;

        // Pre-built wire port click
        if (part.isWirePort && part.linkedPrebuiltWire != null)
        {
            IPrebuiltWire wire = part.GetPrebuiltWire();
            if (wire != null)
            {
                if (wire.IsConnected) HandlePrebuiltWireDisconnect(wire);
                else HandlePrebuiltWireConnect(wire);
                return;
            }
        }

        // Wire mesh click (disconnect)
        if (part.isRemovable && part.linkedPrebuiltWire != null)
        {
            IPrebuiltWire wire = part.GetPrebuiltWire();
            if (wire != null && wire.IsConnected) { HandlePrebuiltWireDisconnect(wire); return; }
        }

        // Dispatch to appropriate handler
        if (part.isInventorySlot) BeginInstallConfirmation(part);
        else if (part.isRemovable) BeginRemovalConfirmation(part);
        else if (part.isPowerButton) TogglePCPower();
        //else if (part.isWirePort) HandleWirePort(part);
    }

    // ─── Hover & Highlight ───────────────────────────────────────

    void HandleHover()
    {
        Ray ray = inspectionCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        GameObject hitObj = null;
        InspectableItem part = null;

        foreach (RaycastHit hit in hits)
        {
            InspectableItem candidate = hit.collider.gameObject.GetComponent<InspectableItem>();
            if (candidate == null) continue;
            if (candidate.isMainObject) continue;
            if (isPlacingFromInventory && !candidate.isInventorySlot) continue;
            hitObj = hit.collider.gameObject;
            part = candidate;
            break;
        }

        if (part != null)
        {
            if (hitObj != lastHitObject)
            {
                if (!isWiring) ClearHighlight();
                HighlightObject(hitObj);
                ShowTooltip(part);
            }
            MoveTooltip();

            // Tutorial hover completion
            if (!tutorialHoverDone && TutorialManager.Instance != null
                && TutorialManager.Instance.GetCurrentStep() == 13)
            {
                tutorialHoverDone = true;
                TutorialManager.Instance.CompleteHoverTask();
            }
            return;
        }

        if (!isWiring) ClearHighlight();
        else lastHitObject = null;
        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    void HighlightObject(GameObject obj)
    {
        lastHitObject = obj;
        if (highlightMaterial == null) return;

        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
        {
            if (rend == null) continue;
            if (!originalMaterialCache.ContainsKey(rend))
                originalMaterialCache.Add(rend, rend.sharedMaterials);

            Material[] currentMats = rend.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length + 1];
            for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
            newMats[newMats.Length - 1] = highlightMaterial;
            rend.sharedMaterials = newMats;
        }
    }

    void ClearHighlight()
    {
        foreach (var kv in originalMaterialCache)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        originalMaterialCache.Clear();
        lastHitObject = null;
    }

    // ─── Tooltip Display ─────────────────────────────────────────

    void ShowTooltip(InspectableItem part)
    {
        if (!tooltipPanel) return;

        // Pre-built wire port
        if (part.isWirePort && part.linkedPrebuiltWire != null)
        {
            IPrebuiltWire wire = part.GetPrebuiltWire();
            if (wire != null)
            {
                tooltipPanel.SetActive(true);
                tooltipAnchored = false;
                if (tooltipTitle) tooltipTitle.text = wire.WireName;
                if (wire.IsConnected)
                { if (tooltipBody) tooltipBody.text = "Click to disconnect."; }
                else if (!wire.IsRequiredComponentInstalled(currentClone.transform))
                { if (tooltipBody) tooltipBody.text = $"Install {wire.RequiredPartCategory} first."; }
                else
                { if (tooltipBody) tooltipBody.text = "Click to connect."; }
                return;
            }
        }

        // Wire mesh hover
        if (part.isRemovable && part.linkedPrebuiltWire != null)
        {
            IPrebuiltWire wire = part.GetPrebuiltWire();
            if (wire != null && wire.IsConnected)
            {
                tooltipPanel.SetActive(true);
                tooltipAnchored = false;
                if (tooltipTitle) tooltipTitle.text = wire.WireName;
                if (tooltipBody) tooltipBody.text = "Click to disconnect.";
                return;
            }
        }

        // Standard part tooltip
        string extra = "";
        if (part.isWirePort)
        {
            string side = part.isPSUPort ? "PSU" : "Device";
            extra = $"\n<size=80%>[{part.connectorType}] {side} port"
                  + (part.isOccupied ? " - CONNECTED" : " - empty") + "</size>";
        }
        else if (part.isRemovable)
            extra = "\n<size=75%><color=#FFD84A>Hold to remove</color></size>";
        else if (part.isInventorySlot)
            extra = "\n<size=75%><color=#4AE0FF>Hold to install</color></size>";

        tooltipPanel.SetActive(true);
        if (tooltipTitle) tooltipTitle.text = part.itemName;
        if (tooltipBody) tooltipBody.text = part.itemDescription + extra;
    }

    void MoveTooltip()
    {
        if (!tooltipPanel) return;
        if (!tooltipAnchored)
        {
            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                tooltipAnchored = true;
            }
        }
        RectTransform rect = tooltipPanel.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = new Vector2(-20, -20);
    }

    // ─── Power Toggle ────────────────────────────────────────────

    void TogglePCPower()
    {
        PCPowerSystem powerSystem = currentClone.GetComponent<PCPowerSystem>();
        if (powerSystem != null)
        {
            string reason;
            bool success = powerSystem.TryTogglePower(out reason);
            isPCOn = powerSystem.isPoweredOn;

            string title = "Power";
            if (!success) title = "Cannot Power On";
            else
            {
                switch (powerSystem.lastPowerResult)
                {
                    case PowerResult.FailedPOST: title = "POST Failed!"; break;
                    case PowerResult.NoDisplay: title = "No Display!"; break;
                    case PowerResult.BootWithIssues: title = "⚠ Issues Detected"; break;
                    default: title = isPCOn ? "Power On" : "Power Off"; break;
                }
            }
            ShowTooltipMessage(title, reason);
            if (TutorialManager.Instance != null) TutorialManager.Instance.OnPCPowerToggled(isPCOn);
            return;
        }

        // Fallback
        isPCOn = !isPCOn;
        foreach (PCFanController fan in currentClone.GetComponentsInChildren<PCFanController>())
        {
            fan.enabled = isPCOn;
            if (!isPCOn) { Renderer r = fan.GetComponentInChildren<Renderer>(); if (r != null) r.material.SetColor("_EmissionColor", Color.black); }
        }
        if (TutorialManager.Instance != null) TutorialManager.Instance.OnPCPowerToggled(isPCOn);
    }

    // ─── Dust Cleaning ───────────────────────────────────────────

    void HandleDustCleaning()
    {
        if (currentClone == null) return;
        DustSystem dust = currentClone.GetComponent<DustSystem>();
        if (dust == null || !dust.isDusty) return;
        if (InspectionToolbarUI.Instance == null || !InspectionToolbarUI.Instance.IsAirCanSelected()) return;

        if (Input.GetMouseButton(0))
        {
            bool cleaned = dust.CleanTick(Time.deltaTime);
            if (cleaned) Debug.Log("PC is now clean!");
        }
    }
}