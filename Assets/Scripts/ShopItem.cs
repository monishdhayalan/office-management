using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class ShopItem : MonoBehaviour
{
    
    public Image Item;
    public TMP_Text ItemName;
    public TMP_Text Cost;

    public ShopItemSO ItemSO;

    public Button BuyItemButton;

    private void Awake()
    {
        BuyItemButton.onClick.AddListener(BuyItem);
    }

    private void BuyItem()
    {
        int currentCost = GameManager.Instance.GetItemCurrentCost(ItemSO);

        if (GameManager.Instance.TrySpendMoney(currentCost))
        {
            // Don't increment purchase count here - wait for confirmed placement
            
            // Refresh visuals (cost will update after placement is confirmed)
            UpdateUI();

            UIManager.Instance.CloseShop();
            
            if (ItemSO.EShopItem == EShopItems.Employee)
            {
                GameObject prefab = GameManager.Instance.GetEmpoyeeSkin();
                GameManager.Instance.StartSpawningEmployee(prefab, ItemSO, currentCost);
            }
            else
            {
                PlacementManager.Instance.StartPlacement(ItemSO.ItemPrefab, ItemSO, currentCost);
            }
        }
        else
        {
            Debug.Log("Not enough money!");
            // Optionally add UI feedback here
        }
    }
    

    
    private void Start()
    {
        Item.sprite = ItemSO.ItemSprite;
        ItemName.SetText(ItemSO.ItemName);
        UpdateUI();
        
        // Subscribe to purchase confirmed to update cost after successful placement
        GameManager.Instance.OnPurchaseConfirmed += UpdateUI;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPurchaseConfirmed -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance != null && ItemSO != null)
        {
            int currentCost = GameManager.Instance.GetItemCurrentCost(ItemSO);
            Cost.SetText(currentCost + "");
        }
    }
}
