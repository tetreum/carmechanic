#if (UNITY_TIMELINE_EXIST || !UNITY_2019_1_OR_NEWER)

using System;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FMODUnity
{
    [Serializable]
    public class FMODEventPlayable : PlayableAsset, ITimelineClipAsset
    {
        public FMODEventPlayableBehavior template = new FMODEventPlayableBehavior();
        public float eventLength; //In seconds.

        [EventRef] [SerializeField] public string eventName;

        [SerializeField] public STOP_MODE stopType;

        [SerializeField] public ParamRef[] parameters = new ParamRef[0];

        private FMODEventPlayableBehavior behavior;

        [NonSerialized] public bool cachedParameters;

        public GameObject TrackTargetObject { get; set; }

        public override double duration
        {
            get
            {
                if (eventName == null)
                    return base.duration;
                return eventLength;
            }
        }

        public TimelineClip OwningClip { get; set; }

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(eventName))
#else
            if (!cachedParameters && !string.IsNullOrEmpty(eventName))
#endif
            {
                EventDescription eventDescription;
                RuntimeManager.StudioSystem.getEvent(eventName, out eventDescription);

                for (var i = 0; i < parameters.Length; i++)
                {
                    PARAMETER_DESCRIPTION parameterDescription;
                    eventDescription.getParameterDescriptionByName(parameters[i].Name, out parameterDescription);
                    parameters[i].ID = parameterDescription.id;
                }

                var parameterLinks = template.parameterLinks;

                for (var i = 0; i < parameterLinks.Count; i++)
                {
                    PARAMETER_DESCRIPTION parameterDescription;
                    eventDescription.getParameterDescriptionByName(parameterLinks[i].Name, out parameterDescription);
                    parameterLinks[i].ID = parameterDescription.id;
                }

                cachedParameters = true;
            }

            var playable = ScriptPlayable<FMODEventPlayableBehavior>.Create(graph, template);
            behavior = playable.GetBehaviour();

            behavior.TrackTargetObject = TrackTargetObject;
            behavior.eventName = eventName;
            behavior.stopType = stopType;
            behavior.parameters = parameters;
            behavior.OwningClip = OwningClip;

            return playable;
        }

#if UNITY_EDITOR
        public void UpdateEventDuration(float duration)
        {
            eventLength = duration / 1000f;
        }

        public void OnValidate()
        {
            if (OwningClip != null && !string.IsNullOrEmpty(eventName))
            {
                var index = eventName.LastIndexOf("/");
                OwningClip.displayName = eventName.Substring(index + 1);
            }

            if (behavior != null && !string.IsNullOrEmpty(behavior.eventName)) behavior.eventName = eventName;
        }
#endif //UNITY_EDITOR
    }

    public enum STOP_MODE
    {
        AllowFadeout,
        Immediate,
        None
    }

    [Serializable]
    public class ParameterAutomationLink
    {
        public string Name;
        public int Slot;
        public PARAMETER_ID ID;
    }

    [Serializable]
    public class FMODEventPlayableBehavior : PlayableBehaviour
    {
        public string eventName;
        public STOP_MODE stopType = STOP_MODE.AllowFadeout;

        [NotKeyable] public ParamRef[] parameters = new ParamRef[0];

        public List<ParameterAutomationLink> parameterLinks = new List<ParameterAutomationLink>();

        public AutomatableSlots parameterAutomation;
        private float currentVolume = 1;

        private EventInstance eventInstance;

        private bool isPlayheadInside;

        [NonSerialized] public TimelineClip OwningClip;

        [NonSerialized] public GameObject TrackTargetObject;

        protected void PlayEvent()
        {
            if (!string.IsNullOrEmpty(eventName))
            {
                eventInstance = RuntimeManager.CreateInstance(eventName);
                // Only attach to object if the game is actually playing, not auditioning.
                if (Application.isPlaying && TrackTargetObject)
                {
#if UNITY_PHYSICS_EXIST || !UNITY_2019_1_OR_NEWER
                    if (TrackTargetObject.GetComponent<Rigidbody>())
                        RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject.transform,
                            TrackTargetObject.GetComponent<Rigidbody>());
                    else
#endif
#if UNITY_PHYSICS2D_EXIST || !UNITY_2019_1_OR_NEWER
                    if (TrackTargetObject.GetComponent<Rigidbody2D>())
                        RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject.transform,
                            TrackTargetObject.GetComponent<Rigidbody2D>());
                    else
#endif
                        RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject.transform);
                }
                else
                {
                    eventInstance.set3DAttributes(Vector3.zero.To3DAttributes());
                }

                foreach (var param in parameters) eventInstance.setParameterByID(param.ID, param.Value);

                eventInstance.setVolume(currentVolume);
                eventInstance.start();
            }
        }

        public void OnEnter()
        {
            if (!isPlayheadInside)
            {
                PlayEvent();
                isPlayheadInside = true;
            }
        }

        public void OnExit()
        {
            if (isPlayheadInside)
            {
                if (eventInstance.isValid())
                {
                    if (stopType != STOP_MODE.None)
                        eventInstance.stop(stopType == STOP_MODE.Immediate
                            ? FMOD.Studio.STOP_MODE.IMMEDIATE
                            : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    eventInstance.release();
                }

                isPlayheadInside = false;
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (eventInstance.isValid())
                foreach (var link in parameterLinks)
                {
                    var value = parameterAutomation.GetValue(link.Slot);
                    eventInstance.setParameterByID(link.ID, value);
                }
        }

        public void UpdateBehavior(float time, float volume)
        {
            if (volume != currentVolume)
            {
                currentVolume = volume;

                if (eventInstance.isValid()) eventInstance.setVolume(volume);
            }

            if (time >= OwningClip.start && time < OwningClip.end)
                OnEnter();
            else
                OnExit();
        }

        public override void OnGraphStop(Playable playable)
        {
            isPlayheadInside = false;
            if (eventInstance.isValid())
            {
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
                RuntimeManager.StudioSystem.update();
            }
        }
    }
}
#endif