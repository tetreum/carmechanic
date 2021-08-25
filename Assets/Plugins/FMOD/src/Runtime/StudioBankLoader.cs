using System.Collections.Generic;
using UnityEngine;

namespace FMODUnity
{
    [AddComponentMenu("FMOD Studio/FMOD Studio Bank Loader")]
    public class StudioBankLoader : MonoBehaviour
    {
        public LoaderGameEvent LoadEvent;
        public LoaderGameEvent UnloadEvent;

        [BankRef] public List<string> Banks;

        public string CollisionTag;
        public bool PreloadSamples;
        private bool isQuitting;

        private void Start()
        {
            RuntimeUtils.EnforceLibraryOrder();
            HandleGameEvent(LoaderGameEvent.ObjectStart);
        }

        private void OnEnable()
        {
            HandleGameEvent(LoaderGameEvent.ObjectEnable);
        }

        private void OnDisable()
        {
            HandleGameEvent(LoaderGameEvent.ObjectDisable);
        }

        private void OnDestroy()
        {
            if (!isQuitting) HandleGameEvent(LoaderGameEvent.ObjectDestroy);
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void HandleGameEvent(LoaderGameEvent gameEvent)
        {
            if (LoadEvent == gameEvent) Load();
            if (UnloadEvent == gameEvent) Unload();
        }

        public void Load()
        {
            foreach (var bankRef in Banks)
                try
                {
                    RuntimeManager.LoadBank(bankRef, PreloadSamples);
                }
                catch (BankLoadException e)
                {
                    Debug.LogException(e);
                }

            RuntimeManager.WaitForAllLoads();
        }

        public void Unload()
        {
            foreach (var bankRef in Banks) RuntimeManager.UnloadBank(bankRef);
        }

#if UNITY_PHYSICS_EXIST || !UNITY_2019_1_OR_NEWER
        private void OnTriggerEnter(Collider other)
        {
            if (string.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                HandleGameEvent(LoaderGameEvent.TriggerEnter);
        }

        private void OnTriggerExit(Collider other)
        {
            if (string.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                HandleGameEvent(LoaderGameEvent.TriggerExit);
        }
#endif

#if UNITY_PHYSICS2D_EXIST || !UNITY_2019_1_OR_NEWER
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (string.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                HandleGameEvent(LoaderGameEvent.TriggerEnter2D);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (string.IsNullOrEmpty(CollisionTag) || other.CompareTag(CollisionTag))
                HandleGameEvent(LoaderGameEvent.TriggerExit2D);
        }
#endif
    }
}