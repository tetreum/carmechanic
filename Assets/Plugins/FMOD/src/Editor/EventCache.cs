using System;
using System.Collections.Generic;
using UnityEngine;

namespace FMODUnity
{
    internal class EventCache : ScriptableObject
    {
        public static int CurrentCacheVersion = 8;

        [SerializeField] public List<EditorBankRef> EditorBanks;

        [SerializeField] public List<EditorEventRef> EditorEvents;

        [SerializeField] public List<EditorParamRef> EditorParameters;

        [SerializeField] public List<EditorBankRef> MasterBanks;

        [SerializeField] public List<EditorBankRef> StringsBanks;

        [SerializeField] private long stringsBankWriteTime;

        [SerializeField] public int cacheVersion;

        public EventCache()
        {
            EditorBanks = new List<EditorBankRef>();
            EditorEvents = new List<EditorEventRef>();
            EditorParameters = new List<EditorParamRef>();
            MasterBanks = new List<EditorBankRef>();
            StringsBanks = new List<EditorBankRef>();
            stringsBankWriteTime = 0;
        }

        public DateTime StringsBankWriteTime
        {
            get => new DateTime(stringsBankWriteTime);
            set => stringsBankWriteTime = value.Ticks;
        }
    }
}