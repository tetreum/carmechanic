using UnityEngine;
using System.Collections;

public class DoorHandle : MonoBehaviour {

	public Texture2D cursorTexture;
	public CursorMode cursorMode = CursorMode.Auto;
	public Vector2 hotSpot = Vector2.zero;
	public string Axis = "X";
	private bool isDragging = false;


	void OnMouseEnter() {
		Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
	}
	void OnMouseExit() {
		Cursor.SetCursor(null, Vector2.zero, cursorMode);
	}

	void Update () {
		if (!isDragging) {
			return;
		}

		Debug.Log(Input.mousePosition.x);
	}

	void OnMouseDown () {
		isDragging = true;

		switch (Axis) {
		case "X":
				break;
		}
	}

	void OnMouseUp () {
		isDragging = false;
	}
}
