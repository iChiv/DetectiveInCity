using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Detective
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class DetectiveNpcWalker : MonoBehaviour
    {
        [SerializeField] private string walkAwayFlag;
        [SerializeField] private string walkAwayClueId;
        [SerializeField] private string alternateWalkAwayFlag;
        [SerializeField] private string reviveAfterClueId;
        [SerializeField] private Vector3 walkAwayPoint;
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float arrivalRadius = 0.3f;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private float walkTimeout = 30f;

        private readonly List<Material> fadeMaterials = new();
        private readonly List<Color> fadeBaseColors = new();

        private NavMeshAgent agent;
        private TestInteractable interactable;
        private Collider bodyCollider;
        private DetectiveWorldLabel worldLabel;
        private bool walkStarted;
        private bool hideCompleted;
        private float walkElapsed;
        private float fadeElapsed = -1f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            interactable = GetComponentInChildren<TestInteractable>(true);
            bodyCollider = interactable != null ? interactable.GetComponent<Collider>() : GetComponentInChildren<Collider>(true);
            worldLabel = GetComponentInChildren<DetectiveWorldLabel>(true);

            // 待机期间禁用 agent，避免区域加载初期的 NavMesh 告警；开走时再启用（参照 DetectiveClickMover）。
            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (IsTriggerMet())
            {
                // 离场发生在本场景加载之前：NPC 早已离开，直接隐藏，保持"已完成"语义。
                FinishHide();
                return;
            }

            DetectiveGameState.OnFlagChanged += HandleFlagChanged;
            DetectiveGameState.OnClueCollected += HandleClueCollected;
        }

        private void OnDisable()
        {
            DetectiveGameState.OnFlagChanged -= HandleFlagChanged;
            DetectiveGameState.OnClueCollected -= HandleClueCollected;
        }

        private void Update()
        {
            if (fadeElapsed >= 0f)
            {
                AdvanceFade();
                return;
            }

            if (!walkStarted || agent == null || !agent.enabled || !agent.isOnNavMesh || agent.pathPending)
            {
                return;
            }

            walkElapsed += Time.deltaTime;
            if (walkElapsed > walkTimeout)
            {
                FallbackHide($"走路超时（{name}）");
                return;
            }

            if (agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                StartFadeOut();
            }
        }

        public void BeginWalkAway()
        {
            if (walkStarted || hideCompleted)
            {
                return;
            }

            walkStarted = true;
            walkElapsed = 0f;
            DisableInteraction();

            if (agent == null)
            {
                FallbackHide($"NavMeshAgent 缺失（{name}）");
                return;
            }

            agent.enabled = true;
            agent.speed = walkSpeed;
            agent.stoppingDistance = arrivalRadius;
            agent.updateRotation = true;

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, 5f, NavMesh.AllAreas))
            {
                FallbackHide($"脚下没有 NavMesh（{name}）");
                return;
            }

            agent.baseOffset = transform.position.y - startHit.position.y;
            if (!agent.Warp(startHit.position))
            {
                FallbackHide($"Warp 到 NavMesh 失败（{name}）");
                return;
            }

            if (!NavMesh.SamplePosition(walkAwayPoint, out NavMeshHit endHit, 5f, NavMesh.AllAreas))
            {
                FallbackHide($"离场点 {walkAwayPoint} 附近没有 NavMesh（{name}）");
                return;
            }

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path)
                || path.status != NavMeshPathStatus.PathComplete)
            {
                FallbackHide($"离场点 {walkAwayPoint} 不可达（{name}）");
                return;
            }

            agent.SetDestination(endHit.position);
        }

        private void HandleFlagChanged(string flag)
        {
            if (walkStarted || hideCompleted)
            {
                return;
            }

            if ((!string.IsNullOrWhiteSpace(walkAwayFlag) && flag == walkAwayFlag)
                || (!string.IsNullOrWhiteSpace(alternateWalkAwayFlag) && flag == alternateWalkAwayFlag))
            {
                BeginWalkAway();
            }
        }

        private void HandleClueCollected(string clueId)
        {
            if (!walkStarted && !hideCompleted && !string.IsNullOrWhiteSpace(walkAwayClueId) && clueId == walkAwayClueId)
            {
                BeginWalkAway();
            }
        }

        private bool IsTriggerMet()
        {
            if (!string.IsNullOrWhiteSpace(walkAwayFlag) && DetectiveGameState.HasFlag(walkAwayFlag))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(alternateWalkAwayFlag)
                && DetectiveGameState.HasFlag(alternateWalkAwayFlag)
                && (string.IsNullOrWhiteSpace(reviveAfterClueId)
                    || !DetectiveInvestigationState.IsCollected(reviveAfterClueId)))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(walkAwayClueId) && DetectiveInvestigationState.IsCollected(walkAwayClueId);
        }

        private void DisableInteraction()
        {
            if (interactable != null)
            {
                interactable.enabled = false;
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            if (worldLabel != null)
            {
                worldLabel.SetVisible(false);
                worldLabel.enabled = false;
            }
        }

        private void FallbackHide(string reason)
        {
            Debug.LogWarning($"[DetectiveNpcWalker] {reason}，退化为原地淡出/隐藏。", this);
            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }

            StartFadeOut();
        }

        private void StartFadeOut()
        {
            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }

            fadeMaterials.Clear();
            fadeBaseColors.Clear();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material shared = renderer.sharedMaterial;
                if (!SupportsFade(shared))
                {
                    continue;
                }

                // 实例化材质再改透明度，避免污染缓存的共享灰盒材质。
                Material instance = renderer.material;
                Color baseColor = instance.HasProperty("_BaseColor") ? instance.GetColor("_BaseColor") : instance.color;
                fadeMaterials.Add(instance);
                fadeBaseColors.Add(baseColor);
            }

            if (fadeMaterials.Count == 0)
            {
                // 材质不支持透明（当前灰盒 Lit 材质均为不透明）：直接隐藏。
                FinishHide();
                return;
            }

            fadeElapsed = 0f;
        }

        private void AdvanceFade()
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeElapsed / fadeDuration);
            for (int i = 0; i < fadeMaterials.Count; i++)
            {
                Color color = fadeBaseColors[i];
                color.a *= 1f - t;
                if (fadeMaterials[i].HasProperty("_BaseColor"))
                {
                    fadeMaterials[i].SetColor("_BaseColor", color);
                }
                else
                {
                    fadeMaterials[i].color = color;
                }
            }

            if (t >= 1f)
            {
                FinishHide();
            }
        }

        private void FinishHide()
        {
            hideCompleted = true;
            gameObject.SetActive(false);
        }

        private static bool SupportsFade(Material material)
        {
            if (material == null || !material.HasProperty("_BaseColor"))
            {
                return false;
            }

            if (material.renderQueue >= 3000)
            {
                return true;
            }

            return material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;
        }
    }
}
