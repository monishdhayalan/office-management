using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private LayerMask obstacleLayer; // Layer for obstacles
    [SerializeField] private Transform originWrapper; // Optional: if grid isn't at (0,0,0)

    private bool[,] grid; // true = occupied, false = empty

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

        grid = new bool[width, height];
    }

    private void Start()
    {
        ScanGrid();
    }

    private void ScanGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 center = GridToWorldPosition(x, y);
                // Check if there is any collider in this cell
                // Half-extents: cell/2. Reducing slightly to avoid edge overlapping
                Vector3 halfExtents = new Vector3(cellSize * 0.45f, 1f, cellSize * 0.45f);
                if (Physics.CheckBox(center, halfExtents, Quaternion.identity, obstacleLayer))
                {
                    grid[x, y] = true;
                }
            }
        }
    }

    public float CellSize => cellSize;

    // Convert World Position to Grid Coordinates
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition;
        if (originWrapper != null)
        {
            localPos = originWrapper.InverseTransformPoint(worldPosition);
        }

        int x = Mathf.FloorToInt(localPos.x / cellSize);
        int z = Mathf.FloorToInt(localPos.z / cellSize);

        return new Vector2Int(x, z);
    }

    // Convert Grid Coordinates to World Position (Center of cell)
    public Vector3 GridToWorldPosition(int x, int y)
    {
        Vector3 localPos = new Vector3(x * cellSize + cellSize * 0.5f, 0, y * cellSize + cellSize * 0.5f);
        
        if (originWrapper != null)
        {
            return originWrapper.TransformPoint(localPos);
        }
        return localPos;
    }

    // Check if a specific cell is valid and empty
    public bool IsCellFree(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return false;
        }
        return !grid[x, y];
    }

    // Check if a larger area is free (for objects > 1x1)
    public bool IsAreaFree(int startX, int startY, int sizeX, int sizeY)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                if (!IsCellFree(startX + x, startY + y))
                {
                    return false;
                }
            }
        }
        return true;
    }

    // Mark an area as occupied
    public void OccupyArea(int startX, int startY, int sizeX, int sizeY)
    {
        SetArea(startX, startY, sizeX, sizeY, true);
    }

    // Free an area (e.g., if we move an object)
    public void FreeArea(int startX, int startY, int sizeX, int sizeY)
    {
        SetArea(startX, startY, sizeX, sizeY, false);
    }

    private void SetArea(int startX, int startY, int sizeX, int sizeY, bool occupied)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                int gridX = startX + x;
                int gridY = startY + y;
                if (gridX >= 0 && gridY >= 0 && gridX < width && gridY < height)
                {
                    grid[gridX, gridY] = occupied;
                }
            }
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        return;
        Gizmos.color = Color.yellow;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 center = GridToWorldPosition(x, y);
                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
}
