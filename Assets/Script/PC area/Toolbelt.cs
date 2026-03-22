using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ToolBelt — Manages tools the player carries during inspection.
/// 
/// HOW IT WORKS:
/// - Player can hold tools: Screwdriver, Compressed Air Can, etc.
/// - Without a screwdriver, you CAN'T remove/install screwed parts
/// - Without compressed air, you CAN'T clean dusty PCs
/// - Tools are acquired from the shop or start as default equipment
/// 
/// SETUP:
/// 1. Attach to your Player (or a manager object)
/// 2. For testing, check "startWithScrewdriver" and "startWithDustCan"
/// 3. Later, uncheck those and make the player buy them from the shop
/// 
/// KEYBINDS (during inspection):
/// - 1 = Equip Screwdriver
/// - 2 = Equip Compressed Air Can
/// - 3 = Unequip (bare hands)
/// </summary>
public class ToolBelt : MonoBehaviour
{
    public static ToolBelt Instance;

    [Header("Starting Tools (for testing)")]
    public bool startWithScrewdriver = true;
    public bool startWithDustCan = true;

    [Header("Tool 3D Models (shown during inspection)")]
    public GameObject screwdriverModel;   // The 3D screwdriver — managed by ScrewdriverInteraction
    public GameObject dustCanModel;       // A 3D compressed air can model

    // --- STATE ---
    private HashSet<ToolType> ownedTools = new HashSet<ToolType>();
    private ToolType equippedTool = ToolType.None;

    public enum ToolType
    {
        None,
        Screwdriver,
        CompressedAir
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (startWithScrewdriver) ownedTools.Add(ToolType.Screwdriver);
        if (startWithDustCan) ownedTools.Add(ToolType.CompressedAir);

        // Hide all tool models at start
        if (screwdriverModel != null) screwdriverModel.SetActive(false);
        if (dustCanModel != null) dustCanModel.SetActive(false);
    }

    // =============================================
    //  PUBLIC API
    // =============================================

    /// <summary>
    /// Add a tool to the player's belt (e.g., after buying from shop).
    /// </summary>
    public void AddTool(ToolType tool)
    {
        ownedTools.Add(tool);
        Debug.Log($"Tool acquired: {tool}");
    }

    /// <summary>
    /// Remove a tool (e.g., if it breaks or is consumed).
    /// </summary>
    public void RemoveTool(ToolType tool)
    {
        ownedTools.Remove(tool);
        if (equippedTool == tool) equippedTool = ToolType.None;
    }

    /// <summary>
    /// Check if the player owns a specific tool.
    /// </summary>
    public bool HasTool(ToolType tool)
    {
        return ownedTools.Contains(tool);
    }

    /// <summary>
    /// Equip a tool (shows the 3D model, changes interaction mode).
    /// </summary>
    public void EquipTool(ToolType tool)
    {
        if (tool != ToolType.None && !HasTool(tool))
        {
            Debug.Log($"You don't have a {tool}!");
            return;
        }

        equippedTool = tool;

        // Toggle models
        if (dustCanModel != null) dustCanModel.SetActive(tool == ToolType.CompressedAir);
        // Screwdriver model is managed by ScrewdriverInteraction, don't toggle here

        Debug.Log($"Equipped: {tool}");
    }

    /// <summary>
    /// Get the currently equipped tool.
    /// </summary>
    public ToolType GetEquipped()
    {
        return equippedTool;
    }

    /// <summary>
    /// Check if the player can perform screwdriver actions.
    /// </summary>
    public bool CanUseScrewdriver()
    {
        return HasTool(ToolType.Screwdriver);
    }

    /// <summary>
    /// Check if the player can clean dust.
    /// </summary>
    public bool CanCleanDust()
    {
        return HasTool(ToolType.CompressedAir);
    }

    /// <summary>
    /// Handle tool switching via number keys. Call this from InspectionManager.Update().
    /// </summary>
    public void HandleToolInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && HasTool(ToolType.Screwdriver))
        {
            EquipTool(equippedTool == ToolType.Screwdriver ? ToolType.None : ToolType.Screwdriver);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && HasTool(ToolType.CompressedAir))
        {
            EquipTool(equippedTool == ToolType.CompressedAir ? ToolType.None : ToolType.CompressedAir);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EquipTool(ToolType.None);
        }
    }
}