using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CategoryIconData
{
    public string categoryName;
    public Sprite categoryIcon;
}

public class ShopManager : MonoBehaviour
{
    [Header("Search UI")]
    public TMP_InputField searchInputField;
    private string currentCategory = "";

    [Header("Category Navigation UI")]
    public GameObject categoryScreen;
    public GameObject itemListScreen;
    public GameObject categoryButtonPrefab;
    public Transform categoryContentContainer;

    public List<CategoryIconData> categoryIcons;

    // --- Filter/Categories Panel ---
    [Header("Filter Panel")]
    public GameObject filterCategoriesPanel;

    // --- Back Navigation ---
    [Header("Back Navigation")]
    public GameObject thisShopPanel;
    public GameObject previousShopPanel;

    [Header("Shop UI References")]
    public GameObject productTemplatePrefab;
    public Transform contentContainer;

    [Header("Shop Inventory")]
    public ItemData[] itemsForSale;

    [Header("Product Details UI")]
    public GameObject productDetailsPanel;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescText;
    public TextMeshProUGUI detailPriceText;
    public Image detailImage;
    public Button detailAddToCartBtn;

    [Header("Cart Button")]
    public Button cartIconButton;

    [Header("Shopping Cart Data")]
    public List<CartItem> shoppingCart = new List<CartItem>();

    [Header("Cart UI References")]
    public GameObject cartItemPrefab;
    public Transform cartContentContainer;
    public TextMeshProUGUI totalCostText;

    [Header("Cart Animation Settings")]
    public RectTransform cartPanelRect;
    public Vector2 cartHiddenPosition;
    public Vector2 cartVisiblePosition;
    public float cartSlideDuration = 0.3f;
    private bool isCartOpen = false;
    private Coroutine activeCartAnimation;

    [Header("Player Wallet Integration")]
    public PlayerWallet playerWallet;

