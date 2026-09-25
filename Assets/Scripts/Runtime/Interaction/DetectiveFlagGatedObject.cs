using UnityEngine;

namespace Detective
{
    public sealed class DetectiveFlagGatedObject : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [SerializeField] private string[] requiredFlags;
        [SerializeField] private string[] forbiddenFlags;

        private void Awake()
        {
            if (target == null && transform.childCount > 0)
            {
                target = transform.GetChild(0).gameObject;
            }
        }

        private void OnEnable()
        {
            DetectiveGameState.OnFlagChanged += HandleFlagChanged;
            Evaluate();
        }

        private void OnDisable()
        {
            DetectiveGameState.OnFlagChanged -= HandleFlagChanged;
        }

        private void HandleFlagChanged(string flag)
        {
            Evaluate();
        }

        private void Evaluate()
        {
            if (target == null)
            {
                return;
            }

            bool active = true;
            if (requiredFlags != null)
            {
                foreach (string flag in requiredFlags)
                {
                    if (!DetectiveGameState.HasFlag(flag))
                    {
                        active = false;
                        break;
                    }
                }
            }

            if (active && forbiddenFlags != null)
            {
                foreach (string flag in forbiddenFlags)
                {
                    if (DetectiveGameState.HasFlag(flag))
                    {
                        active = false;
                        break;
                    }
                }
            }

            if (target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
