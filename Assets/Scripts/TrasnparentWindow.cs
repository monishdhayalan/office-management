using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class TrasnparentWindow : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    private const int GWL_EXSTYLE = -20;
    private const uint WS_EX_LAYERED = 0x00080000;
    private const uint WS_EX_TRANSPARENT = 0x00000020;
    private const uint WS_EX_TOPMOST = 0x00000008;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private IntPtr _hWnd;

    [Tooltip("If true, clicks pass through empty space to the desktop.")]
    public bool enableClickThrough = true;

    [Tooltip("If true, the window will stay on top of other windows.")]
    public bool alwaysOnTop = true;

    private bool _isClickable = true;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    private void Start()
    {
#if !UNITY_EDITOR
        // Transparent Window logic requires the app to run in background to detect mouse moves
        // even when not focused (which happens when clicking through).
        Application.runInBackground = true;

        // 1. Get Window Handle
        _hWnd = GetActiveWindow();

        // 2. Make Background Transparent
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(_hWnd, ref margins);

        // 3. Set Always On Top
        if (alwaysOnTop)
        {
            SetWindowPos(_hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR
        if (!enableClickThrough)
            return;

        // Check if we are hovering over something in the game world (UI or 3D Object)
        bool isHoveringContent = CheckForContent();

        if (isHoveringContent && !_isClickable)
        {
            SetClickable(true);
        }
        else if (!isHoveringContent && _isClickable)
        {
            SetClickable(false);
        }
#endif
    }

    private bool CheckForContent()
    {
        // We must use Windows API to get the mouse position because Input.mousePosition
        // might not update if the window is in "Transparent" (click-through) mode.
        POINT p;
        if (GetCursorPos(out p))
        {
            ScreenToClient(_hWnd, ref p);
            // In Unity, (0,0) is bottom-left. In Windows Client, (0,0) is top-left.
            // We need to invert Y.
            Vector2 mousePos = new Vector2(p.X, Screen.height - p.Y);

            // 1. Check UI (Manual Raycast)
            if (IsPointerOverUI(mousePos))
            {
                return true;
            }

            // 2. Check 3D Physics
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return true;
            }

            // 3. Check 2D Physics (if applicable)
            if (
                Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePos), Vector2.zero).collider
                != null
            )
            {
                return true;
            }
        }
        return false;
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        UnityEngine.EventSystems.PointerEventData eventDataCurrentPosition =
            new UnityEngine.EventSystems.PointerEventData(
                UnityEngine.EventSystems.EventSystem.current
            );
        eventDataCurrentPosition.position = screenPos;
        System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> results =
            new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(
        IntPtr hwnd,
        uint crKey,
        byte bAlpha,
        uint dwFlags
    );

    private void SetClickable(bool clickable)
    {
        _isClickable = clickable;
        uint currentExStyle = GetWindowLong(_hWnd, GWL_EXSTYLE);

        if (clickable)
        {
            // Remove Transparent flag (make clickable)
            SetWindowLong(_hWnd, GWL_EXSTYLE, currentExStyle & ~WS_EX_TRANSPARENT);
        }
        else
        {
            // Add Transparent flag (click-through)
            SetWindowLong(_hWnd, GWL_EXSTYLE, currentExStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
    }
}
