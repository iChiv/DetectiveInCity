using UnityEngine;

namespace Detective
{
    [RequireComponent(typeof(Collider))]
    public sealed class DetectiveZoneGate : MonoBehaviour
    {
        [SerializeField] private string districtName;
        [SerializeField] private int advanceTimeToMinutes = -1;
        [SerializeField] private string crossedFlag;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            DetectiveClickMover mover = other.GetComponentInParent<DetectiveClickMover>();
            if (mover == null)
            {
                return;
            }

            string flag = string.IsNullOrWhiteSpace(crossedFlag) ? $"district_crossed_{districtName}" : crossedFlag;
            if (DetectiveGameState.HasFlag(flag))
            {
                return;
            }

            DetectiveGameState.SetFlag(flag);

            if (advanceTimeToMinutes >= 0)
            {
                int delta = Mathf.Max(0, advanceTimeToMinutes - DetectiveGameState.TotalMinutes);
                if (delta > 0)
                {
                    DetectiveGameState.AdvanceTime(delta);
                }
            }

            if (!string.IsNullOrWhiteSpace(districtName) && DetectiveDistrictBannerUI.Instance != null)
            {
                DetectiveDistrictBannerUI.Instance.Show(districtName);
            }
        }
    }
}
