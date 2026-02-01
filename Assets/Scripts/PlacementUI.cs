using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

    private void OnEnable()
    {
        // Set initial scale to 0
        if (cancelButton != null) cancelButton.transform.localScale = Vector3.zero;
        rotateButton.transform.localScale = Vector3.zero;
        confirmButton.transform.localScale = Vector3.zero;

        // Animate all at once with staggered start times
        Sequence seq = DOTween.Sequence();
        if (cancelButton != null)
        {
            seq.Insert(0f, cancelButton.transform.DOScale(1f, 0.1f).SetEase(Ease.OutBack));
        }
        seq.Insert(0f, rotateButton.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.Insert(0f, confirmButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
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
