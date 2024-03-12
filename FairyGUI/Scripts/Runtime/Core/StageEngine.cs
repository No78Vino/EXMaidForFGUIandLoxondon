using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class StageEngine : MonoBehaviour
    {
        public int ObjectsOnStage;
        public int GraphicsOnStage;

        public static bool beingQuit;

        private void Start()
        {
            useGUILayout = false;
        }

        private void LateUpdate()
        {
            Stage.inst.InternalUpdate();

            ObjectsOnStage = Stats.ObjectCount;
            GraphicsOnStage = Stats.GraphicsCount;
        }

        private void OnGUI()
        {
            Stage.inst.HandleGUIEvents(Event.current);
        }

#if !UNITY_5_4_OR_NEWER
        void OnLevelWasLoaded()
        {
            StageCamera.CheckMainCamera();
        }
#endif

        private void OnApplicationQuit()
        {
            if (Application.isEditor)
            {
                beingQuit = true;
                UIPackage.RemoveAllPackages();
            }
        }
    }
}