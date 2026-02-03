using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ShopItemSO", menuName = "Scriptable Objects/ShopItemSO")]
public class ShopItemSO : ScriptableObject
{
    public EShopItems EShopItem;
    public string ItemName;
    public Sprite ItemSprite;
    public GameObject ItemPrefab;
    public List<int> Costs;

}

public enum EShopItems
{
    Table,
    Employee
}