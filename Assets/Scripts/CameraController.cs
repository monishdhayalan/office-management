using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float dragSpeed = 20f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
    [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

    [Header("Zoom")]
    [SerializeField] private float zoomSensitivity = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    
    // Smooth zoom variables
    [SerializeField] private float zoomSmoothTime = 0.2f;
    private float targetZoom;
    private float zoomVelocity;
    private Camera cam;

    private Vector3 dragOrigin;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        if (cam != null)
        {
            targetZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        }
    }

    private void LateUpdate()
    {
        // Disable movement if placing an object
        if (PlacementManager.Instance != null && PlacementManager.Instance.IsPlacing) return;

        HandleInput();
        HandleZoom();
        HandleMovement();
    }

    private void HandleZoom()
    {
        if (cam == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetZoom -= scroll * zoomSensitivity;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
        }
        else
        {
            cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetZoom, ref zoomVelocity, zoomSmoothTime);
        }
    }

    private void HandleInput()
    {
        // On Mouse Down: Capture the point on ground where interaction started
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = GetGroundIntersection();
        }

        // On Mouse Drag
        if (Input.GetMouseButton(0))
        {
            Vector3 currentGroundPoint = GetGroundIntersection();
            
            // Calculate delta: The difference between where we started dragging and where the mouse is now
            // We want to move the camera such that the point under the mouse stays under the mouse.
            // So if mouse moved RIGHT, we drag camera LEFT? 
            // Dota logic: You grab the ground and PULL it.
            // If I move mouse Left, ground moves Left. Camera moves Right.
            
            Vector3 difference = dragOrigin - currentGroundPoint;
            
            // Apply difference to target
            // IMPORTANT: We cannot modify targetPosition directly if we continue using currentGroundPoint 
            // because moving the camera changes the raycast result of 'currentGroundPoint'.
            // Standard approach: Calculate required move and apply immediately or check delta.
            
            // Let's use simple frame-based delta or recreate origin each frame?
            // "dragOrigin" needs to be stationary relative to world.
            
            // If we move the camera, 'currentGroundPoint' will change even if mouse is static? No.
            // If I hold mouse at pixel (100,100).
            // Start: Ground (0,0,0). Camera (0,10,-10).
            // Drag mouse to (200,100).
            // Ground intersection at (200,100) -> (5,0,0).
            // Delta = (0,0,0) - (5,0,0) = (-5,0,0).
            // Move camera by -5?
            // If camera moves left (-5), grid moves right? No.
            
            // Dota style: "Drag the map".
            // Move mouse LEFT -> Map moves LEFT -> Camera moves RIGHT.
            // Correct.
            
            // However, implementing smooth drag with raycasts requires simply applying the delta:
            // transform.position += difference;
            // But doing this smooths might lag.
            // Let's settle for Frame-to-Frame delta which is easier?
            // Or Drag Origin Logic.
            
            // Frame-to-Frame:
            // 1. Raycast current mouse pos -> Point A.
            // 2. Move camera.
            // 3. Next frame...
            
            // Let's stick to the robust "Drag from Origin" logic but applied to TargetPosition.
            // Actually, for smoothness, we update TargetPosition, and let SmoothDamp follow.
            // But if TargetPosition moves, the raycast result changes implicitly?
            
            // Let's try simple Vector3 delta based on sensitivity first (User might prefer it).
            // "Dota like" usually means 1:1 ground lock.
            // 1:1 Ground Lock requires instant movement.
            
            // Let's implement instant movement for drag (most RTS do this) and smooth for WASD/Edges.
            // But user asked for "click and drag".
            
            // I will implement 1:1 drag.
            // We apply movement immediately.
            
            transform.position += difference;
            targetPosition = transform.position; // Sync target for other inputs
        }
    }

    private void HandleMovement()
    {
        // Clamp bounds
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedZ = Mathf.Clamp(transform.position.z, minBounds.y, maxBounds.y);
        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
    }

    private Vector3 GetGroundIntersection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Calculate center and size
        float centerX = (minBounds.x + maxBounds.x) / 2f;
        float centerZ = (minBounds.y + maxBounds.y) / 2f;
        // Use current camera Y, or 0? 
        // Since we clamp transform.position x/z, the bounds are technically at the camera's height level.
        // But for visualization, maybe ground is better?
        // User asked for "bound of the camera movement". Moving the camera moves it at its height.
        // So drawing at camera Y is accurate to where the CAMERA transform is allowed to be.
        Vector3 center = new Vector3(centerX, transform.position.y, centerZ);
        
        float sizeX = Mathf.Abs(maxBounds.x - minBounds.x);
        float sizeZ = Mathf.Abs(maxBounds.y - minBounds.y);
        Vector3 size = new Vector3(sizeX, 2f, sizeZ); // Thick wire cube or just flat? 2f height to be visible
        
        Gizmos.DrawWireCube(center, size);
    }
}
