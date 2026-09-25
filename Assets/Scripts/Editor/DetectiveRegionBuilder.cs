using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using static Detective.EditorTools.DetectiveCityBuilder;

namespace Detective.EditorTools
{
    public static class DetectiveRegionBuilder
    {
        private const string CorePath = "Assets/Scenes/Core.unity";
        private const string RegionsFolder = "Assets/Scenes/Regions";
        private const float WallHeight = 3f;

        [MenuItem("Detective/Validate NavMesh")]
        public static void ValidateNavMesh()
        {
            var report = new System.Text.StringBuilder();
            int totalPaths = 0;
            int failedPaths = 0;

            foreach (var region in DetectiveRegionCatalog.Regions)
            {
                EditorSceneManager.OpenScene(region.ScenePath, OpenSceneMode.Single);
                DetectiveCityBuilder.ResetMaterialCaches();

                var spawns = new List<(string name, Vector3 position)>();
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.StartsWith("Spawn_"))
                        {
                            spawns.Add((t.name, t.position));
                        }
                    }
                }

                var targets = new List<(string name, Vector3 position)>();
                foreach (var ti in Object.FindObjectsByType<TestInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    targets.Add((ti.name, ti.InteractionPoint.position));
                }

                foreach (var door in Object.FindObjectsByType<DetectiveDoorTeleport>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    targets.Add((door.name, door.transform.position));
                }

