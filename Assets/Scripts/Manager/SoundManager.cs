using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public const string EFFECT_SCREW = "Tools/Screw";
    public const string EFFECT_OPEN_CASE = "CarParts/OpenCase";

    public static SoundManager Instance;

    private void Start()
    {
        Instance = this;
    }

    public void playSound(string name, GameObject obj)
    {
        AudioSource audio;

        audio = obj.GetComponent<AudioSource>();

        if (audio == null) audio = obj.AddComponent<AudioSource>();

        audio.clip = Resources.Load("Sound/" + name) as AudioClip;
        audio.Play();
    }
}