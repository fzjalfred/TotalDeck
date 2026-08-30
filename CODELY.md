

## Codely Structured Memories

### User

### Feedback
- [2026-08-30 17:39:51] Tuanjie 引擎 UI 坑：Image.Type.Filled 对无 Sprite 的 Image（CreateComponent 默认白图无 sprite）不执行裁剪渲染，fillAmount 任意值都显示满宽——进度条会"瞬间全条变色"。**Why:** 2026-08-30 占山为王进度条三轮修复失败后截图对比发现，标准 Unity 会裁剪但 Tuanjie 不会。**How to apply:** TotalDeck 里做任何进度条一律用锚点拉伸（fill Image 的 anchorMax.x = progress，每帧设置），不要用 Filled fillAmount；排查"进度条不显示进度"问题先查这个。

### Project
- [2026-08-30 00:18:07] Tuanjie editor 2022.3.62t14 (TotalDeck machine): launching Tuanjie.exe directly WITHOUT -projectPath hands off to Tuanjie Hub and exits with return code 0 after ~10s — looks like "editor won't open". Correct ways: Tuanjie Hub project list, or "Tuanjie.exe -projectPath <path>". Useful logs: Editor.log at %LOCALAPPDATA%\Tuanjie\Editor\Editor.log, Hub log at %APPDATA%\TuanjieHub\logs\info-log.json, Hub exe at %LOCALAPPDATA%\Programs\Tuanjie Cowork\hub\tuanjie.exe. **Why:** diagnosed 2026-08-30 — 6 bare launches each exited code 0 in 10s while Hub-launched sessions worked fine. **How to apply:** when user reports editor not opening, check Editor.log for missing COMMAND LINE ARGUMENTS/projectPath first.
- [2026-08-30 00:48:54] Codely Unity bridge 工具重载（custom_tools_reloaded）期间执行 exec_runtime_script 协程会丢失全部日志并返回 null——不要重跑长协程取证，改用一次性短查询（读取对象当前状态）验证结果。**Why:** 2026-08-29 会话中多次因重载丢失战斗测试日志，浪费多轮往返。**How to apply:** 遇到 observation invalidated / 工具重载后，直接用短 script 查询世界状态，而不是重新执行耗时协程。
- [2026-08-30 01:16:01] 本机未安装 ffmpeg，exec_runtime_script 的 record_game_view 会直接报错失败；视觉验证替代方案：在协程里调用 UnityEngine.ScreenCapture.CaptureScreenshot("xx.png")（存到项目根目录），然后 read_file 读取 PNG 查看 Game View 实际画面。**Why:** 2026-08-29 排查 UI 方块伪影时录屏不可用，改用截图诊断一次定位（Hints 面板默认 RectTransform 悬在画布中心）。**How to apply:** 需要看游戏画面做视觉诊断时，直接走截图路径，不要尝试 record_game_view 或安装 ffmpeg。
- [2026-08-30 02:41:19] TotalDeck 战斗模型采用全面战争语义（用户多次纠正后确立）：移动命令不使战斗计算失效（行军士兵接触敌人时边走边打、正常互损）；接战移动减速（FightMoveMultiplier=0.55）；攻击命令才允许脱阵追击并冻结军团锚点；歼敌后锚点就地驻停；非攻击命令的自动接敌追击有 3 秒时间窗（ChaseGiveUpTime，追不上就回队列，勿用距离拴绳——用户否决过 ChaseLeash 方案觉得僵硬）；士兵阵亡后阵型懒补位（formationDirty 标记 + 下一帧 CompactFormation，勿每次死亡/移动全量重算）。**Why:** 用户明确要求还原全战手感——此前"移动=纯挨打"、"撤退2秒不还手"、距离拴绳三个方案都被否决。**How to apply:** 后续新增单位/状态/阵型逻辑时，遵守"移动≠免战、接战减速、攻击令无限追、自动追击3秒超时、阵型懒补位"五条规则，勿重新引入已否决的方案。

