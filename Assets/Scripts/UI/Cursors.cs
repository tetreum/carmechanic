using System;
using System.Collections;
using UnityEngine;

public class Cursors
{
    public static Texture2D _handle;

    public static Texture2D handle
    {
        get
        {
            if (_handle == null) _handle = Resources.Load("Cursor/CatchHand") as Texture2D;
            return _handle;
        }
    }

    public static void setFree(bool visible = true)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = visible;
    }

    public static void setLocked()
    {
        Cursor.lockState = CursorLockMode.Locked;
        try
        {
            Menu.Instance.StartCoroutine(disableCursor());
        }
        catch (Exception)
        {
        }
    }

    public static IEnumerator disableCursor()
    {
        yield return new WaitForEndOfFrame();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}