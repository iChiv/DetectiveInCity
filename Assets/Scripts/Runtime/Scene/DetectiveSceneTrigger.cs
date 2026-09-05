using UnityEngine;

namespace Detective
{
    public sealed class DetectiveSceneTrigger : MonoBehaviour
    {
        public enum TriggerAction
        {
            LoadIndoor,
            UnloadIndoor
        }

        public enum TriggerEvent
        {
            Enter,
            Exit
        }

        [SerializeField] private DetectiveAdditiveSceneLoader loader;
        [SerializeField] private TriggerAction action = TriggerAction.LoadIndoor;
        [SerializeField] private TriggerEvent triggerEvent = TriggerEvent.Enter;

        private void Awake()
        {
            if (loader == null)
            {
                loader = FindFirstObjectByType<DetectiveAdditiveSceneLoader>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerEvent == TriggerEvent.Enter)
            {
                HandleTrigger(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (triggerEvent == TriggerEvent.Enter)
            {
                HandleTrigger(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerEvent == TriggerEvent.Exit)
            {
                HandleTrigger(other);
            }
        }

        private void HandleTrigger(Collider other)
        {
            if (other.GetComponentInParent<DetectiveClickMover>() == null || loader == null)
            {
                return;
            }

            if (action == TriggerAction.LoadIndoor)
            {
                if (!loader.IsIndoorLoaded && loader.CanEnterIndoor(other.transform))
                {
                    loader.EnterIndoor();
                }
            }
            else
            {
                loader.ExitIndoor();
            }
        }
    }
}