using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MonitorShopBridge : MonoBehaviour
{
    [Header("=== SHOP UI ===")]

    [Header("Search")]
    public TMP_InputField searchInputField;

    [Header("Category Navigation")]
    public GameObject categoryScreen;
    public GameObject itemListScreen;
    public Transform categoryContentContainer;

    [Header("Filter")]
    public GameObject filterCategoriesPanel;

    [Header("Back Navigation")]
    public GameObject thisShopPanel;
    public GameObject previousShopPanel;

    [Header("Item List")]
    public Transform contentContainer;

    [Header("Product Details")]
    public GameObject productDetailsPanel;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;
    public TextMeshProUGUI detailPriceText;
    public Image detailImage;
    public Button detailAddToCartBtn;

    [Header("Cart Buttons")]
    public Button[] cartIconButtons;

    [Header("Cart Panel")]
    public RectTransform cartPanelRect;
    public Transform cartContentContainer;
    public TextMeshProUGUI totalCostText;

    [Header("Notification")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    public RectTransform notificationRect;

    [Header("=== EMAIL UI ===")]

    [Header("Inbox")]
    public Transform inboxContentContainer;

    [Header("Email Detail")]
    public GameObject emailDetailPanel;
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI subjectText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI objectivesText;
    public Image detailProfilePic;

    [Header("PC Status")]
    public GameObject pcStatusPanel;
    public TextMeshProUGUI pcProblemsText;

    [Header("Build Request")]
    public TextMeshProUGUI requestedPartsText;
    public Transform pcSpecsContentContainer;

    [Header("Email Buttons")]
    public Button acceptButton;
    public Button rejectButton;
    public Button completeButton;

    [Header("Completion Popup")]
    public GameObject completionPanel;
    public TextMeshProUGUI completionTitle;
    public TextMeshProUGUI completionDetails;
    public TextMeshProUGUI completionPay;
    public TextMeshProUGUI completionRating;
    public Button completionOKButton;
    [Header("PC Status Button")]
    public Button pcStatusButton;
    public void ActivateForShop()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null)
        {
            shop.SetActiveMonitorUI(
                searchInputField,
                categoryScreen, itemListScreen, categoryContentContainer,
                filterCategoriesPanel,
                thisShopPanel, previousShopPanel,
                contentContainer,
                productDetailsPanel, detailNameText, detailDescText,
                detailPriceText, detailImage, detailAddToCartBtn,
                cartIconButtons,
                cartPanelRect, cartContentContainer, totalCostText,
                notificationPanel, notificationText, notificationRect
            );
        }
    }

    public void ActivateForEmail()
    {
        EmailManager email = EmailManager.Instance;
        Debug.Log($"[BRIDGE] ActivateForEmail — EmailManager.Instance={email}, " +
                  $"pcStatusPanel={pcStatusPanel}, pcStatusButton={pcStatusButton}");
        if (email != null)
        {
            email.SetActiveMonitorUI(
                inboxContentContainer, emailDetailPanel,
                senderText, subjectText, bodyText,
                rewardText, objectivesText,
                detailProfilePic, pcStatusPanel,
                pcProblemsText, requestedPartsText,
                pcSpecsContentContainer,
                acceptButton, rejectButton, completeButton,
                completionPanel, completionTitle,
                completionDetails, completionPay,
                completionRating, completionOKButton, pcStatusButton
            );
        }
    }
}