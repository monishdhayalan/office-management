using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TMP_Text CoinsText;

    private void Start()
    {
        GameManager.Instance.OnMoneyChanged += UpdateCoins;
    }

    private void UpdateCoins(int NewCount)
    {
        CoinsText.text = "" + NewCount;
    }
}