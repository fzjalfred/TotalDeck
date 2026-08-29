

## Codely Structured Memories

### User

### Feedback

### Project
- [2026-08-30 00:18:07] Tuanjie editor 2022.3.62t14 (TotalDeck machine): launching Tuanjie.exe directly WITHOUT -projectPath hands off to Tuanjie Hub and exits with return code 0 after ~10s — looks like "editor won't open". Correct ways: Tuanjie Hub project list, or "Tuanjie.exe -projectPath <path>". Useful logs: Editor.log at %LOCALAPPDATA%\Tuanjie\Editor\Editor.log, Hub log at %APPDATA%\TuanjieHub\logs\info-log.json, Hub exe at %LOCALAPPDATA%\Programs\Tuanjie Cowork\hub\tuanjie.exe. **Why:** diagnosed 2026-08-30 — 6 bare launches each exited code 0 in 10s while Hub-launched sessions worked fine. **How to apply:** when user reports editor not opening, check Editor.log for missing COMMAND LINE ARGUMENTS/projectPath first.
- [2026-08-30 00:48:54] Codely Unity bridge 工具重载（custom_tools_reloaded）期间执行 exec_runtime_script 协程会丢失全部日志并返回 null——不要重跑长协程取证，改用一次性短查询（读取对象当前状态）验证结果。**Why:** 2026-08-29 会话中多次因重载丢失战斗测试日志，浪费多轮往返。**How to apply:** 遇到 observation invalidated / 工具重载后，直接用短 script 查询世界状态，而不是重新执行耗时协程。
- [2026-08-30 01:16:01] 本机未安装 ffmpeg，exec_runtime_script 的 record_game_view 会直接报错失败；视觉验证替代方案：在协程里调用 UnityEngine.ScreenCapture.CaptureScreenshot("xx.png")（存到项目根目录），然后 read_file 读取 PNG 查看 Game View 实际画面。**Why:** 2026-08-29 排查 UI 方块伪影时录屏不可用，改用截图诊断一次定位（Hints 面板默认 RectTransform 悬在画布中心）。**How to apply:** 需要看游戏画面做视觉诊断时，直接走截图路径，不要尝试 record_game_view 或安装 ffmpeg。

### Reference

