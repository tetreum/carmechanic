using UnityEngine;

public class EditorTrigger : MonoBehaviour
{
    public Type type;

    public enum Type
    {
        TopLeftWheel = 1,
        TopRightWheel = 2,
        RearLeftWheel = 3,
        RearRightWheel = 4,
        Hood = 5,
        Underbody = 6
    }

    public void OnMouseDown()
    {
        CarEngine.Instance.enterEditorMode();
    }
}