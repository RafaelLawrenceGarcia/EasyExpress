using UnityEngine;
using TMPro; 
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; 

public class ShopManager : MonoBehaviour
{
    [Header("Category Navigation UI")]
    public GameObject categoryScreen;        // The "Item Category" ScrollView
    public GameObject itemListScreen;        // The "Item List" ScrollView
    public GameObject categoryButtonPrefab;  // The prefab for the category button
    public Transform categoryContentContainer; 

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

    void Start()
    {
        if (notificationRect != null) notificationRect.anchoredPosition = hiddenPosition;
        if (cartPanelRect != null) cartPanelRect.anchoredPosition = cartHiddenPosition;
        if (productDetailsPanel != null) productDetailsPanel.SetActive(false);

        if (playerWallet == null) playerWallet = FindFirstObjectByType<PlayerWallet>();

        RefreshCartUI();
        
        // NEW: Generate the category buttons and show the category screen!
        GenerateCategories();
        OpenCategoryScreen();
    }

    // --- NEW: CATEGORY NAVIGATION METHODS ---

    void GenerateCategories()
    {
        // 1. Find all the unique categories from your itemsForSale list
        List<string> uniqueCategories = new List<string>();
        foreach (ItemData item in itemsForSale)
        {
            if (!uniqueCategories.Contains(item.category) && !string.IsNullOrEmpty(item.category))
            {
                uniqueCategories.Add(item.category);
            }
        }

        // 2. Clear out any old buttons
        foreach (Transform child in categoryContentContainer)
        {
            Destroy(child.gameObject);
        }

        // 3. Spawn a button for each category found
        foreach (string cat in uniqueCategories)
        {
            GameObject newCatBtn = Instantiate(categoryButtonPrefab, categoryContentContainer);
            
            // Set the text on the button to the category name
            newCatBtn.GetComponentInChildren<TextMeshProUGUI>().text = cat;
            
            // Tell the button to open this specific category when clicked
            string selectedCategory = cat; // Store in local variable for the listener
            newCatBtn.GetComponent<Button>().onClick.AddListener(() => OpenItemList(selectedCategory));
        }
    }

    public void OpenCategoryScreen()
    {
        // Turn ON Categories, turn OFF Items
        categoryScreen.SetActive(true);
        itemListScreen.SetActive(false);
    }

    public void OpenItemList(string categoryName)
    {
        // Turn OFF Categories, turn ON Items
        categoryScreen.SetActive(false);
        itemListScreen.SetActive(true);

        // Clear out the old items from the previous category we looked at
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // Spawn ONLY the items that match the clicked category!
        foreach (ItemData item in itemsForSale)
        {
            if (item.category == categoryName)
            {
                GameObject newProduct = Instantiate(productTemplatePrefab, contentContainer);
                Transform t = newProduct.transform;

                t.Find("Product Name").GetComponent<TextMeshProUGUI>().text = item.itemName; 
                t.Find("Product Price").GetComponent<TextMeshProUGUI>().text = "₱" + item.price.ToString("N0"); 
                t.Find("Product Specs").GetComponent<TextMeshProUGUI>().text = item.description; 
                t.Find("Image").GetComponent<Image>().sprite = item.icon; 

                Button addToCartBtn = t.Find("Add To Cart").GetComponent<Button>();
                addToCartBtn.onClick.AddListener(() => AddToCart(item));

                Button mainPanelBtn = newProduct.GetComponent<Button>();
                if (mainPanelBtn != null)
                {
                    mainPanelBtn.onClick.AddListener(() => OpenProductDetails(item));
                }
            }
        }
    }

    // Hook this method to your new "Back" button!
    public void BackToCategories()
    {
        OpenCategoryScreen();
    }

    // ----------------------------------------

    public void OpenProductDetails(ItemData item)
    {
        if (productDetailsPanel != null) productDetailsPanel.SetActive(true);
        
        if (detailNameText != null) detailNameText.text = item.itemName;
        if (detailDescText != null) detailDescText.text = item.description;
        if (detailPriceText != null) detailPriceText.text = "₱" + item.price.ToString("N0");
        
        if (detailImage != null && item.icon != null)
        {
            detailImage.sprite = item.icon;
        }

        if (detailAddToCartBtn != null)
        {
            detailAddToCartBtn.onClick.RemoveAllListeners(); 
            detailAddToCartBtn.onClick.AddListener(() => AddToCart(item));
        }
    }

