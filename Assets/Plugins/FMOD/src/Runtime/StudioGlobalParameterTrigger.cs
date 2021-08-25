using FMOD;
using FMOD.Studio;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FMODUnity
{
    [AddComponentMenu("FMOD Studio/FMOD Studio Global Parameter Trigger")]
    public class StudioGlobalParameterTrigger : EventHandler
    {
        [ParamRef] public string parameter;

        public EmitterGameEvent TriggerEvent;
        public float value;

        private PARAMETER_DESCRIPTION parameterDescription;
        public PARAMETER_DESCRIPTION ParameterDesctription => parameterDescription;

        private void Awake()
        {
            if (string.IsNullOrEmpty(parameterDescription.name)) Lookup();
        }

        private RESULT Lookup()
        {
            var result = RuntimeManager.StudioSystem.getParameterDescriptionByName(parameter, out parameterDescription);
            return result;
        }

        protected override void HandleGameEvent(EmitterGameEvent gameEvent)
        {
            if (TriggerEvent == gameEvent) TriggerParameters();
        }

        public void TriggerParameters()
        {
            if (!string.IsNullOrEmpty(parameter))
            {
                var result = RuntimeManager.StudioSystem.setParameterByID(parameterDescription.id, value);
                if (result != RESULT.OK)
                    Debug.LogError(string.Format(
                        "[FMOD] StudioGlobalParameterTrigger failed to set parameter {0} : result = {1}", parameter,
                        result));
            }
        }
    }
}