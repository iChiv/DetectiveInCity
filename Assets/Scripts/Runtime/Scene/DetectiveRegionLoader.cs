using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Detective
{
    public sealed class DetectiveRegionLoader : MonoBehaviour
    {
        private const float FadeDuration = 0.35f;

        public static DetectiveRegionLoader Instance { get; private set; }

        public string CurrentRegionId { get; private set; }

        public string PreviousRegionId { get; private set; }

        public bool IsLoading => isLoading;

        private bool isLoading;

        private void Awake()
        {
            Instance = this;
            if (GetComponent<DetectiveStoryFlow>() == null) gameObject.AddComponent<DetectiveStoryFlow>();
        }

        private void OnEnable()
        {
            // 域重载/协程异常中断后自救：isLoading 若卡在 true，所有区域切换会永久失效。
            isLoading = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            if (CurrentRegionId == null && !isLoading && DetectiveTitleMenu.Instance == null)
            {
                LoadRegion("D0_Alley");
            }
        }

        // 由 DetectiveTitleMenu（或旧场景兜底）调用，开始第一局。
        public void BeginGame()
        {
            if (CurrentRegionId == null && !isLoading)
            {
                LoadRegion("D0_Alley");
            }
        }

        public void LoadRegion(string regionId, string spawnId = "default")
        {
            if (isLoading)
            {
                return;
            }

            if (!DetectiveRegionCatalog.TryGetRegion(regionId, out var region))
            {
                Debug.LogWarning($"[DetectiveRegionLoader] 未知区域: {regionId}");
                return;
            }

            StartCoroutine(LoadRoutine(region, spawnId));
        }

        private IEnumerator LoadRoutine(DetectiveRegionCatalog.RegionInfo region, string spawnId)
        {
            isLoading = true;
            InteractionController.DialogueBlock = true;
            try
            {
                yield return LoadRoutineBody(region, spawnId);
            }
            finally
            {
                // 协程被异常/域重载中断时也要解锁，避免游戏永久卡死。
                InteractionController.DialogueBlock = false;
                isLoading = false;
            }

            // Arrival encounters run only after placement and fade-in have completed.
            if (CurrentRegionId == region.RegionId)
            {
                Scene loadedScene = SceneManager.GetSceneByName(region.RegionId);
                foreach (GameObject root in loadedScene.GetRootGameObjects())
                {
                    foreach (DetectiveProximityDialogue dialogue in root.GetComponentsInChildren<DetectiveProximityDialogue>())
                        dialogue.HandleRegionArrival(PreviousRegionId);
                    foreach (DetectiveFinalDuelTrigger trigger in root.GetComponentsInChildren<DetectiveFinalDuelTrigger>())
                    {
                        trigger.HandleRegionArrival(PreviousRegionId);
                    }
                }
            }
        }

        private IEnumerator LoadRoutineBody(DetectiveRegionCatalog.RegionInfo region, string spawnId)
        {
            PreviousRegionId = CurrentRegionId;

            if (DetectiveFadeUI.Instance != null)
            {
                yield return DetectiveFadeUI.Instance.FadeToBlack(FadeDuration);
            }

            GameObject player = GameObject.Find("DetectivePlayer");
            var mover = player != null ? player.GetComponent<DetectiveClickMover>() : null;
            GameObject coreNav = GameObject.Find("CoreNavMesh");
            if (mover != null && coreNav != null && mover.Agent.isOnNavMesh)
            {
                mover.Agent.Warp(coreNav.transform.position + Vector3.up * 0.5f);
            }

            if (CurrentRegionId != null && SceneManager.GetSceneByName(CurrentRegionId).IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(CurrentRegionId);
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(region.RegionId, LoadSceneMode.Additive);
            load.allowSceneActivation = true;
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(region.RegionId));

            if (player != null)
            {
                Transform spawn = FindSpawn(ResolveSpawnId(spawnId));
                if (spawn != null)
                {
                    // 先把人放到出生点（此时可能尚未吸附 NavMesh），再在出生点处吸附并清路径。
                    player.transform.position = spawn.position;
                    if (mover != null && mover.SnapToNavMesh())
                    {
                        mover.Agent.ResetPath();
                    }
                }
            }

            CurrentRegionId = region.RegionId;
            if (region.DistrictId != "redlight" && DetectiveGameState.HasFlag("status_calm"))
            {
                DetectiveGameState.RemoveFlag("status_calm");
                DetectiveToastUI.Instance?.Show("离开红灯区，冷静带来的临时逻辑加成已结束。", 3f);
            }
            DetectiveGameState.SetFlag($"loc_visited_{region.RegionId}");

            if (DetectiveRegionCatalog.TryGetDistrict(region.DistrictId, out var district)
                && !DetectiveGameState.HasFlag($"district_entered_{district.DistrictId}"))
            {
                DetectiveGameState.SetFlag($"district_entered_{district.DistrictId}");
                if (district.FirstEnterMinutes >= 0)
                {
                    int delta = Mathf.Max(0, district.FirstEnterMinutes - DetectiveGameState.TotalMinutes);
                    if (delta > 0)
                    {
                        DetectiveGameState.AdvanceTime(delta);
                    }
                }

                if (DetectiveDistrictBannerUI.Instance != null)
                {
                    DetectiveDistrictBannerUI.Instance.Show(district.DisplayName);
                }
            }

            // 从旧工业区连夜赶到住宅区：横幅追加一句过场文案。
            if (region.RegionId == "D4_Residential"
                && PreviousRegionId == "D3_Industrial"
                && DetectiveDistrictBannerUI.Instance != null)
            {
                DetectiveDistrictBannerUI.Instance.ShowLine("你连夜穿过半个城市");
            }

            if (DetectiveFadeUI.Instance != null)
            {
                yield return DetectiveFadeUI.Instance.FadeFromBlack(FadeDuration);
            }

            InteractionController.DialogueBlock = false;
            isLoading = false;
        }

        private string ResolveSpawnId(string spawnId)
        {
            if (!string.IsNullOrWhiteSpace(spawnId) && spawnId != "default")
            {
                return spawnId;
            }

            if (!string.IsNullOrWhiteSpace(PreviousRegionId)
                && GameObject.Find($"Spawn_from_{PreviousRegionId}") != null)
            {
                return $"from_{PreviousRegionId}";
            }

            return "default";
        }

        private static Transform FindSpawn(string spawnId)
        {
            var go = GameObject.Find($"Spawn_{spawnId}");
            if (go == null)
            {
                go = GameObject.Find("Spawn_default");
            }

            return go != null ? go.transform : null;
        }
    }
}
