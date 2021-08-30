using System;
using FMOD.Studio;
using UnityEngine;

namespace FMODUnity
{
    public enum ParameterType
    {
        Continuous,
        Discrete
    }

    public class EditorParamRef : ScriptableObject
    {
        [SerializeField] public string Name;

        [SerializeField] public float Min;

        [SerializeField] public float Max;

        [SerializeField] public float Default;

        [SerializeField] public ParameterID ID;

        [SerializeField] public ParameterType Type;

        [SerializeField] public bool IsGlobal;

        public bool Exists;

        [Serializable]
        public struct ParameterID
        {
            public uint data1;
            public uint data2;

            public static implicit operator ParameterID(PARAMETER_ID source)
            {
                return new ParameterID
                {
                    data1 = source.data1,
                    data2 = source.data2
                };
            }

            public static implicit operator PARAMETER_ID(ParameterID source)
            {
                return new PARAMETER_ID
                {
                    data1 = source.data1,
                    data2 = source.data2
                };
            }
        }
    }
}