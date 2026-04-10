// ============================================================
//  JobCompletionUI.cs
//  Easy Express – Thesis Project
// ============================================================
//  Structured Job Completion popup with individual fields
//  for customer info, star rating, issue status, payment
//  breakdown, and collect button.
//
//  SETUP:
//    Run EasyExpress > Build Job Completion UI in the Unity
//    menu bar. The editor script creates the full hierarchy
//    and wires all references automatically.
//
//  INTEGRATION:
//    EmailManager.ShowCompletionPopup() calls
//    JobCompletionUI.Instance.Show(result) instead of
//    manually setting 4 text fields.
//
//  BACKWARD COMPAT:
//    The old completionPanel / completionTitle / completionDetails
//    / completionPay / completionRating / completionOKButton fields
//    on EmailManager still work — the builder wires them too.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class JobCompletionUI : MonoBehaviour
{
    public static JobCompletionUI Instance;

    // ═══════════════════════════════════════════════════════════
    //  UI REFERENCES (wired by JobCompletionUIBuilder)
    // ═══════════════════════════════════════════════════════════

    [Header("Root")]
    public GameObject rootPanel;
    public CanvasGroup rootCanvasGroup;

    [Header("Header")]
    public Image headerStrip;
    public TextMeshProUGUI headerLabel;
    public TextMeshProUGUI titleText;

    [Header("Stars")]
    public TextMeshProUGUI[] starTexts;

    [Header("Customer Info")]
    public TextMeshProUGUI customerNameText;
    public TextMeshProUGUI problemText;

    [Header("Status")]
    public GameObject statusResolvedPanel;
    public TextMeshProUGUI statusResolvedText;
    public GameObject statusIssuesPanel;
    public TextMeshProUGUI statusIssuesLabel;
    public TextMeshProUGUI statusIssuesBody;

    [Header("Payment Breakdown")]
    public TextMeshProUGUI baseRewardValue;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI scoreValue;
    public TextMeshProUGUI earnedRewardValue;
    public TextMeshProUGUI tipLabel;
    public TextMeshProUGUI tipValue;
    public GameObject tipRow;

    [Header("Total")]
    public TextMeshProUGUI totalValue;

    [Header("Button")]
    public Button collectButton;
    public TextMeshProUGUI collectButtonText;

    // ═══════════════════════════════════════════════════════════
    //  BACKWARD COMPAT — EmailManager's old fields still work
    // ═══════════════════════════════════════════════════════════

    [Header("Legacy (auto-wired by builder)")]
    public TextMeshProUGUI legacyTitle;
    public TextMeshProUGUI legacyDetails;
    public TextMeshProUGUI legacyPay;
    public TextMeshProUGUI legacyRating;

    // ═══════════════════════════════════════════════════════════
    //  COLORS
    // ═══════════════════════════════════════════════════════════

    [Header("Theme Colors")]
    public Color accentCyan = new Color(0.290f, 0.878f, 1.000f, 1f);
    public Color accentGold = new Color(1.000f, 0.843f, 0.000f, 1f);
    public Color accentAmber = new Color(0.831f, 0.659f, 0.263f, 1f);
    public Color accentGreen = new Color(0.290f, 1.000f, 0.290f, 1f);
    public Color accentRed = new Color(1.000f, 0.420f, 0.420f, 1f);
    public Color mutedText = new Color(0.478f, 0.541f, 0.667f, 1f);
    public Color bodyText = new Color(0.753f, 0.784f, 0.867f, 1f);
    public Color starOff = new Color(0.333f, 0.333f, 0.333f, 1f);

    // ═══════════════════════════════════════════════════════════
    //  ANIMATION
    // ═══════════════════════════════════════════════════════════

    [Header("Animation")]
    public float fadeInDuration = 0.4f;
    public float starStagger = 0.12f;

    private Coroutine _animCoroutine;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════
    //  SHOW — Call this from EmailManager.ShowCompletionPopup()
    // ═══════════════════════════════════════════════════════════

    public void Show(JobCompletionResult result)
    {
        if (rootPanel == null) return;
        rootPanel.SetActive(true);

        // ── Header accent color based on rating ──────────────────
        Color accent = GetAccentForRating(result.starRating);
        // ── Stars ────────────────────────────────────────────────
        if (starTexts != null)
        {
            for (int i = 0; i < starTexts.Length; i++)
            {
                if (starTexts[i] != null)
                {
                    starTexts[i].text = "*";  // ASCII asterisk the font supports
                    starTexts[i].color = (i < result.starRating) ? accentGold : starOff;
                }
            }
        }
        if (headerStrip != null)
            headerStrip.color = new Color(accent.r, accent.g, accent.b, 0.08f);
        if (headerLabel != null)
        {
            headerLabel.color = accent;
            string prefix = (result.jobType == JobType.Build) ? "BUILD COMPLETE" : "REPAIR COMPLETE";
            headerLabel.text = prefix;
        }

        // ── Title ────────────────────────────────────────────────
        if (titleText != null)
            titleText.text = GetTitle(result.starRating, result.jobType);

        // ── Stars ────────────────────────────────────────────────
        if (starTexts != null)
        {
            for (int i = 0; i < starTexts.Length; i++)
            {
                if (starTexts[i] != null)
                    starTexts[i].color = (i < result.starRating) ? accentGold : starOff;
            }
        }

        // ── Customer info ────────────────────────────────────────
        if (customerNameText != null)
            customerNameText.text = result.customerName;
        if (problemText != null)
            problemText.text = result.problemDescription;

        // ── Status ───────────────────────────────────────────────
        bool allFixed = (result.unfixedIssues == null || result.unfixedIssues.Count == 0);

        if (statusResolvedPanel != null)
            statusResolvedPanel.SetActive(allFixed);
        if (statusIssuesPanel != null)
            statusIssuesPanel.SetActive(!allFixed);

        if (!allFixed && statusIssuesBody != null)
        {
            string issueLabel = (result.jobType == JobType.Build)
                ? "Missing parts:" : "Unfixed issues:";
            if (statusIssuesLabel != null)
                statusIssuesLabel.text = issueLabel;

            string lines = "";
            foreach (string issue in result.unfixedIssues)
                lines += "• " + issue + "\n";
            statusIssuesBody.text = lines.TrimEnd('\n');
        }

        if (allFixed && statusResolvedText != null)
        {
            string msg = (result.jobType == JobType.Build)
                ? "All requested parts installed!" : "All issues resolved!";
            statusResolvedText.text = msg;
        }

        // ── Payment breakdown ────────────────────────────────────
        if (baseRewardValue != null)
            baseRewardValue.text = "₱" + result.basePay.ToString("N0");

        if (scoreLabel != null)
            scoreLabel.text = (result.score >= 0.95f) ? "Score" : "Score";
        if (scoreValue != null)
        {
            int pct = Mathf.RoundToInt(result.score * 100f);
            scoreValue.text = pct + "%";
            scoreValue.color = (pct >= 95) ? accentGreen
                             : (pct >= 80) ? accentAmber
                             : accentRed;
        }

        if (earnedRewardValue != null)
            earnedRewardValue.text = "₱" + result.earnedReward.ToString("N0");

        // Tip row
        bool hasTip = result.tipBonus > 0;
        if (tipRow != null) tipRow.SetActive(hasTip);
        if (hasTip)
        {
            if (tipLabel != null)
            {
                string bonusLabel = result.starRating == 5
                    ? "Customer tip (5★ bonus)" : "Customer tip (4★ bonus)";
                tipLabel.text = bonusLabel;
            }
            if (tipValue != null)
                tipValue.text = "+₱" + result.tipBonus.ToString("N0");
        }

        // ── Total ────────────────────────────────────────────────
        if (totalValue != null)
            totalValue.text = "₱" + result.totalPay.ToString("N0");

        // ── Button ───────────────────────────────────────────────
        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(Hide);
        }

        // ── Backward compat: also fill legacy fields ─────────────
        FillLegacyFields(result);

        // ── Animate ──────────────────────────────────────────────
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateIn());
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════
    //  TITLE GENERATOR
    // ═══════════════════════════════════════════════════════════

    string GetTitle(int stars, JobType jobType)
    {
        string prefix = (jobType == JobType.Build) ? "Build" : "Repair";

        string[][] pools =
        {
            new[] { "Terrible Job!", "Customer Furious!", "What Happened?!" },
            new[] { "Sloppy Work...", "Customer Unhappy", "Needs Improvement" },
            new[] { "Job Done", "Acceptable Work", "Could Be Better" },
            new[] { "Great Job!", $"Solid {prefix}!", "Well Done!" },
            new[] { $"Perfect {prefix}!", "Flawless Work!", "Master Technician!" },
        };

        int idx = Mathf.Clamp(stars - 1, 0, 4);
        string[] pool = pools[idx];
        return pool[Random.Range(0, pool.Length)];
    }

    Color GetAccentForRating(int stars)
    {
        if (stars >= 5) return accentCyan;
        if (stars >= 4) return accentAmber;
        if (stars >= 3) return bodyText;
        return accentRed;
    }

    // ═══════════════════════════════════════════════════════════
    //  LEGACY FIELD FILL (so old EmailManager code still works)
    // ═══════════════════════════════════════════════════════════

    void FillLegacyFields(JobCompletionResult result)
    {
        if (legacyTitle != null)
            legacyTitle.text = titleText != null ? titleText.text : "";

        if (legacyRating != null)
        {
            string stars = "";
            for (int i = 0; i < 5; i++)
                stars += (i < result.starRating)
                    ? "<color=#FFD700>★</color>" : "<color=#555555>★</color>";
            legacyRating.text = stars;
        }

        if (legacyDetails != null)
        {
            string details = $"<b>Customer:</b> {result.customerName}\n";
            details += $"<b>Job:</b> {result.problemDescription}\n\n";
            if (result.unfixedIssues == null || result.unfixedIssues.Count == 0)
            {
                string doneMsg = (result.jobType == JobType.Build)
                    ? "All requested parts installed!" : "All issues resolved!";
                details += $"<color=#4AFF4A>{doneMsg}</color>";
            }
            else
            {
                string label = (result.jobType == JobType.Build)
                    ? "Missing parts:" : "Unfixed issues:";
                details += $"<color=#FF6B6B>{label}</color>\n";
                foreach (string issue in result.unfixedIssues)
                    details += $"  <color=#FF8888>• {issue}</color>\n";
            }
            if (result.starRating == 5)
                details += "\n\n<color=#FFD700>Customer left a tip!</color>";
            legacyDetails.text = details;
        }

        if (legacyPay != null)
        {
            string payText = $"Reward: ₱{result.earnedReward:N0}";
            if (result.tipBonus > 0)
                payText += $"\n<color=#FFD700>Tip: +₱{result.tipBonus:N0}</color>";
            payText += $"\n\n<b><size=120%>Total: ₱{result.totalPay:N0}</size></b>";
            legacyPay.text = payText;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ANIMATION
    // ═══════════════════════════════════════════════════════════

    IEnumerator AnimateIn()
    {
        // Fade panel in
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rootCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            rootCanvasGroup.alpha = 1f;
        }

        // Stagger stars
        if (starTexts != null)
        {
            for (int i = 0; i < starTexts.Length; i++)
            {
                if (starTexts[i] == null) continue;

                // Only animate lit stars
                if (starTexts[i].color.r > 0.5f && starTexts[i].color.g > 0.5f)
                {
                    Transform t = starTexts[i].transform;
                    Vector3 orig = t.localScale;
                    t.localScale = Vector3.zero;

                    float dur = 0.25f;
                    float el = 0f;
                    while (el < dur)
                    {
                        el += Time.unscaledDeltaTime;
                        float p = el / dur;
                        // Overshoot bounce
                        float scale = 1f + Mathf.Sin(p * Mathf.PI) * 0.35f;
                        t.localScale = orig * Mathf.Min(scale, 1f + (1f - p) * 0.35f);
                        yield return null;
                    }
                    t.localScale = orig;

                    yield return new WaitForSecondsRealtime(starStagger);
                }
            }
        }
    }
}
