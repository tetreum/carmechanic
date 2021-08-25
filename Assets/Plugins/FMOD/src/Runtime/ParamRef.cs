using System;
using FMOD.Studio;

namespace FMODUnity
{
    [Serializable]
    public class ParamRef
    {
        public string Name;
        public float Value;
        public PARAMETER_ID ID;
    }
}