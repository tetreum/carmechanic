using UnityEngine;
using UnityEngine.UI;

public class EditorModePanel : MonoBehaviour
{
    public static EditorModePanel Instance;
    public PartStatusPanel partStatusPanel;
    public Button assemblyButton;
    public Button disassemblyButton;
    public Button statusButton;
    public Text valueLabel;

    // Use this for initialization
    private void Start()
    {
        Instance = this;
        CarEngine.Instance.SetDisassemblyMode();
    }

    public void setAssemblyMode()
    {
        CarEngine.Instance.SetAssemblyMode();
    }

    public void setDisassemblyMode()
    {
        CarEngine.Instance.SetDisassemblyMode();
    }

    public void setStatusMode()
    {
        CarEngine.Instance.SetStatusMode();
    }

    public void displayMode(CarEngine.Mode mode)
    {
        var label = "Mode: ";

        switch (mode)
        {
            case CarEngine.Mode.Assembly:
                assemblyButton.Select();
                label += "Assembly";
                break;
            case CarEngine.Mode.Disassembly:
                disassemblyButton.Select();
                label += "Disassembly";
                break;
            case CarEngine.Mode.Status:
                statusButton.Select();
                label += "Status";
                break;
        }

        valueLabel.text = label;
    }
}