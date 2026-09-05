# 失忆警探测试场景与点击移动 3C

## Objective
在现有 Unity 6.3 空项目中建立一个可运行的雨夜街巷灰盒测试场，完成类似《极乐迪斯科》的鼠标点击移动、固定俯视跟随镜头和可扩展交互入口，不提前绑定对话、线索拼接或案件逻辑。

## Changes
- `Assets/Scenes/DetectiveTestScene.unity` - 新建雨夜街巷灰盒测试场。
- `Assets/Scripts/Runtime/Player/DetectiveClickMover.cs` - 鼠标点击地面后的 NavMesh 移动与移动状态。
- `Assets/Scripts/Runtime/Interaction/IInteractable.cs` - 后续 NPC、物品、区域入口共用的交互接口。
- `Assets/Scripts/Runtime/Interaction/InteractionController.cs` - 点击目标、接近目标、触发交互的流程协调。
- `Assets/Scripts/Runtime/Interaction/TestInteractable.cs` - 测试 NPC/线索使用的占位交互组件，仅输出调试信息。
- `Assets/Scripts/Runtime/Debug/DetectiveDebugHUD.cs` - 显示时间占位、移动目标、当前交互目标和 NavMesh 状态。
- `Assets/Prefabs/Player/DetectivePlayer.prefab` - 可复用的警探玩家预制体。
- `Assets/Prefabs/Test/TestNPC.prefab` - 可点击的 NPC 占位预制体。
- `Assets/Prefabs/Test/TestClue.prefab` - 可点击的线索占位预制体。
- `Assets/InputSystem_Actions.inputactions` - 保留现有模板映射，补充本项目专用的点击移动/取消操作，不实现对话输入。

## Relevant Assets and Quirks
- 项目版本为 `6000.3.20f1`，渲染管线为 URP `17.3.0`。
- 已安装 Input System `1.19.0`、Cinemachine `3.1.7`、AI Navigation `2.0.13` 和 ProBuilder `6.1.2`。
- 当前 `SampleScene` 只有 Main Camera、Directional Light、Global Volume，没有可复用角色、预制体或 Gameplay 脚本；搜索结果未发现现有游戏框架，因此不能依赖已有 3C 实现。
- 当前相机是普通透视相机，尚未配置 Cinemachine；当前输入资产仍是 Unity 默认模板，已有 UI 的 Point/Click，可作为点击移动输入基础。
- 点击移动依赖可烘焙 NavMesh；地面、墙体和可交互物体必须使用明确的层级，避免射线误点。
- 本阶段不实现对话树、脑内声音、信任/恐惧、线索拼接、时间推进和结局系统；交互组件只提供稳定接入点。

## Steps
- [ ] 1. 使用 Input System action 工具检查并补充 `Assets/InputSystem_Actions.inputactions` 的专用点击移动输入；保留 `UI` Map 和现有模板动作，避免破坏后续 UI 导航。[depends: none]
- [ ] 2. 创建 `DetectiveClickMover`、`IInteractable`、`InteractionController` 和 `TestInteractable`，将移动、点击目标、接近距离和交互触发解耦；测试交互只使用 `Debug.Log` 明确输出目标名称和触发时机。[depends: 1]
- [ ] 3. 创建 `DetectivePlayer.prefab`，配置 `NavMeshAgent`、碰撞体、玩家输入引用和交互控制器；加入地面点击射线、目标过滤、停止距离和无法到达目标时的 `LogWarning`。[depends: 2]
- [ ] 4. 创建 `DetectiveTestScene`，用 ProBuilder 搭建一个可行走的雨夜街巷灰盒，放置玩家出生点、一个测试 NPC、一个测试线索和可阻挡移动的墙体；设置 Ground/Interactable 等必要层级并烘焙 NavMesh。[depends: 3]
- [ ] 5. 使用 Cinemachine 3.1.7 配置固定角度的俯视跟随镜头：以玩家为 Follow 目标，保留透视空间感，提供平滑跟随和滚轮缩放，不加入自由旋转，保持《极乐迪斯科》式观察视角。[depends: 3,4]
- [ ] 6. 配置 URP 雨夜测试表现：Global Volume 使用低照度冷色调、轻微 Bloom/Color Adjustments，街巷加入少量霓虹色点光源和简化雨幕占位；不制作最终美术资产。[depends: 4,5]
- [ ] 7. 创建 `DetectiveDebugHUD` 并完成验证：点击地面移动、点击 NPC 接近后触发、点击线索触发、不可达目标警告、镜头跟随/缩放、无编译错误；记录后续对话和思维面板接入所需的接口位置。[depends: 2,3,4,5,6]

## Verify
- 打开 `Assets/Scenes/DetectiveTestScene.unity` 进入 Play Mode。
- 鼠标点击可行走地面，玩家通过 NavMesh 移动到目标位置；点击墙体或不可达区域时不穿墙并输出警告。
- 点击测试 NPC 或线索，玩家先移动到交互距离，再触发一次占位交互日志。
- 镜头持续跟随玩家，滚轮缩放不改变玩家移动方向；场景保持固定俯视观察角度。
- Console 无 Error/Exception；Debug HUD 能显示当前移动目标、交互目标和 Agent 状态。
- 确认所有交互逻辑没有直接依赖未来的对话 UI、线索卡片或案件数据结构。