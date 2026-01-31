using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public TMP_Text CoinsText;
    public Transform ShopPanel;
    public Vector3 ShopOutOffScreenPos;
    public Vector3 ShopOnScreenPos;

    public Button OpenShopButton;
    public Button CloseShopButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        OpenShopButton.onClick.AddListener(OpenShop);
        CloseShopButton.onClick.AddListener(CloseShop);
    }


    private void Start()
    {
        GameManager.Instance.OnMoneyChanged += UpdateCoins;
        CloseShop();
    }

    public void CloseShop()
    {
        ShopPanel.DOLocalMove(ShopOutOffScreenPos, 1.5f).SetEase(Ease.InBack);
    }

    private void UpdateCoins(int NewCount)
    {
        CoinsText.text = "" + NewCount;
    }

    private void OpenShop()
    {
        ShopPanel.DOLocalMove(ShopOnScreenPos, 1.5f).SetEase(Ease.OutBack);
        
    }
}