    [Header("Notification UI")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationText;
    public RectTransform notificationRect;
    public Vector2 hiddenPosition;
    public Vector2 visiblePosition;
    public float slideDuration = 0.3f;
    public float notificationDuration = 2f;

    private Coroutine activeNotification;
    private bool hasStarted = false;

    private enum LastScreen { CategoryScreen, ItemList, ProductDetails }
    private LastScreen lastActiveScreen = LastScreen.CategoryScreen;
    private ItemData currentViewedItem;

    void Start()
    {
        if (notificationRect != null) notificationRect.anchoredPosition = hiddenPosition;

        if (cartPanelRect != null)
        {
            cartPanelRect.anchoredPosition = cartHiddenPosition;
            cartPanelRect.gameObject.SetActive(false);
        }

        if (productDetailsPanel != null) productDetailsPanel.SetActive(false);
        if (filterCategoriesPanel != null) filterCategoriesPanel.SetActive(false);

        if (playerWallet == null) playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(UpdateSearchFilter);

        if (cartIconButton != null)
            cartIconButton.onClick.AddListener(ToggleCart);

        hasStarted = true;
        GenerateCategories();
        RefreshCartUI();
        OpenCategoryScreen();
    }

    public void OpenShopApp()
    {
        isCartOpen = false;
        if (activeCartAnimation != null) StopCoroutine(activeCartAnimation);

        if (cartPanelRect != null)
        {
            cartPanelRect.anchoredPosition = cartHiddenPosition;
            cartPanelRect.gameObject.SetActive(false);
        }

        if (filterCategoriesPanel != null) filterCategoriesPanel.SetActive(false);

        if (!hasStarted) return;

        GenerateCategories();
        RefreshCartUI();

        if (lastActiveScreen == LastScreen.ProductDetails && currentViewedItem != null)
        {
            OpenProductDetails(currentViewedItem);
        }
        else if (lastActiveScreen == LastScreen.ItemList)
        {
            OpenItemList(currentCategory);
        }
        else
        {
            OpenCategoryScreen();
        }
    }

    public void ToggleFilterPanel()
    {
        if (filterCategoriesPanel == null) return;
        filterCategoriesPanel.SetActive(!filterCategoriesPanel.activeSelf);
    }

    public void CloseFilterPanel()
    {
        if (filterCategoriesPanel == null) return;
        filterCategoriesPanel.SetActive(false);
    }

    public void GoBackToPreviousShop()
    {
        if (filterCategoriesPanel != null) filterCategoriesPanel.SetActive(false);
        if (thisShopPanel != null) thisShopPanel.SetActive(false);
        if (previousShopPanel != null) previousShopPanel.SetActive(true);
    }

    void GenerateCategories()
    {
        List<string> uniqueCategories = new List<string>();
        foreach (ItemData item in itemsForSale)
        {
            if (item != null && !uniqueCategories.Contains(item.category) && !string.IsNullOrEmpty(item.category))
                uniqueCategories.Add(item.category);
        }

        foreach (Transform child in categoryContentContainer)
            Destroy(child.gameObject);

        foreach (string cat in uniqueCategories)
        {
            GameObject newCatBtn = Instantiate(categoryButtonPrefab, categoryContentContainer);
            Transform t = newCatBtn.transform;

            Transform textObj = t.Find("ItemNamePanel/Text (TMP)");
            if (textObj != null)
                textObj.GetComponent<TextMeshProUGUI>().text = cat;

            Transform imgObj = t.Find("ItemImage");
            if (imgObj != null)
            {
                Sprite iconToUse = null;
                foreach (CategoryIconData iconData in categoryIcons)
                {
                    if (iconData.categoryName == cat)
                    {
                        iconToUse = iconData.categoryIcon;
                        break;
                    }
                }

                if (iconToUse != null)
                {
                    imgObj.GetComponent<Image>().sprite = iconToUse;
                    imgObj.GetComponent<Image>().enabled = true;
                }
                else
                {
                    imgObj.GetComponent<Image>().enabled = false;
                }
            }

            string selectedCategory = cat;
            Button btnComponent = newCatBtn.GetComponent<Button>();
            if (btnComponent != null)
                btnComponent.onClick.AddListener(() => OpenItemList(selectedCategory));
        }
    }

    public void OpenCategoryScreen()
    {
        lastActiveScreen = LastScreen.CategoryScreen;
        categoryScreen.SetActive(true);
        itemListScreen.SetActive(false);
        if (productDetailsPanel != null) productDetailsPanel.SetActive(false);
    }

    public void OpenItemList(string categoryName)
    {
        lastActiveScreen = LastScreen.ItemList;
        currentCategory = categoryName;

        categoryScreen.SetActive(false);
        if (productDetailsPanel != null) productDetailsPanel.SetActive(false);
        itemListScreen.SetActive(true);
        if (filterCategoriesPanel != null) filterCategoriesPanel.SetActive(false);

        if (searchInputField != null)
            searchInputField.SetTextWithoutNotify("");

        UpdateSearchFilter("");
    }

    public void BackToCategories()
    {
        OpenCategoryScreen();
    }

    public bool HandleEscape()
    {
        if (filterCategoriesPanel != null && filterCategoriesPanel.activeSelf)
        {
            filterCategoriesPanel.SetActive(false);
            return true;
        }

        if (isCartOpen)
        {
            ToggleCart();
            return true;
        }
        else if (productDetailsPanel != null && productDetailsPanel.activeSelf)
        {
            CloseProductDetails();
            return true;
        }
        else if (itemListScreen != null && itemListScreen.activeSelf)
        {
            ReturnToCategoryList();
            return true;
        }

        return false;
    }

    public void UpdateSearchFilter(string searchTerm)
    {
        foreach (Transform child in contentContainer)
            Destroy(child.gameObject);

        string lowerSearchTerm = searchTerm.ToLower();

        foreach (ItemData item in itemsForSale)
        {
            bool matchesCategory = string.IsNullOrEmpty(currentCategory) || item.category == currentCategory;

            if (matchesCategory)
            {
                if (string.IsNullOrEmpty(lowerSearchTerm) || item.itemName.ToLower().Contains(lowerSearchTerm))
                {
                    GameObject newProduct = Instantiate(productTemplatePrefab, contentContainer);
                    Transform t = newProduct.transform;

                    Transform nameObj = t.Find("Product Name/Text (TMP)");
                    if (nameObj != null) nameObj.GetComponent<TextMeshProUGUI>().text = item.itemName;

                    Transform specsObj = t.Find("Product Specs");
                    if (specsObj != null) specsObj.GetComponent<TextMeshProUGUI>().text = item.description;

                    Transform priceObj = t.Find("Product Price");
                    if (priceObj != null) priceObj.GetComponent<TextMeshProUGUI>().text = "₱" + item.price.ToString("N0");

                    Transform imgObj = t.Find("Image");
                    if (imgObj != null) imgObj.GetComponent<Image>().sprite = item.icon;

                    Transform cartBtnObj = t.Find("Add To Cart");
                    if (cartBtnObj != null)
                    {
                        Button addToCartBtn = cartBtnObj.GetComponent<Button>();
                        addToCartBtn.onClick.AddListener(() => AddToCart(item));
                    }

                    Button mainPanelBtn = newProduct.GetComponent<Button>();
                    if (mainPanelBtn != null)
                        mainPanelBtn.onClick.AddListener(() => OpenProductDetails(item));
                }
            }
        }
    }

    public void OpenProductDetails(ItemData item)
    {
        lastActiveScreen = LastScreen.ProductDetails;
        currentViewedItem = item;

        if (categoryScreen != null) categoryScreen.SetActive(false);
        if (itemListScreen != null) itemListScreen.SetActive(false);
        if (productDetailsPanel != null) productDetailsPanel.SetActive(true);

        if (cartIconButton != null)
            cartIconButton.transform.SetAsLastSibling();

        if (detailNameText != null) detailNameText.text = item.itemName;
        if (detailDescText != null) detailDescText.text = item.description;
        if (detailPriceText != null) detailPriceText.text = "₱" + item.price.ToString("N0");

        if (detailImage != null && item.icon != null)
            detailImage.sprite = item.icon;

        if (detailAddToCartBtn != null)
        {
            detailAddToCartBtn.onClick.RemoveAllListeners();
            detailAddToCartBtn.onClick.AddListener(() => AddToCart(item));
        }
    }

    public void CloseProductDetails()
    {
        OpenItemList(currentCategory);
    }

    public void ReturnToCategoryList()
    {
        OpenCategoryScreen();
    }

    public void AddToCart(ItemData itemAdded)
    {
        CartItem existingItem = shoppingCart.Find(x => x.item == itemAdded);

        if (existingItem != null)
        {
            existingItem.amount++;
            existingItem.isChecked = true;
        }
        else
        {
            CartItem newItem = new CartItem();
            newItem.item = itemAdded;
            newItem.amount = 1;
            newItem.deliveryDays = 2;
            newItem.isChecked = true;
            shoppingCart.Add(newItem);
        }

        RefreshCartUI();

        if (activeNotification != null) StopCoroutine(activeNotification);
        activeNotification = StartCoroutine(SlideNotificationCoroutine(itemAdded.itemName + " added to cart!"));
    }

    public void ChangeItemAmount(CartItem itemToChange, int amountChange)
    {
        itemToChange.amount += amountChange;
        if (itemToChange.amount < 1) itemToChange.amount = 1;
        RefreshCartUI();
    }

    public void RemoveFromCartCompletely(CartItem itemToRemove)
    {
        shoppingCart.Remove(itemToRemove);
        RefreshCartUI();
    }

    public void CalculateCartTotal()
    {
        float finalTotal = 0f;

        foreach (CartItem cartEntry in shoppingCart)
        {
            if (cartEntry.isChecked)
                finalTotal += (cartEntry.item.price * cartEntry.amount);
        }

        if (totalCostText != null)
            totalCostText.text = "Total: ₱" + finalTotal.ToString("N0");
    }

    public void RefreshCartUI()
    {
        if (cartItemPrefab == null || cartContentContainer == null) return;

        foreach (Transform child in cartContentContainer)
            Destroy(child.gameObject);

        foreach (CartItem cartEntry in shoppingCart)
        {
            GameObject newCartRow = Instantiate(cartItemPrefab, cartContentContainer);
            Transform t = newCartRow.transform;
            CartItem currentItem = cartEntry;

            Transform toggleObj = t.Find("ItemToggle");
            if (toggleObj != null)
            {
                Toggle itemToggle = toggleObj.GetComponent<Toggle>();
                itemToggle.onValueChanged.RemoveAllListeners();
                itemToggle.isOn = currentItem.isChecked;
                itemToggle.onValueChanged.AddListener((bool isTicked) =>
                {
                    currentItem.isChecked = isTicked;
                    CalculateCartTotal();
                });
            }

            Transform removeBtnObj = t.Find("Remove Button");
            if (removeBtnObj != null)
                removeBtnObj.GetComponent<Button>().onClick.AddListener(() => RemoveFromCartCompletely(currentItem));

            Transform inputFieldObj = t.Find("InputField (TMP)");
            if (inputFieldObj != null)
            {
                TMP_InputField amountField = inputFieldObj.GetComponent<TMP_InputField>();
                if (amountField != null) amountField.text = currentItem.amount.ToString();

                Transform increaseBtn = inputFieldObj.Find("Increase Amount");
                if (increaseBtn != null) increaseBtn.GetComponent<Button>().onClick.AddListener(() => ChangeItemAmount(currentItem, 1));

                Transform decreaseBtn = inputFieldObj.Find("Decrease Amount");
                if (decreaseBtn != null) decreaseBtn.GetComponent<Button>().onClick.AddListener(() => ChangeItemAmount(currentItem, -1));
            }

            Transform nameObj = t.Find("Item Name");
            if (nameObj != null) nameObj.GetComponent<TextMeshProUGUI>().text = currentItem.item.itemName;

            Transform descObj = t.Find("Item Description");
            if (descObj != null) descObj.GetComponent<TextMeshProUGUI>().text = currentItem.item.description;

            float totalRowPrice = currentItem.item.price * currentItem.amount;

            Transform priceObj = t.Find("Item Price");
            if (priceObj != null) priceObj.GetComponent<TextMeshProUGUI>().text = "Price: ₱" + totalRowPrice.ToString("N0");

            Transform arrivalObj = t.Find("Arrival Day");
            if (arrivalObj != null) arrivalObj.GetComponent<TextMeshProUGUI>().text = "Time of Arrival: " + currentItem.deliveryDays + " Days";
        }

        CalculateCartTotal();
    }

    public void Checkout()
    {
        List<CartItem> itemsToBuy = shoppingCart.FindAll(x => x.isChecked);

        if (itemsToBuy.Count == 0)
        {
            if (activeNotification != null) StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(SlideNotificationCoroutine("Select an item to checkout!"));
            return;
        }

        if (playerWallet == null) return;

        float finalCheckoutTotal = 0f;
        foreach (CartItem cartEntry in itemsToBuy)
            finalCheckoutTotal += (cartEntry.item.price * cartEntry.amount);

        if (playerWallet.SpendGold(finalCheckoutTotal))
        {
            Debug.Log("Order successfully placed!");

            if (activeNotification != null) StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(SlideNotificationCoroutine("Order Placed Successfully!"));

            foreach (CartItem boughtItem in itemsToBuy)
            {
                if (DeliveryManager.Instance != null)
                {
                    DeliveryManager.Instance.PlaceOrder(
                        boughtItem.item,
                        boughtItem.amount,
                        boughtItem.deliveryDays
                    );
                }
                else
                {
                    Debug.LogError("DeliveryManager not found! Did you add it to the scene?");
                }

                shoppingCart.Remove(boughtItem);
            }

            if (CloudDataHandler.Instance != null)
                CloudDataHandler.Instance.SaveGameData();
            else
                Debug.LogWarning("CloudDataHandler not found, wallet changes might not be saved!");

            RefreshCartUI();

            if (isCartOpen) ToggleCart();
        }
        else
        {
            if (activeNotification != null) StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(SlideNotificationCoroutine("Not enough money!"));
        }
    }

    public void ToggleCart()
    {
        isCartOpen = !isCartOpen;

        if (activeCartAnimation != null) StopCoroutine(activeCartAnimation);

        if (isCartOpen)
        {
            cartPanelRect.gameObject.SetActive(true);
            activeCartAnimation = StartCoroutine(SlideCartCoroutine(cartVisiblePosition));
        }
        else
        {
            activeCartAnimation = StartCoroutine(SlideCartCoroutine(cartHiddenPosition, hideWhenDone: true));
        }
    }

    private IEnumerator SlideCartCoroutine(Vector2 targetPosition, bool hideWhenDone = false)
    {
        Vector2 startPosition = cartPanelRect.anchoredPosition;
        float timeElapsed = 0f;

        while (timeElapsed < cartSlideDuration)
        {
            cartPanelRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, timeElapsed / cartSlideDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        cartPanelRect.anchoredPosition = targetPosition;

        if (hideWhenDone)
            cartPanelRect.gameObject.SetActive(false);
    }

    private IEnumerator SlideNotificationCoroutine(string message)
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);

        float timeElapsed = 0f;
        while (timeElapsed < slideDuration)
        {
            notificationRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, timeElapsed / slideDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        notificationRect.anchoredPosition = visiblePosition;

        yield return new WaitForSeconds(notificationDuration);

        timeElapsed = 0f;
        while (timeElapsed < slideDuration)
        {
            notificationRect.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, timeElapsed / slideDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        notificationRect.anchoredPosition = hiddenPosition;
        notificationPanel.SetActive(false);
    }
}

[System.Serializable]
public class CartItem
{
    public ItemData item;
    public int amount;
    public int deliveryDays;
    public bool isChecked;
}