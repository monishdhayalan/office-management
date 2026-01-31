using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Money { get; private set; } = 0;

    public event Action<int> OnMoneyChanged;
    public GameObject Table;
    public GameObject EmployeePrefab;
    public LayerMask floorLayer; // Assign in Inspector

    private bool isSpawningEmployee = false;
    private GameObject employeePrefabToSpawn;

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
        // Optionally cancel other placement modes
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing)
        {
            PlacementManager.Instance.CancelPlacement();
        }
    }

    private void HandleEmployeeSpawning()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, floorLayer))
            {
                Instantiate(employeePrefabToSpawn, hit.point, Quaternion.identity);
                isSpawningEmployee = false;
                employeePrefabToSpawn = null;
            }
        }
        
        // Right click cancel
        if (Input.GetMouseButtonDown(1))
        {
            isSpawningEmployee = false;
            employeePrefabToSpawn = null;
        }
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
        Debug.Log($"Money Added: {amount}. Total: {Money}");
    }
}
