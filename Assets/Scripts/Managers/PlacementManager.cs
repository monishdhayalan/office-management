using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [SerializeField] private PlacementUI placementUI;
    [SerializeField] private LayerMask floorLayer;
    [Header("Visual Feedback")]
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    [SerializeField] private Material originalMaterial; // Helper to revert if needed

    private PlaceableObject currentGhost;
    private Renderer[] ghostRenderers;
    public bool IsPlacing { get; private set; } = false;
    private bool isPlacing
    {
        get => IsPlacing;
        set => IsPlacing = value;
    }
    private bool isLocked = false; // Waiting for confirmation
    private Vector2Int lockedGridPos;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    // Pending purchase info for refund on cancel
    private ShopItemSO pendingItemSO;
    private int pendingCost;
    
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
    }
    

    public void StartPlacement(GameObject prefab, ShopItemSO itemSO = null, int cost = 0)
    {
        if (isPlacing) CancelPlacement();

        pendingItemSO = itemSO;
        pendingCost = cost;

        GameObject obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<PlaceableObject>();
        if (currentGhost == null)
        {
            Debug.LogError("Prefab does not have PlaceableObject component!");
            Destroy(obj);
            RefundPendingPurchase();
            return;
        }

        ghostRenderers = obj.GetComponentsInChildren<Renderer>();
        originalMaterials.Clear();
        foreach (var r in ghostRenderers)
        {
            originalMaterials[r] = r.sharedMaterials;
        }
        
        // Disable collider/physics on ghost to avoid self-raycast
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        isPlacing = true;
        isLocked = false;
        placementUI.Hide();
    }

    private void Update()
    {
        if (!isPlacing) return;

        // Use scroll wheel to rotate
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            if (isLocked)
            {
                RotateCurrentObject();
            }
            else
            {
                AudioManager.Instance.PlaySFX(SoundType.RotateObject);
                currentGhost.Rotate();
            }
        }

        // Right click to cancel placement
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        if (isLocked) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, floorLayer))
        {
            Vector3 worldPoint = hit.point;
            Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPoint);
            Vector3 snapPos = GridManager.Instance.GridToWorldPosition(gridPos.x, gridPos.y);

            // Center the object based on its size
            // If the object width is even, the "center" of the occupied area lies on a grid line, not a cell center.
            // GridToWorldPosition returns the center of the cell (x,y).
            // If width=2, we occupy (x,y) and (x+1,y). The visual center is between them.
            // visualCenter = (Cell(x,y) + Cell(x+1,y)) / 2
            // diff = (CellSize * 0.5)
            
            float xOffset = (currentGhost.Width % 2 == 0) ? 0.5f * GridManager.Instance.CellSize : 0;
            float zOffset = (currentGhost.Height % 2 == 0) ? 0.5f * GridManager.Instance.CellSize : 0;
            
            // Adjust because GridToWorldPosition is center of ONE cell.
            // If even, we want to shift +0.5 cell to be on the border.
            // However, it depends on pivot. Assuming pivot is Center.
            
            // Let's rely on the assumption that the prefab pivot is centered.
            Vector3 finalPos = snapPos + new Vector3(xOffset, 0, zOffset);
            
            currentGhost.transform.position = finalPos;

            bool isValid = CheckValidity(gridPos);
            UpdateVisuals(isValid);

            if (Input.GetMouseButtonDown(0) && isValid)
            {
                LockPlacement(gridPos);
            }
        }
    }

    private bool CheckValidity(Vector2Int gridPos)
    {
        return GridManager.Instance.IsAreaFree(gridPos.x, gridPos.y, currentGhost.Width, currentGhost.Height);
    }

    private void UpdateVisuals(bool isValid)
    {
        Material targetMat = isValid ? validMaterial : invalidMaterial;
        foreach (var r in ghostRenderers)
        {
            // Apply overlay or swap material. For ghost, usually swap everything to one semi-transparent mat.
            Material[] newMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < newMats.Length; i++) newMats[i] = targetMat;
            r.materials = newMats;
        }
    }

    private void LockPlacement(Vector2Int gridPos)
    {
        isLocked = true;
        lockedGridPos = gridPos;
        placementUI.Show(currentGhost.transform.position);
    }

    public void RotateCurrentObject()
    {
        if (currentGhost == null) return;
        
        currentGhost.Rotate();
        AudioManager.Instance.PlaySFX(SoundType.RotateObject);
        
        // Recalculate world position based on the LOCKED grid anchor
        Vector3 snapPos = GridManager.Instance.GridToWorldPosition(lockedGridPos.x, lockedGridPos.y);
        
        float xOffset = (currentGhost.Width % 2 == 0) ? GridManager.Instance.CellSize * 0.5f : 0;
        float zOffset = (currentGhost.Height % 2 == 0) ? GridManager.Instance.CellSize * 0.5f : 0;
        
        currentGhost.transform.position = snapPos + new Vector3(xOffset, 0, zOffset);
        
        bool isValid = CheckValidity(lockedGridPos);
        UpdateVisuals(isValid);
    }

    public void ConfirmPlacement()
    {
        if (currentGhost == null) return;

        // Use the explicitly locked grid position
        if (CheckValidity(lockedGridPos))
        {
            GridManager.Instance.OccupyArea(lockedGridPos.x, lockedGridPos.y, currentGhost.Width, currentGhost.Height);
            
            Collider[] colliders = currentGhost.GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = true;
            
            // Restore materials
            foreach (var r in ghostRenderers)
            {
                if (originalMaterials.ContainsKey(r))
                {
                    if (originalMaterials.ContainsKey(r))
                    {
                        r.materials = originalMaterials[r];
                    }
                }
            }

            // Animate Scale
            currentGhost.transform.localScale = Vector3.zero;

            currentGhost.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(.5f);

            currentGhost.OnPlaced();

            // Spawn Effect
            if (GameManager.Instance != null && GameManager.Instance.TableSpawnEffectPrefab != null)
            {
                GameObject fx = Instantiate(GameManager.Instance.TableSpawnEffectPrefab, currentGhost.transform.position + Vector3.up * 2, Quaternion.Euler(-90, 0, 0));
                fx.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.OutBack).SetDelay(.9f);
                Destroy(fx, 2f);
            }

            // Confirm purchase (increment count) only on success
            ConfirmPendingPurchase();

            currentGhost = null;
            isPlacing = false;
            isLocked = false;
            placementUI.Hide();
        }
        else
        {
            Debug.Log("Cannot place here!");
        }
    }

    public void CancelPlacement()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
        }
        
        // Refund money on cancel
        RefundPendingPurchase();
        
        isPlacing = false;
        isLocked = false;
        placementUI.Hide();
    }

    private void ConfirmPendingPurchase()
    {
        if (pendingItemSO != null)
        {
            GameManager.Instance.IncrementItemPurchaseCount(pendingItemSO);
            pendingItemSO = null;
            pendingCost = 0;
            GameManager.Instance.TriggerPurchaseConfirmed();
        }
    }

    private void RefundPendingPurchase()
    {
        if (pendingItemSO != null && pendingCost > 0)
        {
            GameManager.Instance.RefundMoney(pendingCost);
            pendingItemSO = null;
            pendingCost = 0;
        }
    }
}
