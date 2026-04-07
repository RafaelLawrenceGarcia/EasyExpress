using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public partial class TutorialManager
{
    private Coroutine activeUIGuide;
    private bool uiGuideItemAdded;
    private PCController activeGuidePC;

    // ── Track which parts the player used in the tutorial repair ──
    // These are set during the tutorial repair so the shop guide can
    // point the player at matching GPU/RAM items in the store.
    [HideInInspector] public string tutorialGPUName = "";
    [HideInInspector] public string tutorialRAMName = "";

    /// <summary>
    /// Call from PlayerInteract.OpenWorkstationMonitor() at the end.
    /// Pass the monitor being opened so we have a direct reference.
    /// </summary>
    public void NotifyMonitorOpenedForTutorial(WorkstationMonitor monitor)
    {
        if (activeUIGuide != null) StopCoroutine(activeUIGuide);

        // Grab the PC directly — no searching
        activeGuidePC = (monitor != null) ? monitor.localOS : null;

        if (step == 25) activeUIGuide = StartCoroutine(EmailUIGuide());
        else if (step == 26) activeUIGuide = StartCoroutine(ShopUIGuide());
    }

    void StopUIGuide()
    {
        if (activeUIGuide != null) { StopCoroutine(activeUIGuide); activeUIGuide = null; }
        HidePointer();
    }

    // ═══════════════════════════════════════════════════════════
    //  POINTER CANVAS — force on top of the monitor overlay
    // ═══════════════════════════════════════════════════════════

    void EnsurePointerOnTop()
    {
        if (TutorialUIPointer.Instance == null)
        {
            Debug.LogWarning("[Tutorial] TutorialUIPointer.Instance is NULL — pointer won't show!");
            return;
        }
        Canvas c = TutorialUIPointer.Instance.parentCanvas;
        if (c != null)
        {
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 200;
            Debug.Log("[Tutorial] Pointer canvas set to Overlay, sortOrder=200");
        }
        else
        {
            Debug.LogWarning("[Tutorial] Pointer parentCanvas is NULL!");
        }
    }

    void PointAt(RectTransform target, string label)
    {
        if (TutorialUIPointer.Instance == null)
        {
            Debug.LogWarning($"[Tutorial] Can't point at '{label}' — TutorialUIPointer.Instance is NULL");
            return;
        }
        if (target == null)
        {
            Debug.LogWarning($"[Tutorial] Can't point at '{label}' — target is NULL");
            return;
        }
        Debug.Log($"[Tutorial] Pointing at '{label}' → {target.name} (pos={target.position})");
        TutorialUIPointer.Instance.ShowAtUI(target);
    }

    // ═══════════════════════════════════════════════════════════
    //  EMAIL UI GUIDE  (Step 25)
    //
    //  Flow: Email icon → IN PROGRESS entry → Mark Complete → Collect
    // ═══════════════════════════════════════════════════════════

    IEnumerator EmailUIGuide()
    {
        var pointer = TutorialUIPointer.Instance;
        if (pointer == null) { Debug.LogError("[Tutorial] No TutorialUIPointer in scene!"); yield break; }

        EnsurePointerOnTop();
        yield return new WaitForSeconds(0.3f);

        // ── 1. Point to Email app icon on desktop ────────────────
        SetTask("SUBMIT JOB", "Click the Email app");

        RectTransform emailIcon = null;
        yield return WaitFor(() =>
        {
            emailIcon = FindDesktopIcon("Email");
            return emailIcon != null;
        }, 5f);

        if (emailIcon != null)
        {
            PointAt(emailIcon, "Email icon");
            // Wait for email app panel to open
            yield return WaitFor(() =>
            {
                if (activeGuidePC == null) return false;
                return activeGuidePC.emailAppPanel != null && activeGuidePC.emailAppPanel.activeSelf;
            });
            pointer.Hide();
        }
        else
        {
            Debug.LogWarning("[Tutorial] Could not find Email icon — skipping to step 2");
        }

        yield return new WaitForSeconds(0.5f);

        // ── 2. Point to the IN PROGRESS email entry ──────────────
        SetTask("SUBMIT JOB", "Click the completed job email");

        RectTransform emailEntry = null;
        yield return WaitFor(() =>
        {
            emailEntry = FindInboxEntry("IN PROGRESS");
            return emailEntry != null;
        }, 10f);

        if (emailEntry != null)
        {
            PointAt(emailEntry, "IN PROGRESS email");
            // Wait for email detail panel to open
            EmailManager em = EmailManager.Instance;
            yield return WaitFor(() => em != null && em.detailPanel != null && em.detailPanel.activeSelf);
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.3f);

        // ── 3. Point to Mark as Complete button ──────────────────
        EmailManager email = EmailManager.Instance;
        if (email != null && email.completeButton != null)
        {
            SetTask("SUBMIT JOB", "Click 'Mark as Complete'");

            yield return WaitFor(() =>
                email.completeButton.gameObject.activeSelf, 10f);

            if (email.completeButton.gameObject.activeSelf)
            {
                PointAt(email.completeButton.GetComponent<RectTransform>(), "Complete button");

                yield return WaitFor(() =>
                    email.completionPanel != null && email.completionPanel.activeSelf);
                pointer.Hide();
            }
        }

        yield return new WaitForSeconds(0.3f);

        // ── 4. Point to Collect Payment / OK button ──────────────
        if (email != null && email.completionOKButton != null)
        {
            SetTask("SUBMIT JOB", "Collect your payment!");
            PointAt(email.completionOKButton.GetComponent<RectTransform>(), "OK button");

            yield return WaitFor(() =>
                email.completionPanel == null || !email.completionPanel.activeSelf);
            pointer.Hide();
        }

        activeUIGuide = null;
    }

    // ═══════════════════════════════════════════════════════════
    //  SHOP UI GUIDE  (Step 26)
    //
    //  Flow: Store icon → GPU category → GPU item → Add to Cart
    //        → Back → RAM category → RAM item → Add to Cart
    //        → Cart button → Checkout
    //
    //  FIXED: Arrow now waits for layout rebuild before pointing,
    //         and searches for the specific GPU/RAM used in repair.
    // ═══════════════════════════════════════════════════════════

    IEnumerator ShopUIGuide()
    {
        var pointer = TutorialUIPointer.Instance;
        if (pointer == null) { Debug.LogError("[Tutorial] No TutorialUIPointer in scene!"); yield break; }

        EnsurePointerOnTop();
        yield return new WaitForSeconds(0.3f);

        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop == null) { Debug.LogError("[Tutorial] No ShopManager found!"); yield break; }

        // ── 1. Point to Store app icon on desktop ────────────────
        SetTask("SHOP TUTORIAL", "Click the Store app");

        RectTransform storeIcon = null;
        yield return WaitFor(() =>
        {
            storeIcon = FindDesktopIcon("Store");
            return storeIcon != null;
        }, 5f);

        if (storeIcon != null)
        {
            PointAt(storeIcon, "Store icon");
            yield return WaitFor(() =>
            {
                if (activeGuidePC == null) return false;
                return activeGuidePC.storeAppPanel != null && activeGuidePC.storeAppPanel.activeSelf;
            });
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.5f);

        // ── 2. Point to GPU category ─────────────────────────────
        SetTask("SHOP TUTORIAL", "Click the GPU category");

        RectTransform gpuCat = null;
        yield return WaitFor(() =>
        {
            gpuCat = FindCategoryButton("GPU");
            return gpuCat != null;
        }, 5f);

        if (gpuCat != null)
        {
            // Wait for layout to settle before pointing
            yield return WaitForLayoutRebuild();
            PointAt(gpuCat, "GPU category");
            yield return WaitFor(() =>
                shop.itemListScreen != null && shop.itemListScreen.activeSelf);
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.5f);

        // ── 3. Point to GPU item matching the one used in repair ─
        SetTask("SHOP TUTORIAL", "Click a GPU to view details");

        RectTransform gpuItem = null;
        yield return WaitFor(() =>
        {
            gpuItem = FindItemInListByPartName(shop, tutorialGPUName, "GPU");
            return gpuItem != null;
        }, 5f);

        if (gpuItem != null)
        {
            // CRITICAL: Wait for layout to rebuild so position is correct
            yield return WaitForLayoutRebuild();
            PointAt(gpuItem, "GPU item");
            yield return WaitFor(() =>
                shop.productDetailsPanel != null && shop.productDetailsPanel.activeSelf);
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.3f);

        // ── 4. Point to Add to Cart button (detail view) ─────────
        if (shop.detailAddToCartBtn != null)
        {
            uiGuideItemAdded = false;
            SetTask("SHOP TUTORIAL", "Click 'Add to Cart'");
            PointAt(shop.detailAddToCartBtn.GetComponent<RectTransform>(), "Add to Cart (GPU)");

            yield return WaitFor(() => uiGuideItemAdded);
            pointer.Hide();
            uiGuideItemAdded = false;
        }

        yield return new WaitForSeconds(0.5f);

        // ── 5. Go back to categories ─────────────────────────────
        SetTask("SHOP TUTORIAL", "Go back to categories");

        // Try to find a back button
        RectTransform backBtn = FindBackButton(shop);
        if (backBtn != null)
        {
            yield return WaitForLayoutRebuild();
            PointAt(backBtn, "Back button");
        }

        yield return WaitFor(() =>
            shop.categoryScreen != null && shop.categoryScreen.activeSelf, 15f);
        pointer.Hide();

        yield return new WaitForSeconds(0.5f);

        // ── 6. Point to RAM category ─────────────────────────────
        SetTask("SHOP TUTORIAL", "Click the RAM category");

        RectTransform ramCat = null;
        yield return WaitFor(() =>
        {
            ramCat = FindCategoryButton("RAM");
            return ramCat != null;
        }, 5f);

        if (ramCat != null)
        {
            yield return WaitForLayoutRebuild();
            PointAt(ramCat, "RAM category");
            yield return WaitFor(() =>
                shop.itemListScreen != null && shop.itemListScreen.activeSelf);
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.5f);

        // ── 7. Point to RAM item matching the one used in repair ─
        SetTask("SHOP TUTORIAL", "Click a RAM stick to view details");

        RectTransform ramItem = null;
        yield return WaitFor(() =>
        {
            ramItem = FindItemInListByPartName(shop, tutorialRAMName, "RAM");
            return ramItem != null;
        }, 5f);

        if (ramItem != null)
        {
            // CRITICAL: Wait for layout to rebuild so position is correct
            yield return WaitForLayoutRebuild();
            PointAt(ramItem, "RAM item");
            yield return WaitFor(() =>
                shop.productDetailsPanel != null && shop.productDetailsPanel.activeSelf);
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.3f);

        // ── 8. Point to Add to Cart button (detail view) ─────────
        if (shop.detailAddToCartBtn != null)
        {
            uiGuideItemAdded = false;
            SetTask("SHOP TUTORIAL", "Click 'Add to Cart'");
            PointAt(shop.detailAddToCartBtn.GetComponent<RectTransform>(), "Add to Cart (RAM)");

            yield return WaitFor(() => uiGuideItemAdded);
            pointer.Hide();
            uiGuideItemAdded = false;
        }

        yield return new WaitForSeconds(0.5f);

        // ── 9. Point to Cart button ──────────────────────────────
        if (shop.cartIconButtons != null && shop.cartIconButtons.Length > 0
            && shop.cartIconButtons[0] != null)
        {
            SetTask("SHOP TUTORIAL", "Open your cart");
            PointAt(shop.cartIconButtons[0].GetComponent<RectTransform>(), "Cart button");

            yield return WaitFor(() =>
                shop.cartPanelRect != null && shop.cartPanelRect.gameObject.activeSelf);
            pointer.Hide();
        }

        yield return new WaitForSeconds(0.5f);

        // ── 10. Point to Checkout button ─────────────────────────
        SetTask("SHOP TUTORIAL", "Checkout your order!");

        RectTransform checkoutBtn = null;
        yield return WaitFor(() =>
        {
            if (shop.cartPanelRect == null) return false;
            checkoutBtn = FindButtonWithText(shop.cartPanelRect.transform,
                "checkout", "check out", "place order", "buy");
            return checkoutBtn != null;
        }, 10f);

        if (checkoutBtn != null)
        {
            PointAt(checkoutBtn, "Checkout button");
            yield return WaitFor(() => step != 26, 30f);
            pointer.Hide();
        }

        activeUIGuide = null;
    }

    // ═══════════════════════════════════════════════════════════
    //  UI ELEMENT FINDERS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Wait 2 frames for Unity's layout system to recalculate positions.
    /// Without this, freshly-instantiated UI elements report position (0,0).
    /// </summary>
    IEnumerator WaitForLayoutRebuild()
    {
        yield return null; // end of frame — layout pass runs
        yield return null; // one more for safety (ContentSizeFitter, etc.)
        Canvas.ForceUpdateCanvases(); // force any remaining layout
    }

    /// <summary>Find a desktop app icon by its label text using the stored PC reference.</summary>
    RectTransform FindDesktopIcon(string appName)
    {
        PCController pc = activeGuidePC;
        if (pc == null || pc.desktopPanel == null || !pc.desktopPanel.activeInHierarchy)
        {
            Debug.Log($"[Tutorial] FindDesktopIcon('{appName}'): PC={pc}, desktop={(pc != null ? pc.desktopPanel : null)}");
            return null;
        }

        foreach (TextMeshProUGUI tmp in pc.desktopPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string trimmed = tmp.text.Trim();
            if (trimmed.Equals(appName, System.StringComparison.OrdinalIgnoreCase))
            {
                // Return the clickable parent (button or icon container)
                Button parentBtn = tmp.GetComponentInParent<Button>();
                if (parentBtn != null) return parentBtn.GetComponent<RectTransform>();
                return tmp.transform.parent != null
                    ? tmp.transform.parent.GetComponent<RectTransform>()
                    : tmp.GetComponent<RectTransform>();
            }
        }

        // Debug: list what texts we DID find
        Debug.Log($"[Tutorial] FindDesktopIcon('{appName}'): NOT FOUND. Desktop texts:");
        foreach (TextMeshProUGUI tmp in pc.desktopPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            Debug.Log($"  - '{tmp.text.Trim()}' on {tmp.gameObject.name}");

        return null;
    }

    /// <summary>Find an inbox entry whose subject contains the given text.</summary>
    RectTransform FindInboxEntry(string statusContains)
    {
        EmailManager em = EmailManager.Instance;
        if (em == null || em.inboxContentContainer == null) return null;

        foreach (Transform child in em.inboxContentContainer)
        {
            if (!child.gameObject.activeSelf) continue;
            foreach (TextMeshProUGUI t in child.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (t.text.Contains(statusContains))
                    return child.GetComponent<RectTransform>();
            }
        }
        return null;
    }

    /// <summary>Find a shop category button by its label.</summary>
    RectTransform FindCategoryButton(string categoryName)
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop == null || shop.categoryContentContainer == null) return null;

        foreach (Transform child in shop.categoryContentContainer)
        {
            if (!child.gameObject.activeSelf) continue;
            foreach (TextMeshProUGUI t in child.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (t.text.Trim().Equals(categoryName, System.StringComparison.OrdinalIgnoreCase))
                    return child.GetComponent<RectTransform>();
            }
        }
        return null;
    }

    /// <summary>Find the first product item in the current item list.</summary>
    RectTransform FindFirstItemInList(ShopManager shop)
    {
        if (shop == null || shop.contentContainer == null) return null;

        foreach (Transform child in shop.contentContainer)
        {
            if (child.gameObject.activeSelf)
                return child.GetComponent<RectTransform>();
        }
        return null;
    }

    /// <summary>
    /// Find a product item in the shop list whose name contains the given part name.
    /// Falls back to the first item if no specific match is found.
    /// </summary>
    RectTransform FindItemInListByPartName(ShopManager shop, string partName, string fallbackCategory)
    {
        if (shop == null || shop.contentContainer == null) return null;

        // First: try to find an item whose text contains the part name
        if (!string.IsNullOrEmpty(partName))
        {
            string lowerName = partName.ToLower();
            foreach (Transform child in shop.contentContainer)
            {
                if (!child.gameObject.activeSelf) continue;
                foreach (TextMeshProUGUI t in child.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (t.text.ToLower().Contains(lowerName))
                        return child.GetComponent<RectTransform>();
                }
            }
        }

        // Fallback: return the first active item in the list
        return FindFirstItemInList(shop);
    }

    /// <summary>Find the back/categories button across shop panels.</summary>
    RectTransform FindBackButton(ShopManager shop)
    {
        if (shop == null) return null;

        // Check product details panel first
        if (shop.productDetailsPanel != null)
        {
            RectTransform btn = FindButtonWithText(shop.productDetailsPanel.transform,
                "back", "categories", "return", "close");
            if (btn != null) return btn;
        }

        // Check item list screen
        if (shop.itemListScreen != null)
        {
            RectTransform btn = FindButtonWithText(shop.itemListScreen.transform,
                "back", "categories", "return");
            if (btn != null) return btn;
        }

        return null;
    }

    /// <summary>Search a parent for a button whose text contains any keyword.</summary>
    RectTransform FindButtonWithText(Transform parent, params string[] keywords)
    {
        if (parent == null) return null;

        foreach (Button btn in parent.GetComponentsInChildren<Button>(true))
        {
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null) continue;
            string lower = txt.text.ToLower();
            foreach (string kw in keywords)
            {
                if (lower.Contains(kw.ToLower()))
                    return btn.GetComponent<RectTransform>();
            }
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  WAIT HELPER
    // ═══════════════════════════════════════════════════════════

    IEnumerator WaitFor(System.Func<bool> condition, float timeout = 30f)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}