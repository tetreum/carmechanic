using System;
using FMOD.Studio;
using UnityEngine;

namespace FMODUnity
{
    [Serializable]
    public class EmitterRef
    {
        public StudioEventEmitter Target;
        public ParamRef[] Params;
    }

    [AddComponentMenu("FMOD Studio/FMOD Studio Parameter Trigger")]
    public class StudioParameterTrigger : EventHandler
    {
        public EmitterRef[] Emitters;
        public EmitterGameEvent TriggerEvent;

        private void Awake()
        {
            for (var i = 0; i < Emitters.Length; i++)
            {
                var emitterRef = Emitters[i];
                if (emitterRef.Target != null && !string.IsNullOrEmpty(emitterRef.Target.Event))
                {
                    var eventDesc = RuntimeManager.GetEventDescription(emitterRef.Target.Event);
                    if (eventDesc.isValid())
                        for (var j = 0; j < Emitters[i].Params.Length; j++)
                        {
                            PARAMETER_DESCRIPTION param;
                            eventDesc.getParameterDescriptionByName(emitterRef.Params[j].Name, out param);
                            emitterRef.Params[j].ID = param.id;
                        }
                }
            }
        }

        protected override void HandleGameEvent(EmitterGameEvent gameEvent)
        {
            if (TriggerEvent == gameEvent) TriggerParameters();
        }

        public void TriggerParameters()
        {
            for (var i = 0; i < Emitters.Length; i++)
            {
                var emitterRef = Emitters[i];
                if (emitterRef.Target != null && emitterRef.Target.EventInstance.isValid())
                    for (var j = 0; j < Emitters[i].Params.Length; j++)
                        emitterRef.Target.EventInstance.setParameterByID(Emitters[i].Params[j].ID,
                            Emitters[i].Params[j].Value);
            }
        }
    }
}