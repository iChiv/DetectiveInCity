# 侦探测试场景：角色、交互、Debug 标签与 Additive 室内流程

## Objective
修正玩家落地、范围交互、NPC/线索反馈、固定相机运动和 Debug 标识，并建立可验证的 Additive 室内测试流程。

## Changes
- `Assets/Scripts/Runtime/Player/DetectiveClickMover.cs` - 修正玩家根节点、NavMeshAgent 停止距离和地面高度处理，避免角色半截埋入地面。
- `Assets/Scripts/Runtime/Interaction/InteractionController.cs` - 将点击交互改为“移动到交互范围内后执行”，不再移动到固定交互点。
- `Assets/Scripts/Runtime/Interaction/TestInteractable.cs` - 增加 NPC/线索类型、显示名称、交互结果和拾取状态。
- `Assets/Scripts/Runtime/Interaction/DetectiveInvestigationState.cs` - （创建）记录已收集线索，供 HUD 与场景切换后继续使用。
- `Assets/Scripts/Runtime/Debug/DetectiveDebugHUD.cs` - 增加 Debug 标签开关、已收集线索列表和交互状态显示。
- `Assets/Scripts/Runtime/Debug/DetectiveWorldLabel.cs` - （创建）使用 TextMeshPro 世界空间文本显示 NPC/线索名称。
- `Assets/Scripts/Runtime/Camera/DetectiveFollowCamera.cs` - 保持固定俯视旋转，只平滑移动位置，不再追踪玩家产生左右摇摆。
- `Assets/Scripts/Runtime/Scene/DetectiveAdditiveSceneLoader.cs` - （创建）加载/卸载室内场景、控制雨幕和室内外状态。
- `Assets/Scripts/Runtime/Scene/DetectiveSceneTrigger.cs` - （创建）处理玩家进入/离开室内入口。
- `Assets/Scenes/DetectiveIndoorTestScene.unity` - （创建）室内测试房间、入口、NavMeshSurface、室内灯光和测试交互物。
- `Assets/Scenes/DetectiveTestScene.unity` - 增加室内入口触发区域并调整玩家测试出生/落地配置。

## Relevant Assets and Quirks
- 当前实际活动场景是 `Assets/Scenes/DetectiveTestScene.unity`；项目中另有同名副本 `Assets/DetectiveTestScene.unity`，后续只把前者作为权威测试场景。
- 玩家当前 `Transform.y=1`，但 `NavMeshAgent.baseOffset=0`，Mesh/CapsuleCollider 直接挂在根节点；移动或 Warp 后根节点可能回到 NavMesh 高度，导致模型下沉。
- `DetectiveClickMover` 当前只支持固定 `stoppingDistance=0.1`；`InteractionController` 当前把目标点击点或 `InteractionPoint` 作为目的地。
- `TestInteractable` 当前只有 `interactionLabel` 和日志，没有 NPC 朝向、线索收集、显示名称或交互完成状态。
- NPC 和线索均为 `Interactable` 层，且对应预制体分别为 `Assets/Prefabs/Test/TestNPC.prefab` 与 `Assets/Prefabs/Test/TestClue.prefab`。
- 当前使用 Unity 6.3、URP 17.3.0、AI Navigation 2.0.13、Input System 1.19.0、Cinemachine 3.1.7 和 TMP/UGUI；运行时 UI 必须继续使用 TMP。

## Steps
- [x] 1. 修正 `DetectivePlayer.prefab` 与测试场景实例的根节点/视觉/碰撞高度关系，设置稳定的 NavMeshAgent 落地策略，并验证移动后 CapsuleCollider 底部与地面重合 [depends: none]
- [x] 2. 重构 `InteractionController` 的目标移动流程，使玩家先进入可配置交互半径，再通过第二次点击确认交互；NPC 交互时水平朝向玩家，线索只有在 `Interact` 真正执行后才登记为已收集并更新显示状态 [depends: 1]
- [x] 3. 保留并接入 NPC/线索的 TMP 世界空间名称标签，在 `DetectiveDebugHUD` 增加二次确认提示、交互完成信息和已收集线索列表 [depends: 2]
- [x] 4. 保持 `DetectiveFollowCamera` 固定俯视旋转，仅对位置使用平滑跟随；保留滚轮 FOV 缩放 [depends: none]
- [x] 5. 使用现有 `DetectiveIndoorTestScene.unity`，保留室内地面、墙体、出口、NavMeshSurface、室内灯光和测试交互对象；清理室外重复 Loader 与重复入口触发器 [depends: 1,2]
- [x] 6. 实现 Additive 加载/卸载流程：进入入口加载室内场景并构建室内 NavMesh，隐藏室外雨幕；离开室内出口卸载室内场景并恢复雨幕，同时保留玩家与已收集线索状态 [depends: 5]
- [ ] 7. 完成 Play Mode 中的点击交互、二次确认、NPC 朝向、线索收集、F3 标签开关、室内进出和相机稳定性验证 [depends: 1,2,3,4,6]

## Verify
- Play Mode 中玩家移动后 Mesh/CapsuleCollider 不再穿入地面，NavMeshAgent 状态保持 `On NavMesh`。
- 点击 NPC 后玩家停在交互半径内，NPC 朝向玩家并只执行一次交互。
- 点击线索后玩家进入范围，第二次实际交互执行后线索才变为已收集并出现在 HUD 列表中。
- Debug 标签开关同时控制 NPC 和线索名称；关闭后世界空间名称不可见。
- 玩家持续移动时相机只改变位置，不改变固定俯视方位。
- 玩家进入室内后 Additive 场景可见、雨幕关闭；离开后室内场景卸载、雨幕恢复。
- 编译检查、运行时 Console 和两个场景的 Play Mode 截图均无错误；若现有搜索结果无法确认某个对象在两个场景中的归属，必须在验证记录中明确标记为未确认。

## Execution Record (2026-09-05)
- 已完成脚本与场景清理：`InteractionController` 改为范围内二次点击确认；`DetectiveClickMover` 使用独立 NavMesh feet position 同步，修正玩家下沉；`DetectiveDebugHUD` 显示确认提示；Additive Loader 增加状态保护；室外场景已删除重复 Loader 和重复入口触发器。
- 已将 `DetectivePlayer` Prefab 与测试场景实例的 `NavMeshAgent.baseOffset` 设为 `0`。
- 编译检查通过，Play Mode 中玩家完整站立于地面，Agent 状态为 `On NavMesh`，本次运行无新增 Console Error/Exception。
- 由于当前验证工具无法向 Play Mode 注入鼠标/键盘输入，点击交互、F3、NPC 朝向、线索收集、室内进出和相机动态行为仍标记为未完成运行时验证；代码检查与场景结构检查已完成。