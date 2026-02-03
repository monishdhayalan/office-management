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
        OpenShopButton.onClick.AddListener((() => AudioManager.Instance.PlaySFX(SoundType.ButtonClick)));
        CloseShopButton.onClick.AddListener(CloseShop);
        CloseShopButton.onClick.AddListener((() => AudioManager.Instance.PlaySFX(SoundType.ButtonClick)));
    }


    private void Start()
    {
        GameManager.Instance.OnMoneyChanged += UpdateCoins;
        CloseShop();
    }

    

    private void UpdateCoins(int NewCount)
    {
        CoinsText.text = "" + NewCount;
    }

    private void OpenShop()
    {
        ShopPanel.DOLocalMove(ShopOnScreenPos, .55f).SetEase(Ease.OutBack);
        
        
    }
    public void CloseShop()
    {
        ShopPanel.DOLocalMove(ShopOutOffScreenPos, .35f).SetEase(Ease.InBack);
    }
}