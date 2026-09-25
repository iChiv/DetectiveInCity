using UnityEngine;

namespace Detective
{
    // A local cue after the woman conversation; answering remains the nearby dialogue trigger.
    public sealed class DetectivePhoneRing : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip ring;
        private float nextRing;
        private bool prompted;

        private void Update()
        {
            if (!DetectiveGameState.HasFlag("dlg_mysterious_woman_done")
                || DetectiveGameState.HasFlag("dlg_phone_booth_done")
                || DetectiveGameState.HasFlag("proximity_done_PhoneBoothProximity")
                || InteractionController.DialogueBlock) return;
            var player = GameObject.Find("DetectivePlayer");
            if (player == null || Vector3.Distance(player.transform.position, transform.position) > 7f) return;
            if (!prompted)
            {
                prompted = true;
                DetectiveToastUI.Instance?.Show("附近的电话亭响了，过去接听。", 5f);
            }
            if (Time.time < nextRing) return;
            nextRing = Time.time + 3f;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 0.7f;
                source.minDistance = 2f; source.maxDistance = 15f; source.volume = 0.25f;
                const int rate = 22050;
                var samples = new float[rate];
                for (int i = 0; i < samples.Length; i++)
                {
                    float t = i / (float)rate;
                    bool pulse = t < 0.32f || (t > 0.48f && t < 0.8f);
                    samples[i] = pulse ? 0.3f * (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 480f * t)) : 0f;
                }
                ring = AudioClip.Create("PhoneRing", samples.Length, 1, rate, false);
                ring.SetData(samples, 0);
            }
            source.PlayOneShot(ring);
        }
        private void OnDestroy() { if (ring != null) Destroy(ring); }
    }
}