    public void CloseProductDetails()
    {
        if (productDetailsPanel != null) productDetailsPanel.SetActive(false);
    }

    public void AddToCart(ItemData itemAdded)
    {
        CartItem existingItem = shoppingCart.Find(x => x.item == itemAdded);

        if (existingItem != null)
        {
            existingItem.amount++;
        }
        else
        {
            CartItem newItem = new CartItem();
            newItem.item = itemAdded;
            newItem.amount = 1;
            newItem.deliveryDays = 2; 
            shoppingCart.Add(newItem);
        }

        RefreshCartUI();

        if (activeNotification != null) StopCoroutine(activeNotification);
        activeNotification = StartCoroutine(SlideNotificationCoroutine(itemAdded.itemName + " added to cart!"));
    }

    public void RemoveFromCart(CartItem itemToRemove)
    {
        itemToRemove.amount--;

        if (itemToRemove.amount <= 0)
        {
            shoppingCart.Remove(itemToRemove);
        }

        RefreshCartUI();
    }

    public void RefreshCartUI()
    {
        if (cartItemPrefab == null || cartContentContainer == null) return;

        float finalTotal = 0f;

        foreach (Transform child in cartContentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (CartItem cartEntry in shoppingCart)
        {
            GameObject newCartRow = Instantiate(cartItemPrefab, cartContentContainer);
            Transform t = newCartRow.transform;

            t.Find("Item Image").GetComponent<Image>().sprite = cartEntry.item.icon;
            t.Find("Item Amount").GetComponent<TextMeshProUGUI>().text = "Qty: " + cartEntry.amount;

            float totalRowPrice = cartEntry.item.price * cartEntry.amount;
            t.Find("Item Price").GetComponent<TextMeshProUGUI>().text = "₱" + totalRowPrice.ToString("N0");
            t.Find("Arrival Day").GetComponent<TextMeshProUGUI>().text = "Arrives in " + cartEntry.deliveryDays + " Days";

            finalTotal += totalRowPrice;

            CartItem currentItem = cartEntry; 
            Button removeBtn = t.Find("Remove Button").GetComponent<Button>();
            removeBtn.onClick.AddListener(() => RemoveFromCart(currentItem));
        }

        if (totalCostText != null)
        {
            totalCostText.text = "Total: ₱" + finalTotal.ToString("N0");
        }
    }

    public void Checkout()
    {
        if (shoppingCart.Count == 0)
        {
            if (activeNotification != null) StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(SlideNotificationCoroutine("Cart is empty!"));
            return; 
        }

        if (playerWallet == null) return;

        float finalCheckoutTotal = 0f;
        foreach (CartItem cartEntry in shoppingCart)
        {
            finalCheckoutTotal += (cartEntry.item.price * cartEntry.amount);
        }

        if (playerWallet.SpendGold(finalCheckoutTotal))
        {
            Debug.Log("Order successfully placed!");

            if (activeNotification != null) StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(SlideNotificationCoroutine("Order Placed Successfully!"));

            shoppingCart.Clear();
            RefreshCartUI();
            
            if (isCartOpen) ToggleCart();
        }
        else
        {
            Debug.Log("Checkout failed. Player doesn't have enough money or loan capacity.");
            
            if (activeNotification != null) StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(SlideNotificationCoroutine("Checkout Failed: Not Enough Funds!"));
        }
    }

    public void ToggleCart()
    {
        isCartOpen = !isCartOpen;

        if (activeCartAnimation != null) StopCoroutine(activeCartAnimation);

        Vector2 targetPos = isCartOpen ? cartVisiblePosition : cartHiddenPosition;

        activeCartAnimation = StartCoroutine(SlideCartCoroutine(targetPos));
    }

    private IEnumerator SlideCartCoroutine(Vector2 targetPosition)
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
}