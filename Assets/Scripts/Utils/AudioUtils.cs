using FMOD.Studio;
using UnityEngine;

public class AudioUtils : MonoBehaviour
{
    public static bool IsPlaying(EventInstance instance)
    {
        PLAYBACK_STATE state;
        instance.getPlaybackState(out state);
        return state != PLAYBACK_STATE.STOPPED;
    }
}