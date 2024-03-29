﻿using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CarPart : MonoBehaviour
{
    public enum ShaderMode
    {
        Normal = 1,
        Outline = 2,
        RedOutline = 3,
        Transparent = 4,
        Invisible = 5
    }

    public CarEngine.Part type;

    public List<CarPart> disassemblyRequirements;

    [Header("Autogenerated")] [Tooltip("Autogenerated (CarMechanic => Generate assembly requirements)")]
    // We leave it public for any possible manual requirement
    public List<CarPart> assemblyRequirements;

    public bool isTransparent;
    private bool _assembled = true;
    private Material mat;
    private MeshRenderer meshRendered;
    private Color originalColor;

    private Shader originalShader;
    public PartData partData;
    public int status => partData.status;

    public bool isAssembled
    {
        get => _assembled;
        set
        {
            _assembled = value;

            if (_assembled)
            {
                CarEngine.Instance.disassembledParts.Remove(GetInstanceID());
                SetShader(ShaderMode.Normal);
                isTransparent = false;
            }
            else
            {
                CarEngine.Instance.disassembledParts.Add(GetInstanceID(), this);
                SetShader(ShaderMode.Invisible);
            }
        }
    }

    private void Start()
    {
        meshRendered = gameObject.GetComponent<MeshRenderer>();
        mat = meshRendered.material;
        originalShader = mat.shader;
        originalColor = mat.color;

        // TMP temporal
        partData = new PartData();
    }

    private void OnMouseDown()
    {
        // wants to assembly or disassembly the part?
        if (CarEngine.Instance.currentMode == CarEngine.Mode.Disassembly && isAssembled)
        {
            if (!CanDisassembly()) return;

            isAssembled = false;
            playSound(type);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Inventory.add(type, status, 1);
        }
        else if (CarEngine.Instance.currentMode == CarEngine.Mode.Assembly && CanAssembly())
        {
            isAssembled = true;
            playSound(type);
            Inventory.del(type, status, 1);

            // update assembly mode as they may be new parts that can be assembled now
            CarEngine.Instance.SetAssemblyMode();
        }
    }

    private void OnMouseEnter()
    {
        if (CarEngine.Instance.currentMode == CarEngine.Mode.Disassembly && isAssembled)
        {
            higlightRequirements(true);
            SetShader(ShaderMode.Outline);
            Cursor.SetCursor(Cursors.handle, new Vector2(6, 0), CursorMode.Auto);
        }
        else if (isTransparent)
        {
            SetShader(ShaderMode.Outline);
        }
        else if (CarEngine.Instance.currentMode == CarEngine.Mode.Status)
        {
            EditorModePanel.Instance.partStatusPanel.show(type, status);
        }
    }

    private void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (CarEngine.Instance.currentMode == CarEngine.Mode.Disassembly && isAssembled)
        {
            higlightRequirements(false);
            SetShader(ShaderMode.Normal);
        }
        else if (isTransparent)
        {
            SetShader(ShaderMode.Transparent);
        }
        else if (CarEngine.Instance.currentMode == CarEngine.Mode.Status)
        {
            EditorModePanel.Instance.partStatusPanel.hide();
        }
    }

    private void OnMouseOver()
    {
        if (CarEngine.Instance.currentSection == CarEngine.Section.Engine && Input.GetKeyDown(KeyCode.Mouse1))
            MouseOrbit.Instance.target = transform;
    }

    public void SetShader(ShaderMode mode, Color color = default)
    {
        meshRendered.enabled = true;

        switch (mode)
        {
            case ShaderMode.Normal:
                mat.shader = originalShader;

                if (color == default)
                    mat.color = originalColor;
                else
                    mat.color = color;

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

    private void higlightRequirements(bool enable)
    {
        foreach (var part in disassemblyRequirements)
        {
            // do not highlight disassembled parts
            if (!part.isAssembled) continue;
            if (enable)
                part.SetShader(ShaderMode.RedOutline);
            else
                part.SetShader(ShaderMode.Normal);
        }
    }

    private void playSound(CarEngine.Part type)
    {
        switch (type)
        {
            case CarEngine.Part.BOLT:
                RuntimeManager.PlayOneShotAttached(AudioManager.ScrewPath, gameObject);
                break;
            case CarEngine.Part.COVER:
                RuntimeManager.PlayOneShotAttached(AudioManager.OpenCasePath, gameObject);
                break;
        }

        //if (CanAssembly()){ }
    }

    private bool CanDisassembly()
    {
        foreach (var part in disassemblyRequirements)
            if (part.isAssembled)
                return false;
        return true;
    }

    public bool CanAssembly()
    {
        foreach (var part in assemblyRequirements)
            if (!part.isAssembled)
                return false;
        return true;
    }
}