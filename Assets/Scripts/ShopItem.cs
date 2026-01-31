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
            GameManager.Instance.IncrementItemPurchaseCount(ItemSO);
            
            // Refresh visuals 
            UpdateUI();

            UIManager.Instance.CloseShop();
            
            if (ItemSO.EShopItem == EShopItems.Employee)
            {
                GameManager.Instance.StartSpawningEmployee(ItemSO.ItemPrefab);
            }
            else
            {
                PlacementManager.Instance.StartPlacement(ItemSO.ItemPrefab);
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
