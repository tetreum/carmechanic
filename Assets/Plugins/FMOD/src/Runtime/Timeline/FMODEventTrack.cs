#if (UNITY_TIMELINE_EXIST || !UNITY_2019_1_OR_NEWER)

using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
#endif

namespace FMODUnity
{
    [TrackColor(0.066f, 0.134f, 0.244f)]
    [TrackClipType(typeof(FMODEventPlayable))]
    [TrackBindingType(typeof(GameObject))]
    [DisplayName("FMOD/Event Track")]
    public class FMODEventTrack : TrackAsset
    {
        public FMODEventMixerBehaviour template = new FMODEventMixerBehaviour();

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var director = go.GetComponent<PlayableDirector>();
            var trackTargetObject = director.GetGenericBinding(this) as GameObject;

            foreach (var clip in GetClips())
            {
                var playableAsset = clip.asset as FMODEventPlayable;

                if (playableAsset)
                {
                    playableAsset.TrackTargetObject = trackTargetObject;
                    playableAsset.OwningClip = clip;
                }
            }

            var scriptPlayable = ScriptPlayable<FMODEventMixerBehaviour>.Create(graph, template, inputCount);
            return scriptPlayable;
        }
    }

    [Serializable]
    public class FMODEventMixerBehaviour : PlayableBehaviour
    {
        [Range(0, 1)] public float volume = 1;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
#if UNITY_EDITOR
            /*
             * Process frame is called from OnGUI() when auditioning.
             * Check playing to avoid retriggering sounds while scrubbing or repainting.
             * Check IsQuitting to avoid accessing the RuntimeManager during the Play-In-Editor to Editor transition.
             */
            var playing = playable.GetGraph().IsPlaying();
            if (!playing) return;
            /* When auditioning manually update the StudioSystem in place of the RuntimeManager. */
            if (!Application.isPlaying) RuntimeManager.StudioSystem.update();
#endif //UNITY_EDITOR

            var inputCount = playable.GetInputCount();
            var time = (float) playable.GetGraph().GetRootPlayable(0).GetTime();

            for (var i = 0; i < inputCount; i++)
            {
                var inputPlayable = (ScriptPlayable<FMODEventPlayableBehavior>) playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();

                input.UpdateBehavior(time, volume);
            }
        }
    }
}
#endif