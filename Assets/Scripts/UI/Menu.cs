using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Menu : MonoBehaviour {
	
	public List<GameObject> Menus;
	
	public static Menu _instance;
	
	public static Menu Instance {
		get
		{
			return _instance;
		}
	}
	
	void Start () {
		_instance = this;
		OnLevelWasLoaded (0);
	}
	
	void Awake() {
		DontDestroyOnLoad(transform.gameObject);
		DontDestroyOnLoad(GameObject.Find("EventSystem"));
	}
	
	public void OnLevelWasLoaded(int level)
	{
		switch (level) {
		case 0: // Main
			showPanel("MainPanel");
			break;
		default: // Test
			hidePanel("MainPanel");
			showPanel("EditorModePanel");
            Cursors.setLocked();
            break;
		}
		
		// always disable DevCamera
		try {
			GameObject.Find ("DevCamera").SetActive(false);
		} catch (System.Exception) {}
	}
	
	public GameObject getPanel (string name) {
		foreach (GameObject panel in Menus) {
			if (panel.name == name) {
				return panel;
			}
		}
		throw new UnityException ("UI Panel "+ name +" not found");
	}
	
	public void togglePanel (string name) {
		GameObject panel = getPanel (name);
		
		panel.SetActive (!panel.activeSelf);
	}
	
	public GameObject showPanel (string name, bool hidePanels = true) {
		if (hidePanels) {
			hideAllPanels ();
		}
		
		GameObject panel = this.getPanel (name);
		panel.SetActive (true);
		
		return panel;
	}
	
	public void hidePanel (string name) {
		foreach (GameObject panel in Menus) {
			if (panel.name == name) {
				panel.SetActive(false);
			}
		}
	}
	
	public void hideAllPanels() {
		foreach (GameObject panel in Menus) {
			panel.SetActive(false);
		}
	}
	
	/*
	 * We place this here since SmartphonePanel gets disabled when taking a screenshot
	 */
	public void afterScreenshot (string file) {
		StartCoroutine(showUI(file));
	}

	IEnumerator showUI (string file) {
		yield return new WaitForSeconds(0.2f);
		
		Debug.Log("Screenshot saved as " + file);
		
		Menu.Instance.showPanel("SmartphonePanel", false);
		Menu.Instance.showPanel("PlayerPanel", false);
	}
}