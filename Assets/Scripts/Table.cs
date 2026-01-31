using UnityEngine;
using System.Collections.Generic;

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
        if (InteractionPositions != null && InteractionPositions.Count > 0)
        {
            foreach (Transform t in InteractionPositions)
            {
                if (t != null)
                {
                    // Check if the position is clear of obstacles
                    // Using a small sphere check. 
                    // Note: We assume the interaction point is at ground level or slightly above.
                    // We want to verify no walls/furniture block this specific point.
                    // Radius 0.3f matches roughly half a standard unit width.
                    // We filter normally for Default/Obstacle layers. Since we can't access GridManager's layer easily,
                    // we'll rely on global physics or assume obstacles are on Default.
                    // To be safe, we exclude the Floor layer if possible, but usually Floor is on "Default"?
                    // Let's assume hitting "Anything" is an obstacle EXCEPT the floor?
                    // Better: User prompt "check for obstacles".
                    // Let's Raycast from Above.
                    
                    // Simple check: Is there a collider here?
                    // Excluding the Table itself might be tricky if it has a large collider.
                    // But Interaction points are usually outside.
                    
                    // Let's try IsAreaFree from GridManager?
                    Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(t.position);
                    if (GridManager.Instance.IsCellFree(gridPos.x, gridPos.y))
                    {
                        return t.position;
                    }
                }
            }
            // If all blocked, fallback to first? or Transform.
            return InteractionPositions[0].position; 
        }

        return transform.position;
    }
}
