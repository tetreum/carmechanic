using System.Collections;
using FMOD.Studio;
using UnityEngine;

[RequireComponent (typeof (Collider))]
[RequireComponent (typeof (AudioSource))]
public class Lift : MonoBehaviour
{
	public Transform arms;
	public float speed = 0.01f;
	public float maxHeight = 3.01f;
	public float minHeight = 0.240625f;

	private EventInstance instance;
	private bool isElevated = false;

	private bool isMoving = false;
	private Material mat;
	private Shader originalShader;
	private bool isPlaying;

	public void Start ()
	{
		mat = this.gameObject.GetComponent<MeshRenderer>().material;
		originalShader = mat.shader;
		
	}

	public void OnMouseDown ()
	{
		if (isMoving)
		{
			instance.start();
			return;
		}
		
		isMoving = true;
		StartCoroutine("startAscension");
	}

	public void OnMouseEnter () {
		Cursor.SetCursor(Cursors.handle, new Vector2(6, 0), CursorMode.Auto);
		mat.shader = Shaders.standardOutlined;
	}

	public void OnMouseExit () {
		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		mat.shader = originalShader;
	}
	
	bool IsPlaying(EventInstance instance) {
		PLAYBACK_STATE state;   
		instance.getPlaybackState(out state);
		return state != PLAYBACK_STATE.STOPPED;
	}
	IEnumerator startAscension()
	{
		Vector3 expectedPos = arms.position;

		if (isElevated) {
			expectedPos.y  = minHeight;
		} else {
			expectedPos.y  = maxHeight;
		}

		while (IsPlaying(instance))
		{
			arms.position = Vector3.Lerp(arms.position, expectedPos, speed);
			yield return new WaitForSeconds(0.03f);
		}
		isElevated = !isElevated;
		isMoving = false;
	}
}
