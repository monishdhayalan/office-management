using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.AI;

public class Table : PlaceableObject
{
    // Static list to easily find tables
    public static List<Table> FreeTables = new List<Table>();
    public static List<Table> AllTables = new List<Table>();
    

    public Employee AssignedEmployee { get; private set; }
    public bool IsOccupied => AssignedEmployee != null;

    public List<Transform> InteractionPositions;

    private bool isPlaced = false;

    private void OnEnable()
    {
        if (isPlaced)
        {
            RegisterTable();
        }
    }

    public override void OnPlaced()
    {
        isPlaced = true;
        AudioManager.Instance.PlaySFX(SoundType.TableSpawned);
        Camera.main.transform.DOShakePosition(0.25f, Random.onUnitSphere * 0.15f).SetEase(Ease.OutBack).SetDelay(0.1f);
        RegisterTable();
    }

    private void RegisterTable()
    {
        if (!AllTables.Contains(this)) AllTables.Add(this);
        if (!IsOccupied && !FreeTables.Contains(this)) FreeTables.Add(this);
    }

    private void OnDisable()
    {
        AllTables.Remove(this);
        if (FreeTables.Contains(this)) FreeTables.Remove(this);
        
        // Handle case where occupied table is destroyed/disabled
        if (AssignedEmployee != null)
        {
            AssignedEmployee.UnassignTable();
            AssignedEmployee = null;
        }
    }

    public bool AssignEmployee(Employee employee)
    {
        if (IsOccupied) return false;

        AssignedEmployee = employee;
        FreeTables.Remove(this);
        return true;
    }

    public void FreeTable()
    {
        AssignedEmployee = null;
        if (!FreeTables.Contains(this)) FreeTables.Add(this);
    }
    
    public Vector3 GetInteractionPosition()
    {
        Vector3 targetPos = transform.position;

        if (InteractionPositions != null && InteractionPositions.Count > 0 && InteractionPositions[0] != null)
        {
            targetPos = InteractionPositions[0].position; 
        }

        // Project to NavMesh
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return targetPos;
    }
}
