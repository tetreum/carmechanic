using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EditorModePanel : MonoBehaviour {

	public static EditorModePanel Instance;
	public PartStatusPanel partStatusPanel;
	public Button assemblyButton;
	public Button disassemblyButton;
	public Button statusButton;
	public Text valueLabel;

	// Use this for initialization
	void Start () {
		Instance = this;
		CarEngine.Instance.setDisassemblyMode();
	}

	public void setAssemblyMode () {
		CarEngine.Instance.setAssemblyMode();
	}
	public void setDisassemblyMode () {
		CarEngine.Instance.setDisassemblyMode();
	}
	public void setStatusMode () {
		CarEngine.Instance.setStatusMode();
	}

	public void displayMode (CarEngine.Mode mode)
	{
		string label = "Mode: ";

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
