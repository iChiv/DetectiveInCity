using System.Collections.Generic;
using UnityEngine;

namespace Detective
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(Camera))]
    public sealed class DetectiveCameraOcclusion : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask occluderLayers = 1 << 9;
        [SerializeField, Range(0.1f, 0.8f)] private float transparency = 0.28f;

        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        private readonly HashSet<Renderer> visibleOccluders = new();
        private Camera sceneCamera;
        private Material transparentMaterial;

        private void Awake()
        {
            sceneCamera = GetComponent<Camera>();
            transparentMaterial = CreateTransparentMaterial();
        }

        private void LateUpdate()
        {
            ResolveTarget();
            RestorePreviousOccluders();

            if (target == null)
            {
                return;
            }

            Vector3 direction = target.position - sceneCamera.transform.position;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                sceneCamera.transform.position,
                direction / distance,
                distance,
                occluderLayers,
                QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Renderer renderer = hit.collider.GetComponentInParent<Renderer>();
                if (renderer == null || renderer.transform == target || visibleOccluders.Contains(renderer))
                {
                    continue;
                }

                visibleOccluders.Add(renderer);
                originalMaterials[renderer] = renderer.sharedMaterials;

                Material[] fadedMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < fadedMaterials.Length; i++)
                {
                    fadedMaterials[i] = transparentMaterial;
                }

                renderer.sharedMaterials = fadedMaterials;
            }
        }

        private void RestorePreviousOccluders()
        {
            foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
            {
                if (entry.Key != null)
                {
                    entry.Key.sharedMaterials = entry.Value;
                }
            }

            originalMaterials.Clear();
            visibleOccluders.Clear();
        }

        private void ResolveTarget()
        {
            if (target != null)
            {
                return;
            }

            GameObject player = GameObject.Find("DetectivePlayer");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private Material CreateTransparentMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                name = "Detective Camera Occlusion Fade",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 3000
            };

            material.SetColor("_BaseColor", new Color(0.08f, 0.12f, 0.2f, transparency));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", 1f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_SrcBlendAlpha", 1f);
            material.SetFloat("_DstBlendAlpha", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return material;
        }

        private void OnDestroy()
        {
            RestorePreviousOccluders();
            if (transparentMaterial != null)
            {
                Destroy(transparentMaterial);
            }
        }
    }
}
