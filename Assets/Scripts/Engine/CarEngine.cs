using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;

public class CarEngine : MonoBehaviour {

	public static CarEngine Instance;
	public Mode currentMode = Mode.Disassembly;
	public Section currentSection = Section.Body;
	public Transform selectedCarPart {
		get {
			return MouseOrbit.Instance.target;
		}
	}

	public enum Mode {
		Assembly = 1,
		Disassembly = 2,
		Status = 3
	}

	public enum Section {
		Engine = 1,
		Body = 2
	}

	// if you add any new part, you will also have to edit this files:
	// - Assets/Resources/translation/parts.json
	// Based on http://www.carenginediagrams.com/wp-content/uploads/2015/09/Car_Body_Parts_Diagram_03.jpg
	public enum Part {
		BOLT = 1,
		COVER = 2,
		OIL_CAP = 3,
		OIL_FILTER = 4,
		FRONT_DOOR_RIGHT = 5,
		FRONT_DOOR_LEFT = 6,
		FRONT_FENDER_RIGHT = 7,
		FRONT_FENDER_LEFT = 8,
		REAR_FENDER_RIGT = 9,
		REAR_FENDER_LEFT = 10,
		FRONT_BUMPER = 11,
		REAR_BUMPER = 12,
		ROOF = 13,
		WING = 14,
		DECKLID = 15,
		ROCKER_RIGHT = 16,
		ROCKER_LEFT = 17,
		MIRROR_RIGHT = 18,
		MIRROR_LEFT = 19,
		BONNET = 20,
		REAR_DOOR_RIGHT = 21,
		REAR_DOOR_LEFT = 22,
		FRONT_DOOR_GLASS_RIGHT = 23,
		FRONT_DOOR_GLASS_LEFT = 24,
		REAR_DOOR_GLASS_RIGHT = 25,
		REAR_DOOR_GLASS_LEFT = 26,
		CAMSHAFT_GEAR = 27,
		TIMING_CHAIN = 28,
	}
	public Dictionary<int, CarPart> disassembledParts = new Dictionary<int, CarPart>();


	public void Start () {
		Instance = this;
	}

	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1)) {
			setDisassemblyMode();
		} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
			setAssemblyMode();
		} else if (Input.GetKeyDown(KeyCode.Alpha3)) {
			setStatusMode();
		} else if (Input.GetKeyDown(KeyCode.Escape)) {
			if (currentSection == Section.Engine) {
				leaveEditorMode();
			} else {
				Cursor.lockState = CursorLockMode.None;
			}
		} else if (Input.GetKeyDown(KeyCode.I)) {
			Menu.Instance.togglePanel("InventoryPanel");
		}
	}

	public void enterEditorMode ()
	{
		// Disable character and join EngineEditor mode
		Vector3 pos = Camera.main.transform.position;
		Quaternion rot = Camera.main.transform.rotation;
		
		FirstPersonController.Instance.gameObject.SetActive(false);
		
		Menu.Instance.showPanel("EditorModePanel"); // must start before initializing EngineEditor
		EditorTriggerController.Instance.init(pos, rot);
		currentSection = Section.Engine;
		currentMode = Mode.Assembly; // join in assembly mode by default

		// Prevent trigger's colliders from blocking user clicks
		EditorTriggerController.Instance.enableTriggers(false);
		Cursor.lockState = CursorLockMode.None;
	}

	// Leaves engine editor mode and enables character controller
	public void leaveEditorMode ()
	{
		Vector3 pos = Camera.main.transform.position;
		Transform lastCarPart = selectedCarPart;
		Vector3 currentPos = FirstPersonController.Instance.gameObject.transform.position;
		
		pos.y = currentPos.y;
		
		// Disable editor mode camera
		EditorTriggerController.Instance.enableTriggers(true);
		EditorTriggerController.Instance.camera.SetActive(false);
		
		FirstPersonController.Instance.transform.position = pos;
		FirstPersonController.Instance.LookAt(lastCarPart);
		FirstPersonController.Instance.gameObject.SetActive(true);
		Menu.Instance.hidePanel("EditorModePanel");

		currentSection = Section.Body;
		#if !UNITY_EDITOR
		Cursor.lockState = CursorLockMode.Locked;
		#endif
	}
				
	public void setAssemblyMode ()
	{
		currentMode = Mode.Assembly;
		disableStatusMode();

		CarPart carPart;
		foreach(KeyValuePair<int, CarPart> entry in disassembledParts)
		{
			carPart = entry.Value;

			if (carPart.canAssembly()) {
				carPart.setShader(CarPart.ShaderMode.Transparent);
			}
		}

		try {
			EditorModePanel.Instance.displayMode(Mode.Assembly);
		} catch (System.Exception) {
		}
	}

	public void setDisassemblyMode ()
	{
		currentMode = Mode.Disassembly;
		disableStatusMode();

		CarPart carPart;
		foreach(KeyValuePair<int, CarPart> entry in disassembledParts)
		{
			carPart = entry.Value;
			
			if (carPart.canAssembly()) {
				carPart.isTransparent = false;
				carPart.setShader(CarPart.ShaderMode.Invisible);
			}
		}

		try {
			EditorModePanel.Instance.displayMode(Mode.Disassembly);
		} catch (System.Exception) {
		}
	}

	/**
	* Temporal, there should be a better way rather than making a global find..
	 */
	public void disableStatusMode ()
	{
		GameObject[] partList = GameObject.FindGameObjectsWithTag("CarPart");
		CarPart carPart;
		
		foreach (GameObject part in partList)
		{
			carPart = part.transform.GetComponent<CarPart>();
			
			if (!carPart.isAssembled) {
				continue;
			}

			carPart.setShader(CarPart.ShaderMode.Normal);
		}
	}

	public void setStatusMode ()
	{
		currentMode = Mode.Status;
		GameObject[] partList = GameObject.FindGameObjectsWithTag("CarPart");
		CarPart carPart;

		foreach (GameObject part in partList)
		{
			carPart = part.transform.GetComponent<CarPart>();

			if (!carPart.isAssembled) {
				continue;
			}

			if (carPart.status < 20) {
				carPart.setShader(CarPart.ShaderMode.Normal, Shaders.Red);
			} else if (carPart.status < 70) {
				carPart.setShader(CarPart.ShaderMode.Normal, Shaders.Orange);
			} else {
				carPart.setShader(CarPart.ShaderMode.Normal, Shaders.Green);
			}
		}

		try {
			EditorModePanel.Instance.displayMode(Mode.Status);
		} catch (System.Exception) {
		}
	}
}
