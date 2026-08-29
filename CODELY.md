

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-08-30 00:18:07] Tuanjie editor 2022.3.62t14 (TotalDeck machine): launching Tuanjie.exe directly WITHOUT -projectPath hands off to Tuanjie Hub and exits with return code 0 after ~10s — looks like "editor won't open". Correct ways: Tuanjie Hub project list, or "Tuanjie.exe -projectPath <path>". Useful logs: Editor.log at %LOCALAPPDATA%\Tuanjie\Editor\Editor.log, Hub log at %APPDATA%\TuanjieHub\logs\info-log.json, Hub exe at %LOCALAPPDATA%\Programs\Tuanjie Cowork\hub\tuanjie.exe. **Why:** diagnosed 2026-08-30 — 6 bare launches each exited code 0 in 10s while Hub-launched sessions worked fine. **How to apply:** when user reports editor not opening, check Editor.log for missing COMMAND LINE ARGUMENTS/projectPath first.
- [2026-08-30 00:48:54] Codely Unity bridge 工具重载（custom_tools_reloaded）期间执行 exec_runtime_script 协程会丢失全部日志并返回 null——不要重跑长协程取证，改用一次性短查询（读取对象当前状态）验证结果。**Why:** 2026-08-29 会话中多次因重载丢失战斗测试日志，浪费多轮往返。**How to apply:** 遇到 observation invalidated / 工具重载后，直接用短 script 查询世界状态，而不是重新执行耗时协程。
- [2026-08-30 01:16:01] 本机未安装 ffmpeg，exec_runtime_script 的 record_game_view 会直接报错失败；视觉验证替代方案：在协程里调用 UnityEngine.ScreenCapture.CaptureScreenshot("xx.png")（存到项目根目录），然后 read_file 读取 PNG 查看 Game View 实际画面。**Why:** 2026-08-29 排查 UI 方块伪影时录屏不可用，改用截图诊断一次定位（Hints 面板默认 RectTransform 悬在画布中心）。**How to apply:** 需要看游戏画面做视觉诊断时，直接走截图路径，不要尝试 record_game_view 或安装 ffmpeg。
- [2026-08-30 02:05:02] TotalDeck 战斗模型采用全面战争语义（用户多次纠正后确立）：移动命令不使战斗计算失效（行军士兵接触敌人时边走边打、正常互损）；接战移动减速（FightMoveMultiplier=0.55）；攻击命令才允许脱阵追击并冻结军团锚点；歼敌后锚点就地驻停。**Why:** 用户明确要求还原全战手感——此前"移动=纯挨打"和"撤退2秒不还手"两个方案都被否决。**How to apply:** 后续新增单位/状态/阵型逻辑时，保持"移动≠免战、接战减速、攻击令才锁敌"三条规则，勿引入脱离保护类 hack。
- [2026-08-30 02:19:05] TotalDeck 单位平衡原则（用户 2026-08-30 确认）：双方只保证同兵种属性完全一致（同一 GameConfig 常量 + 同一 soldierPrefab 引用，审计过无按队伍分支的数值代码），不保证战斗结果对称——50v50 混战的锁敌顺序/朝向/碰撞推挤会混沌放大战损差，这是全战类正常手感，不要再为"战损不对称"做修复。**Why:** 用户明确说"只要确保两者属性数值、攻击都一样就行，不一定非要保证战斗结果"。**How to apply:** 后续遇到战斗结果不对称的反馈，先审计属性来源是否对称即可，勿试图用对称化锁敌/伤害逻辑消除结果差异。

### Reference

