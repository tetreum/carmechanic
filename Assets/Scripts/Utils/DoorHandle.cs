using UnityEngine;

public class DoorHandle : MonoBehaviour
{
    public Texture2D cursorTexture;
    public CursorMode cursorMode = CursorMode.Auto;
    public Vector2 hotSpot = Vector2.zero;
    public string Axis = "X";
    private bool isDragging;

    private void Update()
    {
        if (!isDragging) return;

        Debug.Log(Input.mousePosition.x);
    }

    private void OnMouseDown()
    {
        isDragging = true;

        switch (Axis)
        {
            case "X":
                break;
        }
    }


    private void OnMouseEnter()
    {
        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    }

    private void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }
}