using UnityEngine;
using UnityEngine.UI;

public class PlacementUI : MonoBehaviour
{
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton; // Optional, good UX

    private PlacementManager placementManager;

    private void Start()
    {
        placementManager = PlacementManager.Instance;

        rotateButton.onClick.AddListener(OnRotateClicked);
        confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        Hide();
    }

    private void OnRotateClicked()
    {
        placementManager.RotateCurrentObject();
    }

    private void OnConfirmClicked()
    {
        placementManager.ConfirmPlacement();
    }

    private void OnCancelClicked()
    {
        placementManager.CancelPlacement();
    }

    public void Show(Vector3 position)
    {
        gameObject.SetActive(true);
        // Convert world position to screen position if this is Screen Space Overlay
        // Or just place it in World Space above the object
        transform.position = Camera.main.WorldToScreenPoint(position); 
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
