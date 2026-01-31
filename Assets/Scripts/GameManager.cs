using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Money { get; private set; } = 0;

    public event Action<int> OnMoneyChanged;
    public GameObject Table;
    public GameObject EmployeePrefab;
    public LayerMask floorLayer; // Assign in Inspector
    public Material GhostMaterial;
    public GameObject SpawnEffectPrefab;
    public GameObject TableSpawnEffectPrefab;
    public GameObject MoneyEarningEffectPrefab;

    private bool isSpawningEmployee = false;
    private GameObject employeePrefabToSpawn;
    private GameObject currentEmployeeGhost;

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
        
        #if UNITY_EDITOR
        AddMoney(1000);
        #endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlacementManager.Instance.StartPlacement(Table);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartSpawningEmployee(EmployeePrefab);
        }

        if (isSpawningEmployee)
        {
            HandleEmployeeSpawning();
        }
    }

    public void StartSpawningEmployee(GameObject prefab)
    {
        employeePrefabToSpawn = prefab;
        isSpawningEmployee = true;
        
        // Create Ghost
        if (currentEmployeeGhost != null) Destroy(currentEmployeeGhost);
        currentEmployeeGhost = Instantiate(prefab);
        
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
                Employee employee = Instantiate(employeePrefabToSpawn, hit.point, Quaternion.identity).GetComponentInChildren<Employee>();

                if (SpawnEffectPrefab != null)
                {
                    GameObject fx = Instantiate(SpawnEffectPrefab, hit.point, Quaternion.Euler(-90,0,0));
                    Destroy(fx, 2f); // Cleanup after 2 seconds
                }

                StopSpawning();
            }
        }
        
        // Right click cancel
        if (Input.GetMouseButtonDown(1))
        {
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

    public void AddMoney(int amount)
    {
        Money += amount;
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
