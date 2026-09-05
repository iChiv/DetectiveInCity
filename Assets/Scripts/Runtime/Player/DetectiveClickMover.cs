using UnityEngine;
using UnityEngine.AI;

namespace Detective
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class DetectiveClickMover : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float maxRayDistance = 250f;
        [SerializeField] private float navMeshSampleDistance = 2f;
        [SerializeField] private float stoppingDistance = 0.1f;

        private NavMeshAgent agent;
        private float rootHeightOffset = 1f;
        private bool traversingOffMeshLink;
        private OffMeshLinkData activeOffMeshLinkData;
        private float offMeshLinkProgress;
        private float offMeshLinkDuration;

        public NavMeshAgent Agent => agent;
        public Vector3 Destination => agent != null && agent.hasPath ? agent.destination : GetNavMeshPosition();
        public bool IsMoving => agent != null && agent.isOnNavMesh && (traversingOffMeshLink || agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.05f);
        public bool IsTraversingOffMeshLink => traversingOffMeshLink;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = stoppingDistance;
            agent.baseOffset = 0f;
            agent.updatePosition = false;
            agent.autoTraverseOffMeshLink = false;
            rootHeightOffset = CalculateRootHeightOffset();

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Start()
        {
            SnapToNavMesh();
        }

        private void Update()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                return;
            }

            if (agent.isOnOffMeshLink)
            {
                if (!traversingOffMeshLink)
                {
                    BeginOffMeshLinkTraversal();
                }

                AdvanceOffMeshLinkTraversal();
            }
            else if (traversingOffMeshLink)
            {
                traversingOffMeshLink = false;
            }
        }

        private void LateUpdate()
        {
            SyncTransformToAgent();
        }

        public bool TryMoveToScreenPoint(Vector2 screenPoint)
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[DetectiveClickMover] No camera is assigned.", this);
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return TryMoveToWorldPosition(hit.point);
        }

        public bool TryMoveToWorldPosition(Vector3 worldPosition)
        {
            return TryMoveToWorldPosition(worldPosition, stoppingDistance);
        }

        public bool TryMoveToWorldPosition(Vector3 worldPosition, float requestedStoppingDistance)
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                Debug.LogWarning("[DetectiveClickMover] NavMeshAgent is not currently on a NavMesh.", this);
                return false;
            }

            if (!NavMesh.SamplePosition(worldPosition, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[DetectiveClickMover] No reachable NavMesh position near {worldPosition}.", this);
                return false;
            }

            agent.stoppingDistance = Mathf.Max(0f, requestedStoppingDistance);
            return agent.SetDestination(navHit.position);
        }

        public bool TryMoveToInteractionRange(Vector3 targetPosition, Collider targetCollider, float interactionRange)
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                Debug.LogWarning("[DetectiveClickMover] Cannot move to interaction range while off the NavMesh.", this);
                return false;
            }

            Vector3 targetCenter = targetCollider != null ? targetCollider.bounds.center : targetPosition;
            targetCenter.y = transform.position.y;
            Vector3 approachDirection = transform.position - targetCenter;
            approachDirection.y = 0f;
            if (approachDirection.sqrMagnitude < 0.001f)
            {
                approachDirection = -transform.forward;
                approachDirection.y = 0f;
            }

            approachDirection.Normalize();
            float horizontalTargetRadius = targetCollider != null
                ? Mathf.Max(targetCollider.bounds.extents.x, targetCollider.bounds.extents.z)
                : 0.5f;
            float candidateRadius = horizontalTargetRadius + Mathf.Max(0.1f, interactionRange);

            NavMeshHit bestHit = default;
            float bestScore = float.PositiveInfinity;
            bool foundSampledPosition = false;
            const int candidateCount = 16;
            for (int index = 0; index < candidateCount; index++)
            {
                float angle = (360f / candidateCount) * index;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * approachDirection;
                Vector3 candidate = targetCenter + direction * candidateRadius;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    continue;
                }

                // The entrance link can be resolved by NavMeshAgent one frame later than
                // NavMesh.CalculatePath. Prefer a nearby sampled point and let the agent
                // resolve the final link/path asynchronously instead of rejecting it.
                float score = Vector3.Distance(agent.nextPosition, navHit.position);
                if (!foundSampledPosition || score < bestScore)
                {
                    foundSampledPosition = true;
                    bestScore = score;
                    bestHit = navHit;
                }
            }

            if (!foundSampledPosition)
            {
                Debug.LogWarning($"[DetectiveClickMover] No NavMesh position into range of {targetPosition}.", this);
                return false;
            }

            agent.stoppingDistance = 0.05f;
            return agent.SetDestination(bestHit.position);
        }

        public void RestoreDefaultStoppingDistance()
        {
            if (agent != null)
            {
                agent.stoppingDistance = stoppingDistance;
            }
        }

        public void Stop()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                SyncTransformToAgent();
            }
        }

        private void SnapToNavMesh()
        {
            Vector3 navProbe = transform.position;
            navProbe.y -= rootHeightOffset;
            if (!NavMesh.SamplePosition(navProbe, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                Debug.LogWarning("[DetectiveClickMover] Could not find a NavMesh position for the player.", this);
                return;
            }

            if (!agent.isOnNavMesh || !agent.Warp(navHit.position))
            {
                Debug.LogWarning("[DetectiveClickMover] Could not warp the player onto the NavMesh.", this);
                return;
            }

            SyncTransformToAgent();
        }

        private void BeginOffMeshLinkTraversal()
        {
            activeOffMeshLinkData = agent.currentOffMeshLinkData;
            offMeshLinkProgress = 0f;
            float linkDistance = Vector3.Distance(activeOffMeshLinkData.startPos, activeOffMeshLinkData.endPos);
            offMeshLinkDuration = Mathf.Max(0.05f, linkDistance / Mathf.Max(0.1f, agent.speed));
            traversingOffMeshLink = true;
            agent.nextPosition = activeOffMeshLinkData.startPos;
            transform.position = activeOffMeshLinkData.startPos + Vector3.up * rootHeightOffset;
        }

        private void AdvanceOffMeshLinkTraversal()
        {
            if (!traversingOffMeshLink)
            {
                return;
            }

            offMeshLinkProgress = Mathf.Min(1f, offMeshLinkProgress + Time.deltaTime / offMeshLinkDuration);
            Vector3 linkPosition = Vector3.Lerp(activeOffMeshLinkData.startPos, activeOffMeshLinkData.endPos, offMeshLinkProgress);
            agent.nextPosition = linkPosition;
            transform.position = linkPosition + Vector3.up * rootHeightOffset;

            if (offMeshLinkProgress >= 1f)
            {
                agent.CompleteOffMeshLink();
                traversingOffMeshLink = false;
                SyncTransformToAgent();
            }
        }

        private void SyncTransformToAgent()
        {
            if (agent == null || !agent.isOnNavMesh || traversingOffMeshLink)
            {
                return;
            }

            Vector3 navPosition = agent.nextPosition;
            transform.position = navPosition + Vector3.up * rootHeightOffset;
        }

        private Vector3 GetNavMeshPosition()
        {
            if (agent != null && agent.isOnNavMesh)
            {
                return agent.nextPosition;
            }

            return transform.position - Vector3.up * rootHeightOffset;
        }

        private static float CalculatePathLength(NavMeshPath path)
        {
            if (path == null || path.corners == null || path.corners.Length < 2)
            {
                return float.PositiveInfinity;
            }

            float length = 0f;
            for (int index = 1; index < path.corners.Length; index++)
            {
                length += Vector3.Distance(path.corners[index - 1], path.corners[index]);
            }

            return length;
        }

        private float CalculateRootHeightOffset()
        {
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null && capsule.direction == 1)
            {
                return Mathf.Max(0f, capsule.height * 0.5f - capsule.center.y);
            }

            Renderer renderer = GetComponent<Renderer>();
            return renderer != null ? Mathf.Max(0f, renderer.bounds.extents.y) : 0f;
        }
    }
}