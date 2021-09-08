using System;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class CarEngine : MonoBehaviour
{
    public enum Mode
    {
        Assembly = 1,
        Disassembly = 2,
        Status = 3
    }

    // if you add any new part, you will also have to edit this files:
    // - Assets/Resources/translation/parts.json
    // Based on http://www.carenginediagrams.com/wp-content/uploads/2015/09/Car_Body_Parts_Diagram_03.jpg
    public enum Part
    {
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
        TIMING_CHAIN = 28
    }

    public enum Section
    {
        Engine = 1,
        Body = 2
    }

    public static CarEngine Instance;
    public Mode currentMode = Mode.Disassembly;
    public Section currentSection = Section.Body;

    public Dictionary<int, CarPart> disassembledParts = new Dictionary<int, CarPart>();

    public Transform selectedCarPart => MouseOrbit.Instance.target;

    public void Start()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetDisassemblyMode();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetAssemblyMode();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetStatusMode();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentSection == Section.Engine)
                LeaveEditorMode();
            else
                Cursor.lockState = CursorLockMode.None;
        }
        else if (Input.GetKeyDown(KeyCode.I))
        {
            Menu.Instance.togglePanel("InventoryPanel");
        }
    }

    public void EnterEditorMode()
    {
        // Disable character and join EngineEditor mode
        var pos = Camera.main.transform.position;
        var rot = Camera.main.transform.rotation;

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
    private void LeaveEditorMode()
    {
        var pos = Camera.main.transform.position;
        var lastCarPart = selectedCarPart;
        var currentPos = FirstPersonController.Instance.gameObject.transform.position;

        pos.y = currentPos.y;

        // Disable editor mode camera
        EditorTriggerController.Instance.enableTriggers(true);
        EditorTriggerController.Instance.camera.SetActive(false);
        FirstPersonController.Instance.transform.position = pos;
        FirstPersonController.Instance.LookAt(lastCarPart);
        FirstPersonController.Instance.gameObject.SetActive(true);
        Menu.Instance.hidePanel("EditorModePanel");

        currentSection = Section.Body;
        if (Application.platform == RuntimePlatform.WindowsPlayer)
            Cursor.lockState = CursorLockMode.Locked;
    }

    public void SetAssemblyMode()
    {
        currentMode = Mode.Assembly;
        DisableStatusMode();

        CarPart carPart;
        foreach (var entry in disassembledParts)
        {
            carPart = entry.Value;

            if (carPart.CanAssembly()) carPart.SetShader(CarPart.ShaderMode.Transparent);
        }

        try
        {
            EditorModePanel.Instance.displayMode(Mode.Assembly);
        }
        catch (Exception)
        {
        }
    }

    public void SetDisassemblyMode()
    {
        currentMode = Mode.Disassembly;
        DisableStatusMode();

        CarPart carPart;
        foreach (var entry in disassembledParts)
        {
            carPart = entry.Value;

            if (carPart.CanAssembly())
            {
                carPart.isTransparent = false;
                carPart.SetShader(CarPart.ShaderMode.Invisible);
            }
        }

        try
        {
            EditorModePanel.Instance.displayMode(Mode.Disassembly);
        }
        catch (Exception)
        {
        }
    }

    /**
	* Temporal, there should be a better way rather than making a global find..
	 */
    private static void DisableStatusMode()
    {
        var partList = GameObject.FindGameObjectsWithTag("CarPart");
        CarPart carPart;

        foreach (var part in partList)
        {
            carPart = part.transform.GetComponent<CarPart>();

            if (!carPart.isAssembled) continue;

            carPart.SetShader(CarPart.ShaderMode.Normal);
        }
    }

    public void SetStatusMode()
    {
        currentMode = Mode.Status;
        var partList = GameObject.FindGameObjectsWithTag("CarPart");
        CarPart carPart;

        foreach (var part in partList)
        {
            carPart = part.transform.GetComponent<CarPart>();

            if (!carPart.isAssembled) continue;

            if (carPart.status < 20)
                carPart.SetShader(CarPart.ShaderMode.Normal, Shaders.Red);
            else if (carPart.status < 70)
                carPart.SetShader(CarPart.ShaderMode.Normal, Shaders.Orange);
            else
                carPart.SetShader(CarPart.ShaderMode.Normal, Shaders.Green);
        }

        try
        {
            EditorModePanel.Instance.displayMode(Mode.Status);
        }
        catch (Exception)
        {
        }
    }
}