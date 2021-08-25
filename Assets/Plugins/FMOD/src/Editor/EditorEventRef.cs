using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FMODUnity
{
    public class EditorEventRef : ScriptableObject
    {
        [SerializeField] public string Path;

        [SerializeField] private byte[] guid = new byte[16];

        [SerializeField] public List<EditorBankRef> Banks;

        [SerializeField] public bool IsStream;

        [SerializeField] public bool Is3D;

        [SerializeField] public bool IsOneShot;

        [SerializeField] public List<EditorParamRef> Parameters;

        [SerializeField] public float MinDistance;

        [SerializeField] public float MaxDistance;

        [SerializeField] public int Length;

        public Guid Guid
        {
            get => new Guid(guid);
            set => Array.Copy(value.ToByteArray(), guid, 16);
        }

        public List<EditorParamRef> LocalParameters
        {
            get { return Parameters.Where(p => p.IsGlobal == false).OrderBy(p => p.Name).ToList(); }
        }

        public List<EditorParamRef> GlobalParameters
        {
            get { return Parameters.Where(p => global::p.IsGlobal).OrderBy(p => p.Name).ToList(); }
        }
    }
}