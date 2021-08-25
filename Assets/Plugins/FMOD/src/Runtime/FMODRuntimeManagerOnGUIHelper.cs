using UnityEngine;

namespace FMODUnity
{
    public class FMODRuntimeManagerOnGUIHelper : MonoBehaviour
    {
        public RuntimeManager TargetRuntimeManager;

        private void OnGUI()
        {
            if (TargetRuntimeManager) TargetRuntimeManager.ExecuteOnGUI();
        }
    }
}