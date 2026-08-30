

## Codely Structured Memories

### User

### Feedback
- [2026-08-30 17:39:51] Tuanjie 引擎 UI 坑：Image.Type.Filled 对无 Sprite 的 Image（CreateComponent 默认白图无 sprite）不执行裁剪渲染，fillAmount 任意值都显示满宽——进度条会"瞬间全条变色"。**Why:** 2026-08-30 占山为王进度条三轮修复失败后截图对比发现，标准 Unity 会裁剪但 Tuanjie 不会。**How to apply:** TotalDeck 里做任何进度条一律用锚点拉伸（fill Image 的 anchorMax.x = progress，每帧设置），不要用 Filled fillAmount；排查"进度条不显示进度"问题先查这个。
- [2026-08-31 00:40:19] Tuanjie 引擎 UI 坑 2：Canvas.overrideSorting 不持久（设 true 立即/重载后回 False），修 Dropdown 弹出列表被后续兄弟节点遮挡时勿用它——改用代码级 SetAsLastSibling 控制渲染顺序（每次显示时执行）；另外 Dropdown 只有 1 个选项时点开无变化易被误报"下拉框坏了"，演示时确保 ≥2 个选项。**Why:** 2026-08-30 修配置子菜单下拉框遮挡，overrideSorting 三次设置三次回 False。**How to apply:** TotalDeck 下拉/弹出类 UI 的层级问题一律用 sibling order 方案；GameMenuUI.ShowMenuSubscreens 已含该逻辑（enemy→player→map 依次 SetAsLastSibling）。
- [2026-08-31 01:14:31] Tuanjie/Unity 重建 UI 的坑：手写 `new GameObject("X")` 在 Canvas 下必须带 `typeof(RectTransform)`（如 `new GameObject("X", typeof(RectTransform))`），否则后续 GetComponent<RectTransform>() 返回 null 抛 MissingComponentException——TotalDeckSceneSetup 里 Hints/HUDRoot/GameMenuUI 三处都犯过，已修。**Why:** 2026-08-31 重建 HUD 时连环炸三次，每次都是这个原因。**How to apply:** 审查/编写 Canvas 下的 GameObject 构造时一律带 typeof(RectTransform)；遇到 MissingComponentException: no 'RectTransform' attached 直接查 new GameObject 调用。另：重建 Canvas UI 后要核对序列化字段接线（BeginBattleButton 等会变 NULL），运行时可用 SendMessage("Start") 重新绑定 listener。

### Project
- [2026-08-30 00:18:07] Tuanjie editor 2022.3.62t14 (TotalDeck machine): launching Tuanjie.exe directly WITHOUT -projectPath hands off to Tuanjie Hub and exits with return code 0 after ~10s — looks like "editor won't open". Correct ways: Tuanjie Hub project list, or "Tuanjie.exe -projectPath <path>". Useful logs: Editor.log at %LOCALAPPDATA%\Tuanjie\Editor\Editor.log, Hub log at %APPDATA%\TuanjieHub\logs\info-log.json, Hub exe at %LOCALAPPDATA%\Programs\Tuanjie Cowork\hub\tuanjie.exe. **Why:** diagnosed 2026-08-30 — 6 bare launches each exited code 0 in 10s while Hub-launched sessions worked fine. **How to apply:** when user reports editor not opening, check Editor.log for missing COMMAND LINE ARGUMENTS/projectPath first.
- [2026-08-30 00:48:54] Codely Unity bridge 工具重载（custom_tools_reloaded）期间执行 exec_runtime_script 协程会丢失全部日志并返回 null——不要重跑长协程取证，改用一次性短查询（读取对象当前状态）验证结果。**Why:** 2026-08-29 会话中多次因重载丢失战斗测试日志，浪费多轮往返。**How to apply:** 遇到 observation invalidated / 工具重载后，直接用短 script 查询世界状态，而不是重新执行耗时协程。
- [2026-08-30 01:16:01] 本机未安装 ffmpeg，exec_runtime_script 的 record_game_view 会直接报错失败；视觉验证替代方案：在协程里调用 UnityEngine.ScreenCapture.CaptureScreenshot("xx.png")（存到项目根目录），然后 read_file 读取 PNG 查看 Game View 实际画面。**Why:** 2026-08-29 排查 UI 方块伪影时录屏不可用，改用截图诊断一次定位（Hints 面板默认 RectTransform 悬在画布中心）。**How to apply:** 需要看游戏画面做视觉诊断时，直接走截图路径，不要尝试 record_game_view 或安装 ffmpeg。
- [2026-08-30 02:41:19] TotalDeck 战斗模型采用全面战争语义（用户多次纠正后确立）：移动命令不使战斗计算失效（行军士兵接触敌人时边走边打、正常互损）；接战移动减速（FightMoveMultiplier=0.55）；攻击命令才允许脱阵追击并冻结军团锚点；歼敌后锚点就地驻停；非攻击命令的自动接敌追击有 3 秒时间窗（ChaseGiveUpTime，追不上就回队列，勿用距离拴绳——用户否决过 ChaseLeash 方案觉得僵硬）；士兵阵亡后阵型懒补位（formationDirty 标记 + 下一帧 CompactFormation，勿每次死亡/移动全量重算）。**Why:** 用户明确要求还原全战手感——此前"移动=纯挨打"、"撤退2秒不还手"、距离拴绳三个方案都被否决。**How to apply:** 后续新增单位/状态/阵型逻辑时，遵守"移动≠免战、接战减速、攻击令无限追、自动追击3秒超时、阵型懒补位"五条规则，勿重新引入已否决的方案。

