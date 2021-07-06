﻿using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Collider))]
public class CarPart : MonoBehaviour {

	public CarEngine.Part type;
	public int status
	{
		get {return partData.status;}
	}

	public List<CarPart> disassemblyRequirements;
	[Header("Autogenerated")]
	[Tooltip("Autogenerated (CarMechanic => Generate assembly requirements)")]
	// We leave it public for any possible manual requirement
	public List<CarPart> assemblyRequirements;
	public bool isAssembled {
		get {
			return _assembled;
		}
		set {
			_assembled = value;

			if (_assembled) {
				CarEngine.Instance.disassembledParts.Remove(GetInstanceID());
				SetShader(ShaderMode.Normal);
				isTransparent = false;
			} else {
				CarEngine.Instance.disassembledParts.Add(GetInstanceID(), this);
				SetShader(ShaderMode.Invisible);
			}
		}
	}
	public bool isTransparent = false;
	public PartData partData;

	private Shader originalShader;
	private Color originalColor;
	private MeshRenderer meshRendered;
	private Material mat;
	private bool _assembled = true;

	public enum ShaderMode {
		Normal = 1,
		Outline = 2,
		RedOutline = 3,
		Transparent = 4,
		Invisible = 5,
	}

	void Start () {
		meshRendered = this.gameObject.GetComponent<MeshRenderer>();
		mat = meshRendered.material;
		originalShader = mat.shader;
		originalColor = mat.color;

		// TMP temporal
		partData = new PartData();
	}

	public void SetShader (ShaderMode mode, Color color = default(Color))
	{
		meshRendered.enabled = true;

		switch (mode)
		{
		case ShaderMode.Normal:
			mat.shader = originalShader;

			if (color == default(Color)) {
				mat.color = originalColor;
			} else {
				mat.color = color;
			}

			break;
		case ShaderMode.RedOutline:
			mat.shader = Shaders.outline;
			mat.color = new Color(221, 0, 0, 255);
			break;
		case ShaderMode.Outline:
			mat.shader = Shaders.outline;
			mat.color = originalColor;
			break;
		case ShaderMode.Transparent:
			mat.shader = Shaders.silhouetteOnly;
			mat.color = originalColor;
			isTransparent = true;
			break;
		case ShaderMode.Invisible:
			mat.shader = originalShader;
			mat.color = originalColor;

			meshRendered.enabled = false;
			break;
		}
	}

	void HiglightRequirements (bool enable)
	{
		foreach (CarPart part in disassemblyRequirements)
		{
			// do not highlight disassembled parts
			if (!part.isAssembled) {
				continue;
			}
			if (enable) {
				part.SetShader(ShaderMode.RedOutline);
			} else {
				part.SetShader(ShaderMode.Normal);
			}
		}
	}

	void OnMouseDown ()
	{
		// wants to assembly or disassembly the part?
		if (CarEngine.Instance.currentMode == CarEngine.Mode.Disassembly && isAssembled)
		{
			if (!CanDisassembly()) {
				return;
			}

			isAssembled = false;
			PlaySound(type);
			Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			Inventory.add(type, status, 1);
		}
		else if (CarEngine.Instance.currentMode == CarEngine.Mode.Assembly && CanAssembly())
		{
			isAssembled = true;
			PlaySound(type);
			Inventory.del(type, status, 1);

			// update assembly mode as they may be new parts that can be assembled now
			CarEngine.Instance.SetAssemblyMode();
		}
	}
	
	void PlaySound (CarEngine.Part type)
	{
		switch (type)
		{
		case CarEngine.Part.BOLT:
			SoundManager.Instance.playSound(SoundManager.EFFECT_SCREW, this.gameObject);
			break;
		//case CarEngine.Part.COVER:
		//case CarEngine.Part.OIL_CAP:
		default:
			SoundManager.Instance.playSound(SoundManager.EFFECT_OPEN_CASE, this.gameObject);
			break;
		}

		// @ToDo: if disassembly mode add an end sound of the part hitting the floor
	}
	
	void OnMouseEnter ()
	{
		if (CarEngine.Instance.currentMode == CarEngine.Mode.Disassembly && isAssembled) {
			HiglightRequirements(true);
			SetShader(ShaderMode.Outline);
			Cursor.SetCursor(Cursors.handle, new Vector2(6, 0), CursorMode.Auto);
		} else if (isTransparent) {
			SetShader(ShaderMode.Outline);
		} else if (CarEngine.Instance.currentMode == CarEngine.Mode.Status) {
			EditorModePanel.Instance.partStatusPanel.show(type, status);
		}
	}

	void OnMouseOver ()
	{
		if (CarEngine.Instance.currentSection == CarEngine.Section.Engine && Input.GetKeyDown(KeyCode.Mouse1)) {
			MouseOrbit.Instance.target = transform;
		}
	}

	void OnMouseExit ()
	{
		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

		if (CarEngine.Instance.currentMode == CarEngine.Mode.Disassembly && isAssembled) {
			HiglightRequirements(false);
			SetShader(ShaderMode.Normal);
		} else if (isTransparent) {
			SetShader(ShaderMode.Transparent);
		} else if (CarEngine.Instance.currentMode == CarEngine.Mode.Status) {
			EditorModePanel.Instance.partStatusPanel.hide();
		}
	}

	private bool CanDisassembly ()
	{
		foreach (CarPart part in disassemblyRequirements) {
			if (part.isAssembled) {
				return false;
			}
		}
		return true;
	}

	public bool CanAssembly ()
	{
		foreach (CarPart part in assemblyRequirements) {
			if (!part.isAssembled) {
				return false;
			}
		}
		return true;
	}
}