                foreach (var spawn in spawns)
                {
                    if (!UnityEngine.AI.NavMesh.SamplePosition(spawn.position, out var spawnHit, 1.5f, 1))
                    {
                        failedPaths++;
                        totalPaths++;
                        report.AppendLine($"[FAIL] {region.RegionId} / {spawn.name}: 出生点不在 NavMesh 上");
                        continue;
                    }

                    foreach (var target in targets)
                    {
                        totalPaths++;
                        bool reachable = false;
                        foreach (float radius in new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 4f, 5f, 6f })
                        {
                            if (!UnityEngine.AI.NavMesh.SamplePosition(target.position, out var targetHit, radius, 1))
                            {
                                continue;
                            }

                            var path = new UnityEngine.AI.NavMeshPath();
                            UnityEngine.AI.NavMesh.CalculatePath(spawnHit.position, targetHit.position, 1, path);
                            if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                            {
                                reachable = true;
                                break;
                            }
                        }

                        if (!reachable)
                        {
                            failedPaths++;
                            report.AppendLine($"[FAIL] {region.RegionId} / {spawn.name} → {target.name}: 6m 内无可达落点");
                        }
                    }
                }
            }

            EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Single);
            if (failedPaths == 0)
            {
                Debug.Log($"[ValidateNavMesh] 全部通过：{totalPaths} 条路径（13 场景 × 各出生点 → 全部交互物与门）。");
            }
            else
            {
                Debug.LogError("[ValidateNavMesh] " + failedPaths + "/" + totalPaths + " 条路径失败：" + System.Environment.NewLine + report);
            }
        }

        [MenuItem("Detective/Build Core And Regions")]
        public static void BuildCoreAndRegions()
        {
            BuildCoreScene();
            BuildAllRegions();
            EditorSceneManager.OpenScene(CorePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DetectiveRegionBuilder] Core + 13 区域场景构建完成。");
        }

        [MenuItem("Detective/Build Regions")]
        public static void BuildRegions()
        {
            BuildAllRegions();
            if (System.IO.File.Exists(CorePath))
            {
                EditorSceneManager.OpenScene(CorePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DetectiveRegionBuilder] {DetectiveRegionCatalog.Regions.Length} 个区域场景重建完成（未触碰 Core）。");
        }

        private static void BuildAllRegions()
        {
            if (!AssetDatabase.IsValidFolder(RegionsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Scenes", "Regions");
            }

            foreach (var region in DetectiveRegionCatalog.Regions)
            {
                BuildRegionScene(region);
            }

            UpdateBuildSettings();
        }

        private static void BuildCoreScene()
        {
            if (System.IO.File.Exists(CorePath))
            {
                EditorSceneManager.OpenScene(CorePath, OpenSceneMode.Single);
                DetectiveCityBuilder.ResetMaterialCaches();
                if (GameObject.Find("UICanvas") != null)
                {
                    EnsureCoreLogicObjects();
                    Scene existingScene = SceneManager.GetActiveScene();
                    EditorSceneManager.MarkSceneDirty(existingScene);
                    EditorSceneManager.SaveScene(existingScene);
                    Debug.Log("[DetectiveRegionBuilder] Core 已存在且包含 UICanvas：跳过 UI 生成，仅补齐 Systems/Player/Camera/Loader。");
                    return;
                }
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DetectiveCityBuilder.ResetMaterialCaches();
            DetectiveCityBuilder.ConfigureRenderSettings();
            var root = new GameObject("Core");

            var coreNav = DetectiveCityBuilder.CreateCube(root.transform, "CoreNavMesh", new Vector3(0f, -5.5f, 0f), new Vector3(20f, 1f, 20f), new Color(0.02f, 0.02f, 0.03f));
            coreNav.GetComponent<MeshRenderer>().enabled = false;
            var coreSurface = coreNav.AddComponent<NavMeshSurface>();
            coreSurface.collectObjects = CollectObjects.All;

            DetectiveCityBuilder.BuildPlayerRig(root.transform);
            DetectiveCityBuilder.BuildCameras(root.transform);
            DetectiveCityBuilder.BuildLighting(root.transform);

            DetectiveCityBuilder.UIRefs refs = DetectiveCityBuilder.BuildSceneUI(root.transform);

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            var runner = systems.AddComponent<DetectiveDialogueRunner>();
            var runnerSo = new SerializedObject(runner);
            runnerSo.FindProperty("dialogueUI").objectReferenceValue = refs.dialogueUI;
            runnerSo.FindProperty("voiceCornerUI").objectReferenceValue = refs.voiceCorner;
            runnerSo.ApplyModifiedPropertiesWithoutUndo();
            systems.AddComponent<DetectivePlotHooks>();
            systems.AddComponent<DetectiveRegionLoader>();
            systems.AddComponent<DetectiveTitleMenu>();
            systems.AddComponent<DetectivePauseMenu>();
            systems.AddComponent<DetectiveClueToastListener>();

            coreSurface.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CorePath);
        }

        // Core 已存在时的增量补齐：只确保逻辑对象存在，绝不重建 UI。
        private static void EnsureCoreLogicObjects()
        {
            GameObject root = GameObject.Find("Core");
            if (root == null)
            {
                root = new GameObject("Core");
            }

            if (GameObject.Find("CoreNavMesh") == null)
            {
                var coreNav = DetectiveCityBuilder.CreateCube(root.transform, "CoreNavMesh", new Vector3(0f, -5.5f, 0f), new Vector3(20f, 1f, 20f), new Color(0.02f, 0.02f, 0.03f));
                coreNav.GetComponent<MeshRenderer>().enabled = false;
                var coreSurface = coreNav.AddComponent<NavMeshSurface>();
                coreSurface.collectObjects = CollectObjects.All;
                coreSurface.BuildNavMesh();
            }

            if (GameObject.Find("DetectivePlayer") == null)
            {
                DetectiveCityBuilder.BuildPlayerRig(root.transform);
            }

            if (GameObject.Find("Main Camera") == null || GameObject.Find("DetectiveCamera") == null)
            {
                foreach (string name in new[] { "Main Camera", "DetectiveCamera" })
                {
                    GameObject old = GameObject.Find(name);
                    if (old != null)
                    {
                        Object.DestroyImmediate(old);
                    }
                }

                DetectiveCityBuilder.BuildCameras(root.transform);
            }

            if (GameObject.Find("DirectionalLight") == null)
            {
                DetectiveCityBuilder.BuildLighting(root.transform);
            }

            GameObject systems = GameObject.Find("Systems");
            if (systems == null)
            {
                systems = new GameObject("Systems");
                systems.transform.SetParent(root.transform, false);
            }

            var runner = systems.GetComponent<DetectiveDialogueRunner>();
            if (runner == null)
            {
                runner = systems.AddComponent<DetectiveDialogueRunner>();
            }

            var runnerSo = new SerializedObject(runner);
            if (runnerSo.FindProperty("dialogueUI").objectReferenceValue == null)
            {
                runnerSo.FindProperty("dialogueUI").objectReferenceValue =
                    Object.FindFirstObjectByType<DetectiveDialogueUI>(FindObjectsInactive.Include);
            }

            if (runnerSo.FindProperty("voiceCornerUI").objectReferenceValue == null)
            {
                runnerSo.FindProperty("voiceCornerUI").objectReferenceValue =
                    Object.FindFirstObjectByType<DetectiveVoiceCornerUI>(FindObjectsInactive.Include);
            }

            runnerSo.ApplyModifiedPropertiesWithoutUndo();

            if (systems.GetComponent<DetectivePlotHooks>() == null)
            {
                systems.AddComponent<DetectivePlotHooks>();
            }

            if (systems.GetComponent<DetectiveRegionLoader>() == null)
            {
                systems.AddComponent<DetectiveRegionLoader>();
            }

            if (systems.GetComponent<DetectiveTitleMenu>() == null)
            {
                systems.AddComponent<DetectiveTitleMenu>();
            }

            if (systems.GetComponent<DetectivePauseMenu>() == null)
            {
                systems.AddComponent<DetectivePauseMenu>();
            }

            if (systems.GetComponent<DetectiveClueToastListener>() == null)
            {
                systems.AddComponent<DetectiveClueToastListener>();
            }
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new(CorePath, true) };
            scenes.AddRange(DetectiveRegionCatalog.Regions.Select(region => new EditorBuildSettingsScene(region.ScenePath, true)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void BuildRegionScene(DetectiveRegionCatalog.RegionInfo region)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DetectiveCityBuilder.ResetMaterialCaches();
            DetectiveCityBuilder.ConfigureRenderSettings();
            var root = new GameObject(region.RegionId);
            var navigation = new GameObject("Navigation");
            navigation.transform.SetParent(root.transform, false);

            switch (region.RegionId)
            {
                case "D0_Alley": BuildAlley(root.transform); break;
                case "D1_RedLight": BuildRedLight(root.transform); break;
                case "I1_Bar": BuildBar(root.transform); break;
                case "D2_Financial": BuildFinancial(root.transform); break;
                case "I2_Apartment": BuildApartment(root.transform); break;
                case "I2_Coffee": BuildCoffee(root.transform); break;
                case "D3_Industrial": BuildIndustrial(root.transform); break;
                case "I3_Warehouse": BuildWarehouse(root.transform); break;
                case "I3_Basement": BuildBasement(root.transform); break;
                case "I3_Rooftop": BuildRooftop(root.transform); break;
                case "D4_Residential": BuildResidential(root.transform); break;
                case "I4_YourHome": BuildYourHome(root.transform); break;
                case "I4_VictimHome": BuildVictimHome(root.transform); break;
            }

            DressRegion(root.transform, region.RegionId);
            if (region.IsOutdoor)
            {
                // Low foreground boundaries keep the street readable; buildings provide the skyline.
                foreach (Transform wall in root.transform)
                {
                    if (!wall.name.StartsWith("Wall_")) continue;
                    float cap = wall.name.StartsWith("Wall_S_") || wall.name.StartsWith("Wall_W_") ? 1f : 3.2f;
                    float height = Mathf.Min(wall.localScale.y, cap);
                    Vector3 scale = wall.localScale; scale.y = height; wall.localScale = scale;
                    Vector3 position = wall.position; position.y = height * 0.5f; wall.position = position;
                }
            }
            DetectiveLayoutPolish.Apply(root.transform, region.RegionId);
            if (region.RegionId == "I4_YourHome") DetectiveHomeStoryInstaller.Apply(root.transform);
            if (region.RegionId == "I2_Apartment") DetectiveApartmentStoryInstaller.Apply(root.transform);
            DetectiveSceneBeatInstaller.Apply(root.transform, region.RegionId);
            AddNavBlockModifiers(root.transform);

            if (region.IsOutdoor)
            {
                BuildRegionRain(root.transform);
            }

            var surface = navigation.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            DetectiveFurnitureNavigation.Apply(root.transform);
            surface.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, region.ScenePath);
        }

        #region 区域内容

        private static void BuildAlley(Transform root)
        {
            BuildGroundAndWalls(root, 10f, 26f, 5.5f, new[] { (side: 'E', center: 0f, gap: 4f) }, new Color(0.10f, 0.12f, 0.16f));
            CreateRegionLight(root, new Color(0.36f, 0.49f, 0.69f), 105f, 32f, 4.5f);

            CreateCube(root, "Dumpster", new Vector3(-2.8f, 0.75f, -8.5f), new Vector3(1.5f, 1.5f, 1.1f), new Color(0.16f, 0.22f, 0.20f));
            CreateProp(root, "TrashBag_A", new Vector3(-1.2f, 0.25f, -9.2f), 0.35f, 0.5f, new Color(0.08f, 0.08f, 0.10f));
            CreateProp(root, "TrashBag_B", new Vector3(-3.9f, 0.22f, -9.6f), 0.3f, 0.44f, new Color(0.09f, 0.09f, 0.11f));
            CreateCube(root, "Cardbox_A", new Vector3(2.6f, 0.3f, -6.5f), new Vector3(0.7f, 0.6f, 0.7f), new Color(0.32f, 0.26f, 0.18f));
            CreateCube(root, "Cardbox_B", new Vector3(3.3f, 0.2f, -7.4f), new Vector3(0.5f, 0.4f, 0.5f), new Color(0.30f, 0.25f, 0.17f));
            CreateCube(root, "Puddle_A", new Vector3(0.8f, 0.02f, -3.5f), new Vector3(1.6f, 0.02f, 1.1f), new Color(0.05f, 0.07f, 0.10f));
            CreateCube(root, "Puddle_B", new Vector3(-1.8f, 0.02f, 2.5f), new Vector3(1.2f, 0.02f, 0.9f), new Color(0.05f, 0.07f, 0.10f));
            CreateCube(root, "FireEscape_A", new Vector3(4.65f, 4.2f, 2f), new Vector3(0.15f, 0.15f, 4f), new Color(0.06f, 0.06f, 0.07f));
            CreateCube(root, "FireEscape_B", new Vector3(4.65f, 3.4f, 2f), new Vector3(0.15f, 0.15f, 4f), new Color(0.06f, 0.06f, 0.07f));
            CreateCube(root, "FireEscape_C", new Vector3(4.65f, 3.8f, 0.2f), new Vector3(0.15f, 1.6f, 0.15f), new Color(0.06f, 0.06f, 0.07f));

            // P1-1：墙面管线（西墙）+ 空调外机 + 壁灯 + 点缀光，消灭死黑。
            CreatePipeRun(root, new Vector3(-4.55f, 1.6f, 5f), 8f, false);
            CreatePipeRun(root, new Vector3(-4.55f, 2.3f, -5.5f), 5f, false);
            CreateCube(root, "AcUnit_Alley_A", new Vector3(-4.42f, 2.4f, -5f), new Vector3(0.55f, 0.8f, 1.0f), new Color(0.35f, 0.36f, 0.40f));
            CreateCube(root, "AcUnit_Alley_B", new Vector3(-4.42f, 3.2f, 7f), new Vector3(0.55f, 0.7f, 0.9f), new Color(0.34f, 0.35f, 0.39f));
            CreateGlowCube(root, "WallLamp_A", new Vector3(-4.66f, 2.7f, 4f), new Vector3(0.1f, 0.35f, 0.6f), new Color(1f, 0.82f, 0.55f));
            CreateGlowCube(root, "WallLamp_B", new Vector3(4.66f, 2.7f, -4f), new Vector3(0.1f, 0.35f, 0.6f), new Color(1f, 0.82f, 0.55f));
            CreateAccentLight(root, new Color(0.55f, 0.65f, 0.85f), new Vector3(-1.5f, 3.2f, -5f));
            CreateAccentLight(root, new Color(0.55f, 0.65f, 0.85f), new Vector3(1.5f, 3.2f, 6f));
            // 入口门牌：东墙门洞上方，字面朝巷内（-x）。
            CreateSignBoard(root, "AlleyNameSign", new Vector3(4.68f, 3.2f, 3.4f), new Vector3(0.08f, 0.7f, 1.8f), new Color(0.14f, 0.15f, 0.19f), "第7巷", new Color(0.95f, 0.85f, 0.55f), 90f, 5f, 0.55f);

            CreateClue(root, "Badge", new Vector3(-1.6f, 0.10f, -7.6f), new Vector3(0.4f, 0.08f, 0.4f), new Color(0.85f, 0.72f, 0.30f), DetectiveClueIds.Badge, "警徽", null, true, markerStyle: TestInteractable.MarkerStyle.GlowRing);
            CreateClue(root, "BloodyNote", new Vector3(-0.6f, 0.04f, -8.4f), new Vector3(0.4f, 0.04f, 0.4f), new Color(0.85f, 0.30f, 0.30f), DetectiveClueIds.BloodyNote, "染血纸条", null, true, markerStyle: TestInteractable.MarkerStyle.GlowRing);

            CreateDoor(root, "Exit_East", new Vector3(4.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "前往：红灯区", "D1_RedLight", "from_D0_Alley");
            CreateSpawn(root, "default", new Vector3(0f, 1f, -10f));
            CreateSpawn(root, "from_D1_RedLight", new Vector3(3.5f, 1f, 0f));
        }

        private static void BuildRedLight(Transform root)
        {
            BuildGroundAndWalls(root, 44f, 18f, 9f, new[] { (side: 'N', center: 8f, gap: 5f), (side: 'E', center: 0f, gap: 5f), (side: 'W', center: 0f, gap: 4f) }, new Color(0.09f, 0.10f, 0.13f));
            CreateRegionLight(root, new Color(0.13f, 0.83f, 0.93f), 140f, 44f, 7f);
            CreateAccentLight(root, new Color(0.88f, 0.31f, 0.79f), new Vector3(-10f, 6f, 4f));
            CreateAccentLight(root, new Color(0.88f, 0.31f, 0.79f), new Vector3(8f, 4f, 5.5f));
            CreateAccentLight(root, new Color(0.13f, 0.83f, 0.93f), new Vector3(-8f, 3.5f, -6f));

            CreateCube(root, "Facade_N1", new Vector3(-14f, 4f, 11.5f), new Vector3(10f, 8f, 4f), new Color(0.12f, 0.12f, 0.16f));
            CreateCube(root, "Facade_N2", new Vector3(-2f, 5f, 11.5f), new Vector3(9f, 10f, 4f), new Color(0.14f, 0.13f, 0.17f));
            CreateCube(root, "Facade_N3", new Vector3(17f, 3.5f, 11.5f), new Vector3(10f, 7f, 4f), new Color(0.11f, 0.11f, 0.15f));
            CreateCube(root, "Facade_S1", new Vector3(-12f, 3.5f, -11.5f), new Vector3(12f, 7f, 4f), new Color(0.12f, 0.12f, 0.15f));
            CreateCube(root, "Facade_S2", new Vector3(6f, 4.5f, -11.5f), new Vector3(10f, 9f, 4f), new Color(0.13f, 0.12f, 0.16f));
            CreateCube(root, "Facade_S3", new Vector3(18f, 3f, -11.5f), new Vector3(7f, 6f, 4f), new Color(0.11f, 0.11f, 0.14f));

            // P1-1：霓虹全部贴到墙内侧可见位置（墙内侧面 z=±8.75），另加两块垂直墙面的立式挑招。
            CreateNeonSign(root, "Neon_A", new Vector3(-14f, 5.5f, 8.6f), new Vector3(4f, 1.4f, 0.2f), new Color(0.88f, 0.31f, 0.79f));
            CreateNeonSign(root, "Neon_B", new Vector3(-2f, 7.5f, 8.6f), new Vector3(3f, 1.1f, 0.2f), new Color(0.13f, 0.83f, 0.93f));
            CreateNeonSign(root, "Neon_C", new Vector3(17f, 4.5f, 8.6f), new Vector3(5f, 1.6f, 0.2f), new Color(0.95f, 0.45f, 0.70f));
            CreateNeonSign(root, "Neon_D", new Vector3(-12f, 4.5f, -8.6f), new Vector3(4f, 1.2f, 0.2f), new Color(0.13f, 0.83f, 0.93f));
            CreateNeonSign(root, "Neon_E", new Vector3(6f, 6f, -8.6f), new Vector3(3.5f, 1.2f, 0.2f), new Color(0.88f, 0.31f, 0.79f));
            CreateGlowCube(root, "Neon_Blade_A", new Vector3(-7f, 5.2f, 7.3f), new Vector3(0.18f, 1.7f, 2.6f), new Color(0.95f, 0.45f, 0.70f));
            CreateGlowCube(root, "Neon_Blade_B", new Vector3(13f, 5.6f, -7.3f), new Vector3(0.18f, 1.7f, 2.6f), new Color(0.13f, 0.83f, 0.93f));

            // 酒吧门脸：门上方 BAR 发光招牌 + 门口两侧窗光条 + 灯笼 + 营业中挂牌 + 酒吧门牌。
            CreateSignBoard(root, "BarSign", new Vector3(8f, 3.75f, 8.66f), new Vector3(3.4f, 1.0f, 0.16f), new Color(0.16f, 0.10f, 0.13f), "酒吧", new Color(1f, 0.90f, 0.45f), 0f, 5f, 0.55f);
            CreateGlowCube(root, "BarWindowLight_A", new Vector3(6.55f, 1.8f, 8.44f), new Vector3(0.28f, 1.4f, 0.12f), new Color(1f, 0.75f, 0.40f));
            CreateGlowCube(root, "BarWindowLight_B", new Vector3(9.45f, 1.8f, 8.44f), new Vector3(0.28f, 1.4f, 0.12f), new Color(1f, 0.75f, 0.40f));
            CreateGlowCube(root, "BarLantern_A", new Vector3(6.3f, 2.55f, 8.35f), new Vector3(0.3f, 0.4f, 0.3f), new Color(0.95f, 0.25f, 0.20f));
            CreateGlowCube(root, "BarLantern_B", new Vector3(9.7f, 2.55f, 8.35f), new Vector3(0.3f, 0.4f, 0.3f), new Color(0.95f, 0.25f, 0.20f));
            CreateSignBoard(root, "OpenSign", new Vector3(10.9f, 2.0f, 8.66f), new Vector3(1.3f, 0.62f, 0.1f), new Color(0.10f, 0.14f, 0.11f), "营业中", new Color(0.45f, 1f, 0.55f), 0f, 5f, 0.45f);
            CreateDoorPlate(root, new Vector3(11.9f, 2.05f, 8.66f), "樱花町 7 号", "门牌：樱花町 7 号。酒吧隔壁，招牌熄了一半。", new Color(0.16f, 0.14f, 0.20f));

            CreateCube(root, "Puddle_A", new Vector3(-6f, 0.02f, 2f), new Vector3(2.2f, 0.02f, 1.4f), new Color(0.05f, 0.06f, 0.09f));
            CreateCube(root, "Puddle_B", new Vector3(8f, 0.02f, -3f), new Vector3(1.6f, 0.02f, 1.1f), new Color(0.05f, 0.06f, 0.09f));
            CreateStreetLamp(root, new Vector3(2f, 0f, -5f));
            CreateStreetLamp(root, new Vector3(-12f, 0f, 4f));
            CreateStreetLamp(root, new Vector3(14f, 0f, 4f));

            // P1-3：对话完成后由 DetectiveNpcWalker 走向街角东侧再消失（不再用 gate 瞬隐）。
            CreateNpcGate(root, "MysteriousWoman", new Vector3(-17f, 1f, 1f), new Color(0.75f, 0.75f, 0.85f), "dlg_mysterious_woman", "NPC_MysteriousWoman", null, null, "dlg_mysterious_woman_done", new Vector3(20f, 0f, 5f));
            var womanTrigger = CreateProximityTrigger(root, "WomanProximity", new Vector3(-17f, 1.5f, 1f), new Vector3(3f, 3f, 3f), "dlg_mysterious_woman");
            AddTriggerRing(womanTrigger, new Color(0.13f, 0.83f, 0.93f), "dlg_mysterious_woman_done");

            CreateCube(root, "PhoneBooth", new Vector3(-13f, 1.3f, 6.8f), new Vector3(1.2f, 2.6f, 1.2f), new Color(0.85f, 0.15f, 0.12f))
                .GetComponent<MeshRenderer>().sharedMaterial = GetUnlitMaterial(new Color(0.85f, 0.15f, 0.12f));
            CreateSignBoard(root, "PhoneBoothSign", new Vector3(-13f, 2.95f, 6.8f), new Vector3(1.6f, 0.5f, 0.1f), new Color(0.10f, 0.10f, 0.12f), "电话亭", new Color(1f, 0.85f, 0.45f), 0f, 5f, 0.55f);
            var boothTrigger = CreateProximityTrigger(root, "PhoneBoothProximity", new Vector3(-13f, 1.5f, 6.8f), new Vector3(2f, 3f, 2f), "dlg_phone_booth");
            SetStringField(boothTrigger.GetComponent<DetectiveProximityDialogue>(), "requiredFlag", "dlg_mysterious_woman_done");
            boothTrigger.AddComponent<DetectivePhoneRing>();
            AddTriggerRing(boothTrigger, new Color(0.13f, 0.83f, 0.93f), "dlg_phone_booth_done");

            CreateDoor(root, "BarDoor", new Vector3(8f, 1.5f, 8.75f), new Vector3(3.5f, 3f, 0.5f), "进入：酒吧", "I1_Bar", "default", skipLabel: true);
            CreateDoor(root, "Exit_East", new Vector3(21.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "前往：金融街", "D2_Financial", "default");
            CreateDoor(root, "Exit_West", new Vector3(-21.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "返回：小巷", "D0_Alley", "from_D1_RedLight");

            CreateSpawn(root, "default", new Vector3(-14f, 1f, 0f));
            CreateSpawn(root, "from_D0_Alley", new Vector3(-14f, 1f, 0f));
            CreateSpawn(root, "bar", new Vector3(8f, 1f, 6f));
            CreateSpawn(root, "from_I1_Bar", new Vector3(8f, 1f, 6f));
            CreateSpawn(root, "from_D2_Financial", new Vector3(19f, 1f, 0f));
        }

        private static void BuildBar(Transform root)
        {
            BuildGroundAndWalls(root, 14f, 9f, 3.2f, new[] { (side: 'S', center: 0f, gap: 2.6f) }, new Color(0.16f, 0.12f, 0.10f), 0.5f);
            CreateRegionLight(root, new Color(1f, 0.70f, 0.40f), 50f, 18f, 2.8f);
            CreateAccentLight(root, new Color(1f, 0.80f, 0.50f), new Vector3(3f, 2.4f, 0f));

            // 顾客从南门进入：吧台正面朝南，短边收在右侧，酒保站在吧台与北墙酒架之间。
            CreateCube(root, "Counter_A", new Vector3(-2.5f, 0.55f, 2.45f), new Vector3(5.5f, 1.1f, 1f), new Color(0.30f, 0.20f, 0.14f));
            CreateCube(root, "Counter_B", new Vector3(0.5f, 0.55f, 1.35f), new Vector3(1f, 1.1f, 2.8f), new Color(0.30f, 0.20f, 0.14f));
            CreateProp(root, "Stool_A", new Vector3(-4.8f, 0.3f, 1.35f), 0.28f, 0.6f, new Color(0.40f, 0.26f, 0.18f));
            CreateProp(root, "Stool_B", new Vector3(-3.7f, 0.3f, 1.35f), 0.28f, 0.6f, new Color(0.40f, 0.26f, 0.18f));
            CreateProp(root, "Stool_C", new Vector3(-1.2f, 0.3f, 1.35f), 0.28f, 0.6f, new Color(0.40f, 0.26f, 0.18f));
            CreateProp(root, "Stool_D", new Vector3(-0.1f, 0.3f, 1.35f), 0.28f, 0.6f, new Color(0.40f, 0.26f, 0.18f));
            CreateProp(root, "Table_A", new Vector3(2.8f, 0.38f, 0.8f), 0.75f, 0.75f, new Color(0.34f, 0.22f, 0.16f));
            CreateProp(root, "Table_B", new Vector3(4.6f, 0.38f, -1.8f), 0.75f, 0.75f, new Color(0.34f, 0.22f, 0.16f));
            CreateCube(root, "Chair_A", new Vector3(1.8f, 0.25f, 0.8f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.36f, 0.24f, 0.17f));
            CreateCube(root, "Chair_B", new Vector3(4.6f, 0.25f, -2.8f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.36f, 0.24f, 0.17f));

            CreateCube(root, "BarShelf", new Vector3(-2.5f, 2.2f, 4.3f), new Vector3(6f, 0.12f, 0.4f), new Color(0.24f, 0.16f, 0.12f));
            for (int i = 0; i < 6; i++)
            {
                CreateNeonSign(root, $"Bottle_{i}", new Vector3(-4.8f + i * 0.9f, 2.55f, 4.3f), new Vector3(0.14f, 0.5f, 0.14f), new Color(1f, 0.75f, 0.35f));
            }

            CreateNpcGate(root, "Bartender", new Vector3(-3f, 1f, 3.6f), new Color(0.65f, 0.55f, 0.45f), "dlg_bartender", "NPC_Bartender", null, null);
            var bartenderTrigger = CreateTrigger(root, "BartenderProximity", new Vector3(-3f, 1.5f, 0.85f), new Vector3(3f, 3f, 2f));
            var bartenderProximity = bartenderTrigger.AddComponent<DetectiveProximityDialogue>();
            var bartenderSo = new SerializedObject(bartenderProximity);
            bartenderSo.FindProperty("dialogueDefinition").objectReferenceValue = LoadAsset<DetectiveDialogueDefinition>("Assets/Data/Dialogues/dlg_bartender.asset");
            bartenderSo.FindProperty("onceOnly").boolValue = true;
            bartenderSo.ApplyModifiedPropertiesWithoutUndo();

            // P1-1：内侧店招字 + 第二排酒架装饰 + 卡座区点缀光。
            CreateSignBoard(root, "BarInnerSign", new Vector3(-2.5f, 2.95f, 4.32f), new Vector3(2.6f, 0.7f, 0.12f), new Color(0.14f, 0.09f, 0.11f), "BAR", new Color(1f, 0.55f, 0.35f), 0f, 5f, 0.55f);
            CreateCube(root, "BarShelf_Lower", new Vector3(-2.5f, 1.55f, 4.32f), new Vector3(6f, 0.1f, 0.35f), new Color(0.24f, 0.16f, 0.12f));
            for (int i = 0; i < 6; i++)
            {
                CreateNeonSign(root, $"Bottle_Lower_{i}", new Vector3(-4.8f + i * 0.9f, 1.85f, 4.32f), new Vector3(0.14f, 0.4f, 0.14f), new Color(0.90f, 0.65f, 0.30f));
            }

            CreateAccentLight(root, new Color(1f, 0.80f, 0.50f), new Vector3(-2f, 2.3f, -1.5f));

            CreateDoor(root, "ExitDoor", new Vector3(0f, 1.5f, -4.25f), new Vector3(2.4f, 3f, 0.5f), "回到街道：红灯区", "D1_RedLight", "bar");
            CreateSpawn(root, "default", new Vector3(0f, 1f, -3f));
            CreateSpawn(root, "from_D1_RedLight", new Vector3(1.5f, 1f, -2f));
        }

        private static void BuildFinancial(Transform root)
        {
            BuildGroundAndWalls(root, 50f, 24f, 10f, new[] { (side: 'S', center: 10f, gap: 5f), (side: 'W', center: 0f, gap: 5f), (side: 'E', center: 0f, gap: 5f) }, new Color(0.16f, 0.18f, 0.21f));
            CreateRegionLight(root, new Color(0.81f, 0.89f, 0.94f), 150f, 46f, 8f);
            CreateAccentLight(root, new Color(0.45f, 0.62f, 1.00f), new Vector3(0f, 4f, 4f));
            CreateAccentLight(root, new Color(0.45f, 0.62f, 1.00f), new Vector3(-14f, 4f, -4f));

            CreateCube(root, "ApartmentTower", new Vector3(-8f, 10f, 9f), new Vector3(14f, 20f, 6f), new Color(0.24f, 0.28f, 0.33f));
            CreateCube(root, "TowerCanopy", new Vector3(-8f, 3.1f, 5.6f), new Vector3(6f, 0.3f, 2.2f), new Color(0.30f, 0.34f, 0.39f));
            CreateCube(root, "PoliceLineBar", new Vector3(-8f, 0.9f, 3.6f), new Vector3(7f, 0.08f, 0.08f), new Color(0.90f, 0.80f, 0.15f));
            CreatePoliceLineBlock(root, new Vector3(-8f, 0.5f, 3.6f), new Vector3(7.2f, 1f, 0.3f));
            CreateProp(root, "PoliceLinePost_A", new Vector3(-11.3f, 0.5f, 3.6f), 0.06f, 1f, new Color(0.70f, 0.70f, 0.72f));
            CreateProp(root, "PoliceLinePost_B", new Vector3(-4.7f, 0.5f, 3.6f), 0.06f, 1f, new Color(0.70f, 0.70f, 0.72f));

            var beggar = CreateNpcGate(root, "Beggar", new Vector3(1.2f, 1f, 3.5f), new Color(0.45f, 0.40f, 0.35f), "dlg_beggar", "NPC_Beggar", null, null);
            SetNpcDialogueStages(beggar, "unlock_beggar", "dlg_beggar_waiting", null, null);
            // P1-1：乞丐身边纸板 + 讨饭碗。
            CreateCube(root, "BeggarCardboard", new Vector3(0.7f, 0.03f, 4.2f), new Vector3(0.9f, 0.03f, 0.6f), new Color(0.45f, 0.38f, 0.26f));
            CreateProp(root, "BeggarBowl", new Vector3(1.7f, 0.08f, 3.8f), 0.18f, 0.14f, new Color(0.55f, 0.55f, 0.58f));

            // P1-1：广场中央喷泉（石质基座 + 水面 + 中心柱 + 顶球）。
            CreateProp(root, "FountainBase", new Vector3(6f, 0.25f, 3f), 1.9f, 0.5f, new Color(0.42f, 0.45f, 0.50f));
            CreateProp(root, "FountainWater", new Vector3(6f, 0.45f, 3f), 1.5f, 0.5f, new Color(0.22f, 0.38f, 0.52f));
            CreateProp(root, "FountainPillar", new Vector3(6f, 1.1f, 3f), 0.25f, 1.3f, new Color(0.45f, 0.48f, 0.53f));
            var fountainTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fountainTop.name = "FountainTop";
            fountainTop.transform.SetParent(root, false);
            fountainTop.transform.position = new Vector3(6f, 2.05f, 3f);
            fountainTop.transform.localScale = Vector3.one * 0.55f;
            fountainTop.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(new Color(0.50f, 0.54f, 0.60f));

            // P1-1：长椅 + 花坛 + 铺装分色条带 + 斑马线 + 楼入口门灯/雨棚。
            CreateCube(root, "Bench_Plaza_A", new Vector3(3.2f, 0.25f, 5.2f), new Vector3(1.8f, 0.5f, 0.55f), new Color(0.38f, 0.30f, 0.22f));
            CreateCube(root, "Bench_Plaza_B", new Vector3(9.2f, 0.25f, 5.0f), new Vector3(1.8f, 0.5f, 0.55f), new Color(0.38f, 0.30f, 0.22f));
            CreateCube(root, "Planter_D", new Vector3(-2f, 0.25f, 5f), new Vector3(1f, 0.5f, 1f), new Color(0.30f, 0.24f, 0.18f));
            CreateProp(root, "Tree_D", new Vector3(-2f, 1.3f, 5f), 0.45f, 1.8f, new Color(0.16f, 0.34f, 0.18f));
            CreateCube(root, "Planter_E", new Vector3(14f, 0.25f, 4f), new Vector3(1f, 0.5f, 1f), new Color(0.30f, 0.24f, 0.18f));
            CreateProp(root, "Tree_E", new Vector3(14f, 1.3f, 4f), 0.45f, 1.8f, new Color(0.16f, 0.34f, 0.18f));
            CreateCube(root, "Paving_N", new Vector3(0f, 0.012f, -3f), new Vector3(34f, 0.015f, 0.5f), new Color(0.20f, 0.22f, 0.25f));
            CreateCube(root, "Paving_S", new Vector3(0f, 0.012f, 7.5f), new Vector3(30f, 0.015f, 0.4f), new Color(0.20f, 0.22f, 0.25f));
            CreateCube(root, "Crosswalk_A", new Vector3(10f, 0.014f, -6.2f), new Vector3(3.6f, 0.016f, 0.35f), new Color(0.55f, 0.57f, 0.60f));
            CreateCube(root, "Crosswalk_B", new Vector3(10f, 0.014f, -6.8f), new Vector3(3.6f, 0.016f, 0.35f), new Color(0.55f, 0.57f, 0.60f));
            CreateCube(root, "Crosswalk_C", new Vector3(10f, 0.014f, -7.4f), new Vector3(3.6f, 0.016f, 0.35f), new Color(0.55f, 0.57f, 0.60f));
            CreateGlowCube(root, "TowerDoorLamp_A", new Vector3(-10.05f, 2.3f, 5.65f), new Vector3(0.28f, 0.35f, 0.28f), new Color(1f, 0.88f, 0.60f));
            CreateGlowCube(root, "TowerDoorLamp_B", new Vector3(-5.95f, 2.3f, 5.65f), new Vector3(0.28f, 0.35f, 0.28f), new Color(1f, 0.88f, 0.60f));
            CreateGlowCube(root, "CoffeeDoorLamp_A", new Vector3(7.2f, 2.3f, -8.12f), new Vector3(0.28f, 0.35f, 0.28f), new Color(1f, 0.85f, 0.55f));
            CreateGlowCube(root, "CoffeeDoorLamp_B", new Vector3(12.8f, 2.3f, -8.12f), new Vector3(0.28f, 0.35f, 0.28f), new Color(1f, 0.85f, 0.55f));
            CreateCube(root, "CoffeeAwning", new Vector3(10f, 2.95f, -7.95f), new Vector3(3.8f, 0.12f, 1.1f), new Color(0.20f, 0.30f, 0.24f));

            CreateCube(root, "CoffeeShop", new Vector3(10f, 2f, -10f), new Vector3(10f, 4f, 3.5f), new Color(0.30f, 0.25f, 0.20f));
            CreateCube(root, "CoffeeGlass", new Vector3(7f, 1.6f, -8.14f), new Vector3(3.4f, 2.2f, 0.12f), new Color(0.55f, 0.70f, 0.78f));
            CreateStreetLamp(root, new Vector3(0f, 0f, 0f));
            CreateStreetLamp(root, new Vector3(-18f, 0f, -6f));
            CreateStreetLamp(root, new Vector3(18f, 0f, 6f));
            CreateCube(root, "TreePot_A", new Vector3(-2f, 0.25f, -8f), new Vector3(1f, 0.5f, 1f), new Color(0.30f, 0.24f, 0.18f));
            CreateProp(root, "Tree_A", new Vector3(-2f, 1.3f, -8f), 0.45f, 1.8f, new Color(0.16f, 0.34f, 0.18f));
            CreateCube(root, "TreePot_B", new Vector3(16f, 0.25f, 8f), new Vector3(1f, 0.5f, 1f), new Color(0.30f, 0.24f, 0.18f));
            CreateProp(root, "Tree_B", new Vector3(16f, 1.3f, 8f), 0.45f, 1.8f, new Color(0.16f, 0.34f, 0.18f));

            CreateDoor(root, "ApartmentDoor", new Vector3(-8f, 1.5f, 5.85f), new Vector3(3.5f, 3f, 0.4f), "进入：死者公寓", "I2_Apartment", "default");
            CreateDoor(root, "CoffeeDoor", new Vector3(10f, 1.5f, -8.05f), new Vector3(3.5f, 3f, 0.5f), "进入：咖啡店", "I2_Coffee", "default");
            CreateDoor(root, "Exit_West", new Vector3(-24.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "返回：红灯区", "D1_RedLight", "from_D2_Financial");
            CreateDoor(root, "Exit_East", new Vector3(24.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "前往：旧工业区", "D3_Industrial", "from_D2_Financial");

            var duelTrigger = CreateTrigger(root, "DuelGateTrigger", new Vector3(-8f, 1.5f, 2.5f), new Vector3(9f, 3f, 4f));
            var duel = duelTrigger.AddComponent<DetectiveFinalDuelTrigger>();
            SetObjectField(duel, "duelDefinition", LoadAsset<DetectiveDuelDefinition>("Assets/Data/Duels/duel_trenchcoat_gate.asset"));
            SetIntField(duel, "minClueCount", 4);
            SetStringField(duel, "requiredFlag", "left_I2_Apartment");
            SetStringField(duel, "arrivalFromRegion", "I2_Apartment");
            SetStringField(duel, "doneFlag", "duel_trenchcoat_triggered");
            SetStringField(duel, "degradedRequiredClueId", DetectiveClueIds.Recording);
            SetStringField(duel, "degradedEncounterFlag", "duel_gate_degraded_once");
            SetObjectField(duel, "degradedDialogue", LoadAsset<DetectiveDialogueDefinition>("Assets/Data/Dialogues/dlg_trenchcoat_warning.asset"));
            SetStringField(duel, "degradedMessage", "风衣男撑着黑伞拦住你：“你不该查下去的。”（专注力 -10，拿到录音再来。）");
            SetIntField(duel, "degradedFocusCost", 10);
            AddTriggerRing(duelTrigger, new Color(0.85f, 0.20f, 0.20f), "duel_trenchcoat_triggered", minCluesToShow: 4);

            // P1-3：风衣男站在警戒线前的对决触发区；对决胜利（获得"风衣男承认在场"线索）后向东出口逃走。
            // 注：duel_trenchcoat_gate 没有 OnWinSetFlags，胜利线索 clue_coatman_confessed 由 WinDuel 当场收集，据此触发离场。
            var trenchcoatMan = CreateNpcGate(root, "TrenchcoatMan", new Vector3(-8f, 1f, 2.2f), new Color(0.25f, 0.28f, 0.22f), null, "NPC_Trenchcoat", new[] { "left_I2_Apartment" }, null, null, new Vector3(23f, 0f, 0f), DetectiveClueIds.CoatManConfessed);
            var trenchcoatWalker = trenchcoatMan.GetComponent<DetectiveNpcWalker>();
            var trenchcoatSo = new SerializedObject(trenchcoatWalker);
            trenchcoatSo.FindProperty("walkAwayFlag").stringValue = "duel_gate_failed";
            trenchcoatSo.FindProperty("alternateWalkAwayFlag").stringValue = "duel_gate_degraded_once";
            trenchcoatSo.FindProperty("reviveAfterClueId").stringValue = DetectiveClueIds.Recording;
            trenchcoatSo.ApplyModifiedPropertiesWithoutUndo();

            CreateSpawn(root, "default", new Vector3(-16f, 1f, 0f));
            CreateSpawn(root, "from_D1_RedLight", new Vector3(-16f, 1f, 0f));
            CreateSpawn(root, "from_D3_Industrial", new Vector3(22f, 1f, 0f));
            CreateSpawn(root, "from_I2_Apartment", new Vector3(-8f, 1f, 5.1f));
            CreateSpawn(root, "from_I2_Coffee", new Vector3(10f, 1f, -6.2f));
            CreateSpawn(root, "apartment", new Vector3(-8f, 1f, 5.1f));
            CreateSpawn(root, "coffee", new Vector3(10f, 1f, -6.2f));
        }

        private static void BuildApartment(Transform root)
        {
            BuildGroundAndWalls(root, 18f, 12f, 3f, new[] { (side: 'S', center: 0f, gap: 2.6f) }, new Color(0.22f, 0.23f, 0.27f), 0.5f);
            CreateRegionLight(root, new Color(0.85f, 0.90f, 0.95f), 60f, 22f, 2.8f);
            CreateAccentLight(root, new Color(0.75f, 0.80f, 0.90f), new Vector3(0f, 2.5f, 3f));
            CreateAccentLight(root, new Color(0.70f, 0.80f, 1.00f), new Vector3(-1f, 2.4f, 1.5f));
            CreateAccentLight(root, new Color(1.00f, 0.85f, 0.60f), new Vector3(5.5f, 2.3f, 2.5f));

            // P1-1：南墙窗户冷光色块。
            CreateGlowCube(root, "WindowGlow_A", new Vector3(-2.5f, 1.9f, -5.66f), new Vector3(1.0f, 1.3f, 0.08f), new Color(0.55f, 0.70f, 0.90f));
            CreateGlowCube(root, "WindowGlow_B", new Vector3(3.5f, 1.9f, -5.66f), new Vector3(1.0f, 1.3f, 0.08f), new Color(0.50f, 0.65f, 0.88f));
            CreateGlowCube(root, "WindowGlow_C", new Vector3(7f, 1.9f, -5.66f), new Vector3(1.0f, 1.3f, 0.08f), new Color(0.42f, 0.55f, 0.80f));

            CreateCube(root, "InnerWall_A", new Vector3(-5.6f, 1.5f, -0.5f), new Vector3(6.8f, 3f, 0.3f), new Color(0.30f, 0.31f, 0.36f));
            CreateCube(root, "InnerWall_B", new Vector3(4.6f, 1.5f, -0.5f), new Vector3(8.8f, 3f, 0.3f), new Color(0.30f, 0.31f, 0.36f));
            CreateCube(root, "BedroomWall_A", new Vector3(-6f, 1.5f, 3.5f), new Vector3(6f, 3f, 0.3f), new Color(0.30f, 0.31f, 0.36f));
            CreateCube(root, "BedroomWall_B", new Vector3(8f, 1.5f, 3.5f), new Vector3(2f, 3f, 0.3f), new Color(0.30f, 0.31f, 0.36f));

            CreateClue(root, "DoorPanel", new Vector3(-6.5f, 1.3f, -5.6f), new Vector3(0.4f, 0.7f, 0.15f), new Color(0.30f, 0.60f, 0.90f), DetectiveClueIds.DoorRecord, "门禁系统", null, false);
            var guard = CreateNpcGate(root, "Guard", new Vector3(-3f, 1f, -3.5f), new Color(0.30f, 0.40f, 0.60f), "dlg_guard", "NPC_Guard", null, null);
            SetNpcDialogueStages(guard, null, null, "dlg_guard_done", "dlg_guard_decision");

            CreateCube(root, "Rug", new Vector3(0f, 0.02f, 1.6f), new Vector3(4.2f, 0.02f, 3f), new Color(0.32f, 0.16f, 0.16f));
            // P1-1：尸体位置白描轮廓（贴地白色细框，不阻挡导航）。
            CreateGlowCube(root, "CorpseOutline_N", new Vector3(-1.2f, 0.045f, 1.88f), new Vector3(2.3f, 0.02f, 0.08f), new Color(0.95f, 0.95f, 0.97f));
            CreateGlowCube(root, "CorpseOutline_S", new Vector3(-1.2f, 0.045f, 0.92f), new Vector3(2.3f, 0.02f, 0.08f), new Color(0.95f, 0.95f, 0.97f));
            CreateGlowCube(root, "CorpseOutline_W", new Vector3(-2.28f, 0.045f, 1.4f), new Vector3(0.08f, 0.02f, 1.04f), new Color(0.95f, 0.95f, 0.97f));
            CreateGlowCube(root, "CorpseOutline_E", new Vector3(-0.12f, 0.045f, 1.4f), new Vector3(0.08f, 0.02f, 1.04f), new Color(0.95f, 0.95f, 0.97f));
            CreateClue(root, "Corpse", new Vector3(-1.2f, 0.16f, 1.4f), new Vector3(1.8f, 0.32f, 0.8f), new Color(0.88f, 0.88f, 0.90f), DetectiveClueIds.CorpseChestWound, "尸体", "dlg_corpse", false);
            CreateClue(root, "BulletHole", new Vector3(-3f, 1.5f, -0.68f), new Vector3(0.2f, 0.2f, 0.1f), new Color(0.05f, 0.05f, 0.06f), DetectiveClueIds.BulletHole9mm, "墙上弹孔", null, false);
            CreateClue(root, "TakeoutBox", new Vector3(2.2f, 0.14f, 2.4f), new Vector3(0.45f, 0.3f, 0.45f), new Color(0.85f, 0.60f, 0.25f), DetectiveClueIds.TakeoutBox, "打翻的外卖", null, true, markerStyle: TestInteractable.MarkerStyle.GlowRing);
            CreateCube(root, "TeaTable", new Vector3(3.8f, 0.25f, 0.6f), new Vector3(1.2f, 0.5f, 0.7f), new Color(0.36f, 0.30f, 0.24f));
            CreateClue(root, "TornPhoto", new Vector3(3.8f, 0.54f, 0.6f), new Vector3(0.35f, 0.03f, 0.35f), new Color(0.90f, 0.88f, 0.80f), DetectiveClueIds.TornPhoto, "碎照片", null, true);
            CreateCube(root, "Sofa", new Vector3(5.5f, 0.45f, 2.8f), new Vector3(2.4f, 0.9f, 1f), new Color(0.30f, 0.32f, 0.40f));
            CreateCube(root, "TvCabinet", new Vector3(6.5f, 0.25f, 0.2f), new Vector3(1.6f, 0.5f, 0.4f), new Color(0.26f, 0.26f, 0.30f));

            CreateCube(root, "Bed", new Vector3(-5.5f, 0.3f, 4.9f), new Vector3(2.1f, 0.6f, 1.6f), new Color(0.36f, 0.38f, 0.46f));
            CreateCube(root, "Nightstand", new Vector3(-3.4f, 0.45f, 5.2f), new Vector3(0.7f, 0.9f, 0.7f), new Color(0.35f, 0.28f, 0.20f));
            CreateClue(root, "SleepingPills", new Vector3(-3.4f, 0.95f, 5.2f), new Vector3(0.22f, 0.26f, 0.22f), new Color(0.90f, 0.90f, 0.95f), DetectiveClueIds.SleepingPills, "安眠药", null, true);
            CreateCube(root, "Wardrobe", new Vector3(1.6f, 1.0f, 5.3f), new Vector3(1.6f, 2f, 0.8f), new Color(0.32f, 0.26f, 0.20f));
            CreateClue(root, "TrenchcoatItem", new Vector3(1.3f, 1.1f, 4.6f), new Vector3(0.55f, 1.1f, 0.28f), new Color(0.25f, 0.28f, 0.22f), DetectiveClueIds.TrenchcoatItem, "衣柜里的风衣", null, true);
            CreateCube(root, "Desk", new Vector3(2.8f, 0.35f, 5.2f), new Vector3(1.4f, 0.7f, 0.8f), new Color(0.38f, 0.30f, 0.22f));
            CreateClue(root, "DrawerDiary", new Vector3(2.8f, 0.82f, 5.2f), new Vector3(0.45f, 0.26f, 0.35f), new Color(0.85f, 0.80f, 0.65f), DetectiveClueIds.DiaryThreatened, "书桌抽屉（日记）", null, false, DetectiveVoiceType.Logic, 2, "dlg_drawer_diary_locked", "dlg_drawer_diary");

            var safe = CreateClue(root, "Safe", new Vector3(7.5f, 0.45f, 5f), new Vector3(0.9f, 0.9f, 0.8f), new Color(0.20f, 0.22f, 0.26f), DetectiveClueIds.Recording, "保险箱", null, false);
            var safeGate = safe.AddComponent<DetectiveFlagGatedObject>();
            SetObjectArrayField(safeGate, "requiredFlags", new[] { "dlg_drawer_diary_done" });

            CreateDoor(root, "ExitDoor", new Vector3(0f, 1.5f, -5.75f), new Vector3(2.4f, 3f, 0.5f), "回到街道：金融街", "D2_Financial", "apartment");
            CreateSpawn(root, "default", new Vector3(0f, 1f, -3f));
            CreateSpawn(root, "from_D2_Financial", new Vector3(1.5f, 1f, -3.5f));
        }

        private static void BuildCoffee(Transform root)
        {
            BuildGroundAndWalls(root, 12f, 9f, 3.2f, new[] { (side: 'S', center: -1f, gap: 5f), (side: 'S', center: 3.5f, gap: 2.6f) }, new Color(0.20f, 0.17f, 0.14f), 0.5f);
            CreateRegionLight(root, new Color(1f, 0.85f, 0.60f), 45f, 16f, 2.8f);
            CreateAccentLight(root, new Color(1f, 0.85f, 0.55f), new Vector3(-1.5f, 2.4f, -2.5f));
            CreateAccentLight(root, new Color(1f, 0.82f, 0.55f), new Vector3(2.5f, 2.3f, 0.5f));

            // 橱窗需透视室内陈设；灰盒实体玻璃只用于视觉，不挡点击或导航。
            var windowGlass = CreateCube(root, "WindowGlass", new Vector3(-1f, 1.6f, -4.15f), new Vector3(4.6f, 2.4f, 0.1f), new Color(0.55f, 0.70f, 0.78f));
            windowGlass.GetComponent<MeshRenderer>().sharedMaterial = GetWindowGlassMaterial();
            windowGlass.GetComponent<BoxCollider>().enabled = false;
            windowGlass.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;
            CreateCube(root, "MenuBoard", new Vector3(4.5f, 2f, -4.16f), new Vector3(1.8f, 1.2f, 0.08f), new Color(0.10f, 0.10f, 0.11f));
            // P1-1：菜单板写字（南墙板面朝室内 +z）+ 橱窗内展示柜（糕点）。
            AddBoardText(root, new Vector3(4.5f, 2f, -4.10f), Quaternion.Euler(0f, 180f, 0f), "菜单\n咖啡 12  拿铁 15\n蛋糕 18", new Color(0.92f, 0.88f, 0.75f), 5f, 0.45f);
            CreateCube(root, "DisplayShelf", new Vector3(-2.4f, 1.0f, -3.95f), new Vector3(3.2f, 0.08f, 0.5f), new Color(0.46f, 0.38f, 0.30f));
            CreateProp(root, "DisplayPlate_A", new Vector3(-3.4f, 1.06f, -3.95f), 0.16f, 0.04f, new Color(0.90f, 0.90f, 0.92f));
            CreateProp(root, "DisplayPlate_B", new Vector3(-2.5f, 1.06f, -3.95f), 0.16f, 0.04f, new Color(0.90f, 0.90f, 0.92f));
            CreateProp(root, "DisplayPlate_C", new Vector3(-1.6f, 1.06f, -3.95f), 0.16f, 0.04f, new Color(0.90f, 0.90f, 0.92f));
            CreateCube(root, "Cake_A", new Vector3(-3.4f, 1.17f, -3.95f), new Vector3(0.22f, 0.18f, 0.22f), new Color(0.85f, 0.60f, 0.40f));
            CreateCube(root, "Cake_B", new Vector3(-2.5f, 1.15f, -3.95f), new Vector3(0.2f, 0.14f, 0.2f), new Color(0.75f, 0.45f, 0.30f));
            CreateCube(root, "Cake_C", new Vector3(-1.6f, 1.16f, -3.95f), new Vector3(0.18f, 0.16f, 0.18f), new Color(0.90f, 0.75f, 0.50f));

            CreateCube(root, "Counter", new Vector3(-1.5f, 0.5f, -2.3f), new Vector3(4f, 1f, 0.9f), new Color(0.42f, 0.34f, 0.26f));
            CreateCube(root, "CoffeeMachine", new Vector3(-2.3f, 1.25f, -2.3f), new Vector3(0.7f, 0.5f, 0.6f), new Color(0.15f, 0.15f, 0.17f));
            CreateProp(root, "Cup", new Vector3(-1.2f, 1.12f, -2.2f), 0.09f, 0.22f, new Color(0.90f, 0.88f, 0.84f));
            CreateProp(root, "Table_A", new Vector3(2f, 0.38f, 0.6f), 0.55f, 0.75f, new Color(0.40f, 0.33f, 0.26f));
            CreateProp(root, "Table_B", new Vector3(3.8f, 0.38f, -1.2f), 0.55f, 0.75f, new Color(0.40f, 0.33f, 0.26f));
            CreateProp(root, "Table_C", new Vector3(1.2f, 0.38f, -1.8f), 0.55f, 0.75f, new Color(0.40f, 0.33f, 0.26f));
            CreateCube(root, "Chair_A", new Vector3(1.2f, 0.25f, -0.9f), new Vector3(0.45f, 0.5f, 0.45f), new Color(0.42f, 0.35f, 0.28f));
            CreateCube(root, "Chair_B", new Vector3(4.6f, 0.25f, -1.2f), new Vector3(0.45f, 0.5f, 0.45f), new Color(0.42f, 0.35f, 0.28f));

            CreateNpcGate(root, "Barista", new Vector3(0f, 1f, -3.4f), new Color(0.80f, 0.60f, 0.55f), "dlg_barista", "NPC_Barista", null, null);

            CreateDoor(root, "ExitDoor", new Vector3(3.5f, 1.5f, -4.25f), new Vector3(2.4f, 3f, 0.5f), "回到街道：金融街", "D2_Financial", "coffee");
            CreateSpawn(root, "default", new Vector3(0f, 1f, 1.5f));
            CreateSpawn(root, "from_D2_Financial", new Vector3(3f, 1f, -2f));
        }

        private static void BuildIndustrial(Transform root)
        {
            BuildGroundAndWalls(root, 48f, 30f, 2.6f, new[] { (side: 'S', center: -16f, gap: 5f), (side: 'W', center: 0f, gap: 5f), (side: 'E', center: -5f, gap: 5f) }, new Color(0.18f, 0.15f, 0.12f));
            CreateRegionLight(root, new Color(1.00f, 0.66f, 0.30f), 130f, 40f, 7f);
            CreateAccentLight(root, new Color(1.00f, 0.60f, 0.25f), new Vector3(-14f, 5f, 6f));
            CreateAccentLight(root, new Color(1.00f, 0.66f, 0.30f), new Vector3(14f, 5f, 2f));
            CreateAccentLight(root, new Color(1.00f, 0.75f, 0.30f), new Vector3(-16f, 3.5f, -10f));

            var gate = CreateClue(root, "IronGate", new Vector3(-16f, 1.2f, -14.5f), new Vector3(3.4f, 2.4f, 0.18f), new Color(0.35f, 0.30f, 0.25f), DetectiveClueIds.GateForced, "铁门", null, false);
            gate.transform.rotation = Quaternion.Euler(0f, 0f, 14f);
            CreateCube(root, "TireTrack_A", new Vector3(-13.5f, 0.02f, -8f), new Vector3(0.4f, 0.02f, 11f), new Color(0.08f, 0.07f, 0.06f));
            CreateCube(root, "TireTrack_B", new Vector3(-12.5f, 0.02f, -8f), new Vector3(0.4f, 0.02f, 11f), new Color(0.08f, 0.07f, 0.06f));
            // 涂鸦移到北墙西段入口视线内（玩家从西侧进入向东走，北墙在屏幕上侧、无遮挡），面朝场景内。
            CreateCube(root, "GraffitiWall", new Vector3(-2f, 1.25f, 14.55f), new Vector3(5f, 2.5f, 0.3f), new Color(0.30f, 0.28f, 0.26f));
            CreateClue(root, "Graffiti", new Vector3(-2f, 1.4f, 14.30f), new Vector3(3.2f, 1.2f, 0.08f), new Color(0.85f, 0.15f, 0.15f), DetectiveClueIds.GraffitiTraitor, "墙上涂鸦", "dlg_graffiti", false);

            CreateCube(root, "Warehouse", new Vector3(-14f, 3f, 8f), new Vector3(12f, 6f, 8f), new Color(0.26f, 0.24f, 0.20f));
            CreateDoor(root, "WarehouseDoor", new Vector3(-14f, 1.5f, 3.85f), new Vector3(3.5f, 3f, 0.4f), "进入：废弃仓库", "I3_Warehouse", "default");
            CreateCube(root, "Factory", new Vector3(12f, 4f, 6f), new Vector3(14f, 8f, 10f), new Color(0.24f, 0.22f, 0.20f));
            // 地下室入口是厂房前侧的地面检修门，与外部楼梯分开。
            CreateCube(root, "BasementEntryShed", new Vector3(3.5f, 1.35f, 3f), new Vector3(4.2f, 2.7f, 2.2f), new Color(0.30f, 0.27f, 0.23f));
            CreateDoor(root, "BasementDoor", new Vector3(3.5f, 1.3f, 1.85f), new Vector3(2.4f, 2.6f, 0.35f), "进入：地下室", "I3_Basement", "default");
            CreateCube(root, "BasementEntryLamp", new Vector3(3.5f, 2.75f, 1.72f), new Vector3(0.55f, 0.12f, 0.16f), new Color(0.85f, 0.72f, 0.38f));
            CreateCube(root, "StairPlatform", new Vector3(18.5f, 3.4f, 0f), new Vector3(3.2f, 0.3f, 3f), new Color(0.30f, 0.28f, 0.25f));
            for (int i = 0; i < 10; i++)
            {
                float stepHeight = 0.35f * (i + 1);
                CreateCube(root, $"StairStep_{i}", new Vector3(6.5f + i * 1.05f, stepHeight * 0.5f, 0f), new Vector3(1.15f, stepHeight, 2.6f), new Color(0.30f, 0.28f, 0.25f));
            }
            // 楼梯尽头接一座平台和楼梯间，玩家先走到平台再进入天台。
            CreateCube(root, "RooftopEntryPlatform", new Vector3(18.7f, 3.35f, 0f), new Vector3(4.5f, 0.28f, 3.4f), new Color(0.36f, 0.33f, 0.29f));
            CreateCube(root, "RooftopEntryHut", new Vector3(20.3f, 4.7f, 0f), new Vector3(2.0f, 2.7f, 3.2f), new Color(0.27f, 0.26f, 0.25f));
            CreateDoor(root, "RooftopDoor", new Vector3(19.15f, 4.6f, 0f), new Vector3(0.35f, 2.4f, 2.2f), "登上：天台", "I3_Rooftop", "default");

            CreateCube(root, "Container_A", new Vector3(-4f, 1.3f, -4f), new Vector3(6f, 2.6f, 2.4f), new Color(0.35f, 0.22f, 0.16f));
            CreateCube(root, "Container_B", new Vector3(2f, 1.3f, -8f), new Vector3(5f, 2.6f, 2.4f), new Color(0.20f, 0.28f, 0.32f));
            // P1-1：黄黑警示牌（集装箱正面 + 厂房侧面）、卡车/叉车占位、烟囱、油桶堆、地面油渍反光。
            CreateSignBoard(root, "WarningSign_A", new Vector3(-4f, 1.7f, -2.74f), new Vector3(1.4f, 0.9f, 0.06f), new Color(0.90f, 0.75f, 0.10f), "危险", new Color(0.10f, 0.10f, 0.10f), 0f, 5f, 0.55f);
            CreateSignBoard(root, "WarningSign_B", new Vector3(4.94f, 1.8f, 6f), new Vector3(0.06f, 1.0f, 1.4f), new Color(0.90f, 0.75f, 0.10f), "危险", new Color(0.10f, 0.10f, 0.10f), 90f, 5f, 0.55f);
            CreateCube(root, "TruckCab", new Vector3(-9.6f, 0.9f, -10f), new Vector3(1.7f, 1.8f, 2.0f), new Color(0.50f, 0.35f, 0.15f));
            CreateCube(root, "TruckCargo", new Vector3(-7.3f, 1.25f, -10f), new Vector3(3.8f, 2.5f, 2.2f), new Color(0.35f, 0.38f, 0.42f));
            var truckWheelA = CreateProp(root, "TruckWheel_A", new Vector3(-9.6f, 0.45f, -8.95f), 0.45f, 0.35f, new Color(0.08f, 0.08f, 0.09f));
            truckWheelA.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var truckWheelB = CreateProp(root, "TruckWheel_B", new Vector3(-9.6f, 0.45f, -11.05f), 0.45f, 0.35f, new Color(0.08f, 0.08f, 0.09f));
            truckWheelB.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var truckWheelC = CreateProp(root, "TruckWheel_C", new Vector3(-6.4f, 0.45f, -8.95f), 0.45f, 0.35f, new Color(0.08f, 0.08f, 0.09f));
            truckWheelC.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var truckWheelD = CreateProp(root, "TruckWheel_D", new Vector3(-6.4f, 0.45f, -11.05f), 0.45f, 0.35f, new Color(0.08f, 0.08f, 0.09f));
            truckWheelD.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            CreateCube(root, "ForkliftBody", new Vector3(1.5f, 0.6f, 4f), new Vector3(1.3f, 1.0f, 0.9f), new Color(0.85f, 0.60f, 0.10f));
            CreateCube(root, "ForkliftMast", new Vector3(0.8f, 1.1f, 4f), new Vector3(0.14f, 2.2f, 0.7f), new Color(0.20f, 0.20f, 0.22f));
            CreateCube(root, "ForkliftFork_A", new Vector3(0.35f, 0.15f, 3.75f), new Vector3(0.9f, 0.1f, 0.14f), new Color(0.20f, 0.20f, 0.22f));
            CreateCube(root, "ForkliftFork_B", new Vector3(0.35f, 0.15f, 4.25f), new Vector3(0.9f, 0.1f, 0.14f), new Color(0.20f, 0.20f, 0.22f));
            CreateProp(root, "Chimney_A", new Vector3(9f, 10.5f, 8f), 0.55f, 5f, new Color(0.30f, 0.27f, 0.25f));
            CreateProp(root, "Chimney_B", new Vector3(15f, 10.5f, 5f), 0.55f, 5f, new Color(0.28f, 0.25f, 0.23f));
            CreateProp(root, "OilDrum_D", new Vector3(5.2f, 0.6f, -6.3f), 0.5f, 1.2f, new Color(0.30f, 0.26f, 0.20f));
            CreateProp(root, "OilDrum_E", new Vector3(6.1f, 0.6f, -7.0f), 0.5f, 1.2f, new Color(0.32f, 0.24f, 0.18f));
            CreateProp(root, "OilDrum_F", new Vector3(5.65f, 1.7f, -6.65f), 0.5f, 1.2f, new Color(0.28f, 0.28f, 0.30f));
            CreateGroundGlow(root, "OilStain_A", new Vector3(-7f, 0.02f, 5.5f), new Vector3(2.2f, 0.02f, 1.5f), new Color(0.05f, 0.04f, 0.03f, 0.55f));
            CreateGroundGlow(root, "OilStain_B", new Vector3(3f, 0.02f, -5f), new Vector3(1.6f, 0.02f, 1.1f), new Color(0.06f, 0.05f, 0.03f, 0.5f));
            // 铁门前地面黄黑警示条纹。
            for (int i = 0; i < 5; i++)
            {
                CreateCube(root, $"HazardStripe_{i}", new Vector3(-17.8f + i * 0.9f, 0.014f, -12.6f), new Vector3(0.5f, 0.016f, 2.4f), i % 2 == 0 ? new Color(0.85f, 0.70f, 0.10f) : new Color(0.10f, 0.10f, 0.11f));
            }
            CreateProp(root, "OilDrum_A", new Vector3(-8f, 0.6f, 6f), 0.5f, 1.2f, new Color(0.30f, 0.26f, 0.20f));
            CreateProp(root, "OilDrum_B", new Vector3(-9.2f, 0.6f, 7.1f), 0.5f, 1.2f, new Color(0.32f, 0.24f, 0.18f));

            CreateDoor(root, "Exit_West", new Vector3(-23.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "返回：金融街", "D2_Financial", "from_D3_Industrial");
            CreateDoor(root, "Exit_East", new Vector3(23.75f, 1.5f, -5f), new Vector3(0.5f, 3f, 4f), "前往：住宅区", "D4_Residential", "default");

            CreateSpawn(root, "default", new Vector3(-17f, 1f, 0f));
            CreateSpawn(root, "from_D2_Financial", new Vector3(-17f, 1f, 0f));
            CreateSpawn(root, "from_D4_Residential", new Vector3(20f, 1f, 0f));
            CreateSpawn(root, "warehouse", new Vector3(-14f, 1f, 1.5f));
            CreateSpawn(root, "from_I3_Warehouse", new Vector3(-14f, 1f, 1.5f));
            CreateSpawn(root, "basement", new Vector3(3.5f, 1f, 0f));
            CreateSpawn(root, "from_I3_Basement", new Vector3(3.5f, 1f, 0f));
            CreateSpawn(root, "rooftop", new Vector3(9.5f, 1f, -2.5f));
            CreateSpawn(root, "from_I3_Rooftop", new Vector3(9.5f, 1f, -2.5f));
        }

        private static void BuildWarehouse(Transform root)
        {
            BuildGroundAndWalls(root, 22f, 14f, 6f, new[] { (side: 'S', center: 0f, gap: 3.2f) }, new Color(0.16f, 0.15f, 0.13f), 0.5f);
            CreateRegionLight(root, new Color(1.00f, 0.75f, 0.45f), 55f, 20f, 5f);
            CreateAccentLight(root, new Color(1.00f, 0.80f, 0.55f), new Vector3(0f, 4.5f, 3f));

            // P1-1：吊灯阵列（吊线 + 发光灯罩，首尾两盏带点光）。
            float[] pendantXs = { -4f, 0f, 4f };
            for (int i = 0; i < pendantXs.Length; i++)
            {
                CreateCube(root, $"PendantCord_{i}", new Vector3(pendantXs[i], 5.5f, 0f), new Vector3(0.05f, 0.9f, 0.05f), new Color(0.10f, 0.10f, 0.11f));
                CreateGlowCube(root, $"PendantShade_{i}", new Vector3(pendantXs[i], 5.0f, 0f), new Vector3(0.55f, 0.18f, 0.55f), new Color(1f, 0.82f, 0.55f));
            }

            for (int i = 0; i < 2; i++)
            {
                var lampGo = new GameObject($"PendantLight_{i}");
                lampGo.transform.SetParent(root, false);
                lampGo.transform.position = new Vector3(i == 0 ? -4f : 4f, 4.6f, 0f);
                var lamp = lampGo.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.80f, 0.55f);
                lamp.intensity = 28f;
                lamp.range = 12f;
            }

            // P1-1：栈板堆 + 货架分区色条。
            CreateCube(root, "PalletStack_A", new Vector3(-2f, 0.07f, -5.5f), new Vector3(1.4f, 0.14f, 1.2f), new Color(0.40f, 0.32f, 0.22f));
            CreateCube(root, "PalletStack_B", new Vector3(-2f, 0.21f, -5.5f), new Vector3(1.4f, 0.14f, 1.2f), new Color(0.38f, 0.30f, 0.21f));
            CreateCube(root, "PalletStack_C", new Vector3(-2f, 0.35f, -5.5f), new Vector3(1.4f, 0.14f, 1.2f), new Color(0.42f, 0.34f, 0.23f));
            CreateCube(root, "PalletSack_A", new Vector3(-2.3f, 0.6f, -5.4f), new Vector3(0.7f, 0.5f, 0.5f), new Color(0.45f, 0.40f, 0.32f));
            CreateCube(root, "PalletSack_B", new Vector3(-1.6f, 0.55f, -5.7f), new Vector3(0.6f, 0.4f, 0.5f), new Color(0.42f, 0.38f, 0.30f));
            CreateGlowCube(root, "ShelfStrip_A1", new Vector3(6f, 2.9f, -4.16f), new Vector3(3.4f, 0.22f, 0.05f), new Color(0.90f, 0.80f, 0.15f));
            CreateGlowCube(root, "ShelfStrip_A2", new Vector3(6f, 1.8f, -4.16f), new Vector3(3.4f, 0.22f, 0.05f), new Color(0.25f, 0.55f, 0.90f));
            CreateGlowCube(root, "ShelfStrip_A3", new Vector3(6f, 0.7f, -4.16f), new Vector3(3.4f, 0.22f, 0.05f), new Color(0.85f, 0.30f, 0.25f));
            CreateGlowCube(root, "ShelfStrip_B1", new Vector3(7.66f, 2.9f, 2f), new Vector3(0.05f, 0.22f, 3.4f), new Color(0.90f, 0.80f, 0.15f));
            CreateGlowCube(root, "ShelfStrip_B2", new Vector3(7.66f, 1.8f, 2f), new Vector3(0.05f, 0.22f, 3.4f), new Color(0.25f, 0.55f, 0.90f));
            CreateGlowCube(root, "ShelfStrip_B3", new Vector3(7.66f, 0.7f, 2f), new Vector3(0.05f, 0.22f, 3.4f), new Color(0.85f, 0.30f, 0.25f));

            CreateCube(root, "CrateStack_A", new Vector3(-7f, 0.9f, -3f), new Vector3(1.6f, 1.8f, 1.6f), new Color(0.34f, 0.28f, 0.20f));
            CreateCube(root, "CrateStack_B", new Vector3(-5.4f, 0.6f, -4.2f), new Vector3(1.2f, 1.2f, 1.2f), new Color(0.36f, 0.30f, 0.22f));
            CreateCube(root, "CrateStack_C", new Vector3(-6.4f, 2.1f, -3.2f), new Vector3(1.1f, 0.9f, 1.1f), new Color(0.32f, 0.26f, 0.19f));
            CreateCube(root, "Shelf_A", new Vector3(6f, 1.8f, -4.5f), new Vector3(4f, 3.6f, 0.6f), new Color(0.28f, 0.28f, 0.30f));
            CreateCube(root, "Shelf_B", new Vector3(8f, 1.8f, 2f), new Vector3(0.6f, 3.6f, 4f), new Color(0.28f, 0.28f, 0.30f));
            CreateCube(root, "Crate_C", new Vector3(2f, 0.5f, 4.5f), new Vector3(1f, 1f, 1f), new Color(0.34f, 0.28f, 0.20f));

            var monkey = CreateNpcGate(root, "Monkey", new Vector3(-2.5f, 1f, 1f), new Color(0.60f, 0.58f, 0.50f), "dlg_monkey", "NPC_Monkey", null, null);
            SetNpcDialogueStages(monkey, null, null, "dlg_monkey_done", "dlg_monkey_decision");

            CreateDoor(root, "ExitDoor", new Vector3(0f, 1.5f, -6.75f), new Vector3(2.8f, 3f, 0.5f), "回到外场", "D3_Industrial", "warehouse");
            CreateSpawn(root, "default", new Vector3(0f, 1f, -4.5f));
            CreateSpawn(root, "from_D3_Industrial", new Vector3(1.5f, 1f, 1.5f));
        }

        private static void BuildBasement(Transform root)
        {
            BuildGroundAndWalls(root, 14f, 10f, 2.6f, new[] { (side: 'E', center: 0f, gap: 3.2f) }, new Color(0.15f, 0.14f, 0.13f), 0.5f);
            CreateRegionLight(root, new Color(0.80f, 0.88f, 0.95f), 35f, 16f, 2.3f);
            CreateAccentLight(root, new Color(0.70f, 0.80f, 0.95f), new Vector3(-3f, 1.9f, -2f));
            CreateAccentLight(root, new Color(0.65f, 0.75f, 0.90f), new Vector3(4f, 2.0f, 2f));

            var pipeA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipeA.name = "Pipe_A";
            pipeA.transform.SetParent(root, false);
            pipeA.transform.position = new Vector3(0f, 2.35f, -4.4f);
            pipeA.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            pipeA.transform.localScale = new Vector3(0.3f, 4.5f, 0.3f);
            pipeA.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(new Color(0.30f, 0.28f, 0.26f));
            var pipeB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipeB.name = "Pipe_B";
            pipeB.transform.SetParent(root, false);
            pipeB.transform.position = new Vector3(-6.4f, 2.2f, 0f);
            pipeB.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pipeB.transform.localScale = new Vector3(0.3f, 3.5f, 0.3f);
            pipeB.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(new Color(0.30f, 0.28f, 0.26f));
            CreateCube(root, "Blood_A", new Vector3(-3f, 0.02f, -1.5f), new Vector3(2.6f, 0.02f, 0.5f), new Color(0.45f, 0.08f, 0.08f));
            CreateCube(root, "Blood_B", new Vector3(-0.5f, 0.02f, -0.8f), new Vector3(1.6f, 0.02f, 0.4f), new Color(0.45f, 0.08f, 0.08f));
            CreateCube(root, "Blood_C", new Vector3(2f, 0.02f, -0.2f), new Vector3(1.2f, 0.02f, 0.35f), new Color(0.45f, 0.08f, 0.08f));
            // P1-1：阀门/压力表（贴管小圆盘）+ 墙面锈色条。
            var gaugeA = CreateProp(root, "Gauge_A", new Vector3(-3f, 1.95f, -4.32f), 0.18f, 0.06f, new Color(0.90f, 0.90f, 0.88f));
            gaugeA.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            CreateGlowCube(root, "GaugeNeedle_A", new Vector3(-3f, 1.90f, -4.38f), new Vector3(0.05f, 0.05f, 0.03f), new Color(0.80f, 0.20f, 0.15f));
            var gaugeB = CreateProp(root, "Gauge_B", new Vector3(6.68f, 1.7f, -1.5f), 0.2f, 0.06f, new Color(0.90f, 0.90f, 0.88f));
            gaugeB.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            var valveB = CreateProp(root, "Valve_B", new Vector3(2f, 2.1f, -4.32f), 0.2f, 0.1f, new Color(0.70f, 0.20f, 0.15f));
            valveB.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            CreateCube(root, "RustStrip_A", new Vector3(-4.5f, 1.3f, 4.70f), new Vector3(0.35f, 2.0f, 0.05f), new Color(0.35f, 0.20f, 0.12f));
            CreateCube(root, "RustStrip_B", new Vector3(5.8f, 1.1f, 4.70f), new Vector3(0.25f, 1.6f, 0.05f), new Color(0.32f, 0.18f, 0.11f));
            CreateCube(root, "RustStrip_C", new Vector3(-1f, 1.4f, -4.70f), new Vector3(0.3f, 1.8f, 0.05f), new Color(0.34f, 0.19f, 0.12f));
            CreateClue(root, "Toolbox", new Vector3(-4f, 0.25f, -3f), new Vector3(0.75f, 0.5f, 0.45f), new Color(0.55f, 0.35f, 0.15f), DetectiveClueIds.LockpickTool, "工具箱", null, true, markerStyle: TestInteractable.MarkerStyle.GlowRing);

            CreateCube(root, "PhotoFrame", new Vector3(2f, 1.6f, 4.65f), new Vector3(2.4f, 1.5f, 0.08f), new Color(0.30f, 0.24f, 0.16f));
            for (int i = 0; i < 4; i++)
            {
                CreateCube(root, $"Photo_{i}", new Vector3(1.2f + (i % 2) * 1.2f, 1.85f - (i / 2) * 0.7f, 4.38f), new Vector3(0.8f, 0.5f, 0.04f), new Color(0.85f, 0.82f, 0.70f));
            }

            CreateClue(root, "PhotoWall", new Vector3(2f, 1.6f, 4.5f), new Vector3(2.2f, 1.3f, 0.1f), new Color(0.85f, 0.82f, 0.70f), DetectiveClueIds.FormerPartners, "照片墙", null, false);
            CreateCube(root, "Desk", new Vector3(3.5f, 0.38f, 2f), new Vector3(2f, 0.75f, 1f), new Color(0.30f, 0.26f, 0.22f));
            CreateCube(root, "Monitor", new Vector3(3.5f, 1.15f, 2.3f), new Vector3(0.8f, 0.6f, 0.12f), new Color(0.10f, 0.10f, 0.12f));
            CreateNeonSign(root, "MonitorScreen", new Vector3(3.5f, 1.15f, 2.17f), new Vector3(0.68f, 0.48f, 0.04f), new Color(0.65f, 0.80f, 0.90f));
            CreateCube(root, "Keyboard", new Vector3(3.5f, 0.78f, 1.6f), new Vector3(0.7f, 0.05f, 0.3f), new Color(0.12f, 0.12f, 0.14f));
            CreateClue(root, "Computer", new Vector3(3.5f, 0.95f, 2f), new Vector3(1.1f, 0.4f, 0.8f), new Color(0.15f, 0.18f, 0.22f), DetectiveClueIds.WasUndercover, "电脑", null, false, DetectiveVoiceType.Logic, 3, "dlg_computer_locked", "dlg_computer");

            CreateDoor(root, "ExitDoor", new Vector3(6.75f, 1.3f, 0f), new Vector3(0.5f, 2.6f, 3f), "回到外场", "D3_Industrial", "basement");
            CreateSpawn(root, "default", new Vector3(-4f, 1f, 2f));
            CreateSpawn(root, "from_D3_Industrial", new Vector3(3f, 1f, 1f));
        }

        private static void BuildRooftop(Transform root)
        {
            BuildGroundAndWalls(root, 18f, 14f, 1.0f, new[] { (side: 'S', center: 0f, gap: 3f) }, new Color(0.20f, 0.21f, 0.26f));
            CreateRegionLight(root, new Color(0.50f, 0.60f, 0.80f), 90f, 32f, 6f);
            CreateAccentLight(root, new Color(0.55f, 0.65f, 0.90f), new Vector3(0f, 2.8f, -3f));
            CreateAccentLight(root, new Color(0.60f, 0.68f, 0.92f), new Vector3(-5f, 2.5f, 1f));

            // P1-1：楼顶大字广告牌（字面朝南，朝默认相机）+ 支撑柱。
            CreateCube(root, "BillboardPole_A", new Vector3(-2.2f, 1.65f, 5.6f), new Vector3(0.25f, 3.3f, 0.25f), new Color(0.24f, 0.24f, 0.28f));
            CreateCube(root, "BillboardPole_B", new Vector3(2.2f, 1.65f, 5.6f), new Vector3(0.25f, 3.3f, 0.25f), new Color(0.24f, 0.24f, 0.28f));
            CreateCube(root, "Billboard", new Vector3(0f, 4.6f, 5.6f), new Vector3(6.5f, 2.6f, 0.3f), new Color(0.10f, 0.10f, 0.13f));
            AddBoardText(root, new Vector3(0f, 4.6f, 5.41f), Quaternion.identity, "天恒集团", new Color(1f, 0.88f, 0.55f), 5f, 1.2f);
            CreateGlowCube(root, "BillboardEdge_A", new Vector3(-3.1f, 4.6f, 5.44f), new Vector3(0.12f, 2.6f, 0.06f), new Color(0.95f, 0.55f, 0.25f));
            CreateGlowCube(root, "BillboardEdge_B", new Vector3(3.1f, 4.6f, 5.44f), new Vector3(0.12f, 2.6f, 0.06f), new Color(0.95f, 0.55f, 0.25f));

            // P1-1：南墙顶铁丝网围栏段（矮栏杆柱 + 半透明网片）。
            for (int i = 0; i < 4; i++)
            {
                float fx = -7f + i * 2f;
                CreateCube(root, $"FencePost_{i}", new Vector3(fx, 1.35f, -6.72f), new Vector3(0.06f, 0.7f, 0.06f), new Color(0.30f, 0.30f, 0.34f));
                if (i < 3)
                {
                    CreateGroundGlow(root, $"FenceMesh_{i}", new Vector3(fx + 1f, 1.35f, -6.72f), new Vector3(1.94f, 0.5f, 0.03f), new Color(0.60f, 0.65f, 0.70f, 0.30f));
                }
            }

            // P1-1：停机坪 H 标记（贴地白色，不参与导航）。
            CreateGlowCube(root, "Helipad_N", new Vector3(0f, 0.025f, 2.25f), new Vector3(3.9f, 0.02f, 0.15f), new Color(0.92f, 0.93f, 0.95f));
            CreateGlowCube(root, "Helipad_S", new Vector3(0f, 0.025f, -1.25f), new Vector3(3.9f, 0.02f, 0.15f), new Color(0.92f, 0.93f, 0.95f));
            CreateGlowCube(root, "Helipad_W", new Vector3(-1.88f, 0.025f, 0.5f), new Vector3(0.15f, 0.02f, 3.65f), new Color(0.92f, 0.93f, 0.95f));
            CreateGlowCube(root, "Helipad_E", new Vector3(1.88f, 0.025f, 0.5f), new Vector3(0.15f, 0.02f, 3.65f), new Color(0.92f, 0.93f, 0.95f));
            CreateGlowCube(root, "HelipadH_L", new Vector3(-0.55f, 0.025f, 0.5f), new Vector3(0.2f, 0.02f, 1.6f), new Color(0.92f, 0.93f, 0.95f));
            CreateGlowCube(root, "HelipadH_R", new Vector3(0.55f, 0.025f, 0.5f), new Vector3(0.2f, 0.02f, 1.6f), new Color(0.92f, 0.93f, 0.95f));
            CreateGlowCube(root, "HelipadH_M", new Vector3(0f, 0.025f, 0.5f), new Vector3(1.3f, 0.02f, 0.2f), new Color(0.92f, 0.93f, 0.95f));

            CreateCube(root, "AcUnit_A", new Vector3(-4f, 0.4f, -2f), new Vector3(1f, 0.8f, 0.7f), new Color(0.38f, 0.39f, 0.43f));
            CreateCube(root, "AcUnit_B", new Vector3(5.5f, 0.4f, 3f), new Vector3(1f, 0.8f, 0.7f), new Color(0.38f, 0.39f, 0.43f));
            CreateProp(root, "WaterTank", new Vector3(-5.5f, 1.0f, 4f), 1.2f, 2f, new Color(0.35f, 0.34f, 0.36f));
            CreateCube(root, "StairHut", new Vector3(4.5f, 1.3f, -4.5f), new Vector3(3f, 2.6f, 2.5f), new Color(0.28f, 0.28f, 0.32f));
            CreateDoor(root, "ExitDoor", new Vector3(4.5f, 1.1f, -5.7f), new Vector3(1.8f, 2.2f, 0.3f), "回到外场", "D3_Industrial", "rooftop");

            // Skyline 移到北侧（+z）：默认相机在西南侧，南侧楼群永远在玩家背后不可见。
            CreateCube(root, "Skyline_A", new Vector3(-6f, 3f, 9.5f), new Vector3(5f, 6f, 2f), new Color(0.05f, 0.06f, 0.09f));
            CreateCube(root, "Skyline_B", new Vector3(1f, 4.5f, 11f), new Vector3(6f, 9f, 2f), new Color(0.04f, 0.05f, 0.08f));
            CreateCube(root, "Skyline_C", new Vector3(8f, 2.5f, 9.5f), new Vector3(4f, 5f, 2f), new Color(0.05f, 0.06f, 0.09f));

            // P1-3：对话完成后退到天台边缘（西南角围栏内侧）再消失。
            var sniper = CreateNpcGate(root, "Sniper", new Vector3(-7f, 1f, -4f), new Color(0.20f, 0.22f, 0.24f), "dlg_sniper", "NPC_Sniper", null, null, "dlg_sniper_done", new Vector3(-8f, 0f, -5.5f));
            CreateSniperSpotlight(root, sniper.transform.position);
            CreateSpawn(root, "default", new Vector3(0f, 1f, 2.5f));
            CreateSpawn(root, "from_D3_Industrial", new Vector3(2f, 1f, -2f));
        }

        private static void BuildResidential(Transform root)
        {
            BuildGroundAndWalls(root, 40f, 18f, 7f, new[] { (side: 'N', center: 6f, gap: 4f), (side: 'N', center: 14f, gap: 4f), (side: 'W', center: 0f, gap: 5f) }, new Color(0.20f, 0.18f, 0.14f));
            CreateRegionLight(root, new Color(1.00f, 0.85f, 0.56f), 125f, 42f, 6f);
            CreateAccentLight(root, new Color(1.00f, 0.85f, 0.50f), new Vector3(-6f, 3.5f, 3f));
            CreateAccentLight(root, new Color(1.00f, 0.82f, 0.50f), new Vector3(10f, 3.5f, -3f));

            // P1-1：北侧公寓楼立面窗格色块（暖黄亮窗与暗窗交错）+ 门框色块。
            float[] houseNX = { -14f, -2f, 6f };
            for (int i = 0; i < houseNX.Length; i++)
            {
                for (int floor = 0; floor < 2; floor++)
                {
                    for (int w = -1; w <= 1; w += 2)
                    {
                        bool lit = (i + floor + (w + 1) / 2) % 2 == 0;
                        CreateGlowCube(root, $"HouseN{i}Win_{floor}_{(w + 1) / 2}", new Vector3(houseNX[i] + w * 1.6f, 2.6f + floor * 1.7f, 9.46f), new Vector3(1.0f, 0.9f, 0.08f), lit ? new Color(0.95f, 0.85f, 0.55f) : new Color(0.16f, 0.18f, 0.24f));
                    }
                }
            }

            // P1-1：南侧房屋窗格。
            float[] houseSX = { -10f, -2f, 6f };
            for (int i = 0; i < houseSX.Length; i++)
            {
                bool lit = i % 2 == 0;
                CreateGlowCube(root, $"HouseS{i}Win_0", new Vector3(houseSX[i], 2.5f, -9.46f), new Vector3(1.1f, 0.9f, 0.08f), lit ? new Color(0.90f, 0.78f, 0.50f) : new Color(0.15f, 0.17f, 0.22f));
                CreateGlowCube(root, $"HouseS{i}Win_1", new Vector3(houseSX[i], 4.2f, -9.46f), new Vector3(1.1f, 0.9f, 0.08f), lit ? new Color(0.15f, 0.17f, 0.22f) : new Color(0.90f, 0.78f, 0.50f));
            }

            // P1-1：晾衣绳（两杆一绳 + 衣物色块）。
            CreateProp(root, "ClotheslinePole_A", new Vector3(-2f, 1.35f, 4f), 0.05f, 2.7f, new Color(0.30f, 0.29f, 0.30f));
            CreateProp(root, "ClotheslinePole_B", new Vector3(4f, 1.35f, 4f), 0.05f, 2.7f, new Color(0.30f, 0.29f, 0.30f));
            CreateCube(root, "ClotheslineWire", new Vector3(1f, 2.62f, 4f), new Vector3(6f, 0.03f, 0.03f), new Color(0.20f, 0.20f, 0.22f));
            CreateCube(root, "Cloth_A", new Vector3(-0.5f, 2.32f, 4f), new Vector3(0.5f, 0.55f, 0.04f), new Color(0.75f, 0.80f, 0.90f));
            CreateCube(root, "Cloth_B", new Vector3(0.8f, 2.28f, 4f), new Vector3(0.45f, 0.65f, 0.04f), new Color(0.90f, 0.65f, 0.70f));
            CreateCube(root, "Cloth_C", new Vector3(2.2f, 2.35f, 4f), new Vector3(0.5f, 0.5f, 0.04f), new Color(0.88f, 0.84f, 0.70f));

            // P1-1：绿化带矮绿篱（南侧，留出门店与通道缺口）。
            CreateCube(root, "Hedge_A", new Vector3(-8f, 0.3f, -7.5f), new Vector3(6f, 0.6f, 0.8f), new Color(0.14f, 0.28f, 0.14f));
            CreateCube(root, "Hedge_B", new Vector3(2f, 0.3f, -7.5f), new Vector3(6f, 0.6f, 0.8f), new Color(0.15f, 0.29f, 0.15f));
            CreateCube(root, "Hedge_C", new Vector3(16f, 0.3f, -7.5f), new Vector3(4f, 0.6f, 0.8f), new Color(0.14f, 0.28f, 0.14f));

            for (int i = 0; i < 3; i++)
            {
                CreateCube(root, $"HouseN_{i}", new Vector3(houseNX[i], 3f, 11.5f), new Vector3(7f, 6f, 4f), new Color(0.36f + i * 0.02f, 0.32f, 0.26f));
                CreateCube(root, $"HouseS_{i}", new Vector3(-10f + i * 8f, 3f, -11.5f), new Vector3(7f, 6f, 4f), new Color(0.34f, 0.30f + i * 0.02f, 0.25f));
            }

            CreateCube(root, "HouseN_3", new Vector3(13.5f, 3f, 11.5f), new Vector3(5f,6f,4f), new Color(0.38f,0.32f,0.26f));
            CreateCube(root, "HouseN_4", new Vector3(18f, 3f, 11.5f), new Vector3(4f,6f,4f), new Color(0.36f,0.32f,0.26f));
            CreateStreetLamp(root, new Vector3(-4f, 0f, 0f));
            CreateStreetLamp(root, new Vector3(8f, 0f, 3f));
            CreateStreetLamp(root, new Vector3(-14f, 0f, -3f));

            CreateCube(root, "Mailbox", new Vector3(-12f, 0.55f, -6f), new Vector3(0.5f, 1.1f, 0.5f), new Color(0.40f, 0.45f, 0.55f));
            CreateClue(root, "LetterBox", new Vector3(-12f, 1.0f, -6f), new Vector3(0.4f, 0.35f, 0.4f), new Color(0.75f, 0.72f, 0.60f), DetectiveClueIds.LetterWarning, "信箱", null, false);

            CreateProp(root, "StreetlampPole", new Vector3(-4f, 1.75f, 2f), 0.12f, 3.5f, new Color(0.25f, 0.25f, 0.28f));
            CreateCube(root, "StreetlampHead", new Vector3(-4f, 3.6f, 2f), new Vector3(0.6f, 0.25f, 0.6f), new Color(1f, 0.9f, 0.6f));
            CreateStreetlampScratches(root, new Vector3(-4f, 0f, 2f));
            CreateClue(root, "StreetlampMarks", new Vector3(-4f, 1.15f, 1.86f), new Vector3(0.2f, 0.45f, 0.05f), new Color(0.18f, 0.16f, 0.14f), DetectiveClueIds.StreetlampScratches, "路灯刻痕", null, true, DetectiveVoiceType.None, 0, null, null, new[] { new DetectiveCityBuilder.VoiceDelta(DetectiveVoiceType.Madness, 1) }, TestInteractable.MarkerStyle.GlowRing);

            CreateCube(root, "TrashBin", new Vector3(-16f, 0.5f, 3f), new Vector3(0.8f, 1f, 0.8f), new Color(0.22f, 0.26f, 0.24f));
            CreateClue(root, "StrayCat", new Vector3(-15f, 0.2f, 4.2f), new Vector3(0.35f, 0.4f, 0.6f), new Color(0.55f, 0.50f, 0.45f), DetectiveClueIds.CatCollar, "流浪猫", null, true, markerStyle: TestInteractable.MarkerStyle.GlowRing);

            CreateDoor(root, "YourHomeDoor", new Vector3(6f, 1.5f, 8.75f), new Vector3(2.6f, 3f, 0.5f), "NC-2077", "I4_YourHome", "default");
            CreateDoor(root, "VictimHomeDoor", new Vector3(14f, 1.5f, 8.75f), new Vector3(2.6f, 3f, 0.5f), "305", "I4_VictimHome", "default");
            // P1-4 找公寓玩法：两扇外观相同的假门（不可进入），与真门混排。
            CreateCube(root, "FakeDoor_A", new Vector3(-2f, 1.5f, 8.75f), new Vector3(2.6f, 3f, 0.5f), new Color(0.85f, 0.70f, 0.30f));
            CreateCube(root, "FakeDoor_B", new Vector3(17.5f, 1.5f, 8.75f), new Vector3(2.6f, 3f, 0.5f), new Color(0.85f, 0.70f, 0.30f));
            foreach (string fakeName in new[] { "FakeDoor_A", "FakeDoor_B" })
            {
                var lockedDoor = root.Find(fakeName).gameObject.AddComponent<DetectiveDoorPlate>();
                SetStringField(lockedDoor, "plateLabel", fakeName == "FakeDoor_A" ? "301" : "308");
                SetStringField(lockedDoor, "toastMessage", fakeName == "FakeDoor_A" ? "301：门锁着，无人应答，无法进入。" : "308：门锁着，里面传来电视声，无法进入。");
            }
            CreateGroundGlow(root, "FakeDoorMat_A", new Vector3(-2f, 0.02f, 8.2f), new Vector3(2.2f, 0.02f, 0.9f), new Color(1f, 0.85f, 0.55f, 0.20f));
            CreateGroundGlow(root, "FakeDoorMat_B", new Vector3(17.5f, 0.02f, 8.2f), new Vector3(2.2f, 0.02f, 0.9f), new Color(1f, 0.85f, 0.55f, 0.20f));

            // 门牌（可交互检视）：NC-2077 是玩家自己的公寓（剧情规格 4-1）。
            CreateDoorPlate(root, new Vector3(7.8f, 2.05f, 8.66f), "NC-2077", "门牌：NC-2077。", new Color(0.20f, 0.22f, 0.30f));
            CreateDoorPlate(root, new Vector3(15.8f, 2.05f, 8.66f), "305", "门牌 305。林某的家。门缝里透出一点暖光。", new Color(0.20f, 0.22f, 0.30f));
            CreateDoorPlate(root, new Vector3(-0.2f, 2.05f, 8.66f), "301", "门牌 301……锁着。敲了敲门，无人应答。", new Color(0.20f, 0.22f, 0.30f));
            CreateDoorPlate(root, new Vector3(19.25f, 2.05f, 8.66f), "308", "门牌 308……锁着。屋里电视声很大，却没人来开门。", new Color(0.20f, 0.22f, 0.30f));
            CreateDoor(root, "Exit_West", new Vector3(-19.75f, 1.5f, 0f), new Vector3(0.5f, 3f, 4f), "返回：旧工业区", "D3_Industrial", "from_D4_Residential");

            CreateSpawn(root, "default", new Vector3(-12.5f, 1f, 0f));
            CreateSpawn(root, "from_D3_Industrial", new Vector3(-12.5f, 1f, 0f));
            CreateSpawn(root, "yourhome", new Vector3(6f, 1f, 7f));
            CreateSpawn(root, "from_I4_YourHome", new Vector3(6f, 1f, 6.5f));
            CreateSpawn(root, "victimhome", new Vector3(14f, 1f, 7f));
            CreateSpawn(root, "from_I4_VictimHome", new Vector3(14f, 1f, 6.5f));
        }

        private static void BuildYourHome(Transform root)
        {
            BuildGroundAndWalls(root, 12f, 10f, 3.2f, new[] { (side: 'S', center: 0f, gap: 2.6f) }, new Color(0.18f, 0.17f, 0.15f), 0.5f);
            CreateRegionLight(root, new Color(0.70f, 0.78f, 0.92f), 40f, 16f, 2.8f);
            CreateAccentLight(root, new Color(1f, 0.85f, 0.60f), new Vector3(1.5f, 1.9f, -3.2f));

            // P1-1：个人化陈设——警徽摆件、奖状框、案情软木板（与死者家拉开差异：冷色调+侦探工作感）。
            CreateCube(root, "BadgeStand", new Vector3(1.6f, 0.82f, -3.5f), new Vector3(0.3f, 0.1f, 0.2f), new Color(0.40f, 0.32f, 0.24f));
            CreateProp(root, "BadgeKeepsake", new Vector3(1.6f, 0.92f, -3.5f), 0.13f, 0.05f, new Color(0.85f, 0.72f, 0.30f));
            CreateCube(root, "CertificateFrame_A", new Vector3(5.68f, 1.9f, -0.5f), new Vector3(0.06f, 0.75f, 0.55f), new Color(0.55f, 0.45f, 0.25f));
            CreateCube(root, "CertificatePaper_A", new Vector3(5.64f, 1.9f, -0.5f), new Vector3(0.04f, 0.62f, 0.42f), new Color(0.88f, 0.85f, 0.72f));
            CreateCube(root, "CertificateFrame_B", new Vector3(5.68f, 1.7f, 0.8f), new Vector3(0.06f, 0.6f, 0.45f), new Color(0.55f, 0.45f, 0.25f));
            CreateCube(root, "CertificatePaper_B", new Vector3(5.64f, 1.7f, 0.8f), new Vector3(0.04f, 0.48f, 0.34f), new Color(0.86f, 0.82f, 0.68f));
            CreateCube(root, "CorkBoard", new Vector3(-5.68f, 1.8f, 1.5f), new Vector3(0.06f, 1.3f, 1.8f), new Color(0.45f, 0.35f, 0.25f));
            CreateCube(root, "CorkPhoto_A", new Vector3(-5.63f, 2.1f, 1.0f), new Vector3(0.04f, 0.28f, 0.22f), new Color(0.85f, 0.82f, 0.70f));
            CreateCube(root, "CorkPhoto_B", new Vector3(-5.63f, 1.6f, 1.7f), new Vector3(0.04f, 0.28f, 0.22f), new Color(0.80f, 0.78f, 0.66f));
            CreateCube(root, "CorkPhoto_C", new Vector3(-5.63f, 2.0f, 2.2f), new Vector3(0.04f, 0.24f, 0.2f), new Color(0.82f, 0.80f, 0.68f));
            var corkStringA = CreateCube(root, "CorkString_A", new Vector3(-5.62f, 1.85f, 1.35f), new Vector3(0.02f, 0.02f, 0.85f), new Color(0.80f, 0.15f, 0.15f));
            corkStringA.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            var corkStringB = CreateCube(root, "CorkString_B", new Vector3(-5.62f, 2.05f, 1.6f), new Vector3(0.02f, 0.02f, 0.9f), new Color(0.80f, 0.15f, 0.15f));
            corkStringB.transform.rotation = Quaternion.Euler(-30f, 0f, 0f);

            CreateCube(root, "Bed", new Vector3(-3.5f, 0.3f, -2.5f), new Vector3(2.1f, 0.6f, 1.6f), new Color(0.32f, 0.34f, 0.42f));
            CreateCube(root, "Desk", new Vector3(2f, 0.38f, -3.5f), new Vector3(2f, 0.75f, 0.9f), new Color(0.34f, 0.28f, 0.22f));
            CreateCube(root, "DeskLamp", new Vector3(1.2f, 0.95f, -3.6f), new Vector3(0.18f, 0.4f, 0.18f), new Color(0.90f, 0.82f, 0.60f));
            CreateClue(root, "Mirror", new Vector3(-5.6f, 1.5f, 0f), new Vector3(0.12f, 1.3f, 0.9f), new Color(0.70f, 0.78f, 0.85f), DetectiveClueIds.MirrorBruise, "镜子", null, false, DetectiveVoiceType.None, 0, null, null, new[] { new DetectiveCityBuilder.VoiceDelta(DetectiveVoiceType.Madness, 1) });
            CreateCube(root, "DrawerCabinet", new Vector3(4.5f, 0.4f, -3.5f), new Vector3(1.2f, 0.8f, 0.8f), new Color(0.34f, 0.28f, 0.22f));
            CreateClue(root, "DismissalNotice", new Vector3(4.5f, 0.85f, -3.5f), new Vector3(0.45f, 0.3f, 0.35f), new Color(0.85f, 0.80f, 0.65f), DetectiveClueIds.DismissalNotice, "抽屉（开除通知）", null, true);
            CreateClue(root, "PillBottle", new Vector3(2.6f, 0.85f, -3.4f), new Vector3(0.22f, 0.45f, 0.22f), new Color(0.90f, 0.90f, 0.92f), DetectiveClueIds.SleepingPillsEmpty, "药瓶", null, true);
            CreateClue(root, "YourDiary", new Vector3(3.3f, 0.82f, -3.2f), new Vector3(0.45f, 0.26f, 0.35f), new Color(0.80f, 0.70f, 0.55f), DetectiveClueIds.YourDiaryDoubt, "日记本", null, false, DetectiveVoiceType.Empathy, 2, "dlg_your_diary_locked", "dlg_your_diary");
            // 内心独白是门边的可检视便笺，不是站在出口处的第二个人。
            var leavingHome = CreateNpcGate(root, "LeavingHome", new Vector3(-1.8f, 1.4f, -4.43f), new Color(0.75f, 0.67f, 0.50f), "dlg_leaving_home", "NPC_Narrator", null, new[] { "dlg_leaving_home_done" });
            var leavingBody = leavingHome.transform.Find("Body").gameObject;
            var cubeTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leavingBody.GetComponent<MeshFilter>().sharedMesh = cubeTemplate.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(cubeTemplate);
            Object.DestroyImmediate(leavingBody.GetComponent<CapsuleCollider>());
            leavingBody.AddComponent<BoxCollider>();
            leavingBody.transform.localScale = new Vector3(0.55f, 0.4f, 0.12f);
            var leavingInteractable = leavingBody.GetComponent<TestInteractable>();
            var leavingSo = new SerializedObject(leavingInteractable);
            leavingSo.FindProperty("characterDefinition").objectReferenceValue = null;
            leavingSo.FindProperty("displayName").stringValue = "出门前的念头";
            leavingSo.FindProperty("interactionLabel").stringValue = "检视";
            leavingSo.ApplyModifiedPropertiesWithoutUndo();

            var finalTrigger = CreateTrigger(root, "FinalDuelTrigger", new Vector3(0f, 1.5f, 0.5f), new Vector3(5f, 3f, 4f));
            var finalDuel = finalTrigger.AddComponent<DetectiveFinalDuelTrigger>();
            SetObjectField(finalDuel, "duelDefinition", LoadAsset<DetectiveDuelDefinition>("Assets/Data/Duels/duel_final.asset"));
            SetIntField(finalDuel, "minClueCount", 10);
            SetStringField(finalDuel, "doneFlag", "final_duel_triggered");
            SetStringField(finalDuel, "requiredFlag", "left_I4_YourHome");
            AddTriggerRing(finalTrigger, new Color(0.85f, 0.20f, 0.20f), "final_duel_triggered", minCluesToShow: 10);

            CreateDoor(root, "ExitDoor", new Vector3(0f, 1.5f, -4.75f), new Vector3(2.4f, 3f, 0.5f), "回到街道：住宅区", "D4_Residential", "yourhome");
            CreateSpawn(root, "default", new Vector3(0f, 1f, -2.5f));
            CreateSpawn(root, "from_D4_Residential", new Vector3(1.5f, 1f, 1.8f));
        }

        private static void BuildVictimHome(Transform root)
        {
            BuildGroundAndWalls(root, 12f, 10f, 3.2f, new[] { (side: 'S', center: 0f, gap: 2.6f) }, new Color(0.22f, 0.19f, 0.16f), 0.5f);
            CreateRegionLight(root, new Color(1f, 0.78f, 0.55f), 38f, 15f, 2.8f);
            CreateAccentLight(root, new Color(0.90f, 0.70f, 0.50f), new Vector3(-3f, 2.2f, 2f));
            CreateAccentLight(root, new Color(1f, 0.80f, 0.55f), new Vector3(2.5f, 2.2f, 1.5f));

            // P1-1：家庭照片墙扩充 + 暖色生活物件（玩具积木、拖鞋、毛毯、台灯，与玩家家拉开差异）。
            CreateCube(root, "FamilyPhoto_3", new Vector3(-3.6f, 1.5f, 4.65f), new Vector3(0.5f, 0.4f, 0.06f), new Color(0.82f, 0.78f, 0.66f));
            CreateCube(root, "FamilyPhoto_4", new Vector3(2.8f, 2.3f, 4.65f), new Vector3(0.6f, 0.45f, 0.06f), new Color(0.80f, 0.76f, 0.64f));
            CreateCube(root, "ToyBlock_A", new Vector3(0.5f, 0.09f, 2.3f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.80f, 0.30f, 0.30f));
            CreateCube(root, "ToyBlock_B", new Vector3(0.75f, 0.07f, 2.5f), new Vector3(0.14f, 0.14f, 0.14f), new Color(0.30f, 0.55f, 0.80f));
            CreateCube(root, "ToyBlock_C", new Vector3(0.55f, 0.22f, 2.42f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.85f, 0.75f, 0.30f));
            CreateCube(root, "Slipper_A", new Vector3(1.2f, 0.04f, -2.6f), new Vector3(0.28f, 0.08f, 0.12f), new Color(0.55f, 0.25f, 0.22f));
            CreateCube(root, "Slipper_B", new Vector3(1.55f, 0.04f, -2.72f), new Vector3(0.28f, 0.08f, 0.12f), new Color(0.55f, 0.25f, 0.22f));
            CreateCube(root, "SofaBlanket", new Vector3(-2f, 0.92f, 2.3f), new Vector3(0.7f, 0.12f, 0.5f), new Color(0.75f, 0.55f, 0.40f));
            CreateProp(root, "TableLampBase", new Vector3(-2.3f, 0.55f, 0.8f), 0.07f, 0.25f, new Color(0.40f, 0.34f, 0.28f));
            CreateGlowCube(root, "TableLampShade", new Vector3(-2.3f, 0.75f, 0.8f), new Vector3(0.24f, 0.18f, 0.24f), new Color(1f, 0.85f, 0.60f));

            CreateCube(root, "Sofa", new Vector3(-2f, 0.45f, 2.5f), new Vector3(2.4f, 0.9f, 1f), new Color(0.42f, 0.32f, 0.28f));
            CreateCube(root, "TeaTable", new Vector3(-2f, 0.22f, 0.9f), new Vector3(1.2f, 0.45f, 0.7f), new Color(0.38f, 0.32f, 0.26f));
            for (int i = 0; i < 3; i++)
            {
                CreateCube(root, $"FamilyPhoto_{i}", new Vector3(-4f + i * 1.2f, 1.9f, 4.65f), new Vector3(0.8f, 0.6f, 0.06f), new Color(0.82f, 0.78f, 0.66f));
            }
            CreateCube(root, "DiningTable", new Vector3(4f, 0.38f, -1.8f), new Vector3(1.8f, 0.75f, 1.1f), new Color(0.40f, 0.34f, 0.28f));
            CreateCube(root, "DiningChair_A", new Vector3(4f, 0.25f, -3.3f), new Vector3(0.45f, 0.5f, 0.45f), new Color(0.42f, 0.36f, 0.30f));
            CreateCube(root, "DiningChair_B", new Vector3(5.1f, 0.25f, -1.8f), new Vector3(0.45f, 0.5f, 0.45f), new Color(0.42f, 0.36f, 0.30f));
            CreateCube(root, "MementoBox", new Vector3(4.5f, 0.3f, 3.5f), new Vector3(1f, 0.6f, 0.7f), new Color(0.36f, 0.30f, 0.24f));

            var wife = CreateNpcGate(root, "Wife", new Vector3(0f, 1f, -1.5f), new Color(0.75f, 0.65f, 0.65f), "dlg_wife", "NPC_Wife", null, null);
            SetNpcDialogueStages(wife, null, null, "dlg_wife_done", "dlg_wife_decision");

            CreateDoor(root, "ExitDoor", new Vector3(0f, 1.5f, -4.75f), new Vector3(2.4f, 3f, 0.5f), "回到街道：住宅区", "D4_Residential", "victimhome");
            CreateSpawn(root, "default", new Vector3(0f, 1f, -2.5f));
            CreateSpawn(root, "from_D4_Residential", new Vector3(1.5f, 1f, 1.5f));
        }



        #endregion

        #region 共享工具

        private static void BuildGroundAndWalls(Transform root, float width, float depth, float wallHeight, (char side, float center, float gap)[] gaps, Color groundColor, float southWestHeight = -1f)
        {
            CreateCube(root, "Ground", new Vector3(0f, -0.25f, 0f), new Vector3(width, 0.5f, depth), groundColor);
            float swHeight = southWestHeight > 0f ? southWestHeight : wallHeight;
            BuildWallSide(root, 'N', width, depth, wallHeight, gaps);
            BuildWallSide(root, 'S', width, depth, swHeight, gaps);
            BuildWallSide(root, 'E', width, depth, wallHeight, gaps);
            BuildWallSide(root, 'W', width, depth, swHeight, gaps);
        }

        private static void BuildWallSide(Transform root, char side, float width, float depth, float wallHeight, (char side, float center, float gap)[] gaps)
        {
            bool horizontal = side == 'N' || side == 'S';
            float length = horizontal ? width : depth;
            float fixedCoord = horizontal ? (side == 'N' ? depth * 0.5f : -depth * 0.5f) : (side == 'E' ? width * 0.5f : -width * 0.5f);

            var spans = new List<(float start, float end)> { (-length * 0.5f, length * 0.5f) };
            foreach (var gap in gaps)
            {
                if (gap.side != side)
                {
                    continue;
                }

                for (int i = spans.Count - 1; i >= 0; i--)
                {
                    var span = spans[i];
                    float gapStart = gap.center - gap.gap * 0.5f;
                    float gapEnd = gap.center + gap.gap * 0.5f;
                    if (gapEnd <= span.start || gapStart >= span.end)
                    {
                        continue;
                    }

                    spans.RemoveAt(i);
                    if (gapStart > span.start)
                    {
                        spans.Add((span.start, gapStart));
                    }

                    if (gapEnd < span.end)
                    {
                        spans.Add((gapEnd, span.end));
                    }
                }
            }

            int index = 0;
            foreach (var span in spans)
            {
                float spanLength = span.end - span.start;
                if (spanLength < 0.5f)
                {
                    continue;
                }

                float center = (span.start + span.end) * 0.5f;
                Vector3 position = horizontal ? new Vector3(center, wallHeight * 0.5f, fixedCoord) : new Vector3(fixedCoord, wallHeight * 0.5f, center);
                Vector3 scale = horizontal ? new Vector3(spanLength, wallHeight, 0.5f) : new Vector3(0.5f, wallHeight, spanLength);
                CreateWall(root, $"Wall_{side}_{index++}", position, scale);
            }
        }

        private static GameObject CreateDoor(Transform parent, string name, Vector3 position, Vector3 scale, string displayName, string targetRegionId, string targetSpawnId, bool skipLabel = false)
        {
            var go = CreateCube(parent, name, position, scale, new Color(0.85f, 0.70f, 0.30f));
            // 碰撞体加厚加深（门框/招牌会挡住薄门板）：让玩家在游玩视角下容易点到门
            var doorCollider = go.GetComponent<BoxCollider>();
            if (doorCollider != null)
            {
                bool inflateX = scale.z <= scale.x;
                doorCollider.size = inflateX
                    ? new Vector3(doorCollider.size.x + 0.4f, doorCollider.size.y + 0.8f, 2.2f)
                    : new Vector3(2.2f, doorCollider.size.y + 0.8f, doorCollider.size.z + 0.4f);
            }
            var door = go.AddComponent<DetectiveDoorTeleport>();
            var so = new SerializedObject(door);
            so.FindProperty("interactionId").stringValue = name;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("targetRegionId").stringValue = targetRegionId;
            so.FindProperty("targetSpawnId").stringValue = targetSpawnId;
            so.ApplyModifiedPropertiesWithoutUndo();

            bool onNsWall = scale.z <= scale.x;
            float width = onNsWall ? scale.x : scale.z;
            float height = scale.y;
            float bar = 0.14f;
            var frameMat = GetUnlitMaterial(new Color(1f, 0.88f, 0.45f));
            for (int face = -1; face <= 1; face += 2)
            {
                float zOff = onNsWall ? face * (scale.z * 0.5f + 0.06f) : 0f;
                float xOff = onNsWall ? 0f : face * (scale.x * 0.5f + 0.06f);
                CreateFrameBar(go.transform, frameMat, new Vector3(xOff - (onNsWall ? width * 0.5f + bar : 0f), 0f, zOff - (onNsWall ? 0f : width * 0.5f + bar)), onNsWall ? new Vector3(bar, height + bar, bar) : new Vector3(bar, height + bar, bar));
                CreateFrameBar(go.transform, frameMat, new Vector3(xOff + (onNsWall ? width * 0.5f + bar : 0f), 0f, zOff + (onNsWall ? 0f : width * 0.5f + bar)), new Vector3(bar, height + bar, bar));
                CreateFrameBar(go.transform, frameMat, new Vector3(xOff, height * 0.5f + bar, zOff), onNsWall ? new Vector3(width + bar * 3f, bar, bar) : new Vector3(bar, bar, width + bar * 3f));
            }

            if (!skipLabel)
            {
                // 标签固定朝向（不广告牌）：朝区域内侧挑出并固定旋转，玩家从哪个方向来就从哪面读。
                // 门楣高度：标签中心约 y=2.3（标准 3m 门）。
                float labelLintelY = Mathf.Min(height - 0.3f, 2.3f) - height * 0.5f;
                float inwardSign = onNsWall ? -Mathf.Sign(position.z) : -Mathf.Sign(position.x);
                Vector3 inwardLocal = onNsWall
                    ? new Vector3(0f, 0f, inwardSign * (scale.z * 0.5f + 0.06f))
                    : new Vector3(inwardSign * (scale.x * 0.5f + 0.06f), 0f, 0f);
                float labelYaw = onNsWall
                    ? (inwardSign < 0f ? 0f : 180f)
                    : (inwardSign < 0f ? 90f : -90f);
                var labelGo = new GameObject("DoorLabel");
                labelGo.transform.SetParent(go.transform, false);
                labelGo.transform.localPosition = new Vector3(inwardLocal.x, labelLintelY, inwardLocal.z);
                labelGo.transform.localRotation = Quaternion.Euler(0f, labelYaw, 0f);
                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.font = DetectiveUIWidgets.GetFont();
                tmp.fontSize = 5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.text = displayName;
                tmp.color = new Color(1f, 0.92f, 0.65f);
                tmp.fontStyle = FontStyles.Bold;
                tmp.outlineWidth = 0.25f;
                tmp.outlineColor = Color.black;
                tmp.rectTransform.sizeDelta = new Vector2(11f, 2.2f);
                labelGo.transform.localScale = Vector3.one * 0.55f;
                var labelMod = labelGo.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                labelMod.ignoreFromBuild = true;
            }
            CreateDoorThreshold(go.transform, position.y - height * 0.5f, width, onNsWall);
            door.NormalizeDecorationScale();
            return go;
        }

        // 门口发光门槛条：贴地金色半透明光带，宽同门洞、深 0.5m，常亮（不参与标签距离淡入）。
        private static void CreateDoorThreshold(Transform doorTransform, float doorBottomY, float width, bool onNsWall)
        {
            var threshold = GameObject.CreatePrimitive(PrimitiveType.Cube);
            threshold.name = "DoorThreshold";
            threshold.transform.SetParent(doorTransform, false);
            threshold.transform.localPosition = new Vector3(0f, doorBottomY + 0.03f - doorTransform.position.y, 0f);
            threshold.transform.localScale = onNsWall
                ? new Vector3(width, 0.02f, 0.5f)
                : new Vector3(0.5f, 0.02f, width);
            threshold.GetComponent<MeshRenderer>().sharedMaterial = GetDoorThresholdMaterial();
            var thresholdNavMod = threshold.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            thresholdNavMod.ignoreFromBuild = true;
            var thresholdCollider = threshold.GetComponent<Collider>();
            if (thresholdCollider != null)
            {
                Object.DestroyImmediate(thresholdCollider);
            }
        }

        private static Material doorThresholdMaterial;

        private static Material GetDoorThresholdMaterial()
        {
            if (doorThresholdMaterial == null)
            {
                doorThresholdMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "DoorThresholdGold" };
                doorThresholdMaterial.SetColor("_BaseColor", new Color(1f, 0.85f, 0.35f, 0.45f));
                doorThresholdMaterial.SetFloat("_Surface", 1f);
                doorThresholdMaterial.SetFloat("_Blend", 0f);
                doorThresholdMaterial.SetFloat("_ZWrite", 0f);
                doorThresholdMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                doorThresholdMaterial.renderQueue = 3000;
            }

            return doorThresholdMaterial;
        }

        private static void CreateFrameBar(Transform parent, Material material, Vector3 localPosition, Vector3 scale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "FrameBar";
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localScale = scale;
            bar.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(bar.GetComponent<Collider>());
        }

        private static void AddTriggerRing(GameObject triggerGo, Color color, string hideFlag, int minCluesToShow = 0)
        {
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringGo.name = "TriggerRing";
            ringGo.transform.SetParent(triggerGo.transform, false);
            var box = triggerGo.GetComponent<BoxCollider>();
            float diameter = box != null ? Mathf.Max(box.size.x, box.size.z) : 2f;
            // 收敛为细线圆环样式：直径缩到触发区的 60%，透明度降到 0.2（材质在 GetRingMaterial 统一）。
            diameter *= 0.6f;
            float groundLocalY = 0.02f - triggerGo.transform.position.y;
            ringGo.transform.localPosition = new Vector3(0f, groundLocalY, 0f);
            ringGo.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            ringGo.GetComponent<MeshRenderer>().sharedMaterial = GetRingMaterial(color);
            var collider = ringGo.GetComponent<CapsuleCollider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var ring = triggerGo.AddComponent<DetectiveTriggerRing>();
            var so = new SerializedObject(ring);
            so.FindProperty("ring").objectReferenceValue = ringGo;
            so.FindProperty("hideFlag").stringValue = hideFlag;
            so.FindProperty("minCluesToShow").intValue = minCluesToShow;
            so.ApplyModifiedPropertiesWithoutUndo();
            ringGo.SetActive(minCluesToShow <= 0);
        }

        private static Material GetRingMaterial(Color color)
        {
            string folder = "Assets/Art/Materials/Greybox";
            string key = ColorUtility.ToHtmlStringRGBA(color);
            string path = $"{folder}/Ring_{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                color.a = 0.2f;
                existing.SetColor("_BaseColor", color);
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = $"Ring_{key}" };
            color.a = 0.2f;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetNpcDialogueStages(GameObject npc, string unlockFlag, string beforeUnlockAsset,
            string followupFlag, string followupAsset)
        {
            var interactable = npc.GetComponentInChildren<TestInteractable>(true);
            var so = new SerializedObject(interactable);
            if (!string.IsNullOrWhiteSpace(unlockFlag))
            {
                so.FindProperty("dialogueUnlockFlag").stringValue = unlockFlag;
                so.FindProperty("beforeUnlockDialogue").objectReferenceValue =
                    LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{beforeUnlockAsset}.asset");
            }
            if (!string.IsNullOrWhiteSpace(followupFlag))
            {
                so.FindProperty("followupDialogueFlag").stringValue = followupFlag;
                so.FindProperty("followupDialogue").objectReferenceValue =
                    LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{followupAsset}.asset");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateSpawn(Transform parent, string spawnId, Vector3 position)
        {
            var go = new GameObject($"Spawn_{spawnId}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
        }

        private static void CreateRegionLight(Transform parent, Color color, float intensity = 130f, float range = 48f, float height = 6f)
        {
            var lightGo = new GameObject("RegionLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = new Vector3(0f, height, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;

            var dirGo = new GameObject("DirectionalLight");
            dirGo.transform.SetParent(parent, false);
            dirGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var dir = dirGo.AddComponent<Light>();
            dir.type = LightType.Directional;
            dir.color = new Color(0.62f, 0.71f, 0.85f);
            dir.intensity = 0.5f;
        }

        // 金融街警戒线横杆太细（y 0.08），AddNavBlockModifiers 按尺寸跳过；
        // 加一根隐形碰撞挡住 NavMesh 与玩家穿行。
        private static void CreatePoliceLineBlock(Transform parent, Vector3 position, Vector3 scale)
        {
            var block = CreateCube(parent, "PoliceLineBlock", position, scale, new Color(0.10f, 0.10f, 0.12f));
            block.GetComponent<MeshRenderer>().enabled = false;
        }

        // 狙击手头顶聚光：深色胶囊融入黑屋面，用一盏窄聚光提亮轮廓。
        private static void CreateSniperSpotlight(Transform parent, Vector3 sniperPosition)
        {
            var spotGo = new GameObject("SniperSpotlight");
            spotGo.transform.SetParent(parent, false);
            spotGo.transform.position = sniperPosition + new Vector3(0.4f, 2.8f, 1.2f);
            spotGo.transform.rotation = Quaternion.LookRotation(sniperPosition + Vector3.up * 1.2f - spotGo.transform.position);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.95f, 0.85f);
            spot.intensity = 18f;
            spot.range = 9f;
            spot.spotAngle = 38f;
            spot.innerSpotAngle = 24f;
        }

        // 路灯刻痕：贴在灯杆表面的深色细横条，环绕朝相机方向的半圈分布。
        private static void CreateStreetlampScratches(Transform parent, Vector3 poleBase)
        {
            var scratchColor = new Color(0.12f, 0.10f, 0.09f);
            float[] angles = { 180f, 200f, 220f, 240f };
            for (int i = 0; i < angles.Length; i++)
            {
                float radians = angles[i] * Mathf.Deg2Rad;
                var bar = CreateCube(
                    parent,
                    $"StreetlampScratch_{i}",
                    poleBase + new Vector3(Mathf.Sin(radians) * 0.135f, 1.02f + i * 0.08f, Mathf.Cos(radians) * 0.135f),
                    new Vector3(0.22f, 0.03f, 0.02f),
                    scratchColor);
                bar.transform.rotation = Quaternion.Euler(0f, angles[i], 0f);
            }
        }

        private static void CreateAccentLight(Transform parent, Color color, Vector3 position)
        {
            var accentGo = new GameObject("AccentLight");
            accentGo.transform.SetParent(parent, false);
            accentGo.transform.position = position;
            var accent = accentGo.AddComponent<Light>();
            accent.type = LightType.Point;
            accent.color = color;
            accent.intensity = 60f;
            accent.range = 30f;
        }

        // —— P1-1 第 3 批：场景丰富度辅助 ——

        // 自发光立方面（霓虹/灯管/窗光），不参与导航阻挡判定（厚度 < 0.3）。
        private static GameObject CreateGlowCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = GetUnlitMaterial(color);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return go;
        }

        private static Material GetWindowGlassMaterial()
        {
            const string path = "Assets/Art/Materials/Greybox/WindowGlass_Transparent.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "WindowGlass_Transparent" };
            material.SetColor("_BaseColor", new Color(0.55f, 0.70f, 0.78f, 0.18f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", 5f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static readonly Dictionary<string, Material> transparentGlowMaterials = new();

        private static Material GetTransparentGlowMaterial(Color color)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color);
            if (transparentGlowMaterials.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = $"GroundGlow_{key}" };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            transparentGlowMaterials[key] = material;
            return material;
        }

        // 贴地半透明色块（霓虹反光/油渍/门槛光晕）：无碰撞、不参与 NavMesh。
        private static void CreateGroundGlow(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = GetTransparentGlowMaterial(color);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var navMod = go.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            navMod.ignoreFromBuild = true;
        }

        // TMP 世界文字：与板子同级（避免继承板子的非均匀缩放），统一 textScale 世界缩放。
        // faceRotation 为文字的朝向（ TMP 正面为本地 +z ）。
        private static void AddBoardText(Transform parent, Vector3 position, Quaternion faceRotation, string text, Color color, float fontSize, float textScale)
        {
            var labelGo = new GameObject("BoardText");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.position = position;
            labelGo.transform.rotation = faceRotation;
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.font = DetectiveUIWidgets.GetFont();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.text = text;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;
            // 关键：TMP 世界文字必须用小 rect + 大缩放（与门标签同一约定），
            // 大 rect + 小缩放会导致渲染尺寸异常缩小、文字不可读。
            float maxLineUnits = 0f;
            float lineUnits = 0f;
            int lines = 1;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    maxLineUnits = Mathf.Max(maxLineUnits, lineUnits);
                    lineUnits = 0f;
                    lines++;
                    continue;
                }
                lineUnits += c > 0x2E7F ? 1f : 0.55f;
            }
            maxLineUnits = Mathf.Max(maxLineUnits, lineUnits);
            float unit = fontSize * 0.48f; // 与门标签 (11x2.2, fontSize5) 反推的每 CJK 字符 rect 宽度
            tmp.rectTransform.sizeDelta = new Vector2(
                Mathf.Max(3f, maxLineUnits * unit + fontSize * 0.5f),
                fontSize * 0.44f * lines + fontSize * 0.4f);
            labelGo.transform.localScale = Vector3.one * textScale;
            tmp.rectTransform.anchoredPosition3D = parent.InverseTransformPoint(position);
            var navMod = labelGo.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
            navMod.ignoreFromBuild = true;
        }

        // 挂牌/招牌：底板 + 可选 TMP 字。yRotationDegrees=0 时字面朝南（-z，朝默认相机）。
        private static GameObject CreateSignBoard(Transform parent, string name, Vector3 position, Vector3 scale, Color boardColor, string text, Color textColor, float yRotationDegrees = 0f, float fontSize = 5f, float textScale = 0.08f)
        {
            var board = CreateCube(parent, name, position, scale, boardColor);
            Quaternion rotation = Quaternion.Euler(0f, yRotationDegrees, 0f);
            board.transform.rotation = rotation;
            if (!string.IsNullOrEmpty(text))
            {
                Vector3 textPosition = position + rotation * new Vector3(0f, 0f, -(scale.z * 0.5f + 0.02f));
                // TMP 世界文字从本地 -Z 侧阅读：文字块放在板子挑出面的外侧，朝向与板子一致。
                AddBoardText(parent, textPosition, rotation, text, textColor, fontSize, textScale);
            }

            return board;
        }

        // 可交互门牌（找公寓玩法）：小板 + 门牌号 TMP 字 + DetectiveDoorPlate（点击弹 toast）。
        // 挂在门的旁边墙面上。yRotation=0 字面朝南（-z）；南墙牌子传 180。
        private static void CreateDoorPlate(Transform parent, Vector3 position, string plateNumber, string toastMessage, Color boardColor, float yRotation = 0f)
        {
            // 板宽按文字长度自适应（CJK 按 1、其他按 0.55 估），保证 0.085 缩放下文字不溢出
            float textUnits = 0f;
            foreach (char c in plateNumber)
            {
                textUnits += c > 0x2E7F ? 1f : 0.55f;
            }
            float plateWidth = Mathf.Max(1.15f, textUnits * 0.22f + 0.45f);
            var plate = CreateSignBoard(parent, $"DoorPlate_{plateNumber}", position, new Vector3(plateWidth, 0.62f, 0.08f), boardColor, plateNumber, new Color(0.95f, 0.92f, 0.80f), yRotation, 5f, 0.40f);
            var plateComp = plate.AddComponent<DetectiveDoorPlate>();
            var so = new SerializedObject(plateComp);
            so.FindProperty("plateLabel").stringValue = $"门牌 {plateNumber}";
            so.FindProperty("toastMessage").stringValue = toastMessage;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildRegionRain(Transform parent)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 8f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = 0.045f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 450f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(48f, 0.1f, 48f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = -22f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.05f;
            renderer.sharedMaterial = GetRainMaterial();
        }

        private static void DressRegion(Transform root, string regionId)
        {
            switch (regionId)
            {
                case "D0_Alley":
                    CreateProp(root, "TrashBag_C", new Vector3(1.8f, 0.2f, -9.8f), 0.28f, 0.4f, new Color(0.08f, 0.08f, 0.10f));
                    CreateProp(root, "TrashBag_D", new Vector3(3.6f, 0.18f, -2.2f), 0.26f, 0.36f, new Color(0.09f, 0.09f, 0.11f));
                    CreateCube(root, "Cardbox_C", new Vector3(-3.4f, 0.25f, 0.5f), new Vector3(0.6f, 0.5f, 0.6f), new Color(0.30f, 0.25f, 0.17f));
                    CreateCube(root, "Puddle_C", new Vector3(1.2f, 0.02f, 6.5f), new Vector3(1.4f, 0.02f, 1f), new Color(0.05f, 0.07f, 0.10f));
                    CreateCube(root, "Poster_A", new Vector3(-4.675f, 2.2f, 4f), new Vector3(0.05f, 1.1f, 0.8f), new Color(0.55f, 0.45f, 0.30f));
                    CreatePipeRun(root, new Vector3(4.55f, 1.2f, -8f), 6f, false);
                    // P1-1：散落垃圾/纸箱/易拉罐加倍 + 地面裂缝与修补色块（全部贴墙或小型，不挡巷内通道）。
                    CreateProp(root, "TrashBag_E", new Vector3(-3.9f, 0.2f, -4.2f), 0.3f, 0.42f, new Color(0.08f, 0.08f, 0.10f));
                    CreateProp(root, "TrashBag_F", new Vector3(-4.0f, 0.22f, 3.2f), 0.3f, 0.46f, new Color(0.09f, 0.09f, 0.11f));
                    CreateCube(root, "Cardbox_D", new Vector3(-3.5f, 0.2f, -1.5f), new Vector3(0.55f, 0.4f, 0.55f), new Color(0.30f, 0.25f, 0.17f));
                    CreateCube(root, "Cardbox_E", new Vector3(3.8f, 0.25f, 4.5f), new Vector3(0.6f, 0.5f, 0.6f), new Color(0.31f, 0.26f, 0.18f));
                    CreateProp(root, "Can_A", new Vector3(1.4f, 0.09f, -5.2f), 0.08f, 0.18f, new Color(0.55f, 0.56f, 0.58f));
                    CreateProp(root, "Can_B", new Vector3(0.3f, 0.09f, 1.2f), 0.08f, 0.18f, new Color(0.60f, 0.45f, 0.25f));
                    CreateProp(root, "Can_C", new Vector3(-1.2f, 0.09f, 6.0f), 0.08f, 0.18f, new Color(0.45f, 0.55f, 0.50f));
                    CreateCube(root, "Crack_A", new Vector3(0.5f, 0.012f, -6f), new Vector3(2.8f, 0.015f, 0.12f), new Color(0.05f, 0.05f, 0.06f));
                    CreateCube(root, "Crack_B", new Vector3(-1.2f, 0.012f, 2f), new Vector3(2.2f, 0.015f, 0.1f), new Color(0.05f, 0.05f, 0.06f));
                    CreateCube(root, "Crack_C", new Vector3(2.2f, 0.012f, 8.2f), new Vector3(1.8f, 0.015f, 0.1f), new Color(0.05f, 0.05f, 0.06f));
                    CreateCube(root, "Patch_A", new Vector3(0.8f, 0.014f, -0.5f), new Vector3(1.5f, 0.016f, 1.1f), new Color(0.14f, 0.15f, 0.19f));
                    CreateCube(root, "Patch_B", new Vector3(-2.4f, 0.014f, -7.2f), new Vector3(1.2f, 0.016f, 0.9f), new Color(0.13f, 0.14f, 0.18f));
                    break;
                case "D1_RedLight":
                    CreateProp(root, "TrashBin_A", new Vector3(-16f, 0.5f, -7.5f), 0.4f, 1f, new Color(0.22f, 0.26f, 0.24f));
                    CreateCube(root, "Cardbox_Street", new Vector3(12f, 0.3f, 7.2f), new Vector3(0.8f, 0.6f, 0.8f), new Color(0.32f, 0.26f, 0.18f));
                    CreateCube(root, "Cone_A", new Vector3(-2f, 0.25f, -6.8f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.85f, 0.45f, 0.10f));
                    CreateCube(root, "Hydrant", new Vector3(18f, 0.35f, -7.6f), new Vector3(0.35f, 0.7f, 0.35f), new Color(0.75f, 0.15f, 0.12f));
                    CreateCube(root, "AcUnit_Street", new Vector3(-6f, 0.5f, 8.6f), new Vector3(1f, 0.9f, 0.5f), new Color(0.38f, 0.39f, 0.43f));
                    CreateCube(root, "Poster_B", new Vector3(2f, 2f, 8.83f), new Vector3(1.2f, 1.6f, 0.06f), new Color(0.85f, 0.50f, 0.70f));
                    CreateCube(root, "Poster_C", new Vector3(-16f, 1.8f, -8.67f), new Vector3(1.4f, 1.1f, 0.06f), new Color(0.30f, 0.70f, 0.80f));
                    CreateCube(root, "Wire_A", new Vector3(0f, 7.2f, 9.2f), new Vector3(20f, 0.05f, 0.05f), new Color(0.05f, 0.05f, 0.06f));
                    CreateCube(root, "Stain_A", new Vector3(6f, 0.015f, 1f), new Vector3(2.4f, 0.02f, 1.6f), new Color(0.06f, 0.06f, 0.07f));
                    // P1-1：霓虹贴回墙内侧；加路边摊、倒地自行车、湿地反光片、街角门牌。
                    CreateNeonSign(root, "Neon_F", new Vector3(8f, 3.2f, -8.6f), new Vector3(2.4f, 0.8f, 0.2f), new Color(0.95f, 0.55f, 0.30f));
                    CreateNeonSign(root, "Neon_G", new Vector3(-18f, 2.6f, 8.6f), new Vector3(2f, 0.7f, 0.2f), new Color(0.55f, 0.40f, 0.90f));
                    CreateGroundGlow(root, "NeonReflect_A", new Vector3(-14f, 0.02f, 7.4f), new Vector3(4.5f, 0.02f, 1.8f), new Color(0.88f, 0.31f, 0.79f, 0.16f));
                    CreateGroundGlow(root, "NeonReflect_B", new Vector3(-2f, 0.02f, 7.4f), new Vector3(3.5f, 0.02f, 1.6f), new Color(0.13f, 0.83f, 0.93f, 0.14f));
                    CreateGroundGlow(root, "NeonReflect_C", new Vector3(6f, 0.02f, -7.4f), new Vector3(4f, 0.02f, 1.6f), new Color(0.88f, 0.31f, 0.79f, 0.14f));
                    CreateGroundGlow(root, "NeonReflect_D", new Vector3(-18f, 0.02f, 7.3f), new Vector3(2.5f, 0.02f, 1.4f), new Color(0.55f, 0.40f, 0.90f, 0.15f));
                    // 路边摊：棚子 + 四根立柱 + 柜台 + 小食盒。
                    CreateCube(root, "StallCanopy", new Vector3(-5f, 2.05f, -4.5f), new Vector3(2.8f, 0.14f, 2.0f), new Color(0.55f, 0.20f, 0.20f));
                    CreateProp(root, "StallPole_A", new Vector3(-6.3f, 1.0f, -3.6f), 0.05f, 2.0f, new Color(0.30f, 0.28f, 0.28f));
                    CreateProp(root, "StallPole_B", new Vector3(-3.7f, 1.0f, -3.6f), 0.05f, 2.0f, new Color(0.30f, 0.28f, 0.28f));
                    CreateProp(root, "StallPole_C", new Vector3(-6.3f, 1.0f, -5.4f), 0.05f, 2.0f, new Color(0.30f, 0.28f, 0.28f));
                    CreateProp(root, "StallPole_D", new Vector3(-3.7f, 1.0f, -5.4f), 0.05f, 2.0f, new Color(0.30f, 0.28f, 0.28f));
                    CreateCube(root, "StallCounter", new Vector3(-5f, 0.45f, -4.5f), new Vector3(2.2f, 0.9f, 1.2f), new Color(0.42f, 0.32f, 0.22f));
                    CreateCube(root, "StallBox_A", new Vector3(-5.6f, 0.98f, -4.3f), new Vector3(0.4f, 0.16f, 0.3f), new Color(0.85f, 0.60f, 0.25f));
                    CreateCube(root, "StallBox_B", new Vector3(-4.7f, 0.98f, -4.6f), new Vector3(0.35f, 0.14f, 0.28f), new Color(0.80f, 0.50f, 0.30f));
                    // 倒地自行车：两个平放轮圈 + 横杆（旋转件不参与导航阻挡）。
                    var bikeWheelA = CreateProp(root, "FallenBikeWheel_A", new Vector3(13.6f, 0.12f, -5f), 0.35f, 0.07f, new Color(0.10f, 0.10f, 0.11f));
                    bikeWheelA.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    var bikeWheelB = CreateProp(root, "FallenBikeWheel_B", new Vector3(14.7f, 0.12f, -5f), 0.35f, 0.07f, new Color(0.10f, 0.10f, 0.11f));
                    bikeWheelB.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    CreateCube(root, "FallenBikeFrame", new Vector3(14.15f, 0.18f, -5f), new Vector3(1.1f, 0.07f, 0.07f), new Color(0.60f, 0.20f, 0.20f));
                    CreateCube(root, "FallenBikeBar", new Vector3(14.6f, 0.28f, -5f), new Vector3(0.07f, 0.3f, 0.07f), new Color(0.60f, 0.20f, 0.20f));
                    // 街角门牌小牌。
                    CreateDoorPlate(root, new Vector3(0f, 2.1f, 8.66f), "樱花町 3 号", "门牌：樱花町 3 号。这一带都是霓虹招牌的店面。", new Color(0.16f, 0.14f, 0.20f));
                    CreateDoorPlate(root, new Vector3(-12f, 2.1f, -8.66f), "樱花町 4 号", "门牌：樱花町 4 号。铁门紧闭，里面没有灯。", new Color(0.16f, 0.14f, 0.20f), 180f);
                    break;
                case "I1_Bar":
                    CreateProp(root, "WineGlass_A", new Vector3(-4.3f, 1.16f, 2.45f), 0.06f, 0.18f, new Color(0.85f, 0.80f, 0.70f));
                    CreateProp(root, "WineGlass_B", new Vector3(-2.2f, 1.16f, 3.1f), 0.06f, 0.18f, new Color(0.85f, 0.80f, 0.70f));
                    CreateCube(root, "NapkinBox", new Vector3(-1.4f, 1.18f, 3.3f), new Vector3(0.3f, 0.25f, 0.2f), new Color(0.80f, 0.78f, 0.72f));
                    CreateCube(root, "WallClock", new Vector3(6.66f, 2.4f, 0f), new Vector3(0.08f, 0.7f, 0.7f), new Color(0.85f, 0.82f, 0.70f));
                    CreateProp(root, "Pendant_A", new Vector3(0f, 2.9f, 0f), 0.25f, 0.12f, new Color(1f, 0.80f, 0.50f));
                    CreateProp(root, "Pendant_B", new Vector3(3.5f, 2.9f, -0.5f), 0.25f, 0.12f, new Color(1f, 0.80f, 0.50f));
                    CreateCube(root, "BarrelDeco", new Vector3(6.2f, 0.4f, 3.5f), new Vector3(0.8f, 0.8f, 0.8f), new Color(0.36f, 0.26f, 0.18f));
                    break;
                case "D2_Financial":
                    CreateProp(root, "TrashBin_B", new Vector3(-2f, 0.5f, 8.5f), 0.4f, 1f, new Color(0.24f, 0.27f, 0.30f));
                    CreateCube(root, "Cone_B", new Vector3(-6f, 0.25f, 2.8f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.85f, 0.45f, 0.10f));
                    CreateCube(root, "Cone_C", new Vector3(-10.5f, 0.25f, 2.2f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.85f, 0.45f, 0.10f));
                    CreateCube(root, "Planter_C", new Vector3(2f, 0.25f, 8.5f), new Vector3(1f, 0.5f, 1f), new Color(0.30f, 0.24f, 0.18f));
                    CreateProp(root, "Tree_C", new Vector3(2f, 1.3f, 8.5f), 0.45f, 1.8f, new Color(0.16f, 0.34f, 0.18f));
                    CreateCube(root, "Hydrant_B", new Vector3(14f, 0.35f, 8.8f), new Vector3(0.35f, 0.7f, 0.35f), new Color(0.75f, 0.15f, 0.12f));
                    CreateCube(root, "Stain_B", new Vector3(-2f, 0.015f, -4f), new Vector3(2f, 0.02f, 1.4f), new Color(0.06f, 0.06f, 0.07f));
                    CreateCube(root, "Drain", new Vector3(5f, 0.02f, 2f), new Vector3(0.9f, 0.02f, 0.9f), new Color(0.10f, 0.10f, 0.11f));
                    CreateCube(root, "Poster_D", new Vector3(4f, 1.6f, -11.67f), new Vector3(1f, 1.4f, 0.06f), new Color(0.45f, 0.55f, 0.70f));
                    CreateCube(root, "Crack_A", new Vector3(0f, 0.012f, 0f), new Vector3(3.4f, 0.015f, 0.12f), new Color(0.05f, 0.05f, 0.06f));
                    break;
                case "I2_Apartment":
                    CreateCube(root, "Bookshelf", new Vector3(-8.5f, 1.1f, 1.5f), new Vector3(0.6f, 2.2f, 2.4f), new Color(0.34f, 0.27f, 0.20f));
                    // P1-1：书架上的书脊色块。
                    Color[] bookColors = { new Color(0.70f, 0.30f, 0.25f), new Color(0.30f, 0.45f, 0.65f), new Color(0.75f, 0.65f, 0.35f), new Color(0.35f, 0.55f, 0.40f), new Color(0.60f, 0.35f, 0.55f), new Color(0.55f, 0.40f, 0.25f) };
                    for (int i = 0; i < bookColors.Length; i++)
                    {
                        CreateCube(root, $"BookSpine_{i}", new Vector3(-8.02f, 1.15f + (i % 2) * 0.75f, 0.75f + i * 0.33f), new Vector3(0.18f, 0.55f + (i % 3) * 0.1f, 0.24f), bookColors[i]);
                    }
                    CreateCube(root, "Papers_A", new Vector3(1.6f, 0.05f, 0.8f), new Vector3(0.5f, 0.02f, 0.7f), new Color(0.88f, 0.86f, 0.78f));
                    CreateCube(root, "Papers_B", new Vector3(2.1f, 0.05f, 1.3f), new Vector3(0.4f, 0.02f, 0.5f), new Color(0.86f, 0.84f, 0.76f));
                    CreateProp(root, "Mug", new Vector3(3.9f, 0.56f, 0.4f), 0.07f, 0.16f, new Color(0.85f, 0.85f, 0.88f));
                    CreateCube(root, "Chair_C", new Vector3(4.8f, 0.25f, 1.6f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.32f, 0.33f, 0.40f));
                    CreateCube(root, "Books_Bed", new Vector3(-6.6f, 0.65f, 4.2f), new Vector3(0.5f, 0.12f, 0.35f), new Color(0.70f, 0.30f, 0.25f));
                    CreateProp(root, "CoatRack", new Vector3(7.8f, 0.9f, -5f), 0.08f, 1.8f, new Color(0.30f, 0.24f, 0.18f));
                    break;
                case "I2_Coffee":
                    CreateCube(root, "DessertCase", new Vector3(-3.6f, 0.95f, -3.2f), new Vector3(1.2f, 0.9f, 0.8f), new Color(0.55f, 0.70f, 0.78f));
                    CreateProp(root, "StoolHigh_A", new Vector3(-0.5f, 0.35f, -2.3f), 0.25f, 0.7f, new Color(0.42f, 0.35f, 0.28f));
                    CreateProp(root, "StoolHigh_B", new Vector3(-2.5f, 0.35f, -2.3f), 0.25f, 0.7f, new Color(0.42f, 0.35f, 0.28f));
                    CreateCube(root, "MenuSticker_A", new Vector3(5.67f, 1.8f, -1f), new Vector3(0.06f, 0.9f, 0.6f), new Color(0.85f, 0.80f, 0.65f));
                    CreateCube(root, "MenuSticker_B", new Vector3(5.67f, 1.5f, 1f), new Vector3(0.06f, 0.7f, 0.5f), new Color(0.80f, 0.75f, 0.60f));
                    CreateProp(root, "Mug_B", new Vector3(2.2f, 0.78f, 0.7f), 0.07f, 0.16f, new Color(0.90f, 0.85f, 0.80f));
                    break;
                case "D3_Industrial":
                    CreateProp(root, "OilDrum_C", new Vector3(0f, 0.6f, 10f), 0.5f, 1.2f, new Color(0.28f, 0.28f, 0.30f));
                    CreateCube(root, "Pallet_A", new Vector3(-6f, 0.08f, 0f), new Vector3(1.4f, 0.16f, 1.2f), new Color(0.40f, 0.32f, 0.22f));
                    CreateCube(root, "Sack_A", new Vector3(-5.6f, 0.35f, 0.2f), new Vector3(0.7f, 0.55f, 0.5f), new Color(0.45f, 0.40f, 0.32f));
                    CreateCube(root, "Sack_B", new Vector3(-6.4f, 0.3f, -0.4f), new Vector3(0.6f, 0.45f, 0.5f), new Color(0.42f, 0.38f, 0.30f));
                    CreateCube(root, "ScrapPile", new Vector3(4f, 0.4f, 8f), new Vector3(1.6f, 0.8f, 1.2f), new Color(0.24f, 0.22f, 0.20f));
                    CreateCube(root, "Poster_E", new Vector3(23.67f, 1.4f, -4f), new Vector3(0.06f, 1f, 1.4f), new Color(0.80f, 0.60f, 0.20f));
                    CreatePipeRun(root, new Vector3(4.7f, 6.5f, 6f), 8f, true);
                    break;
                case "I3_Warehouse":
                    CreateCube(root, "Pallet_B", new Vector3(4f, 0.08f, -3f), new Vector3(1.4f, 0.16f, 1.2f), new Color(0.40f, 0.32f, 0.22f));
                    CreateCube(root, "Sack_C", new Vector3(4.2f, 0.35f, -3f), new Vector3(0.7f, 0.55f, 0.5f), new Color(0.45f, 0.40f, 0.32f));
                    CreateCube(root, "Sack_D", new Vector3(3.4f, 0.3f, -3.5f), new Vector3(0.6f, 0.45f, 0.5f), new Color(0.42f, 0.38f, 0.30f));
                    CreateCube(root, "CrateStack_D", new Vector3(8.5f, 0.7f, -4.5f), new Vector3(1.4f, 1.4f, 1.4f), new Color(0.34f, 0.28f, 0.20f));
                    CreateCube(root, "MetalSheet", new Vector3(-9f, 0.9f, 3.5f), new Vector3(0.15f, 1.8f, 2.4f), new Color(0.32f, 0.33f, 0.36f));
                    CreateProp(root, "BarrelRust", new Vector3(-8.4f, 0.55f, -4.8f), 0.45f, 1.1f, new Color(0.45f, 0.28f, 0.16f));
                    break;
                case "I3_Basement":
                    CreateProp(root, "Valve_A", new Vector3(-6.5f, 1.4f, -4.5f), 0.22f, 0.1f, new Color(0.70f, 0.20f, 0.15f));
                    CreatePipeRun(root, new Vector3(0f, 2.0f, 4.5f), 8f, true);
                    CreateCube(root, "Puddle_D", new Vector3(-2f, 0.02f, 1.5f), new Vector3(1.8f, 0.02f, 1.2f), new Color(0.06f, 0.08f, 0.10f));
                    CreateCube(root, "Bucket", new Vector3(5.5f, 0.25f, -3.5f), new Vector3(0.4f, 0.5f, 0.4f), new Color(0.35f, 0.35f, 0.38f));
                    CreateCube(root, "ShelfRust", new Vector3(-5.5f, 0.9f, 3.5f), new Vector3(0.5f, 1.8f, 1.8f), new Color(0.32f, 0.28f, 0.24f));
                    break;
                case "I3_Rooftop":
                    CreateCube(root, "PigeonCage", new Vector3(7f, 0.5f, 4.5f), new Vector3(0.9f, 1f, 0.9f), new Color(0.40f, 0.38f, 0.34f));
                    CreateProp(root, "Antenna", new Vector3(-8.2f, 2.2f, -5.5f), 0.06f, 2.4f, new Color(0.30f, 0.30f, 0.34f));
                    CreateCube(root, "JunkPile", new Vector3(-2f, 0.35f, 5.5f), new Vector3(1.4f, 0.7f, 1f), new Color(0.26f, 0.26f, 0.29f));
                    CreateCube(root, "VentPipe", new Vector3(1.5f, 0.5f, -5.5f), new Vector3(0.5f, 1f, 0.5f), new Color(0.34f, 0.34f, 0.38f));
                    break;
                case "D4_Residential":
                    CreateCube(root, "BikeFrame", new Vector3(-8f, 0.45f, -7.8f), new Vector3(1.3f, 0.9f, 0.15f), new Color(0.60f, 0.20f, 0.20f));
                    CreateProp(root, "BikeWheel_A", new Vector3(-8.5f, 0.35f, -7.7f), 0.35f, 0.06f, new Color(0.10f, 0.10f, 0.11f));
                    CreateProp(root, "BikeWheel_B", new Vector3(-7.5f, 0.35f, -7.7f), 0.35f, 0.06f, new Color(0.10f, 0.10f, 0.11f));
                    CreateProp(root, "PlantPot_A", new Vector3(2f, 0.3f, 8f), 0.3f, 0.6f, new Color(0.55f, 0.35f, 0.25f));
                    CreateProp(root, "PlantPot_B", new Vector3(10f, 0.3f, -8f), 0.3f, 0.6f, new Color(0.55f, 0.35f, 0.25f));
                    CreateCube(root, "MilkBox", new Vector3(5f, 0.3f, 8.3f), new Vector3(0.5f, 0.6f, 0.4f), new Color(0.90f, 0.90f, 0.92f));
                    CreateCube(root, "ParcelStack", new Vector3(13f, 0.35f, 8.2f), new Vector3(0.9f, 0.7f, 0.7f), new Color(0.55f, 0.42f, 0.28f));
                    CreateCube(root, "Puddle_E", new Vector3(0f, 0.02f, -4f), new Vector3(1.6f, 0.02f, 1.1f), new Color(0.05f, 0.07f, 0.10f));
                    CreateCube(root, "Bench", new Vector3(-2f, 0.3f, 7.6f), new Vector3(1.8f, 0.6f, 0.5f), new Color(0.36f, 0.28f, 0.20f));
                    break;
                case "I4_YourHome":
                    CreateCube(root, "BookshelfHome", new Vector3(-5.5f, 1.0f, -4.3f), new Vector3(0.5f, 2f, 1.6f), new Color(0.34f, 0.28f, 0.22f));
                    CreateCube(root, "Laundry", new Vector3(-4.8f, 0.2f, 1.5f), new Vector3(0.9f, 0.4f, 0.7f), new Color(0.50f, 0.52f, 0.58f));
                    CreateCube(root, "PhotoSelf", new Vector3(2f, 1.9f, -4.65f), new Vector3(0.7f, 0.5f, 0.06f), new Color(0.82f, 0.78f, 0.66f));
                    CreateProp(root, "Mug_C", new Vector3(1.4f, 0.82f, -3.3f), 0.07f, 0.16f, new Color(0.85f, 0.85f, 0.88f));
                    CreateCube(root, "ChairHome", new Vector3(1f, 0.25f, -1.8f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.32f, 0.30f, 0.36f));
                    break;
                case "I4_VictimHome":
                    CreateCube(root, "FlowerVase", new Vector3(3.2f, 0.85f, -1.5f), new Vector3(0.25f, 0.4f, 0.25f), new Color(0.70f, 0.75f, 0.85f));
                    CreateCube(root, "TissueBox", new Vector3(-2f, 0.5f, 0.9f), new Vector3(0.35f, 0.15f, 0.25f), new Color(0.88f, 0.86f, 0.80f));
                    CreateCube(root, "PhotoFrameMore_A", new Vector3(1.5f, 1.85f, 4.65f), new Vector3(0.6f, 0.45f, 0.06f), new Color(0.80f, 0.76f, 0.64f));
                    CreateCube(root, "CurtainBox", new Vector3(-4f, 2.6f, 4.5f), new Vector3(3f, 0.5f, 0.2f), new Color(0.55f, 0.45f, 0.40f));
                    CreateCube(root, "CarpetVictim", new Vector3(-2f, 0.02f, 1.2f), new Vector3(3.4f, 0.02f, 2.2f), new Color(0.40f, 0.28f, 0.24f));
                    break;
            }
        }

        private static void AddNavBlockModifiers(Transform root)
        {
            Physics.SyncTransforms();
            var colliders = root.GetComponentsInChildren<BoxCollider>(true);
            foreach (var collider in colliders)
            {
                if (collider.isTrigger || collider.name == "Ground" || collider.name.StartsWith("StairStep") || collider.name == "StairPlatform" || collider.name == "StairRamp")
                {
                    continue;
                }

                if (Quaternion.Angle(collider.transform.rotation, Quaternion.identity) > 5f)
                {
                    continue;
                }

                Vector3 size = collider.bounds.size;
                if (size.y < 0.25f || size.x < 0.3f || size.z < 0.3f)
                {
                    continue;
                }

                if (collider.GetComponent<Unity.AI.Navigation.NavMeshModifier>() != null)
                {
                    continue;
                }

                var modifier = collider.gameObject.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = 1;
            }
        }

        private static void CreatePipeRun(Transform root, Vector3 center, float length, bool horizontalZ)
        {
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "PipeRun";
            pipe.transform.SetParent(root, false);
            pipe.transform.position = center;
            pipe.transform.rotation = horizontalZ ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 90f);
            pipe.transform.localScale = new Vector3(0.24f, length * 0.5f, 0.24f);
            pipe.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(new Color(0.30f, 0.28f, 0.26f));
        }

        #endregion
    }
}
