using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class Lift : MonoBehaviour
{
    [SerializeField] public Transform arms;
    public float speed = 0.01f;
    public float maxHeight = 1.22f;
    public float minHeight = 0;
    private new AudioSource audio;
    private bool isElevated;
    private bool isMoving;
    private Material mat;

    public void Start()
    {
        audio = gameObject.GetComponent<AudioSource>();
    }

    private void OnMouseDown()
    {
        if (isMoving) return;
        audio.Play();
        isMoving = true;
        StartCoroutine(nameof(StartAscension));
    }

    private IEnumerator StartAscension()
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