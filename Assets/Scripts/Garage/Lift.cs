using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class Lift : MonoBehaviour
{
    public Transform arms;
    public float speed = 0.01f;
    public float maxHeight = 3.01f;
    public float minHeight = 0.240625f;

    private bool isMoving;
    private bool isElevated;
    private Shader originalShader;
    private Material mat;
    private new AudioSource audio;

    public void Start()
    {
        audio = gameObject.GetComponent<AudioSource>();
        mat = gameObject.GetComponent<MeshRenderer>().material;
        originalShader = mat.shader;

        //TMP - Related to MainPanel.cs#21
        GameObject.Find("LANCEREVOX").transform.SetParent(arms);
    }

    public void OnMouseEnter()
    {
        Cursor.SetCursor(Cursors.handle, new Vector2(6, 0), CursorMode.Auto);
        mat.shader = Shaders.standardOutlined;
    }

    public void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        mat.shader = originalShader;
    }

    public void OnMouseDown()
    {
        if (isMoving) return;

        audio.Play();
        isMoving = true;
        StartCoroutine("startAscension");
    }

    private IEnumerator startAscension()
    {
        var expectedPos = arms.position;

        if (isElevated)
            expectedPos.y = minHeight;
        else
            expectedPos.y = maxHeight;

        while (audio.isPlaying)
        {
            arms.position = Vector3.Lerp(arms.position, expectedPos, speed);
            yield return new WaitForSeconds(0.03f);
        }

        isElevated = !isElevated;
        isMoving = false;
    }
}