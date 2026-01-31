using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    [Header("Grid Basic Settings")]
    [SerializeField] private int width = 1;
    [SerializeField] private int height = 1;

    // Current dimensions (might swap if rotated)
    public int Width => width;
    public int Height => height;

    public void Rotate()
    {
        // Swap width and height logic
        int temp = width;
        width = height;
        height = temp;

        // Visual rotation
        transform.Rotate(0, 90, 0);
    }
    public virtual void OnPlaced()
    {
        // Override in subclasses
    }
}
