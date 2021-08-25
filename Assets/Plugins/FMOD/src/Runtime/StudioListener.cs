using FMOD;
using UnityEngine;

namespace FMODUnity
{
    [AddComponentMenu("FMOD Studio/FMOD Studio Listener")]
    public class StudioListener : MonoBehaviour
    {
        public GameObject attenuationObject;

        public int ListenerNumber = -1;
#if UNITY_PHYSICS_EXIST || !UNITY_2019_1_OR_NEWER
        private Rigidbody rigidBody;
#endif
#if UNITY_PHYSICS2D_EXIST || !UNITY_2019_1_OR_NEWER
        private Rigidbody2D rigidBody2D;
#endif

        private void Update()
        {
            if (ListenerNumber >= 0 && ListenerNumber < CONSTANTS.MAX_LISTENERS) SetListenerLocation();
        }

        private void OnEnable()
        {
            RuntimeUtils.EnforceLibraryOrder();
#if UNITY_PHYSICS_EXIST || !UNITY_2019_1_OR_NEWER
            rigidBody = gameObject.GetComponent<Rigidbody>();
#endif
#if UNITY_PHYSICS2D_EXIST || !UNITY_2019_1_OR_NEWER
            rigidBody2D = gameObject.GetComponent<Rigidbody2D>();
#endif
            ListenerNumber = RuntimeManager.AddListener(this);
        }

        private void OnDisable()
        {
            RuntimeManager.RemoveListener(this);
        }

        private void SetListenerLocation()
        {
#if UNITY_PHYSICS_EXIST || !UNITY_2019_1_OR_NEWER
            if (rigidBody)
                RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, rigidBody, attenuationObject);
            else
#endif
#if UNITY_PHYSICS2D_EXIST || !UNITY_2019_1_OR_NEWER
            if (rigidBody2D)
                RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, rigidBody2D, attenuationObject);
            else
#endif
                RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, attenuationObject);
        }
    }
}