- [2026-08-30 02:19:05] TotalDeck 单位平衡原则（用户 2026-08-30 确认）：双方只保证同兵种属性完全一致（同一 GameConfig 常量 + 同一 soldierPrefab 引用，审计过无按队伍分支的数值代码），不保证战斗结果对称——50v50 混战的锁敌顺序/朝向/碰撞推挤会混沌放大战损差，这是全战类正常手感，不要再为"战损不对称"做修复。**Why:** 用户明确说"只要确保两者属性数值、攻击都一样就行，不一定非要保证战斗结果"。**How to apply:** 后续遇到战斗结果不对称的反馈，先审计属性来源是否对称即可，勿试图用对称化锁敌/伤害逻辑消除结果差异。
- [2026-08-30 13:24:41] TotalDeck 经济系统已重构为对称双玩家架构（2026-08-30）：PlayerState 纯 C# 类承载金钱/赏金/抽卡费/手牌/出牌，GameManager 持 Player+Enemy 两个实例走完全相同的规则（抽卡递增费、出牌扣费、收入/维护/破产逃兵结算）；AIController 在准备阶段每 0.5s 把钱花光（抽卡→法术→部署最贵兵）。**Why:** 用户要求"AI 需要是一个玩家的实例，和玩家一样的机制，每回合把钱花光"——旧 EnemyAI 无经济概念且每 2 回合白送兵已被替换。**How to apply:** 后续加新卡种/经济机制时改 PlayerState 一处即可双方生效；勿给 AI 写专属数值特权；场景中 GameManager 需链接 cardPool 且挂 AIController（TotalDeckSceneSetup 已含）。
- [2026-08-30 14:18:01] TotalDeck 测试遗留物污染场景的坑（2026-08-30 确认）：在 Play Mode 下用 exec_runtime_script 生成测试军团/士兵后，工具重载（observation invalidated）瞬间场景可能被意外保存，把运行时 Clone 对象永久写入 .unity 文件——下次开局这些无 Initialize 的"白兵"会和正常出生军团混战，且其维护费会耗尽 AI 经济，表现为"开局玩不了"。**Why:** 用户报告开局即坏，排查发现场景里残留 8 个测试军团 408 个对象（z=±5 测试点位）。**How to apply:** 遇到"开局异常/白兵/军团数量不对"，先 stop 进 Edit Mode 数场景里 name 含 "Prefab(Clone)" 的对象——非 0 就是污染，DestroyImmediate 后 MarkSceneDirty+SaveOpenScenes；测试脚本尽量用 Object.Destroy 而非依赖场景重置，重要 SaveOpenScenes 调用前确认不在测试会话中。
- [2026-08-30 18:39:45] TotalDeck 地图尺寸翻倍版（2026-08-30 定稿）：Ground 100×100（scale 10,1,10）、出生点 z=±30、AI 增援 z=-40、分界线长 80、相机高 70 平移限制 ±40、占山圈半径 16（GameConfig.HillRadius）。占山为王机制：HillZone 每帧统计圈内士兵，10s 结算多数方 +1，100 分胜；计分只在 Combat 走表，Planning 冻结且进度条清空；HillScoreUI 计分板【分数|圈内人数】蓝左红右 + 底部进度条（Tuanjie 的 Filled Image 不裁剪，必须用锚点拉伸 anchorMax.x=progress 实现）；AIController 每 0.5s 花光钱（抽卡→法术→部署最贵兵，站位靠圈），战斗阶段 idle 军团走向圈。**Why:** 用户要求地图和圈扩大一倍；占点节奏、计分板格式、AI 花光钱策略均为用户明确定义。**How to apply:** 地图相关常量分散在场景值（Ground scale/BattleInitializer/相机）和 GameConfig（HillRadius），改尺寸需两处同步；灭团军团会立即移出 SelectedRegiments 并关闭路径视觉（防幽灵路径）；新 UI 进度条一律锚点拉伸实现。  TotalDeck 游戏流程框架（2026-08-30）：GameState 枚举（MainMenu/Playing/GameOver）+ GameManager.State 状态机，Awake 即 timeScale=0 先显示主菜单；StartNewGame()（清场+重置 PlayerState/SideStats/HillZone 分数+重部署+补手牌）；EndGame(winner)（由 HillZone 100 分触发，timeScale=0 冻结战场）；ReturnToMenu()。GameMenuUI 组件管三面板：MainMenuPanel（开始/设置/多人/退出，后两个为占位提示"开发中"）/ GameOverPanel（VICTORY|DEFEAT + 占领积分 + 击杀阵亡表 + 再来一局/返回主菜单）/ HUDRoot（TopBar/BottomPanel/HillScoreboard/Hints 归组显隐）。SideStats 类记每方 Kills/Losses，Soldier.TakeDamage 加 attacker 参数做击杀归因（三个攻击调用点都传 this）。**Why:** 用户要求菜单框架可扩展 + 结算统计每人杀/亡。**How to apply:** 加新菜单项改 GameMenuUI+TotalDeckSceneSetup.CreateMenuFramework 两处；新统计类型扩展 SideStats；Tuanjie 引擎 Image.Type.Filled 对无 Sprite 的 Image 不裁剪（渲染满宽）——进度条一律用锚点拉伸（anchorMax.x=progress）实现。




### Reference

