using UnityEngine;
using System;
using System.Collections;
using SimpleJSON;

public static class Utils {
	
	public static JSONNode getJSON (string file) {
		TextAsset json = Resources.Load(file) as TextAsset;
		return SimpleJSON.JSON.Parse(json.text);
	}
}