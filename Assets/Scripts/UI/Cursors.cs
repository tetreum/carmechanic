using UnityEngine;
using System.Collections;

public class Cursors
{
	public static Texture2D _handle;
	public static Texture2D handle {
		get {
			if (_handle == null) {
				_handle = Resources.Load("Cursor/CatchHand") as Texture2D;
			}
			return _handle;
		}
	}
}
