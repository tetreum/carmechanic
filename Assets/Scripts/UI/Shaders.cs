using System.Globalization;
using UnityEngine;

public class Shaders : MonoBehaviour
{
    public static Shader _transparent;

    public static Shader _outline;

    public static Shader _silhouetteOnly;

    public static Shader _standardOutlined;

    public static Color Red = new Color(1f, 0f, 0f);
    public static Color Orange = new Color(1f, 0.557f, 0f);
    public static Color Green = new Color(0.2f, 1f, 0f);

    public static Shader transparent
    {
        get
        {
            if (_transparent == null) _transparent = Shader.Find("Transparent/Diffuse");
            return _transparent;
        }
    }

    public static Shader outline
    {
        get
        {
            if (_outline == null) _outline = Shader.Find("Outlined/Silhouetted Diffuse");
            return _outline;
        }
    }

    public static Shader silhouetteOnly
    {
        get
        {
            if (_silhouetteOnly == null) _silhouetteOnly = Shader.Find("Outlined/Silhouette Only");
            return _silhouetteOnly;
        }
    }

    public static Shader standardOutlined
    {
        get
        {
            if (_standardOutlined == null) _standardOutlined = Shader.Find("Standard Outlined");
            return _standardOutlined;
        }
    }

    // from http://answers.unity3d.com/questions/812240/convert-hex-int-to-colorcolor32.html
    public static Color hexToColor(string hex)
    {
        hex = hex.Replace("0x", ""); //in case the string is formatted 0xFFFFFF
        hex = hex.Replace("#", ""); //in case the string is formatted #FFFFFF
        byte a = 255; //assume fully visible unless specified in hex
        var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

        //Only use alpha if the string has enough characters
        if (hex.Length == 8) a = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        return new Color32(r, g, b, a);
    }
}