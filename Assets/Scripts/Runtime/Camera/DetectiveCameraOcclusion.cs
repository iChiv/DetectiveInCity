using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Detective
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Camera))]
    public sealed class DetectiveCameraOcclusion : MonoBehaviour
    {
        private sealed class FadeState
        {
            public Material[] originals;
            public Material[] instances;
            public Color[] baseColors;
            public float alpha = 1f;
        }

        [SerializeField] private Transform target;
        [SerializeField] private LayerMask occluderLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Range(0.02f, 0.9f)] private float transparency = 0.12f;
        [SerializeField] private float castRadius = 0.45f;

        private readonly Dictionary<Renderer, FadeState> states = new();
        private readonly HashSet<Renderer> currentOccluders = new();
        private Camera sceneCamera;

        private void Awake()
        {
            sceneCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            ResolveTarget();
            currentOccluders.Clear();

            if (target != null)
            {
                Vector3 origin = sceneCamera.transform.position;
                Vector3 direction = target.position + Vector3.up * 0.9f - origin;
                float distance = direction.magnitude;
                if (distance > 0.01f)
                {
                    RaycastHit[] hits = Physics.SphereCastAll(
                        origin,
                        castRadius,
                        direction / distance,
                        distance,
                        occluderLayers,
                        QueryTriggerInteraction.Ignore);

                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider == null)
                        {
                            continue;
                        }

                        if (target != null && hit.collider.transform.IsChildOf(target.root))
                        {
                            continue;
                        }

                        if (hit.collider.name == "Ground" || hit.collider.CompareTag("Player"))
                        {
                            continue;
                        }

                        Renderer renderer = hit.collider.GetComponentInParent<Renderer>();
                        if (renderer == null || renderer.transform.IsChildOf(target))
                        {
                            continue;
                        }

                        currentOccluders.Add(renderer);
                        EnsureState(renderer);
                    }
                }
            }

            UpdateFades();
        }

        private void EnsureState(Renderer renderer)
        {
            if (states.TryGetValue(renderer, out FadeState state))
            {
                return;
            }

            Material[] originals = renderer.sharedMaterials;
            var instances = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                instances[i] = CreateFadedInstance(originals[i]);
            }

            state = new FadeState { originals = originals, instances = instances, baseColors = new Color[originals.Length] };
            for (int i = 0; i < originals.Length; i++)
            {
                if (originals[i] != null && originals[i].HasProperty("_BaseColor"))
                {
                    state.baseColors[i] = originals[i].GetColor("_BaseColor");
                }
            }

            states[renderer] = state;
            renderer.sharedMaterials = instances;
        }

        private static Material CreateFadedInstance(Material source)
        {
            if (source == null)
            {
                return null;
            }

            var instance = new Material(source) { name = source.name + "_OcclusionFade" };
            instance.SetFloat("_Surface", 1f);
            instance.SetFloat("_Blend", 0f);
            instance.SetFloat("_SrcBlend", 5f);
            instance.SetFloat("_DstBlend", 10f);
            instance.SetFloat("_SrcBlendAlpha", 1f);
            instance.SetFloat("_DstBlendAlpha", 10f);
            instance.SetFloat("_ZWrite", 0f);
            instance.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            instance.renderQueue = (int)RenderQueue.Transparent;
            return instance;
        }

        private void UpdateFades()
        {
            var remove = new List<Renderer>();
            foreach (KeyValuePair<Renderer, FadeState> entry in states)
            {
                Renderer renderer = entry.Key;
                FadeState state = entry.Value;
                if (renderer == null)
                {
                    remove.Add(renderer);
                    continue;
                }

                float goal = currentOccluders.Contains(renderer) ? transparency : 1f;
                state.alpha = Mathf.MoveTowards(state.alpha, goal, Time.deltaTime * 6f);
                ApplyAlpha(state, state.alpha);

                if (goal >= 1f && state.alpha >= 0.99f)
                {
                    renderer.sharedMaterials = state.originals;
                    foreach (Material instance in state.instances)
                    {
                        if (instance != null)
                        {
                            Destroy(instance);
                        }
                    }

                    remove.Add(renderer);
                }
            }

            foreach (Renderer renderer in remove)
            {
                states.Remove(renderer);
            }
        }

        private static void ApplyAlpha(FadeState state, float alpha)
        {
            for (int i = 0; i < state.instances.Length; i++)
            {
                Material material = state.instances[i];
                if (material == null || !material.HasProperty("_BaseColor"))
                {
                    continue;
                }

                Color color = i < state.baseColors.Length ? state.baseColors[i] : material.GetColor("_BaseColor");
                color.r *= alpha;
                color.g *= alpha;
                color.b *= alpha;
                color.a = alpha;
                material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }
            }
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            var mover = FindFirstObjectByType<DetectiveClickMover>();
            if (mover != null)
            {
                target = mover.transform;
                return;
            }

            GameObject player = GameObject.Find("DetectivePlayer");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<Renderer, FadeState> entry in states)
            {
                if (entry.Key != null)
                {
                    entry.Key.sharedMaterials = entry.Value.originals;
                }

                foreach (Material instance in entry.Value.instances)
                {
                    if (instance != null)
                    {
                        Destroy(instance);
                    }
                }
            }

            states.Clear();
        }
    }
}
