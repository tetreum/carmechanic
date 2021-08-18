using SimpleJSON;
using UnityEngine;

public static class Utils
{
    public static JSONNode getJSON(string file)
    {
        var json = Resources.Load(file) as TextAsset;
        return JSON.Parse(json.text);
    }
}