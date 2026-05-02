using UnityEngine;

public class ShopRelay : MonoBehaviour
{
    // These are called by UI buttons via Inspector OnClick
    public void Relay_OpenCategoryScreen()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.OpenCategoryScreen();
    }

    public void Relay_BackToCategories()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.BackToCategories();
    }

    public void Relay_GoBackToPreviousShop()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.GoBackToPreviousShop();
    }

    public void Relay_CloseProductDetails()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.CloseProductDetails();
    }

    public void Relay_ToggleCart()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.ToggleCart();
    }

    public void Relay_ToggleFilterPanel()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.ToggleFilterPanel();
    }

    public void Relay_Checkout()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.Checkout();
    }

    public void Relay_HandleEscape()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.HandleEscape();
    }

    // Email relays
    public void Relay_TogglePCStatusPanel()
    {
        if (EmailManager.Instance != null)
            EmailManager.Instance.TogglePCStatusPanel();
    }

    public void Relay_ClosePCStatusPanel()
    {
        if (EmailManager.Instance != null)
            EmailManager.Instance.ClosePCStatusPanel();
    }

    public void Relay_OpenEmailApp()
    {
        if (EmailManager.Instance != null)
            EmailManager.Instance.OpenEmailApp();
    }
}