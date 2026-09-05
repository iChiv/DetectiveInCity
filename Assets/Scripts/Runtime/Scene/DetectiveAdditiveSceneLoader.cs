using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Detective
{
    public sealed class DetectiveAdditiveSceneLoader : MonoBehaviour
    {
        [SerializeField] private string indoorScenePath = "Assets/Scenes/DetectiveIndoorTestScene.unity";
        [SerializeField] private GameObject rainObject;
        [SerializeField] private Vector3 entranceOutdoorPoint = new Vector3(0f, 0f, 10.6f);
        [SerializeField] private Vector3 entranceIndoorPoint = new Vector3(0f, 0f, 12.8f);
        [SerializeField] private float entranceLinkWidth = 3.5f;
        [SerializeField] private bool preloadIndoorScene = true;

        private Scene indoorScene;
        private NavMeshLink entranceLink;
        private bool playerIsInside;
        private bool transitionInProgress;

        public bool IsIndoorLoaded => indoorScene.IsValid() && indoorScene.isLoaded;
        public bool IsTransitionInProgress => transitionInProgress;
        public bool CanEnterIndoor(Transform player) => player != null && player.position.z >= entranceOutdoorPoint.z - 0.05f;

        private void Awake()
        {
            if (rainObject == null)
            {
                rainObject = GameObject.Find("RainPlaceholder");
            }

            EnsureEntranceLink();
        }

        private void Start()
        {
            if (preloadIndoorScene)
            {
                StartCoroutine(LoadIndoorScene());
            }
        }

        private void Update()
        {
            if (!IsIndoorLoaded || transitionInProgress)
            {
                return;
            }

            DetectiveClickMover player = FindFirstObjectByType<DetectiveClickMover>();
            if (player == null)
            {
                return;
            }

            bool shouldBeInside = player.transform.position.z >= entranceIndoorPoint.z - 0.25f;
            if (shouldBeInside != playerIsInside)
            {
                SetPlayerInside(shouldBeInside);
            }
        }

        public void EnterIndoor()
        {
            if (transitionInProgress)
            {
                return;
            }

            if (IsIndoorLoaded)
            {
                SetPlayerInside(true);
                return;
            }

            StartCoroutine(LoadIndoorScene());
        }

        public void ExitIndoor()
        {
            if (transitionInProgress || !IsIndoorLoaded)
            {
                return;
            }

            SetPlayerInside(false);
        }

        private IEnumerator LoadIndoorScene()
        {
            if (transitionInProgress)
            {
                yield break;
            }

            transitionInProgress = true;
            indoorScene = SceneManager.GetSceneByPath(indoorScenePath);
            if (!indoorScene.IsValid() || !indoorScene.isLoaded)
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(indoorScenePath, LoadSceneMode.Additive);
                if (operation == null)
                {
                    Debug.LogError($"[DetectiveAdditiveSceneLoader] Failed to load {indoorScenePath}.", this);
                    transitionInProgress = false;
                    yield break;
                }

                yield return operation;
                indoorScene = SceneManager.GetSceneByPath(indoorScenePath);
            }

            if (!indoorScene.IsValid() || !indoorScene.isLoaded)
            {
                Debug.LogError($"[DetectiveAdditiveSceneLoader] Loaded scene could not be resolved: {indoorScenePath}.", this);
                transitionInProgress = false;
                yield break;
            }

            BuildLoadedNavMeshes();
            RefreshEntranceLink();
            playerIsInside = false;
            SetRainVisible(true);
            transitionInProgress = false;
        }

        private void SetPlayerInside(bool inside)
        {
            playerIsInside = inside;
            SetRainVisible(!inside);
        }

        private void EnsureEntranceLink()
        {
            entranceLink = GetComponent<NavMeshLink>();
            if (entranceLink == null)
            {
                entranceLink = gameObject.AddComponent<NavMeshLink>();
            }

            entranceLink.startPoint = entranceOutdoorPoint;
            entranceLink.endPoint = entranceIndoorPoint;
            entranceLink.width = Mathf.Max(1f, entranceLinkWidth);
            entranceLink.bidirectional = true;
            entranceLink.area = 0;
            entranceLink.autoUpdate = true;
            entranceLink.enabled = true;
        }

        private void RefreshEntranceLink()
        {
            if (entranceLink == null)
            {
                EnsureEntranceLink();
            }

            entranceLink.enabled = false;
            entranceLink.enabled = true;
        }

        private void BuildLoadedNavMeshes()
        {
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
            foreach (NavMeshSurface surface in surfaces)
            {
                if (surface == null || !surface.gameObject.scene.IsValid() || !surface.gameObject.scene.isLoaded)
                {
                    continue;
                }

                surface.BuildNavMesh();
            }
        }

        private void SetRainVisible(bool visible)
        {
            if (rainObject == null)
            {
                rainObject = GameObject.Find("RainPlaceholder");
            }

            if (rainObject != null)
            {
                rainObject.SetActive(visible);
            }
        }
    }
}