- [2026-08-30 02:19:05] TotalDeck 单位平衡原则（用户 2026-08-30 确认）：双方只保证同兵种属性完全一致（同一 GameConfig 常量 + 同一 soldierPrefab 引用，审计过无按队伍分支的数值代码），不保证战斗结果对称——50v50 混战的锁敌顺序/朝向/碰撞推挤会混沌放大战损差，这是全战类正常手感，不要再为"战损不对称"做修复。**Why:** 用户明确说"只要确保两者属性数值、攻击都一样就行，不一定非要保证战斗结果"。**How to apply:** 后续遇到战斗结果不对称的反馈，先审计属性来源是否对称即可，勿试图用对称化锁敌/伤害逻辑消除结果差异。
- [2026-08-30 13:24:41] TotalDeck 经济系统已重构为对称双玩家架构（2026-08-30）：PlayerState 纯 C# 类承载金钱/赏金/抽卡费/手牌/出牌，GameManager 持 Player+Enemy 两个实例走完全相同的规则（抽卡递增费、出牌扣费、收入/维护/破产逃兵结算）；AIController 在准备阶段每 0.5s 把钱花光（抽卡→法术→部署最贵兵）。**Why:** 用户要求"AI 需要是一个玩家的实例，和玩家一样的机制，每回合把钱花光"——旧 EnemyAI 无经济概念且每 2 回合白送兵已被替换。**How to apply:** 后续加新卡种/经济机制时改 PlayerState 一处即可双方生效；勿给 AI 写专属数值特权；场景中 GameManager 需链接 cardPool 且挂 AIController（TotalDeckSceneSetup 已含）。
- [2026-08-30 14:18:01] TotalDeck 测试遗留物污染场景的坑（2026-08-30 确认）：在 Play Mode 下用 exec_runtime_script 生成测试军团/士兵后，工具重载（observation invalidated）瞬间场景可能被意外保存，把运行时 Clone 对象永久写入 .unity 文件——下次开局这些无 Initialize 的"白兵"会和正常出生军团混战，且其维护费会耗尽 AI 经济，表现为"开局玩不了"。**Why:** 用户报告开局即坏，排查发现场景里残留 8 个测试军团 408 个对象（z=±5 测试点位）。**How to apply:** 遇到"开局异常/白兵/军团数量不对"，先 stop 进 Edit Mode 数场景里 name 含 "Prefab(Clone)" 的对象——非 0 就是污染，DestroyImmediate 后 MarkSceneDirty+SaveOpenScenes；测试脚本尽量用 Object.Destroy 而非依赖场景重置，重要 SaveOpenScenes 调用前确认不在测试会话中。
- [2026-08-31 01:27:06] TotalDeck UI 体系定稿（2026-08-31 UIBuilder 重构后）：UI 出问题一律跑菜单 Tools→TotalDeck→Rebuild UI（Assets/Editor/UIBuilder.cs，幂等：先删除场景所有 UICanvas 再重建唯一一套，含 HUD/主菜单/配置子屏/结算屏/暂停面板，所有字段接线一步到位）。此前场景曾堆积 7 个重复 UICanvas（编辑脚本补丁+工具重载意外保存导致），表现为"局内 UI 出现在主菜单"——诊断 Canvas 问题必须数全部 Canvas 数量（FindObjectsOfType），不能只查第一个。地图与玩家分离架构：MapDef（Duel 对决/Flank 侧袭两图，加图只加一个 MapDef）+ SpawnAssignment；GameManager.StartNewGame(map, playerAssign, enemyAssign)；IsInDeployZone 按出生槽位相对圈心方向判定。游戏流程：GameState（MainMenu/Playing/GameOver）+ 对局中 Esc 暂停菜单（GameMenuUI.Update 检测 GetKeyDown(Escape)，timeScale=0 冻结，RTSInputController 有 timeScale==0/非Playing 输入屏蔽；返回游戏/设置占位/退出到菜单三按钮）。战斗模型全战语义（边走边打/接战减速0.55/攻击令才追击/自动接敌3秒超时/阵型懒补位）。经济对称双 PlayerState（AI 每 tick 从 gm.Enemy 取引用不缓存）。占山为王：10s 结算圈内多数方+1，100 分胜，Planning 冻结并清空进度条，计分板三带布局（大分数0.44-1.0/小人数0.32-0.72/进度条0.04-0.26，面板66px）。地图翻倍尺寸：Ground 100×100、出生 z=±30、圈 r=16。**Why:** 用户要求 Esc 暂停菜单（返回游戏/设置/退出到菜单）、UI 结构重构（补丁式堆叠被证明脆弱）；此前 UI 重复 Canvas 事故频发。**How to apply:** UI 损坏先跑 Rebuild UI；加新 UI 面板改 UIBuilder.cs 一处；改经济/出牌改 PlayerState；新地图加 MapDef；下拉框层级用 sibling order（勿用 overrideSorting，Tuanjie 不持久）；所有 Canvas 下 GameObject 构造必须带 typeof(RectTransform)。






### Reference

