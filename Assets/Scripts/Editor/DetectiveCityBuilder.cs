using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Detective.EditorTools
{
    public static class DetectiveCityBuilder
    {
        private const string CityScenePath = "Assets/Scenes/DetectiveCity.unity";
        private const string CoreScenePath = "Assets/Scenes/Core.unity";
        private const string CityRootName = "City";
        private const float DistrictSpacing = 120f;
        private const float DistrictWidth = 100f;
        private const float DistrictDepth = 40f;
        private const float WallHeight = 3f;
        private const float StreetWidth = 14f;

        internal static readonly Dictionary<Color, Material> LitMaterials = new();
        internal static readonly Dictionary<Color, Material> UnlitMaterials = new();
        internal static Material rainMaterial;

        internal sealed class UIRefs
        {
            public DetectiveGameHUD hud;
            public GameObject hudGo;
            public DetectiveDialogueUI dialogueUI;
            public DetectiveVoiceCornerUI voiceCorner;
            public DetectiveMindPanelUI mindPanel;
            public DetectiveDuelRunner duelRunner;
            public DetectiveEndingUI endingUI;
            public DetectiveDistrictBannerUI banner;
            public DetectiveToastUI toast;
            public DetectiveFadeUI fade;
            public DetectiveMapUI map;
        }

        [MenuItem("Detective/Build City Scene")]
        public static void BuildCityScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != CityScenePath)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            else
            {
                GameObject oldRoot = GameObject.Find(CityRootName);
                if (oldRoot != null)
                {
                    Object.DestroyImmediate(oldRoot);
                }
            }

            LitMaterials.Clear();
            UnlitMaterials.Clear();

            var root = new GameObject(CityRootName);
            ConfigureRenderSettings();

            var navigationRoot = new GameObject("Navigation");
            navigationRoot.transform.SetParent(root.transform, false);

            BuildDistrict(root.transform, 0, "Prologue", new Color(0.16f, 0.22f, 0.32f), new Color(0.36f, 0.49f, 0.69f));
            BuildDistrict(root.transform, 1, "RedLight", new Color(0.10f, 0.16f, 0.20f), new Color(0.13f, 0.83f, 0.93f), new Color(0.88f, 0.31f, 0.79f));
            BuildDistrict(root.transform, 2, "Financial", new Color(0.20f, 0.23f, 0.27f), new Color(0.81f, 0.89f, 0.94f));
            BuildDistrict(root.transform, 3, "Industrial", new Color(0.22f, 0.17f, 0.11f), new Color(1.00f, 0.66f, 0.30f));
            BuildDistrict(root.transform, 4, "Residential", new Color(0.24f, 0.21f, 0.15f), new Color(1.00f, 0.85f, 0.56f));
            BuildStreets(root.transform);

            BuildPlayerRig(root.transform);
            BuildCameras(root.transform);
            BuildLighting(root.transform);
            BuildRain(root.transform);

            UIRefs refs = BuildSceneUI(root.transform);
            BuildSystems(root.transform, refs);

            var surface = navigationRoot.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScenePath);
            AddToBuildSettings();
            Debug.Log("[DetectiveCityBuilder] 城市场景构建完成：" + CityScenePath);
        }

        [MenuItem("Detective/Rebuild Scene UI (覆盖 UICanvas/EventSystem/Systems)")]
        public static void RebuildSceneUI()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != CoreScenePath)
            {
                Debug.LogWarning("[DetectiveCityBuilder] 请先打开 Core.unity 再执行 UI 重建。");
                return;
            }

            foreach (string name in new[] { "UICanvas", "EventSystem", "Systems" })
            {
                GameObject old = GameObject.Find(name);
                if (old != null)
                {
                    Object.DestroyImmediate(old);
                }
            }

            GameObject coreRoot = GameObject.Find("Core");
            if (coreRoot == null)
            {
                coreRoot = new GameObject("Core");
            }

            ResetMaterialCaches();
            UIRefs refs = BuildSceneUI(coreRoot.transform);

            var systems = new GameObject("Systems");
            systems.transform.SetParent(coreRoot.transform, false);
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

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.LogWarning("[DetectiveCityBuilder] Core UI 已重建：UICanvas/EventSystem/Systems 为全新对象，此前对 UI 的手动调整已覆盖。CoreNavMesh 与区域几何未动。");
        }

        [MenuItem("Detective/Repair Interactable References")]
        public static void RepairInteractableReferences()
        {
            Scene scene = SceneManager.GetActiveScene();
            int fixedNpcs = 0;
            int fixedClues = 0;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            foreach (TestInteractable interactable in Object.FindObjectsByType<TestInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(interactable);
                if (interactable.IsNpc)
                {
                    if (so.FindProperty("characterDefinition").objectReferenceValue != null)
                    {
                        continue;
                    }

                    string baseName = interactable.InteractionId;
                    if (baseName.EndsWith("Decision"))
                    {
                        baseName = baseName[..^"Decision".Length];
                    }

                    var character = LoadAsset<DetectiveCharacterDefinition>($"Assets/Data/Characters/NPC_{baseName}.asset");
                    if (character == null)
                    {
                        Debug.LogWarning($"[DetectiveCityBuilder] 未找到角色资产: NPC_{baseName}");
                        continue;
                    }

                    so.FindProperty("characterDefinition").objectReferenceValue = character;
                    so.FindProperty("displayName").stringValue = character.DisplayName;
                    fixedNpcs++;
                }
                else
                {
                    if (so.FindProperty("clueDefinition").objectReferenceValue != null)
                    {
                        continue;
                    }

                    string clueId = (string)typeof(TestInteractable).GetField("clueId", flags).GetValue(interactable);
                    if (string.IsNullOrWhiteSpace(clueId))
                    {
                        continue;
                    }

                    var clue = LoadAsset<DetectiveClueDefinition>($"Assets/Resources/Detective/Clues/{clueId}.asset");
                    if (clue == null)
                    {
                        Debug.LogWarning($"[DetectiveCityBuilder] 未找到线索资产: {clueId}");
                        continue;
                    }

                    so.FindProperty("clueDefinition").objectReferenceValue = clue;
                    so.FindProperty("displayName").stringValue = clue.Title;
                    fixedClues++;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(interactable);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DetectiveCityBuilder] 引用修复完成：NPC {fixedNpcs} 个，线索 {fixedClues} 个。");
        }

        internal static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.26f);
            RenderSettings.fog = false;
        }

        internal static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(entry => entry.path == CityScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(CityScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static void BuildSystems(Transform parent, UIRefs refs)
        {
            var systems = new GameObject("Systems");
            systems.transform.SetParent(parent, false);
            var runner = systems.AddComponent<DetectiveDialogueRunner>();
            var so = new SerializedObject(runner);
            so.FindProperty("dialogueUI").objectReferenceValue = refs.dialogueUI;
            so.FindProperty("voiceCornerUI").objectReferenceValue = refs.voiceCorner;
            so.ApplyModifiedPropertiesWithoutUndo();
            systems.AddComponent<DetectivePlotHooks>();
        }

        #region Districts & Streets

        internal static void BuildDistrict(Transform parent, int index, string label, Color groundColor, Color lightColor, Color accentColor = default)
        {
            float cx = index * DistrictSpacing;
            var district = new GameObject($"District_{index}_{label}");
            district.transform.SetParent(parent, false);

            CreateCube(district.transform, "Ground", new Vector3(cx, -0.25f, 0f), new Vector3(DistrictWidth, 0.5f, DistrictDepth), groundColor);
            CreateWall(district.transform, "Wall_North", new Vector3(cx, WallHeight * 0.5f, DistrictDepth * 0.5f), new Vector3(DistrictWidth, WallHeight, 0.5f));
            CreateWall(district.transform, "Wall_South", new Vector3(cx, WallHeight * 0.5f, -DistrictDepth * 0.5f), new Vector3(DistrictWidth, WallHeight, 0.5f));

            bool hasWestGap = index > 0;
            if (hasWestGap)
            {
                float segmentWidth = (DistrictDepth - 12f) * 0.5f;
                float segmentCenter = 6f + segmentWidth * 0.5f;
                CreateWall(district.transform, "Wall_West_A", new Vector3(cx - DistrictWidth * 0.5f, WallHeight * 0.5f, segmentCenter), new Vector3(0.5f, WallHeight, segmentWidth));
                CreateWall(district.transform, "Wall_West_B", new Vector3(cx - DistrictWidth * 0.5f, WallHeight * 0.5f, -segmentCenter), new Vector3(0.5f, WallHeight, segmentWidth));
            }
            else
            {
                CreateWall(district.transform, "Wall_West", new Vector3(cx - DistrictWidth * 0.5f, WallHeight * 0.5f, 0f), new Vector3(0.5f, WallHeight, DistrictDepth));
            }

            if (index == 4)
            {
                CreateWall(district.transform, "Wall_East", new Vector3(cx + DistrictWidth * 0.5f, WallHeight * 0.5f, 0f), new Vector3(0.5f, WallHeight, DistrictDepth));
            }

            var lightGo = new GameObject("DistrictLight");
            lightGo.transform.SetParent(district.transform, false);
            lightGo.transform.position = new Vector3(cx, 6f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = lightColor;
            light.intensity = 150f;
            light.range = 55f;

            if (accentColor != default)
            {
                var accentGo = new GameObject("AccentLight");
                accentGo.transform.SetParent(district.transform, false);
                accentGo.transform.position = new Vector3(cx - 20f, 5f, -8f);
                var accent = accentGo.AddComponent<Light>();
                accent.type = LightType.Point;
                accent.color = accentColor;
                accent.intensity = 60f;
                accent.range = 30f;
            }

            switch (index)
            {
                case 0: BuildPrologue(district.transform, cx); break;
                case 1: BuildRedLight(district.transform, cx); break;
                case 2: BuildFinancial(district.transform, cx); break;
                case 3: BuildIndustrial(district.transform, cx); break;
                case 4: BuildResidential(district.transform, cx); break;
            }
        }

        internal static void BuildStreets(Transform parent)
        {
            var streets = new GameObject("Streets");
            streets.transform.SetParent(parent, false);

            var boundaries = new (string name, int advanceTo)[]
            {
                ("红灯区", -1),
                ("金融街", 23 * 60 + 30),
                ("旧工业区", 25 * 60 + 30),
                ("住宅区", 27 * 60),
            };

            for (int i = 0; i < boundaries.Length; i++)
            {
                float centerX = (i + 0.5f) * DistrictSpacing;
                var street = new GameObject($"Street_{i}_To_{boundaries[i].name}");
                street.transform.SetParent(streets.transform, false);

                CreateCube(street.transform, "Ground", new Vector3(centerX, -0.25f, 0f), new Vector3(36f, 0.5f, StreetWidth), new Color(0.14f, 0.15f, 0.18f));
                CreateWall(street.transform, "Wall_North", new Vector3(centerX, WallHeight * 0.5f, StreetWidth * 0.5f), new Vector3(36f, WallHeight, 0.5f));
                CreateWall(street.transform, "Wall_South", new Vector3(centerX, WallHeight * 0.5f, -StreetWidth * 0.5f), new Vector3(36f, WallHeight, 0.5f));

                CreateStreetLamp(street.transform, new Vector3(centerX - 6f, 0f, StreetWidth * 0.5f - 1.2f));
                CreateStreetLamp(street.transform, new Vector3(centerX + 6f, 0f, -StreetWidth * 0.5f + 1.2f));
                CreateStreetSign(street.transform, new Vector3(centerX, 0f, StreetWidth * 0.5f - 1.4f), boundaries[i].name);

                var boundary = CreateTrigger(street.transform, $"Boundary_{boundaries[i].name}", new Vector3(centerX, 1.5f, 0f), new Vector3(2f, 3f, StreetWidth));
                var gate = boundary.AddComponent<DetectiveZoneGate>();
                var so = new SerializedObject(gate);
                so.FindProperty("districtName").stringValue = boundaries[i].name;
                so.FindProperty("advanceTimeToMinutes").intValue = boundaries[i].advanceTo;
                so.FindProperty("crossedFlag").stringValue = $"district_crossed_{i + 1}";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        internal static void CreateStreetLamp(Transform parent, Vector3 basePosition)
        {
            CreateProp(parent, "StreetLampPole", basePosition + new Vector3(0f, 1.75f, 0f), 0.10f, 3.5f, new Color(0.25f, 0.25f, 0.28f));
            CreateCube(parent, "StreetLampHead", basePosition + new Vector3(0f, 3.6f, 0f), new Vector3(0.5f, 0.22f, 0.5f), new Color(1f, 0.9f, 0.6f));

            var lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = basePosition + new Vector3(0f, 3.4f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.intensity = 30f;
            light.range = 14f;
        }

        internal static void CreateStreetSign(Transform parent, Vector3 basePosition, string districtName)
        {
            CreateProp(parent, "SignPole", basePosition + new Vector3(0f, 1.75f, 0f), 0.08f, 3.5f, new Color(0.3f, 0.3f, 0.34f));

            for (int i = 0; i < 2; i++)
            {
                var signGo = new GameObject($"Sign_{districtName}_{i}");
                signGo.transform.SetParent(parent, false);
                signGo.transform.position = basePosition + new Vector3(0f, 3.4f, 0f);
                signGo.transform.rotation = Quaternion.Euler(0f, i == 0 ? -90f : 90f, 0f);
                var tmp = signGo.AddComponent<TextMeshPro>();
                tmp.font = DetectiveUIWidgets.GetFont();
                tmp.fontSize = 2.2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.text = districtName;
                tmp.color = new Color(1f, 0.92f, 0.75f);
                tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);
            }
        }

        internal static void BuildPrologue(Transform parent, float cx)
        {
            CreateProp(parent, "TrashCan_A", new Vector3(cx - 8f, 0.5f, -8f), 0.6f, 1f, new Color(0.2f, 0.24f, 0.28f));
            CreateProp(parent, "TrashCan_B", new Vector3(cx - 7f, 0.5f, -9.5f), 0.6f, 1f, new Color(0.2f, 0.24f, 0.28f));
            CreateCube(parent, "Crate_A", new Vector3(cx + 6f, 0.5f, 8f), new Vector3(1.2f, 1f, 1.2f), new Color(0.32f, 0.26f, 0.18f));
            CreateCube(parent, "Crate_B", new Vector3(cx + 7.5f, 0.35f, 6.5f), new Vector3(0.9f, 0.7f, 0.9f), new Color(0.32f, 0.26f, 0.18f));

            CreateClue(parent, "Badge", new Vector3(cx - 5f, 0.15f, -4f), new Vector3(0.5f, 0.12f, 0.5f), new Color(0.85f, 0.72f, 0.30f), DetectiveClueIds.Badge, "警徽：编号NC-2077", null, true);
            CreateClue(parent, "BloodyNote", new Vector3(cx - 2f, 0.06f, -7f), new Vector3(0.45f, 0.05f, 0.45f), new Color(0.85f, 0.30f, 0.30f), DetectiveClueIds.BloodyNote, "染血纸条", null, true);
        }

        internal static void BuildRedLight(Transform parent, float cx)
        {
            CreateNeonSign(parent, "NeonSign_A", new Vector3(cx - 10f, 4f, -14f), new Vector3(6f, 1.5f, 0.3f), new Color(0.13f, 0.83f, 0.93f));
            CreateNeonSign(parent, "NeonSign_B", new Vector3(cx + 5f, 5f, -15f), new Vector3(4f, 1.2f, 0.3f), new Color(0.88f, 0.31f, 0.79f));

            // P1-3：对话完成后由 DetectiveNpcWalker 走向街角东侧再消失（不再用 gate 瞬隐）。
            var woman = CreateNpcGate(parent, "MysteriousWoman", new Vector3(cx - 15f, 1f, 6f), new Color(0.75f, 0.75f, 0.85f), "dlg_mysterious_woman", "NPC_MysteriousWoman", null, null, "dlg_mysterious_woman_done", new Vector3(cx + 20f, 0f, 5f));
            CreateProximityTrigger(parent, "WomanProximity", new Vector3(cx - 15f, 1.5f, 6f), new Vector3(7f, 3f, 7f), "dlg_mysterious_woman");

            CreateCube(parent, "Bar", new Vector3(cx + 22f, 2f, -12f), new Vector3(12f, 4f, 8f), new Color(0.24f, 0.16f, 0.18f));
            CreateNpcGate(parent, "Bartender", new Vector3(cx + 14f, 1f, -6f), new Color(0.65f, 0.55f, 0.45f), "dlg_bartender", "NPC_Bartender", null, null);

            CreateCube(parent, "PhoneBooth", new Vector3(cx - 28f, 1.2f, -8f), new Vector3(1.3f, 2.4f, 1.3f), new Color(0.75f, 0.10f, 0.10f));
            CreateProximityTrigger(parent, "PhoneBoothProximity", new Vector3(cx - 28f, 1.5f, -8f), new Vector3(4f, 3f, 4f), "dlg_phone_booth");
        }

        internal static void BuildFinancial(Transform parent, float cx)
        {
            CreateCube(parent, "ApartmentBuilding", new Vector3(cx - 8f, 4f, -14f), new Vector3(34f, 8f, 10f), new Color(0.36f, 0.40f, 0.46f));

            CreateClue(parent, "DoorPanel", new Vector3(cx - 20f, 1.3f, -8.6f), new Vector3(0.5f, 0.8f, 0.2f), new Color(0.30f, 0.60f, 0.90f), DetectiveClueIds.DoorRecord, "门禁系统", null, false);
            CreateClue(parent, "BulletHole", new Vector3(cx - 12f, 1.6f, -8.8f), new Vector3(0.25f, 0.25f, 0.1f), new Color(0.05f, 0.05f, 0.06f), DetectiveClueIds.BulletHole9mm, "墙上弹孔", null, false);
            CreateClue(parent, "Corpse", new Vector3(cx - 8f, 0.30f, -4f), new Vector3(1.9f, 0.6f, 0.9f), new Color(0.88f, 0.88f, 0.90f), DetectiveClueIds.CorpseChestWound, "尸体", "dlg_corpse", false);
            CreateClue(parent, "TakeoutBox", new Vector3(cx - 4f, 0.18f, -5f), new Vector3(0.5f, 0.35f, 0.5f), new Color(0.85f, 0.60f, 0.25f), DetectiveClueIds.TakeoutBox, "打翻的外卖", null, true);
            CreateClue(parent, "TornPhoto", new Vector3(cx - 1f, 0.06f, -6f), new Vector3(0.4f, 0.04f, 0.4f), new Color(0.90f, 0.88f, 0.80f), DetectiveClueIds.TornPhoto, "碎照片", null, true);

            CreateBedroom(parent, cx);
            CreateNpcGate(parent, "Guard", new Vector3(cx - 15f, 1f, 8f), new Color(0.30f, 0.40f, 0.60f), "dlg_guard", "NPC_Guard", null, null);
            CreateNpcGate(parent, "GuardDecision", new Vector3(cx - 12f, 1f, 8f), new Color(0.30f, 0.40f, 0.60f), "dlg_guard_decision", "NPC_Guard", new[] { "dlg_guard_done" }, null);
            CreateNpcGate(parent, "Beggar", new Vector3(cx - 28f, 1f, 12f), new Color(0.45f, 0.40f, 0.35f), "dlg_beggar", "NPC_Beggar", new[] { "unlock_beggar" }, null);

            CreateCube(parent, "CoffeeShop", new Vector3(cx + 32f, 2f, 12f), new Vector3(10f, 4f, 7f), new Color(0.42f, 0.34f, 0.26f));
            CreateNpcGate(parent, "Barista", new Vector3(cx + 25f, 1f, 8f), new Color(0.80f, 0.60f, 0.55f), "dlg_barista", "NPC_Barista", null, null);

            var duelTrigger = CreateTrigger(parent, "DuelGateTrigger", new Vector3(cx - 6f, 1.5f, 2f), new Vector3(10f, 3f, 5f));
            var duel = duelTrigger.AddComponent<DetectiveFinalDuelTrigger>();
            SetObjectField(duel, "duelDefinition", LoadAsset<DetectiveDuelDefinition>("Assets/Data/Duels/duel_trenchcoat_gate.asset"));
            SetIntField(duel, "minClueCount", 4);
            SetStringField(duel, "doneFlag", "duel_trenchcoat_triggered");
            SetStringField(duel, "degradedRequiredClueId", DetectiveClueIds.Recording);
            SetStringField(duel, "degradedMessage", "风衣男撑着黑伞拦住你：“你不该查下去的。”（专注力 -10，拿到录音再来。）");
            SetIntField(duel, "degradedFocusCost", 10);

            // P1-3：风衣男站在对决触发区；对决胜利（获得"风衣男承认在场"线索）后向东逃走。
            CreateNpcGate(parent, "TrenchcoatMan", new Vector3(cx - 6f, 1f, 0.5f), new Color(0.25f, 0.28f, 0.22f), null, "NPC_Trenchcoat", null, null, null, new Vector3(cx + 30f, 1f, 0f), DetectiveClueIds.CoatManConfessed);
        }

        internal static void CreateBedroom(Transform parent, float cx)
        {
            float rx = cx + 16f;
            float rz = -12f;
            CreateCube(parent, "Bedroom_Back", new Vector3(rx, 1.5f, rz - 4f), new Vector3(12f, 3f, 0.4f), new Color(0.40f, 0.42f, 0.48f));
            CreateCube(parent, "Bedroom_Left", new Vector3(rx - 6f, 1.5f, rz), new Vector3(0.4f, 3f, 8f), new Color(0.40f, 0.42f, 0.48f));
            CreateCube(parent, "Bedroom_Right", new Vector3(rx + 6f, 1.5f, rz), new Vector3(0.4f, 3f, 8f), new Color(0.40f, 0.42f, 0.48f));

            CreateCube(parent, "Nightstand", new Vector3(rx - 4f, 0.35f, rz - 2.5f), new Vector3(0.9f, 0.7f, 0.9f), new Color(0.35f, 0.28f, 0.20f));
            CreateClue(parent, "SleepingPills", new Vector3(rx - 4f, 0.85f, rz - 2.5f), new Vector3(0.25f, 0.3f, 0.25f), new Color(0.90f, 0.90f, 0.95f), DetectiveClueIds.SleepingPills, "安眠药", null, true);

            CreateCube(parent, "Wardrobe", new Vector3(rx + 4f, 1.2f, rz - 3f), new Vector3(1.6f, 2.4f, 0.8f), new Color(0.32f, 0.26f, 0.20f));
            CreateClue(parent, "TrenchcoatItem", new Vector3(rx + 4f, 1.0f, rz - 1.8f), new Vector3(0.6f, 1.2f, 0.3f), new Color(0.25f, 0.28f, 0.22f), DetectiveClueIds.TrenchcoatItem, "衣柜里的风衣", null, true);

            CreateCube(parent, "Desk", new Vector3(rx - 1f, 0.4f, rz - 3.2f), new Vector3(1.6f, 0.8f, 0.8f), new Color(0.38f, 0.30f, 0.22f));
            CreateClue(parent, "DrawerDiary", new Vector3(rx - 1f, 0.95f, rz - 3.2f), new Vector3(0.5f, 0.3f, 0.4f), new Color(0.85f, 0.80f, 0.65f), DetectiveClueIds.DiaryThreatened, "书桌抽屉（日记）", null, false, DetectiveVoiceType.Logic, 2, "dlg_drawer_diary_locked", "dlg_drawer_diary");

            var safe = CreateClue(parent, "Safe", new Vector3(rx + 2.5f, 0.6f, rz - 3.2f), new Vector3(0.9f, 1.2f, 0.9f), new Color(0.20f, 0.22f, 0.26f), DetectiveClueIds.Recording, "保险箱", null, false);
            var safeGate = safe.AddComponent<DetectiveFlagGatedObject>();
            SetObjectArrayField(safeGate, "requiredFlags", new[] { "dlg_drawer_diary_done" });
        }

        internal static void BuildIndustrial(Transform parent, float cx)
        {
            CreateClue(parent, "IronGate", new Vector3(cx - 42f, 1.5f, 0f), new Vector3(0.4f, 3f, 4f), new Color(0.35f, 0.30f, 0.25f), DetectiveClueIds.GateForced, "铁门", null, false);
            CreateClue(parent, "TireTracks", new Vector3(cx - 36f, 0.03f, 4f), new Vector3(3.5f, 0.05f, 0.6f), new Color(0.10f, 0.09f, 0.08f), DetectiveClueIds.TireTracks, "地上车辙", null, false);

            CreateCube(parent, "GraffitiWall", new Vector3(cx - 30f, 1.5f, -10f), new Vector3(6f, 3f, 0.4f), new Color(0.30f, 0.28f, 0.26f));
            CreateClue(parent, "Graffiti", new Vector3(cx - 30f, 1.6f, -9.7f), new Vector3(2.5f, 1.2f, 0.1f), new Color(0.85f, 0.15f, 0.15f), DetectiveClueIds.GraffitiTraitor, "墙上涂鸦", "dlg_graffiti", false);

            CreateCube(parent, "Warehouse", new Vector3(cx + 5f, 3f, -12f), new Vector3(18f, 6f, 12f), new Color(0.30f, 0.27f, 0.22f));
            CreateNpcGate(parent, "Monkey", new Vector3(cx - 4f, 1f, -4f), new Color(0.60f, 0.58f, 0.50f), "dlg_monkey", "NPC_Monkey", null, null);
            CreateNpcGate(parent, "MonkeyDecision", new Vector3(cx - 1f, 1f, -4f), new Color(0.60f, 0.58f, 0.50f), "dlg_monkey_decision", "NPC_Monkey", new[] { "dlg_monkey_done" }, null);

            float bx = cx + 24f;
            float bz = 10f;
            CreateCube(parent, "Basement_Back", new Vector3(bx, 1.5f, bz + 4f), new Vector3(12f, 3f, 0.4f), new Color(0.32f, 0.28f, 0.24f));
            CreateCube(parent, "Basement_Left", new Vector3(bx - 6f, 1.5f, bz), new Vector3(0.4f, 3f, 8f), new Color(0.32f, 0.28f, 0.24f));
            CreateCube(parent, "Basement_Right", new Vector3(bx + 6f, 1.5f, bz), new Vector3(0.4f, 3f, 8f), new Color(0.32f, 0.28f, 0.24f));

            CreateClue(parent, "BloodTrail", new Vector3(bx - 3f, 0.03f, bz + 1f), new Vector3(2.5f, 0.05f, 0.5f), new Color(0.45f, 0.08f, 0.08f), DetectiveClueIds.BloodTrail, "血迹", null, false);
            CreateClue(parent, "Toolbox", new Vector3(bx - 1f, 0.3f, bz - 2f), new Vector3(0.8f, 0.6f, 0.5f), new Color(0.55f, 0.35f, 0.15f), DetectiveClueIds.LockpickTool, "工具箱", null, true);
            CreateClue(parent, "PhotoWall", new Vector3(bx + 3f, 1.5f, bz + 3.7f), new Vector3(1.4f, 1.0f, 0.1f), new Color(0.85f, 0.82f, 0.70f), DetectiveClueIds.FormerPartners, "照片墙", null, false);
            CreateClue(parent, "Computer", new Vector3(bx + 4.5f, 0.9f, bz - 2.5f), new Vector3(0.9f, 0.8f, 0.6f), new Color(0.15f, 0.18f, 0.22f), DetectiveClueIds.WasUndercover, "电脑", null, false, DetectiveVoiceType.Logic, 3, "dlg_computer_locked", "dlg_computer");

            float px = cx - 22f;
            float pz = -12f;
            CreateCube(parent, "RooftopPlatform", new Vector3(px, 2.4f, pz), new Vector3(8f, 0.4f, 8f), new Color(0.35f, 0.32f, 0.28f));
            var ramp = CreateCube(parent, "RooftopRamp", new Vector3(px + 7.2f, 1.3f, pz), new Vector3(7.5f, 0.3f, 3f), new Color(0.35f, 0.32f, 0.28f));
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, -20f);
            CreateNpcGate(parent, "Sniper", new Vector3(px, 3.4f, pz), new Color(0.20f, 0.22f, 0.24f), "dlg_sniper", "NPC_Sniper", null, null, "dlg_sniper_done", new Vector3(px - 1f, 3.4f, pz - 1.5f));
        }

        internal static void BuildResidential(Transform parent, float cx)
        {
            CreateClue(parent, "StrayCat", new Vector3(cx - 35f, 0.20f, 5f), new Vector3(0.35f, 0.4f, 0.6f), new Color(0.55f, 0.50f, 0.45f), DetectiveClueIds.CatCollar, "流浪猫", null, true);
            CreateCube(parent, "Mailbox", new Vector3(cx - 30f, 0.6f, -5f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.40f, 0.45f, 0.55f));
            CreateClue(parent, "LetterBox", new Vector3(cx - 30f, 1.05f, -5f), new Vector3(0.45f, 0.4f, 0.45f), new Color(0.75f, 0.72f, 0.60f), DetectiveClueIds.LetterWarning, "信箱", null, false);

            CreateProp(parent, "StreetlampPole", new Vector3(cx - 20f, 1.75f, -2f), 0.12f, 3.5f, new Color(0.25f, 0.25f, 0.28f));
            CreateCube(parent, "StreetlampHead", new Vector3(cx - 20f, 3.6f, -2f), new Vector3(0.6f, 0.25f, 0.6f), new Color(1f, 0.9f, 0.6f));
            CreateClue(parent, "StreetlampMarks", new Vector3(cx - 20f, 1.2f, -1.7f), new Vector3(0.3f, 0.8f, 0.15f), new Color(0.70f, 0.65f, 0.50f), DetectiveClueIds.StreetlampScratches, "路灯刻痕", null, false, DetectiveVoiceType.None, 0, null, null, new[] { new VoiceDelta(DetectiveVoiceType.Madness, 1) });

            CreateCube(parent, "YourApartment", new Vector3(cx + 10f, 3f, -13f), new Vector3(20f, 6f, 10f), new Color(0.42f, 0.38f, 0.32f));
            CreateClue(parent, "Mirror", new Vector3(cx + 4f, 1.5f, -7.6f), new Vector3(0.9f, 1.4f, 0.1f), new Color(0.70f, 0.78f, 0.85f), DetectiveClueIds.MirrorBruise, "镜子", null, false, DetectiveVoiceType.None, 0, null, null, new[] { new VoiceDelta(DetectiveVoiceType.Madness, 1) });
            CreateClue(parent, "PillBottle", new Vector3(cx + 8f, 0.55f, -7f), new Vector3(0.25f, 0.5f, 0.25f), new Color(0.90f, 0.90f, 0.92f), DetectiveClueIds.SleepingPillsEmpty, "药瓶", null, true);
            CreateClue(parent, "DismissalNotice", new Vector3(cx + 12f, 0.75f, -7f), new Vector3(0.5f, 0.35f, 0.4f), new Color(0.85f, 0.80f, 0.65f), DetectiveClueIds.DismissalNotice, "抽屉（开除通知）", null, true);
            CreateClue(parent, "YourDiary", new Vector3(cx + 16f, 0.8f, -7f), new Vector3(0.5f, 0.3f, 0.4f), new Color(0.80f, 0.70f, 0.55f), DetectiveClueIds.YourDiaryDoubt, "日记本", null, false, DetectiveVoiceType.Empathy, 2, "dlg_your_diary_locked", "dlg_your_diary");
            CreateNpcGate(parent, "LeavingHome", new Vector3(cx + 10f, 1f, -8f), new Color(0.55f, 0.55f, 0.60f), "dlg_leaving_home", "NPC_Narrator", null, new[] { "dlg_leaving_home_done" });

            var finalTrigger = CreateTrigger(parent, "FinalDuelTrigger", new Vector3(cx + 10f, 1.5f, -4f), new Vector3(8f, 3f, 4f));
            var finalDuel = finalTrigger.AddComponent<DetectiveFinalDuelTrigger>();
            SetObjectField(finalDuel, "duelDefinition", LoadAsset<DetectiveDuelDefinition>("Assets/Data/Duels/duel_final.asset"));
            SetIntField(finalDuel, "minClueCount", 10);
            SetStringField(finalDuel, "doneFlag", "final_duel_triggered");

            CreateCube(parent, "VictimHome", new Vector3(cx + 36f, 3f, 11f), new Vector3(14f, 6f, 9f), new Color(0.44f, 0.40f, 0.36f));
            CreateNpcGate(parent, "Wife", new Vector3(cx + 30f, 1f, 5f), new Color(0.75f, 0.65f, 0.65f), "dlg_wife", "NPC_Wife", null, null);
            CreateNpcGate(parent, "WifeDecision", new Vector3(cx + 33f, 1f, 5f), new Color(0.75f, 0.65f, 0.65f), "dlg_wife_decision", "NPC_Wife", new[] { "dlg_wife_done" }, null);
        }

        #endregion

        #region Player / Cameras / Lighting / Rain

        internal static void BuildPlayerRig(Transform parent)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "DetectivePlayer";
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(0f, 1f, 2f);
            player.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(new Color(0.55f, 0.60f, 0.70f));

            var agent = player.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.speed = 3.5f;
            agent.angularSpeed = 720f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.1f;
            agent.baseOffset = 0f;
            // 玩家在 Core 待机区的位置离 CoreNavMesh 较远，启用即报 "not close enough to the NavMesh"；
            // 先禁用，DetectiveClickMover 吸附到区域 NavMesh 时再启用。
            agent.enabled = false;

            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            player.AddComponent<DetectiveClickMover>();
            player.AddComponent<InteractionController>();
        }

        internal static void BuildCameras(Transform parent)
        {
            var vcam = new GameObject("DetectiveCamera");
            vcam.transform.SetParent(parent, false);
            vcam.transform.position = new Vector3(0f, 1f, -10f);
            var cineCam = vcam.AddComponent<CinemachineCamera>();
            LensSettings lens = cineCam.Lens;
            lens.FieldOfView = 45f;
            lens.NearClipPlane = 0.1f;
            lens.FarClipPlane = 5000f;
            cineCam.Lens = lens;
            vcam.AddComponent<CinemachineFollow>();
            cineCam.Follow = GameObject.Find("DetectivePlayer").transform;
            vcam.AddComponent<DetectiveCameraZoom>();

            var mainCamGo = new GameObject("Main Camera");
            mainCamGo.tag = "MainCamera";
            mainCamGo.transform.SetParent(parent, false);
            mainCamGo.transform.position = new Vector3(0f, 13f, -10f);
            mainCamGo.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            var camera = mainCamGo.AddComponent<Camera>();
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 5000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f);
            mainCamGo.AddComponent<AudioListener>();
            var brain = mainCamGo.AddComponent<CinemachineBrain>();
            brain.enabled = false;
            camera.GetUniversalAdditionalCameraData();
            mainCamGo.AddComponent<DetectiveFollowCamera>();
            mainCamGo.AddComponent<DetectiveCameraOcclusion>();

            var followCamera = mainCamGo.GetComponent<DetectiveFollowCamera>();
            var followSo = new SerializedObject(followCamera);
            followSo.FindProperty("followSmoothTime").floatValue = 0.45f;
            followSo.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void BuildLighting(Transform parent)
        {
            var lightGo = new GameObject("DirectionalLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.62f, 0.71f, 0.85f);
            light.intensity = 0.5f;
        }

        internal static void BuildRain(Transform parent)
        {
            var rainRoot = new GameObject("Rain");
            rainRoot.transform.SetParent(parent, false);

            for (int i = 0; i < 5; i++)
            {
                float cx = i * DistrictSpacing;
                var go = new GameObject($"Rain_District_{i}");
                go.transform.SetParent(rainRoot.transform, false);
                go.transform.position = new Vector3(cx, 8f, 0f);
                var ps = go.AddComponent<ParticleSystem>();

                var main = ps.main;
                main.startLifetime = 1.1f;
                main.startSpeed = 0f;
                main.startSize = 0.045f;
                main.maxParticles = 4000;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = ps.emission;
                emission.rateOverTime = 500f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(80f, 0.1f, 44f);

                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.y = -22f;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 2.2f;
                renderer.velocityScale = 0.05f;
                renderer.sharedMaterial = GetRainMaterial();
            }

            for (int i = 0; i < 4; i++)
            {
                float cx = (i + 0.5f) * DistrictSpacing;
                var go = new GameObject($"Rain_Street_{i}");
                go.transform.SetParent(rainRoot.transform, false);
                go.transform.position = new Vector3(cx, 8f, 0f);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 1.1f;
                main.startSpeed = 0f;
                main.startSize = 0.045f;
                main.maxParticles = 1500;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 200f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(24f, 0.1f, StreetWidth);
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.y = -22f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 2.2f;
                renderer.velocityScale = 0.05f;
                renderer.sharedMaterial = GetRainMaterial();
            }
        }

        #endregion

        #region Scene UI (edit-time)

        internal static UIRefs BuildSceneUI(Transform parent)
        {
            var refs = new UIRefs();

            Transform existingCanvas = parent.Find("UICanvas");
            GameObject canvasGo;
            if (existingCanvas != null)
            {
                canvasGo = existingCanvas.gameObject;
            }
            else
            {
                canvasGo = new GameObject("UICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(parent, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (parent.Find("EventSystem") == null)
            {
                var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystemGo.transform.SetParent(parent, false);
            }

            BuildHudUI(canvasGo.transform, refs);
            BuildDialogueUI(canvasGo.transform, refs);
            BuildVoiceCornerUI(canvasGo.transform, refs);
            BuildMindPanelUI(canvasGo.transform, refs);
            BuildDuelUI(canvasGo.transform, refs);
            BuildEndingUI(canvasGo.transform, refs);
            BuildBannerUI(canvasGo.transform, refs);
            BuildToastUI(canvasGo.transform, refs);
            BuildFadeUI(canvasGo.transform, refs);
            BuildMapUI(canvasGo.transform, refs);

            return refs;
        }

        private static void BuildFadeUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("Fade");
            if (existing != null)
            {
                refs.fade = existing.GetComponent<DetectiveFadeUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "Fade", true);
            var image = DetectiveUIWidgets.CreateImage(root, "Image", Color.black);
            DetectiveUIWidgets.Stretch(image.rectTransform, 0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var ui = root.gameObject.AddComponent<DetectiveFadeUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("image").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetAsLastSibling();
            refs.fade = ui;
        }

        private static RectTransform UIRoot(Transform parent, string name, bool active)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.SetActive(active);
            return rect;
        }

        private static void BuildHudUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("GameHUD");
            if (existing != null)
            {
                refs.hud = existing.GetComponent<DetectiveGameHUD>();
                refs.hudGo = existing.gameObject;
                return;
            }

            RectTransform root = UIRoot(canvas, "GameHUD", true);

            var timeText = DetectiveUIWidgets.CreateText(root, "TimeText", "22:00", 38, Color.white);
            timeText.fontStyle = FontStyles.Bold;
            Place(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(220f, 50f));
            timeText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var barBg = DetectiveUIWidgets.CreateImage(root, "FocusBarBackground", new Color(0f, 0f, 0f, 0.6f));
            barBg.raycastTarget = false;
            Place(barBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(320f, 16f));

            var label = DetectiveUIWidgets.CreateText(root, "FocusLabel", "专注力", 16, new Color(1f, 1f, 1f, 0.75f));
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -72f), new Vector2(80f, 20f));
            label.horizontalAlignment = HorizontalAlignmentOptions.Right;

            var fill = DetectiveUIWidgets.CreateImage(barBg.transform, "FocusFill", new Color32(0x5D, 0xBB, 0x63, 0xFF));
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            DetectiveUIWidgets.Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);

            var percent = DetectiveUIWidgets.CreateText(root, "FocusPercent", "100", 16, Color.white);
            Place(percent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(170f, -72f), new Vector2(60f, 20f));
            percent.horizontalAlignment = HorizontalAlignmentOptions.Left;

            var hud = root.gameObject.AddComponent<DetectiveGameHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("timeText").objectReferenceValue = timeText;
            so.FindProperty("focusPercentText").objectReferenceValue = percent;
            so.FindProperty("focusFill").objectReferenceValue = fill;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.hud = hud;
            refs.hudGo = root.gameObject;
        }

        private static void BuildDialogueUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("DialoguePanel");
            if (existing != null)
            {
                refs.dialogueUI = existing.GetComponent<DetectiveDialogueUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "DialoguePanel", false);

            var panel = DetectiveUIWidgets.CreateImage(root, "Panel", new Color(0f, 0f, 0f, 0.82f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(0f, 0f);
            panelRect.offsetMax = new Vector2(0f, 300f);
            var panelButton = panel.gameObject.AddComponent<Button>();
            panelButton.targetGraphic = panel;

            var speaker = DetectiveUIWidgets.CreateText(panel.transform, "SpeakerName", "", 24, new Color32(0xD9, 0xA8, 0x4A, 0xFF));
            speaker.fontStyle = FontStyles.Bold;
            Place(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(600f, 36f));

            var body = DetectiveUIWidgets.CreateText(panel.transform, "BodyText", "", 26, Color.white);
            DetectiveUIWidgets.Stretch(body.rectTransform, 28f, 28f, 60f, 48f);
            body.verticalAlignment = VerticalAlignmentOptions.Top;

            var hint = DetectiveUIWidgets.CreateText(panel.transform, "ContinueHint", "▼ 点击继续", 18, new Color(1f, 1f, 1f, 0.6f));
            Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 12f), new Vector2(180f, 28f));
            hint.horizontalAlignment = HorizontalAlignmentOptions.Right;

            var optionsGo = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
            optionsGo.transform.SetParent(root, false);
            var optionsRect = (RectTransform)optionsGo.transform;
            optionsRect.anchorMin = new Vector2(0f, 0f);
            optionsRect.anchorMax = new Vector2(1f, 0f);
            optionsRect.pivot = new Vector2(0.5f, 0f);
            optionsRect.anchoredPosition = new Vector2(0f, 320f);
            optionsRect.sizeDelta = new Vector2(0f, 430f);
            var layout = optionsGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var templates = new GameObject("Templates");
            templates.transform.SetParent(root, false);
            var optionTemplate = BuildOptionButtonTemplate(templates.transform);
            optionTemplate.SetActive(false);

            var ui = root.gameObject.AddComponent<DetectiveDialogueUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("panelRoot").objectReferenceValue = root.gameObject;
            so.FindProperty("continueButton").objectReferenceValue = panelButton;
            so.FindProperty("speakerText").objectReferenceValue = speaker;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("hintText").objectReferenceValue = hint;
            so.FindProperty("optionsContainer").objectReferenceValue = optionsRect;
            so.FindProperty("optionButtonTemplate").objectReferenceValue = optionTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.dialogueUI = ui;
        }

        private static GameObject BuildOptionButtonTemplate(Transform parent)
        {
            var go = new GameObject("OptionButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 980f;
            layoutElement.preferredHeight = 64f;
            var label = DetectiveUIWidgets.CreateText(go.transform, "Label", "选项", 24, Color.white);
            DetectiveUIWidgets.Stretch(label.rectTransform, 20f, 20f, 6f, 6f);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            return go;
        }

        private static void BuildVoiceCornerUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("VoiceCorners");
            if (existing != null)
            {
                refs.voiceCorner = existing.GetComponent<DetectiveVoiceCornerUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "VoiceCorners", true);

            var entries = new (DetectiveVoiceType voice, Vector2 anchor, Vector2 pos)[]
            {
                (DetectiveVoiceType.Logic, new Vector2(0f, 1f), new Vector2(40f, -40f)),
                (DetectiveVoiceType.Empathy, new Vector2(1f, 1f), new Vector2(-40f, -40f)),
                (DetectiveVoiceType.Authority, new Vector2(0f, 1f), new Vector2(40f, -300f)),
                (DetectiveVoiceType.Madness, new Vector2(1f, 1f), new Vector2(-40f, -300f)),
            };

            var ui = root.gameObject.AddComponent<DetectiveVoiceCornerUI>();
            var so = new SerializedObject(ui);
            var array = so.FindProperty("entryRefs");
            array.arraySize = 4;

            for (int i = 0; i < entries.Length; i++)
            {
                var (voice, anchor, pos) = entries[i];
                Color color = DetectiveVoiceStyle.GetColor(voice);
                bool bubbleOnRight = anchor.x < 0.5f;

                var entryRoot = new GameObject($"Voice_{voice}", typeof(RectTransform));
                entryRoot.transform.SetParent(root, false);
                var entryRect = (RectTransform)entryRoot.transform;
                entryRect.anchorMin = anchor;
                entryRect.anchorMax = anchor;
                entryRect.pivot = anchor;
                entryRect.anchoredPosition = pos;
                entryRect.sizeDelta = new Vector2(90f, 110f);

                var circle = DetectiveUIWidgets.CreateImage(entryRoot.transform, "Circle", color);
                circle.sprite = DetectiveUIWidgets.GetCircleSprite();
                circle.raycastTarget = false;
                var circleRect = circle.rectTransform;
                circleRect.anchorMin = new Vector2(0.5f, 1f);
                circleRect.anchorMax = new Vector2(0.5f, 1f);
                circleRect.pivot = new Vector2(0.5f, 1f);
                circleRect.anchoredPosition = Vector2.zero;
                circleRect.sizeDelta = new Vector2(64f, 64f);
                Color idleColor = color;
                idleColor.a = 0.35f;
                circle.color = idleColor;

                var nameLabel = DetectiveUIWidgets.CreateText(entryRoot.transform, "Name", DetectiveVoiceStyle.GetName(voice), 16, color);
                var nameRect = nameLabel.rectTransform;
                nameRect.anchorMin = new Vector2(0.5f, 0f);
                nameRect.anchorMax = new Vector2(0.5f, 0f);
                nameRect.pivot = new Vector2(0.5f, 0f);
                nameRect.anchoredPosition = new Vector2(0f, 26f);
                nameRect.sizeDelta = new Vector2(120f, 24f);
                nameLabel.horizontalAlignment = HorizontalAlignmentOptions.Center;

                var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Button));
                bubbleGo.transform.SetParent(entryRoot.transform, false);
                var bubbleRect = (RectTransform)bubbleGo.transform;
                bubbleRect.anchorMin = bubbleOnRight ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                bubbleRect.anchorMax = bubbleRect.anchorMin;
                bubbleRect.pivot = bubbleOnRight ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                bubbleRect.anchoredPosition = bubbleOnRight ? new Vector2(76f, 12f) : new Vector2(-76f, 12f);
                bubbleRect.sizeDelta = new Vector2(340f, 120f);
                var bubbleImage = bubbleGo.GetComponent<Image>();
                bubbleImage.color = new Color(0.05f, 0.05f, 0.06f, 0.92f);
                var bubbleGroup = bubbleGo.GetComponent<CanvasGroup>();
                bubbleGroup.alpha = 0f;
                var bubbleButton = bubbleGo.GetComponent<Button>();
                bubbleButton.targetGraphic = bubbleImage;
                var bubbleText = DetectiveUIWidgets.CreateText(bubbleGo.transform, "Text", "", 20, color);
                DetectiveUIWidgets.Stretch(bubbleText.rectTransform, 14f, 14f, 10f, 10f);
                bubbleText.verticalAlignment = VerticalAlignmentOptions.Top;

                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = entryRect;
                element.FindPropertyRelative("circle").objectReferenceValue = circle;
                element.FindPropertyRelative("bubbleGroup").objectReferenceValue = bubbleGroup;
                element.FindPropertyRelative("bubbleText").objectReferenceValue = bubbleText;
                element.FindPropertyRelative("bubbleButton").objectReferenceValue = bubbleButton;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            refs.voiceCorner = ui;
        }

        private static void BuildMindPanelUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("MindPanel");
            if (existing != null)
            {
                refs.mindPanel = existing.GetComponent<DetectiveMindPanelUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "MindPanel", true);
            var windowGo = new GameObject("Window", typeof(RectTransform));
            windowGo.transform.SetParent(root, false);
            var windowRect = (RectTransform)windowGo.transform;
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.offsetMin = Vector2.zero;
            windowRect.offsetMax = Vector2.zero;
            windowGo.SetActive(false);
            Transform win = windowGo.transform;

            var mask = DetectiveUIWidgets.CreateImage(win, "Mask", new Color(0f, 0f, 0f, 0.88f));
            DetectiveUIWidgets.Stretch(mask.rectTransform, 0f, 0f, 0f, 0f);
            var maskButton = mask.gameObject.AddComponent<Button>();
            maskButton.targetGraphic = mask;

            var title = DetectiveUIWidgets.CreateText(win, "Title", "思维面板", 36, Color.white);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 52f));
            title.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var focus = DetectiveUIWidgets.CreateText(win, "FocusText", "专注力：100", 24, new Color(1f, 1f, 1f, 0.85f));
            Place(focus.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(400f, 32f));
            focus.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var gridLabel = DetectiveUIWidgets.CreateText(win, "GridLabel", "已收集线索（拖两张到右侧槽位，点击【推理】）", 20, new Color(1f, 1f, 1f, 0.7f));
            Place(gridLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(70f, -146f), new Vector2(900f, 30f));

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportGo.transform.SetParent(win, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(0.62f, 1f);
            viewportRect.offsetMin = new Vector2(60f, 60f);
            viewportRect.offsetMax = new Vector2(-20f, -190f);
            viewportGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.6f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var grid = contentGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230f, 100f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var slotLabel = DetectiveUIWidgets.CreateText(win, "SlotLabel", "推理槽位", 22, new Color(1f, 1f, 1f, 0.85f));
            Place(slotLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-130f, 170f), new Vector2(260f, 32f));
            slotLabel.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var slotRects = new RectTransform[2];
            for (int i = 0; i < 2; i++)
            {
                var slot = DetectiveUIWidgets.CreateImage(win, $"Slot{i + 1}", new Color(0.16f, 0.16f, 0.2f, 0.95f));
                var slotRect = slot.rectTransform;
                slotRect.anchorMin = new Vector2(1f, 0.5f);
                slotRect.anchorMax = new Vector2(1f, 0.5f);
                slotRect.pivot = new Vector2(1f, 0.5f);
                slotRect.anchoredPosition = new Vector2(-130f, 90f - i * 130f);
                slotRect.sizeDelta = new Vector2(260f, 110f);
                slotRects[i] = slotRect;
            }

            var reasonButton = BuildTextButton(windowRect, "ReasonButton", "【推理】", new Vector2(-130f, -110f), new Vector2(260f, 56f), new Color(0.16f, 0.36f, 0.22f, 0.95f), 26);
            var clearButton = BuildTextButton(windowRect, "ClearButton", "【清空】", new Vector2(-130f, -185f), new Vector2(260f, 48f), new Color(0.30f, 0.22f, 0.18f, 0.95f), 22);

            var dragLayer = new GameObject("DragLayer", typeof(RectTransform));
            dragLayer.transform.SetParent(win, false);
            var dragRect = (RectTransform)dragLayer.transform;
            dragRect.anchorMin = Vector2.zero;
            dragRect.anchorMax = Vector2.one;
            dragRect.offsetMin = Vector2.zero;
            dragRect.offsetMax = Vector2.zero;

            var bannerGo = new GameObject("Banner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Button));
            bannerGo.transform.SetParent(win, false);
            var bannerRect = (RectTransform)bannerGo.transform;
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = new Vector2(-200f, 60f);
            bannerRect.sizeDelta = new Vector2(1000f, 150f);
            var bannerImage = bannerGo.GetComponent<Image>();
            bannerImage.color = new Color(0.05f, 0.05f, 0.06f, 0.95f);
            var bannerGroup = bannerGo.GetComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            var bannerButton = bannerGo.GetComponent<Button>();
            bannerButton.targetGraphic = bannerImage;
            var bannerText = DetectiveUIWidgets.CreateText(bannerGo.transform, "Text", "", 26, Color.white);
            DetectiveUIWidgets.Stretch(bannerText.rectTransform, 24f, 24f, 16f, 16f);
            bannerText.verticalAlignment = VerticalAlignmentOptions.Middle;
            bannerText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var templates = new GameObject("Templates");
            templates.transform.SetParent(root, false);
            var cardTemplate = BuildCardTemplate(templates.transform);
            cardTemplate.SetActive(false);

            var ui = root.gameObject.AddComponent<DetectiveMindPanelUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("panelRoot").objectReferenceValue = windowGo;
            so.FindProperty("hudRoot").objectReferenceValue = refs.hudGo;
            so.FindProperty("closeButton").objectReferenceValue = maskButton;
            so.FindProperty("focusText").objectReferenceValue = focus;
            so.FindProperty("gridContent").objectReferenceValue = contentRect;
            var slotArray = so.FindProperty("slotRects");
            slotArray.arraySize = 2;
            slotArray.GetArrayElementAtIndex(0).objectReferenceValue = slotRects[0];
            slotArray.GetArrayElementAtIndex(1).objectReferenceValue = slotRects[1];
            so.FindProperty("reasonButton").objectReferenceValue = reasonButton;
            so.FindProperty("clearButton").objectReferenceValue = clearButton;
            so.FindProperty("bannerGroup").objectReferenceValue = bannerGroup;
            so.FindProperty("bannerText").objectReferenceValue = bannerText;
            so.FindProperty("bannerButton").objectReferenceValue = bannerButton;
            so.FindProperty("cardTemplate").objectReferenceValue = cardTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.mindPanel = ui;
        }

        private static GameObject BuildCardTemplate(Transform parent)
        {
            var go = new GameObject("ClueCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(230f, 100f);
            go.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var label = DetectiveUIWidgets.CreateText(go.transform, "Title", "线索", 20, Color.white);
            DetectiveUIWidgets.Stretch(label.rectTransform, 10f, 10f, 6f, 6f);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            return go;
        }

        private static Button BuildTextButton(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var label = DetectiveUIWidgets.CreateText(go.transform, "Label", text, fontSize, Color.white);
            DetectiveUIWidgets.Stretch(label.rectTransform, 8f, 8f, 4f, 4f);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            return button;
        }

        private static void BuildDuelUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("DuelPanel");
            if (existing != null)
            {
                refs.duelRunner = existing.GetComponent<DetectiveDuelRunner>();
                return;
            }

            RectTransform root = UIRoot(canvas, "DuelPanel", false);

            var overlay = DetectiveUIWidgets.CreateImage(root, "Overlay", new Color(0f, 0f, 0f, 0.9f));
            DetectiveUIWidgets.Stretch(overlay.rectTransform, 0f, 0f, 0f, 0f);

            var panel = DetectiveUIWidgets.CreateImage(root, "Panel", new Color(0.07f, 0.07f, 0.09f, 0.98f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1240f, 740f);
            panelRect.anchoredPosition = new Vector2(0,-135f);
            var panelButton = panel.gameObject.AddComponent<Button>();
            panelButton.targetGraphic = panel;

            var title = DetectiveUIWidgets.CreateText(panel.transform, "Title", "言语对决", 30, Color.white);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(600f, 44f));
            title.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var opponent = DetectiveUIWidgets.CreateText(panel.transform, "Opponent", "", 22, new Color32(0xD9, 0xA8, 0x4A, 0xFF));
            Place(opponent.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(600f, 32f));
            opponent.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var retreatButton = BuildTextButton((RectTransform)panel.transform, "Retreat", "撤退", new Vector2(-18f, -18f), new Vector2(130f, 42f), new Color(0.2f, 0.2f, 0.24f, 0.95f), 20);
            var retreatRect = (RectTransform)retreatButton.transform;
            retreatRect.anchorMin = new Vector2(1f, 1f);
            retreatRect.anchorMax = new Vector2(1f, 1f);
            retreatRect.pivot = new Vector2(1f, 1f);
            retreatRect.anchoredPosition = new Vector2(-18f, -18f);

            var thesis = DetectiveUIWidgets.CreateText(panel.transform, "Thesis", "", 22, new Color(1f, 1f, 1f, 0.9f));
            thesis.fontStyle = FontStyles.Italic;
            Place(thesis.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1060f, 64f));
            thesis.horizontalAlignment = HorizontalAlignmentOptions.Center;
            thesis.verticalAlignment = VerticalAlignmentOptions.Top;

            var played = DetectiveUIWidgets.CreateText(panel.transform, "Played", "已出牌：", 18, new Color(1f, 1f, 1f, 0.75f));
            Place(played.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -110f), new Vector2(320f, 220f));
            played.verticalAlignment = VerticalAlignmentOptions.Top;

            var mistakes = DetectiveUIWidgets.CreateText(panel.transform, "Mistakes", "", 18, new Color(1f, 1f, 1f, 0.75f));
            Place(mistakes.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -340f), new Vector2(320f, 30f));

            var reply = DetectiveUIWidgets.CreateText(panel.transform, "Reply", "", 26, Color.white);
            Place(reply.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1060f, 150f));
            reply.verticalAlignment = VerticalAlignmentOptions.Top;
            reply.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var hint = DetectiveUIWidgets.CreateText(panel.transform, "Hint", "", 18, new Color(1f, 1f, 1f, 0.55f));
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(600f, 28f));
            hint.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var viewportGo = new GameObject("HandViewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportGo.transform.SetParent(panel.transform, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.anchorMin = new Vector2(0.5f, 0f);
            viewportRect.anchorMax = new Vector2(0.5f, 0f);
            viewportRect.pivot = new Vector2(0.5f, 0f);
            viewportRect.anchoredPosition = new Vector2(0f, 20f);
            viewportRect.sizeDelta = new Vector2(1180f, 210f);
            viewportGo.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.7f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var handGrid = contentGo.GetComponent<GridLayoutGroup>();
            handGrid.cellSize = new Vector2(185f, 84f);
            handGrid.spacing = new Vector2(10f, 10f);
            handGrid.childAlignment = TextAnchor.UpperLeft;
            handGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            handGrid.constraintCount = 6;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var handScroll = viewportGo.AddComponent<ScrollRect>();
            handScroll.content = contentRect;
            handScroll.horizontal = false;
            handScroll.vertical = true;
            handScroll.viewport = viewportRect;
            handScroll.movementType = ScrollRect.MovementType.Clamped;

            var templates = new GameObject("Templates");
            templates.transform.SetParent(root, false);
            var handTemplate = new GameObject("HandCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            handTemplate.transform.SetParent(templates.transform, false);
            var handRect = (RectTransform)handTemplate.transform;
            handRect.sizeDelta = new Vector2(185f, 84f);
            var handImage = handTemplate.GetComponent<Image>();
            handImage.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var handButton = handTemplate.GetComponent<Button>();
            handButton.targetGraphic = handImage;
            var handLayout = handTemplate.GetComponent<LayoutElement>();
            handLayout.preferredWidth = 185f;
            handLayout.preferredHeight = 84f;
            var handLabel = DetectiveUIWidgets.CreateText(handTemplate.transform, "Label", "线索", 18, Color.white);
            DetectiveUIWidgets.Stretch(handLabel.rectTransform, 8f, 8f, 4f, 4f);
            handLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            handLabel.horizontalAlignment = HorizontalAlignmentOptions.Center;
            handTemplate.SetActive(false);

            var ui = root.gameObject.AddComponent<DetectiveDuelRunner>();
            var so = new SerializedObject(ui);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("panelButton").objectReferenceValue = panelButton;
            so.FindProperty("retreatButton").objectReferenceValue = retreatButton;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("opponentText").objectReferenceValue = opponent;
            so.FindProperty("thesisText").objectReferenceValue = thesis;
            so.FindProperty("replyText").objectReferenceValue = reply;
            so.FindProperty("mistakesText").objectReferenceValue = mistakes;
            so.FindProperty("playedText").objectReferenceValue = played;
            so.FindProperty("hintText").objectReferenceValue = hint;
            so.FindProperty("handContent").objectReferenceValue = contentRect;
            so.FindProperty("handCardTemplate").objectReferenceValue = handTemplate;
            so.FindProperty("voiceCornerUI").objectReferenceValue = refs.voiceCorner;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.duelRunner = ui;
        }

        private static void BuildEndingUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("EndingPanel");
            if (existing != null)
            {
                refs.endingUI = existing.GetComponent<DetectiveEndingUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "EndingPanel", false);

            var background = DetectiveUIWidgets.CreateImage(root, "Background", Color.black);
            DetectiveUIWidgets.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);
            var backgroundButton = background.gameObject.AddComponent<Button>();
            backgroundButton.targetGraphic = background;

            var line = DetectiveUIWidgets.CreateText(root, "LineText", "", 30, Color.white);
            Place(line.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1300f, 260f));
            line.verticalAlignment = VerticalAlignmentOptions.Middle;
            line.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var final = DetectiveUIWidgets.CreateText(root, "FinalImageText", "", 22, new Color(1f, 1f, 1f, 0.6f));
            Place(final.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0f, -170f), new Vector2(1300f, 60f));
            final.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var settlementGo = new GameObject("Settlement", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            settlementGo.transform.SetParent(root, false);
            var settlementRect = (RectTransform)settlementGo.transform;
            settlementRect.anchorMin = new Vector2(0.5f, 0.5f);
            settlementRect.anchorMax = new Vector2(0.5f, 0.5f);
            settlementRect.pivot = new Vector2(0.5f, 0.5f);
            settlementRect.anchoredPosition = Vector2.zero;
            settlementRect.sizeDelta = new Vector2(900f, 520f);
            settlementGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 0.95f);
            var settlementGroup = settlementGo.GetComponent<CanvasGroup>();
            settlementGroup.alpha = 0f;

            var title = DetectiveUIWidgets.CreateText(settlementGo.transform, "Title", "", 32, Color.white);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(820f, 60f));
            title.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var stats = DetectiveUIWidgets.CreateText(settlementGo.transform, "Stats", "", 26, new Color(1f, 1f, 1f, 0.9f));
            Place(stats.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(820f, 320f));
            stats.horizontalAlignment = HorizontalAlignmentOptions.Center;
            stats.verticalAlignment = VerticalAlignmentOptions.Bottom;

            var ui = root.gameObject.AddComponent<DetectiveEndingUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("backgroundButton").objectReferenceValue = backgroundButton;
            so.FindProperty("lineText").objectReferenceValue = line;
            so.FindProperty("finalText").objectReferenceValue = final;
            so.FindProperty("settlementGroup").objectReferenceValue = settlementGroup;
            so.FindProperty("settlementTitle").objectReferenceValue = title;
            so.FindProperty("settlementStats").objectReferenceValue = stats;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.endingUI = ui;
        }

        private static void BuildBannerUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("DistrictBanner");
            if (existing != null)
            {
                refs.banner = existing.GetComponent<DetectiveDistrictBannerUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "DistrictBanner", true);

            var groupGo = new GameObject("Banner", typeof(RectTransform), typeof(CanvasGroup));
            groupGo.transform.SetParent(root, false);
            var rect = (RectTransform)groupGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.72f);
            rect.anchorMax = new Vector2(0.5f, 0.72f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 90f);
            var group = groupGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            var text = DetectiveUIWidgets.CreateText(groupGo.transform, "Text", "", 40, Color.white);
            text.fontStyle = FontStyles.Bold;
            DetectiveUIWidgets.Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var ui = root.gameObject.AddComponent<DetectiveDistrictBannerUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.banner = ui;
        }

        private static void BuildToastUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("Toast");
            if (existing != null)
            {
                refs.toast = existing.GetComponent<DetectiveToastUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "Toast", true);

            var toastGo = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            toastGo.transform.SetParent(root, false);
            var rect = (RectTransform)toastGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 340f);
            rect.sizeDelta = new Vector2(1200f, 60f);
            toastGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            var group = toastGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            var label = DetectiveUIWidgets.CreateText(toastGo.transform, "Label", "", 24, Color.white);
            DetectiveUIWidgets.Stretch(label.rectTransform, 20f, 20f, 8f, 8f);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var ui = root.gameObject.AddComponent<DetectiveToastUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            refs.toast = ui;
        }

        private static void BuildMapUI(Transform canvas, UIRefs refs)
        {
            Transform existing = canvas.Find("MapPanel");
            if (existing != null)
            {
                refs.map = existing.GetComponent<DetectiveMapUI>();
                return;
            }

            RectTransform root = UIRoot(canvas, "MapPanel", true);
            var windowGo = new GameObject("Window", typeof(RectTransform));
            windowGo.transform.SetParent(root, false);
            var windowRect = (RectTransform)windowGo.transform;
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.offsetMin = Vector2.zero;
            windowRect.offsetMax = Vector2.zero;
            windowGo.SetActive(false);
            Transform win = windowGo.transform;

            var mask = DetectiveUIWidgets.CreateImage(win, "Mask", new Color(0f, 0f, 0f, 0.9f));
            DetectiveUIWidgets.Stretch(mask.rectTransform, 0f, 0f, 0f, 0f);
            var maskButton = mask.gameObject.AddComponent<Button>();
            maskButton.targetGraphic = mask;

            var title = DetectiveUIWidgets.CreateText(win, "Title", "城市地图", 34, Color.white);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(400f, 52f));
            title.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var time = DetectiveUIWidgets.CreateText(win, "TimeText", "22:00", 24, new Color(1f, 1f, 1f, 0.85f));
            Place(time.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(220f, 32f));
            time.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var cityMapGo = new GameObject("CityMap", typeof(RectTransform));
            cityMapGo.transform.SetParent(win, false);
            var cityMapRect = (RectTransform)cityMapGo.transform;
            cityMapRect.anchorMin = new Vector2(0.5f, 0.5f);
            cityMapRect.anchorMax = new Vector2(0.5f, 0.5f);
            cityMapRect.pivot = new Vector2(0.5f, 0.5f);
            cityMapRect.anchoredPosition = new Vector2(0f, 20f);
            cityMapRect.sizeDelta = new Vector2(1300f, 800f);
            Transform cityMap = cityMapGo.transform;

            var roadColor = new Color(0.5f, 0.5f, 0.55f, 0.5f);
            CreateRoadBetween(cityMap, new Vector2(-410f, 50f), new Vector2(40f, 170f), 30f, roadColor);
            CreateRoadBetween(cityMap, new Vector2(40f, 170f), new Vector2(400f, -20f), 30f, roadColor);
            CreateRoadBetween(cityMap, new Vector2(40f, 170f), new Vector2(-140f, -210f), 30f, roadColor);
            CreateRoadBetween(cityMap, new Vector2(-410f, 50f), new Vector2(-140f, -210f), 30f, roadColor);
            CreateRoadBetween(cityMap, new Vector2(-140f, -210f), new Vector2(400f, -20f), 30f, roadColor);

            var blockData = new (string districtId, string name, Vector2 pos, Vector2 size, Color color)[]
            {
                ("redlight", "红灯区", new Vector2(-430f, 40f), new Vector2(360f, 280f), new Color(0.88f, 0.31f, 0.79f)),
                ("financial", "金融街", new Vector2(150f, 200f), new Vector2(400f, 230f), new Color(0.75f, 0.90f, 0.95f)),
                ("industrial", "旧工业区", new Vector2(-30f, -260f), new Vector2(400f, 240f), new Color(1.00f, 0.66f, 0.30f)),
                ("residential", "住宅区", new Vector2(470f, -40f), new Vector2(360f, 270f), new Color(1.00f, 0.85f, 0.56f)),
            };

            foreach (var block in blockData)
            {
                CreateMapBlock(cityMap, block.name, block.pos, block.size, block.color);
            }

            var pointData = new (string regionId, Vector2 pos)[]
            {
                ("D0_Alley", new Vector2(-550f, 130f)),
                ("D1_RedLight", new Vector2(-410f, 50f)),
                ("I1_Bar", new Vector2(-320f, -20f)),
                ("D2_Financial", new Vector2(40f, 170f)),
                ("I2_Apartment", new Vector2(240f, 140f)),
                ("I2_Coffee", new Vector2(260f, 260f)),
                ("D3_Industrial", new Vector2(-140f, -210f)),
                ("I3_Warehouse", new Vector2(-60f, -320f)),
                ("I3_Basement", new Vector2(70f, -310f)),
                ("I3_Rooftop", new Vector2(90f, -220f)),
                ("D4_Residential", new Vector2(400f, -20f)),
                ("I4_YourHome", new Vector2(530f, 30f)),
                ("I4_VictimHome", new Vector2(540f, -100f)),
            };

            var closeButton = BuildTextButton(windowRect, "CloseButton", "【关闭】", new Vector2(-60f, -40f), new Vector2(200f, 56f), new Color(0.30f, 0.22f, 0.18f, 0.95f), 24);

            var ui = root.gameObject.AddComponent<DetectiveMapUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("windowRoot").objectReferenceValue = windowGo;
            so.FindProperty("hudRoot").objectReferenceValue = refs.hudGo;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("maskButton").objectReferenceValue = maskButton;
            so.FindProperty("timeText").objectReferenceValue = time;
            var pointsArray = so.FindProperty("points");
            pointsArray.arraySize = pointData.Length;
            for (int i = 0; i < pointData.Length; i++)
            {
                var point = CreateMapPoint(cityMap, pointData[i].regionId, pointData[i].pos);
                pointsArray.GetArrayElementAtIndex(i).objectReferenceValue = point;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            refs.map = ui;
        }

        private static void CreateRoadBetween(Transform parent, Vector2 pointA, Vector2 pointB, float inset, Color color)
        {
            Vector2 direction = pointB - pointA;
            float distance = direction.magnitude;
            if (distance <= inset * 2f)
            {
                return;
            }

            direction /= distance;
            Vector2 start = pointA + direction * inset;
            Vector2 end = pointB - direction * inset;
            Vector2 center = (start + end) * 0.5f;
            float length = (end - start).magnitude;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            CreateRoad(parent, center, length, angle, color);
        }

        private static void CreateRoad(Transform parent, Vector2 center, float length, float angleDegrees, Color color)
        {
            var road = DetectiveUIWidgets.CreateImage(parent, "Road", color);
            road.raycastTarget = false;
            var rect = road.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(length, 4f);
            rect.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
        }

        private static void CreateMapBlock(Transform parent, string districtName, Vector2 center, Vector2 size, Color outlineColor)
        {
            var blockGo = new GameObject($"Block_{districtName}", typeof(RectTransform));
            blockGo.transform.SetParent(parent, false);
            var rect = (RectTransform)blockGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;

            var outline = DetectiveUIWidgets.CreateImage(blockGo.transform, "Outline", new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.55f));
            DetectiveUIWidgets.Stretch(outline.rectTransform, 0f, 0f, 0f, 0f);
            outline.raycastTarget = false;

            var fill = DetectiveUIWidgets.CreateImage(outline.transform, "Fill", new Color(0.07f, 0.08f, 0.10f, 0.88f));
            DetectiveUIWidgets.Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
            fill.raycastTarget = false;

            var label = DetectiveUIWidgets.CreateText(blockGo.transform, "Name", districtName, 22, outlineColor);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(14f, -10f);
            labelRect.sizeDelta = new Vector2(160f, 30f);
            label.fontStyle = FontStyles.Bold;
        }

        private static DetectiveMapPoint CreateMapPoint(Transform parent, string regionId, Vector2 position)
        {
            string displayName = DetectiveRegionCatalog.TryGetRegion(regionId, out var region) ? region.DisplayName : regionId;
            var go = new GameObject($"Point_{regionId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(24f, 24f);

            var image = go.GetComponent<Image>();
            image.sprite = DetectiveUIWidgets.GetCircleSprite();
            image.color = Color.white;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var ring = DetectiveUIWidgets.CreateImage(go.transform, "CurrentRing", new Color32(0xD9, 0xA8, 0x4A, 0xFF));
            ring.sprite = DetectiveUIWidgets.GetCircleSprite();
            ring.raycastTarget = false;
            var ringRect = ring.rectTransform;
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = Vector2.zero;
            ringRect.sizeDelta = new Vector2(38f, 38f);
            ring.gameObject.SetActive(false);

            var badge = DetectiveUIWidgets.CreateImage(go.transform, "NewBadge", new Color32(0xE1, 0x4E, 0x4E, 0xFF));
            badge.sprite = DetectiveUIWidgets.GetCircleSprite();
            badge.raycastTarget = false;
            var badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(6f, 6f);
            badgeRect.sizeDelta = new Vector2(12f, 12f);
            badge.gameObject.SetActive(false);

            var label = DetectiveUIWidgets.CreateText(go.transform, "Label", displayName, 18, new Color(1f, 1f, 1f, 0.9f));
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(20f, 0f);
            labelRect.sizeDelta = new Vector2(220f, 26f);

            var point = go.AddComponent<DetectiveMapPoint>();
            var so = new SerializedObject(point);
            so.FindProperty("regionId").stringValue = regionId;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("dot").objectReferenceValue = image;
            so.FindProperty("newBadge").objectReferenceValue = badge.gameObject;
            so.FindProperty("currentRing").objectReferenceValue = ring.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return point;
        }

        internal static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        #endregion

        #region Shared helpers

        internal static GameObject CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            return CreateCube(parent, name, position, scale, new Color(0.22f, 0.23f, 0.27f));
        }

        internal static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(color);
            return go;
        }

        internal static GameObject CreateProp(Transform parent, string name, Vector3 position, float radius, float height, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(color);
            return go;
        }

        internal static void CreateNeonSign(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = GetUnlitMaterial(color);
        }

        internal static GameObject CreateClue(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            string clueId,
            string displayName,
            string examineDialogueAsset,
            bool hideWhenCollected,
            DetectiveVoiceType requiredVoice = DetectiveVoiceType.None,
            int requiredVoiceLevel = 1,
            string lockedDialogueAsset = null,
            string definitionGrantedDialogueAsset = null,
            VoiceDelta[] onCollectVoiceChanges = null,
            TestInteractable.MarkerStyle markerStyle = TestInteractable.MarkerStyle.Label)
        {
            var go = CreateCube(parent, name, position, scale, color);
            var interactable = go.AddComponent<TestInteractable>();
            var clueAsset = LoadAsset<DetectiveClueDefinition>($"Assets/Resources/Detective/Clues/{clueId}.asset");
            var so = new SerializedObject(interactable);
            so.FindProperty("interactionId").stringValue = name;
            so.FindProperty("interactionLabel").stringValue = "检视";
            so.FindProperty("displayName").stringValue = clueAsset != null ? clueAsset.Title : displayName;
            so.FindProperty("clueDefinition").objectReferenceValue = clueAsset;
            so.FindProperty("clueId").stringValue = clueId;
            so.FindProperty("interactableType").enumValueIndex = (int)TestInteractable.InteractableType.Clue;
            so.FindProperty("hideWhenCollected").boolValue = hideWhenCollected;
            // All clue objects use the same ground interaction ring; their eventual assets
            // communicate what they are after the player inspects them.
            so.FindProperty("markerStyle").enumValueIndex = (int)TestInteractable.MarkerStyle.GlowRing;
            if (!string.IsNullOrWhiteSpace(examineDialogueAsset))
            {
                so.FindProperty("examineDialogue").objectReferenceValue = LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{examineDialogueAsset}.asset");
            }

            if (requiredVoice != DetectiveVoiceType.None)
            {
                so.FindProperty("requiredVoice").enumValueIndex = (int)requiredVoice;
                so.FindProperty("requiredVoiceLevel").intValue = requiredVoiceLevel;
            }

            if (!string.IsNullOrWhiteSpace(lockedDialogueAsset))
            {
                so.FindProperty("lockedDialogue").objectReferenceValue = LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{lockedDialogueAsset}.asset");
            }

            if (!string.IsNullOrWhiteSpace(definitionGrantedDialogueAsset))
            {
                so.FindProperty("examineDialogue").objectReferenceValue = LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{definitionGrantedDialogueAsset}.asset");
            }

            if (onCollectVoiceChanges != null && onCollectVoiceChanges.Length > 0)
            {
                var array = so.FindProperty("onCollectVoiceChanges");
                array.arraySize = onCollectVoiceChanges.Length;
                for (int i = 0; i < onCollectVoiceChanges.Length; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("voice").enumValueIndex = (int)onCollectVoiceChanges[i].Voice;
                    element.FindPropertyRelative("delta").intValue = onCollectVoiceChanges[i].Delta;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        internal readonly struct VoiceDelta
        {
            public readonly DetectiveVoiceType Voice;
            public readonly int Delta;

            public VoiceDelta(DetectiveVoiceType voice, int delta)
            {
                Voice = voice;
                Delta = delta;
            }
        }

        internal static GameObject CreateNpcGate(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            string dialogueAsset,
            string characterAsset,
            string[] requiredFlags,
            string[] forbiddenFlags,
            string walkAwayFlag = null,
            Vector3? walkAwayPoint = null,
            string walkAwayClueId = null)
        {
            var rig = new GameObject(name);
            rig.transform.SetParent(parent, false);
            rig.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(rig.transform, false);
            body.GetComponent<MeshRenderer>().sharedMaterial = GetLitMaterial(color);

            var character = LoadAsset<DetectiveCharacterDefinition>($"Assets/Data/Characters/{characterAsset}.asset");
            var interactable = body.AddComponent<TestInteractable>();
            var so = new SerializedObject(interactable);
            so.FindProperty("interactionId").stringValue = name;
            so.FindProperty("interactionLabel").stringValue = "交谈";
            so.FindProperty("displayName").stringValue = character != null ? character.DisplayName : name;
            so.FindProperty("interactableType").enumValueIndex = (int)TestInteractable.InteractableType.Npc;
            so.FindProperty("characterDefinition").objectReferenceValue = character;
            if (!string.IsNullOrWhiteSpace(dialogueAsset))
            {
                so.FindProperty("dialogueDefinition").objectReferenceValue = LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{dialogueAsset}.asset");
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (requiredFlags != null || forbiddenFlags != null)
            {
                var gate = rig.AddComponent<DetectiveFlagGatedObject>();
                if (requiredFlags != null)
                {
                    SetObjectArrayField(gate, "requiredFlags", requiredFlags);
                }

                if (forbiddenFlags != null)
                {
                    SetObjectArrayField(gate, "forbiddenFlags", forbiddenFlags);
                }
            }

            // P1-3：需要离场的 NPC 由 DetectiveNpcWalker 接管隐藏时机（走过去再消失），不再挂立即隐藏的 gate。
            if (!string.IsNullOrWhiteSpace(walkAwayFlag) || !string.IsNullOrWhiteSpace(walkAwayClueId))
            {
                var agent = rig.AddComponent<NavMeshAgent>();
                var agentSo = new SerializedObject(agent);
                agentSo.FindProperty("m_Enabled").boolValue = false;
                agentSo.FindProperty("m_Radius").floatValue = 0.3f;
                agentSo.FindProperty("m_Height").floatValue = 2f;
                agentSo.FindProperty("m_Speed").floatValue = 2.2f;
                agentSo.FindProperty("m_Acceleration").floatValue = 8f;
                agentSo.FindProperty("m_AngularSpeed").floatValue = 360f;
                agentSo.FindProperty("m_StoppingDistance").floatValue = 0.3f;
                agentSo.ApplyModifiedPropertiesWithoutUndo();

                var walker = rig.AddComponent<DetectiveNpcWalker>();
                var walkerSo = new SerializedObject(walker);
                if (!string.IsNullOrWhiteSpace(walkAwayFlag))
                {
                    walkerSo.FindProperty("walkAwayFlag").stringValue = walkAwayFlag;
                }

                if (!string.IsNullOrWhiteSpace(walkAwayClueId))
                {
                    walkerSo.FindProperty("walkAwayClueId").stringValue = walkAwayClueId;
                }

                walkerSo.FindProperty("walkAwayPoint").vector3Value = walkAwayPoint ?? position;
                walkerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return rig;
        }

        internal static GameObject CreateTrigger(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            return go;
        }

        internal static GameObject CreateProximityTrigger(Transform parent, string name, Vector3 position, Vector3 size, string dialogueAsset)
        {
            var go = CreateTrigger(parent, name, position, size);
            var proximity = go.AddComponent<DetectiveProximityDialogue>();
            var so = new SerializedObject(proximity);
            so.FindProperty("dialogueDefinition").objectReferenceValue = LoadAsset<DetectiveDialogueDefinition>($"Assets/Data/Dialogues/{dialogueAsset}.asset");
            so.FindProperty("onceOnly").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        internal static void ResetMaterialCaches()
        {
            LitMaterials.Clear();
            UnlitMaterials.Clear();
            rainMaterial = null;
        }

        internal static Material GetLitMaterial(Color color)
        {
            if (LitMaterials.TryGetValue(color, out Material cached))
            {
                return cached;
            }

            Material material = LoadOrCreateGreyboxMaterial(color, false);
            LitMaterials[color] = material;
            return material;
        }

        internal static Material GetUnlitMaterial(Color color)
        {
            if (UnlitMaterials.TryGetValue(color, out Material cached))
            {
                return cached;
            }

            Material material = LoadOrCreateGreyboxMaterial(color, true);
            UnlitMaterials[color] = material;
            return material;
        }

        private static Material LoadOrCreateGreyboxMaterial(Color color, bool unlit)
        {
            string folder = "Assets/Art/Materials/Greybox";
            if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art"))
                {
                    AssetDatabase.CreateFolder("Assets", "Art");
                }

                AssetDatabase.CreateFolder("Assets/Art", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Art/Materials", "Greybox");
            }

            string key = ColorUtility.ToHtmlStringRGBA(color);
            string path = $"{folder}/Greybox_{(unlit ? "Unlit" : "Lit")}_{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = $"Greybox_{(unlit ? "Unlit" : "Lit")}_{key}" };
            material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static Material GetRainMaterial()
        {
            if (rainMaterial != null)
            {
                return rainMaterial;
            }

            string guid = AssetDatabase.FindAssets("Detective_Rain t:Material").FirstOrDefault();
            if (guid != null)
            {
                rainMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            }

            if (rainMaterial == null)
            {
                rainMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                rainMaterial.SetColor("_BaseColor", new Color(0.55f, 0.75f, 1f, 0.7f));
            }

            return rainMaterial;
        }

        internal static T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[DetectiveCityBuilder] 资产缺失: {path}");
            }

            return asset;
        }

        internal static void SetObjectField(object target, string fieldName, Object value)
        {
            var so = new SerializedObject((Object)target);
            so.FindProperty(fieldName).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetIntField(object target, string fieldName, int value)
        {
            var so = new SerializedObject((Object)target);
            so.FindProperty(fieldName).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetStringField(object target, string fieldName, string value)
        {
            var so = new SerializedObject((Object)target);
            so.FindProperty(fieldName).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetObjectArrayField(Object target, string fieldName, string[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(fieldName);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
