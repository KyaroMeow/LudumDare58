using UnityEngine;

public class SorterCursor : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot = new Vector2(17f, 12f);
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    private void Awake()
    {
        ApplyCursor();
    }

    private void OnEnable()
    {
        ApplyCursor();
    }

    public void ApplyCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (cursorTexture != null)
            Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }

    public void ResetToSystemCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }
}