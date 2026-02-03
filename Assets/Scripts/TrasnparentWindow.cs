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

    private bool _isClickable = true;

    private void Start()
    {
#if !UNITY_EDITOR
        // 1. Get Window Handle
        _hWnd = GetActiveWindow();

        // 2. Make Background Transparent
        MARGINS margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(_hWnd, ref margins);

        // 3. Set Always On Top
        //SetWindowPos(_hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
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

    /// <summary>
    /// Checks if the mouse is currently over any game content (Collider 2D/3D or UI).
    /// </summary>
    private bool CheckForContent()
    {
        // 1. Check UI
        if (
            UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()
        )
        {
            return true;
        }

        // 2. Check 3D Physics
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return true;
        }

        // 3. Check 2D Physics (if applicable)
        if (
            Physics2D
                .Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero)
                .collider != null
        )
        {
            return true;
        }

        return false;
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
