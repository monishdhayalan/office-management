using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Money { get; private set; } = 0;

    public event Action<int> OnMoneyChanged;
    public event Action OnPurchaseConfirmed; // Called when placement/spawn is confirmed
    public GameObject Table;
    public List<GameObject> EmployeeSkinPrefabs;
    public LayerMask floorLayer; // Assign in Inspector
    public Material GhostMaterial;
    public GameObject SpawnEffectPrefab;
    public GameObject TableSpawnEffectPrefab;
    public GameObject MoneyEarningEffectPrefab;
    public GameObject MoneyEarningTextPopup;

    private bool isSpawningEmployee = false;
    private GameObject employeePrefabToSpawn;
    private GameObject currentEmployeeGhost;
    
    // Pending purchase info for employee spawning
    private ShopItemSO pendingEmployeeItemSO;
    private int pendingEmployeeCost;

    private Dictionary<ShopItemSO, int> itemPurchaseCounts = new Dictionary<ShopItemSO, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // #if UNITY_EDITOR
        // AddMoney(1000);
        // #endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlacementManager.Instance.StartPlacement(Table);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            employeePrefabToSpawn = GetEmpoyeeSkin();
            StartSpawningEmployee(employeePrefabToSpawn);
        }

        if (isSpawningEmployee)
        {
            HandleEmployeeSpawning();
        }
        
        // Free coin on click when not doing anything else
        if (Input.GetMouseButtonDown(0) && !isSpawningEmployee && !PlacementManager.Instance.IsPlacing)
        {
            // Randomly give 1 coin (you can adjust the chance)
            if (Random.value > 0.7f) // 50% chance
            {
                // Raycast to get click position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, floorLayer))
                {
                    // Spawn coin particle effect
                    if (MoneyEarningEffectPrefab != null)
                    {
                        GameObject fx = Instantiate(MoneyEarningEffectPrefab, hit.point + Vector3.up * 0.5f, Quaternion.Euler(-90, 0, 0));
                        Destroy(fx, 2f);
                    }
                }
                AddMoney(1);
            }
        }
    }

    private int emplyeeIndx;
    public GameObject GetEmpoyeeSkin()
    {
        emplyeeIndx  = (emplyeeIndx + 1) % EmployeeSkinPrefabs.Count; 
        return EmployeeSkinPrefabs[emplyeeIndx]; 
    }

    public void StartSpawningEmployee(GameObject prefab, ShopItemSO itemSO = null, int cost = 0)
    {
        employeePrefabToSpawn = prefab;
        isSpawningEmployee = true;
        pendingEmployeeItemSO = itemSO;
        pendingEmployeeCost = cost;
        
        // Create Ghost
        if (currentEmployeeGhost != null) Destroy(currentEmployeeGhost);
        currentEmployeeGhost = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0, 180, 0));
        
        // Disable physics on ghost
        Collider[] cols = currentEmployeeGhost.GetComponentsInChildren<Collider>();
        foreach(var c in cols) c.enabled = false;
        
        // Apply Ghost Material
        if (GhostMaterial != null)
        {
            Renderer[] rends = currentEmployeeGhost.GetComponentsInChildren<Renderer>();
            foreach(var r in rends)
            {
                Material[] newMats = new Material[r.sharedMaterials.Length];
                for(int i=0; i<newMats.Length; i++) newMats[i] = GhostMaterial;
                r.materials = newMats;
            }
        }

        // Optionally cancel other placement modes
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing)
        {
            PlacementManager.Instance.CancelPlacement();
        }
    }

    private void HandleEmployeeSpawning()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, floorLayer))
        {
            if (currentEmployeeGhost != null)
            {
                currentEmployeeGhost.transform.position = hit.point;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Employee employee = Instantiate(employeePrefabToSpawn, hit.point, Quaternion.Euler(0,180,0)).GetComponentInChildren<Employee>();
                AudioManager.Instance.PlaySFX(SoundType.EmployeeSpawn);

                if (SpawnEffectPrefab != null)
                {
                    GameObject fx = Instantiate(SpawnEffectPrefab, hit.point, Quaternion.Euler(-90,0,0));
                    Destroy(fx, 2f); // Cleanup after 2 seconds
                }

                // Confirm purchase on successful spawn
                ConfirmEmployeePurchase();
                StopSpawning();
            }
        }
        
        // Right click cancel
        if (Input.GetMouseButtonDown(1))
        {
            RefundEmployeePurchase();
            StopSpawning();
        }
    }

    private void StopSpawning()
    {
        isSpawningEmployee = false;
        employeePrefabToSpawn = null;
        if (currentEmployeeGhost != null)
        {
            Destroy(currentEmployeeGhost);
            currentEmployeeGhost = null;
        }
    }

    private void ConfirmEmployeePurchase()
    {
        if (pendingEmployeeItemSO != null)
        {
            IncrementItemPurchaseCount(pendingEmployeeItemSO);
            pendingEmployeeItemSO = null;
            pendingEmployeeCost = 0;
            OnPurchaseConfirmed?.Invoke();
        }
    }

    private void RefundEmployeePurchase()
    {
        if (pendingEmployeeItemSO != null && pendingEmployeeCost > 0)
        {
            RefundMoney(pendingEmployeeCost);
            pendingEmployeeItemSO = null;
            pendingEmployeeCost = 0;
        }
    }

    public void TriggerPurchaseConfirmed()
    {
        OnPurchaseConfirmed?.Invoke();
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        AudioManager.Instance.PlaySFX(SoundType.MoneyEarned);
        OnMoneyChanged?.Invoke(Money);
        Debug.Log($"Money Added: {amount}. Total: {Money}");
    }

    public bool TrySpendMoney(int amount)
    {
        if (Money >= amount)
        {
            Money -= amount;
            OnMoneyChanged?.Invoke(Money);
            return true;
        }
        return false;
    }

    public void RefundMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
        Debug.Log($"Money Refunded: {amount}. Total: {Money}");
    }

    public int GetItemCurrentCost(ShopItemSO item)
    {
        if (item.Costs == null || item.Costs.Count == 0) return 0;
        
        int count = 0;
        if (itemPurchaseCounts.ContainsKey(item))
        {
            count = itemPurchaseCounts[item];
        }

        // If bought count >= list size, use the last cost in the list (max scaling)
        int index = Mathf.Min(count, item.Costs.Count - 1);
        return item.Costs[index];
    }

    public void IncrementItemPurchaseCount(ShopItemSO item)
    {
        if (itemPurchaseCounts.ContainsKey(item))
        {
            itemPurchaseCounts[item]++;
        }
        else
        {
            itemPurchaseCounts[item] = 1;
        }
    }
}
