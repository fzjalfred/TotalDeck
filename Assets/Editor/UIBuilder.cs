using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TotalDeck.EditorTools
{
    /// <summary>
    /// Idempotent full-UI rebuilder. DESTROYS every "UICanvas" in the scene
    /// and rebuilds one authoritative set: HUD (TopBar/BottomPanel/Hints/
    /// HillScoreboard under HUDRoot), main menu with setup submenu, and the
    /// results screen. Safe to run any number of times — repeated runs
    /// replace instead of duplicate. This replaces the incremental patch
    /// scripts that kept leaving ghost canvases behind on tool reloads.
    /// </summary>
    public static class UIBuilder
    {
        [MenuItem("Tools/TotalDeck/Rebuild UI", false, 10)]
        public static void Rebuild()
        {
            // ── 1. Purge ALL existing UICanvas objects (idempotence) ──
            foreach (var oldCanvas in Object.FindObjectsOfType<Canvas>(true))
            {
                if (oldCanvas.name != "UICanvas") continue;
                string path = GetPath(oldCanvas.transform);
                DestroyTree(oldCanvas.gameObject);
                Debug.Log($"[UIBuilder] destroyed old canvas at {path}");
            }

            var gm = Object.FindObjectOfType<GameManager>();
            var cm = Object.FindObjectOfType<CardManager>();
            if (gm == null || cm == null)
            {
                Debug.LogError("[UIBuilder] GameManager/CardManager missing — aborting");
                return;
            }

            // ── 2. Build the single authoritative canvas ──
            GameObject canvasObj = new GameObject("UICanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            BuildHud(canvasObj.transform);
            BuildMenu(canvasObj.transform, gm, cm);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[UIBuilder] UI rebuilt: 1 canvas, HUD + menu + setup + results. Scene saved.");
        }

        static void DestroyTree(GameObject go)
        {
            // Use DestroyImmediate for edit mode; recurse is unnecessary for roots
            Object.DestroyImmediate(go);
        }

        static string GetPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        // ── HUD ────────────────────────────────────────────

        static void BuildHud(Transform canvasT)
        {
            GameObject hudRoot = MkRect("HUDRoot", canvasT);
            Stretch(Mk(hudRoot.transform));

            // TopBar
            GameObject topBar = Panel("TopBar", hudRoot.transform, new Color(0.17f, 0.17f, 0.17f, 1f));
            var tbRT = Mk(topBar.transform);
            tbRT.anchorMin = new Vector2(0f, 1f); tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.pivot = new Vector2(0.5f, 1f); tbRT.sizeDelta = new Vector2(0f, 55f);
            tbRT.anchoredPosition = Vector2.zero;

            var phaseText = Txt(topBar.transform, "PhaseText", "Planning Phase", 16, TextAnchor.MiddleLeft, Color.white);
            Anch(phaseText.rectTransform, 0.00f, 0f, 0.30f, 1f, 20f, 0f);
            var timerText = Txt(topBar.transform, "TimerText", "20s", 16, TextAnchor.MiddleLeft, Color.white);
            Anch(timerText.rectTransform, 0.30f, 0f, 0.45f, 1f, 0f, 0f);
            var treasuryText = Txt(topBar.transform, "TreasuryText", "$250", 22, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.67f));
            Anch(treasuryText.rectTransform, 0.40f, 0f, 0.60f, 1f, 0f, 10f);
            var incomeText = Txt(topBar.transform, "IncomeText", "$100", 12, TextAnchor.MiddleCenter, new Color(0.87f, 0.87f, 0.87f));
            Anch(incomeText.rectTransform, 0.40f, 0f, 0.60f, 0.4f, 0f, -5f);
            var upkeepText = Txt(topBar.transform, "UpkeepText", "0", 12, TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0.42f));
            Anch(upkeepText.rectTransform, 0.50f, 0f, 0.60f, 0.4f, 0f, -5f);
            var balanceText = Txt(topBar.transform, "BalanceText", "Balance: +$100", 13, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.67f));
            Anch(balanceText.rectTransform, 0.40f, 0f, 0.70f, 0.4f, -30f, -5f);
            var skipBtn = Btn(topBar.transform, "SkipButton", "Engage!");
            Anch(skipBtn.GetComponent<RectTransform>(), 0.90f, 0.2f, 1.0f, 0.8f, -20f, 0f);

            // BottomPanel
            GameObject bottomPanel = Panel("BottomPanel", hudRoot.transform, new Color(0.13f, 0.13f, 0.13f, 1f));
            var bpRT = Mk(bottomPanel.transform);
            bpRT.anchorMin = new Vector2(0f, 0f); bpRT.anchorMax = new Vector2(1f, 0f);
            bpRT.pivot = new Vector2(0.5f, 0f); bpRT.sizeDelta = new Vector2(0f, 150f);
            bpRT.anchoredPosition = Vector2.zero;

            var drawBtn = Btn(bottomPanel.transform, "DrawButton", "Draw Card");
            Anch(drawBtn.GetComponent<RectTransform>(), 0f, 0.1f, 0.15f, 0.9f, 20f, 0f);
            var drawCostText = Txt(drawBtn.transform, "DrawCostText", "$50", 16, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.67f));
            Anch(drawCostText.rectTransform, 0.5f, 0f, 0.5f, 0.3f, 0f, 0f);

            GameObject handContainer = MkRect("HandContainer", bottomPanel.transform);
            var hcRT = Mk(handContainer.transform);
            hcRT.anchorMin = new Vector2(0.15f, 0f); hcRT.anchorMax = new Vector2(1f, 1f);
            hcRT.anchoredPosition = Vector2.zero; hcRT.sizeDelta = Vector2.zero;
            var handLayout = handContainer.AddComponent<HorizontalLayoutGroup>();
            handLayout.childAlignment = TextAnchor.MiddleLeft;
            handLayout.spacing = 15f;
            handLayout.padding = new RectOffset(20, 20, 0, 0);
            var fitter = handContainer.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Hints
            GameObject hintsObj = MkRect("Hints", hudRoot.transform);
            var hintsRT = Mk(hintsObj.transform);
            hintsRT.anchorMin = new Vector2(0f, 1f); hintsRT.anchorMax = new Vector2(0.35f, 0.7f);
            hintsRT.anchoredPosition = new Vector2(20f, -20f); hintsRT.sizeDelta = Vector2.zero;
            var hintsBG = hintsObj.AddComponent<Image>();
            hintsBG.color = new Color(0f, 0f, 0f, 0.6f);
            var hintsText = Txt(hintsObj.transform, "HintText",
                "[Economy & Draw System]\n" +
                "• Draw cost increases each draw (50→100→150), resets next turn\n" +
                "• Kill enemies for bounty, paid at turn end\n" +
                "• Each regiment costs $15 upkeep per turn\n" +
                "• Negative treasury = mass desertion!\n\n" +
                "[Controls]\n" +
                "• Left-click/drag to select your regiments\n" +
                "• Right-click to move or charge enemy",
                13, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.8f));
            Anch(hintsText.rectTransform, 0f, 1f, 0.35f, 0.7f, 20f, -20f);

            // HillScoreboard (under HUDRoot)
            GameObject scorePanel = MkRect("HillScoreboard", hudRoot.transform);
            var scoreRT = Mk(scorePanel.transform);
            scoreRT.anchorMin = new Vector2(0.5f, 1f); scoreRT.anchorMax = new Vector2(0.5f, 1f);
            scoreRT.pivot = new Vector2(0.5f, 1f);
            scoreRT.anchoredPosition = new Vector2(0f, -62f);
            scoreRT.sizeDelta = new Vector2(420f, 66f);
            var scoreBG = scorePanel.AddComponent<Image>();
            scoreBG.color = new Color(0f, 0f, 0f, 0.55f);

            var hillUI = scorePanel.AddComponent<HillScoreUI>();
            var pScore = Txt(scorePanel.transform, "PlayerScoreText", "00", 30, TextAnchor.MiddleRight, new Color(0.3f, 0.67f, 0.97f));
            Anch(pScore.rectTransform, 0.05f, 0.44f, 0.30f, 1.00f, 0f, 0f);
            var pCount = Txt(scorePanel.transform, "PlayerCountText", "000", 15, TextAnchor.MiddleRight, new Color(0.3f, 0.67f, 0.97f, 0.75f));
            Anch(pCount.rectTransform, 0.30f, 0.32f, 0.46f, 0.72f, 0f, 0f);
            var sepL = Txt(scorePanel.transform, "SepL", "|", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.35f));
            Anch(sepL.rectTransform, 0.46f, 0.32f, 0.52f, 1.00f, 0f, 0f);
            var eScore = Txt(scorePanel.transform, "EnemyScoreText", "00", 30, TextAnchor.MiddleLeft, new Color(1f, 0.42f, 0.42f));
            Anch(eScore.rectTransform, 0.55f, 0.44f, 0.80f, 1.00f, 0f, 0f);
            var eCount = Txt(scorePanel.transform, "EnemyCountText", "000", 15, TextAnchor.MiddleLeft, new Color(1f, 0.42f, 0.42f, 0.75f));
            Anch(eCount.rectTransform, 0.80f, 0.32f, 0.96f, 0.72f, 0f, 0f);
            var sepR = Txt(scorePanel.transform, "SepR", "|", 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.35f));
            Anch(sepR.rectTransform, 0.50f, 0.32f, 0.56f, 1.00f, 0f, 0f);

            // Progress bar (bottom strip, anchor-stretch fill)
            GameObject barBG = MkRect("ScoreProgressBG", scorePanel.transform);
            var bgRT = Mk(barBG.transform);
            bgRT.anchorMin = new Vector2(0.05f, 0.04f); bgRT.anchorMax = new Vector2(0.95f, 0.26f);
            bgRT.anchoredPosition = Vector2.zero; bgRT.sizeDelta = Vector2.zero;
            var bgImg = barBG.AddComponent<Image>();
            bgImg.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);

            GameObject barFill = MkRect("ScoreProgressFill", barBG.transform);
            var fillImg = barFill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.67f, 0.97f);
            var fillRT = Mk(barFill.transform);
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.anchoredPosition = Vector2.zero; fillRT.sizeDelta = Vector2.zero;

            hillUI.playerScoreText = pScore;
            hillUI.playerCountText = pCount;
            hillUI.enemyScoreText = eScore;
            hillUI.enemyCountText = eCount;
            hillUI.progressFill = fillImg;

            // GameUI wiring (component lives on canvas)
            var gameUI = canvasT.gameObject.AddComponent<GameUI>();
            gameUI.phaseText = phaseText;
            gameUI.timerText = timerText;
            gameUI.treasuryText = treasuryText;
            gameUI.incomeText = incomeText;
            gameUI.upkeepText = upkeepText;
            gameUI.balanceText = balanceText;
            gameUI.drawButton = drawBtn.GetComponent<Button>();
            gameUI.drawCostText = drawCostText;
            gameUI.skipButton = skipBtn.GetComponent<Button>();
            gameUI.handContainer = handContainer.transform;
            gameUI.cardPrefab = LoadCardPrefab();
            gameUI.bottomPanel = bottomPanel;
        }

        static GameObject LoadCardPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardUI.prefab");
            if (prefab == null)
            {
                // Rebuild via scene setup's method
                prefab = TotalDeckSceneSetup_InvokeCreateCardUIPrefab();
            }
            return prefab;
        }

        static GameObject TotalDeckSceneSetup_InvokeCreateCardUIPrefab()
        {
            var method = typeof(TotalDeckSceneSetup).GetMethod("CreateCardUIPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (GameObject)method.Invoke(null, null);
        }

        // ── Menus ──────────────────────────────────────────

        static void BuildMenu(Transform canvasT, GameManager gm, CardManager cm)
        {
            GameObject menuUIObj = MkRect("GameMenuUI", canvasT);
            var menuUI = menuUIObj.AddComponent<GameMenuUI>();

            // Main menu panel (button stack + setup sub-elements)
            GameObject mainPanel = Panel("MainMenuPanel", canvasT, new Color(0f, 0f, 0f, 0.75f));
            Stretch(Mk(mainPanel.transform));

            var title = Txt(mainPanel.transform, "TitleText", "TOTAL DECK", 64, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.67f));
            title.fontStyle = FontStyle.Bold;
            Anch(title.rectTransform, 0f, 0.75f, 1f, 0.9f, 0f, 0f);
            var subtitle = Txt(mainPanel.transform, "SubtitleText", "占山为王 · 卡牌军团 RTS", 20, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f));
            Anch(subtitle.rectTransform, 0f, 0.68f, 1f, 0.75f, 0f, 0f);

            var startBtn = Btn(mainPanel.transform, "StartButton", "开始游戏");
            Anch(startBtn.GetComponent<RectTransform>(), 0.35f, 0.52f, 0.65f, 0.60f, 0f, 0f);
            var settingsBtn = Btn(mainPanel.transform, "SettingsButton", "设  置");
            Anch(settingsBtn.GetComponent<RectTransform>(), 0.35f, 0.42f, 0.65f, 0.50f, 0f, 0f);
            var multiBtn = Btn(mainPanel.transform, "MultiplayerButton", "多人游戏");
            Anch(multiBtn.GetComponent<RectTransform>(), 0.35f, 0.32f, 0.65f, 0.40f, 0f, 0f);
            var quitBtn = Btn(mainPanel.transform, "QuitButton", "退  出");
            Anch(quitBtn.GetComponent<RectTransform>(), 0.35f, 0.22f, 0.65f, 0.30f, 0f, 0f);
            var notice = Txt(mainPanel.transform, "MenuNoticeText", "", 14, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
            Anch(notice.rectTransform, 0f, 0.14f, 1f, 0.20f, 0f, 0f);

            // Setup sub-elements (hidden initially)
            var setupTitle = Txt(mainPanel.transform, "SetupTitleText", "对局配置", 28, TextAnchor.MiddleCenter, Color.white);
            Anch(setupTitle.rectTransform, 0f, 0.68f, 1f, 0.76f, 0f, 0f);
            setupTitle.gameObject.SetActive(false);

            var mapLabel = Txt(mainPanel.transform, "MapLabel", "地图", 16, TextAnchor.MiddleLeft, Color.white);
            Anch(mapLabel.rectTransform, 0.22f, 0.625f, 0.34f, 0.665f, 0f, 0f);
            mapLabel.gameObject.SetActive(false);
            var pLabel = Txt(mainPanel.transform, "PlayerSlotLabel", "玩家出生点", 16, TextAnchor.MiddleLeft, new Color(0.3f, 0.67f, 0.97f));
            Anch(pLabel.rectTransform, 0.22f, 0.565f, 0.34f, 0.605f, 0f, 0f);
            pLabel.gameObject.SetActive(false);
            var eLabel = Txt(mainPanel.transform, "EnemySlotLabel", "AI出生点", 16, TextAnchor.MiddleLeft, new Color(1f, 0.42f, 0.42f));
            Anch(eLabel.rectTransform, 0.22f, 0.505f, 0.34f, 0.545f, 0f, 0f);
            eLabel.gameObject.SetActive(false);

            var mapDD = Dropdown(mainPanel.transform, "MapDropdown");
            Anch(mapDD.GetComponent<RectTransform>(), 0.36f, 0.615f, 0.64f, 0.675f, 0f, 0f);
            mapDD.gameObject.SetActive(false);
            var pDD = Dropdown(mainPanel.transform, "PlayerSlotDropdown");
            Anch(pDD.GetComponent<RectTransform>(), 0.36f, 0.555f, 0.64f, 0.615f, 0f, 0f);
            pDD.gameObject.SetActive(false);
            var eDD = Dropdown(mainPanel.transform, "EnemySlotDropdown");
            Anch(eDD.GetComponent<RectTransform>(), 0.36f, 0.495f, 0.64f, 0.555f, 0f, 0f);
            eDD.gameObject.SetActive(false);

            var beginBtn = Btn(mainPanel.transform, "BeginBattleButton", "开始战斗");
            Anch(beginBtn.GetComponent<RectTransform>(), 0.30f, 0.40f, 0.70f, 0.47f, 0f, 0f);
            beginBtn.gameObject.SetActive(false);
            var backBtn = Btn(mainPanel.transform, "SetupBackButton", "返回");
            Anch(backBtn.GetComponent<RectTransform>(), 0.30f, 0.31f, 0.70f, 0.38f, 0f, 0f);
            backBtn.gameObject.SetActive(false);

            // GameOver panel
            GameObject overPanel = Panel("GameOverPanel", canvasT, new Color(0f, 0f, 0f, 0.85f));
            Stretch(Mk(overPanel.transform));
            overPanel.SetActive(false);

            var winner = Txt(overPanel.transform, "WinnerText", "VICTORY!", 56, TextAnchor.MiddleCenter, new Color(0f, 1f, 0.67f));
            winner.fontStyle = FontStyle.Bold;
            Anch(winner.rectTransform, 0f, 0.72f, 1f, 0.88f, 0f, 0f);
            var finalScore = Txt(overPanel.transform, "FinalScoreText", "占领积分  0 : 0", 26, TextAnchor.MiddleCenter, Color.white);
            Anch(finalScore.rectTransform, 0f, 0.60f, 1f, 0.70f, 0f, 0f);
            var header = Txt(overPanel.transform, "StatsHeader", "        击杀    阵亡", 18, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));
            Anch(header.rectTransform, 0.15f, 0.50f, 0.85f, 0.58f, 0f, 0f);

            GameObject pRow = Panel("PlayerStatsRow", overPanel.transform, new Color(0.3f, 0.67f, 0.97f, 0.15f));
            var pRowRT = Mk(pRow.transform);
            pRowRT.anchorMin = new Vector2(0.15f, 0.40f); pRowRT.anchorMax = new Vector2(0.85f, 0.50f);
            pRowRT.anchoredPosition = Vector2.zero; pRowRT.sizeDelta = Vector2.zero;
            var pRowText = Txt(pRow.transform, "PlayerStatsText", "玩家  0    0", 18, TextAnchor.MiddleCenter, new Color(0.3f, 0.67f, 0.97f));
            Stretch(Mk(pRowText.rectTransform));

            GameObject eRow = Panel("EnemyStatsRow", overPanel.transform, new Color(1f, 0.42f, 0.42f, 0.15f));
            var eRowRT = Mk(eRow.transform);
            eRowRT.anchorMin = new Vector2(0.15f, 0.30f); eRowRT.anchorMax = new Vector2(0.85f, 0.40f);
            eRowRT.anchoredPosition = Vector2.zero; eRowRT.sizeDelta = Vector2.zero;
            var eRowText = Txt(eRow.transform, "EnemyStatsText", "敌方  0    0", 18, TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0.42f));
            Stretch(Mk(eRowText.rectTransform));

            var rematchBtn = Btn(overPanel.transform, "RematchButton", "再来一局");
            Anch(rematchBtn.GetComponent<RectTransform>(), 0.28f, 0.14f, 0.48f, 0.22f, 0f, 0f);
            var menuBtn = Btn(overPanel.transform, "BackToMenuButton", "返回主菜单");
            Anch(menuBtn.GetComponent<RectTransform>(), 0.52f, 0.14f, 0.72f, 0.22f, 0f, 0f);

            // Wire GameMenuUI
            menuUI.mainMenuPanel = mainPanel;
            menuUI.gameOverPanel = overPanel;
            menuUI.hudRoot = FindChild(canvasT, "HUDRoot").gameObject;
            menuUI.startButton = startBtn.GetComponent<Button>();
            menuUI.settingsButton = settingsBtn.GetComponent<Button>();
            menuUI.multiplayerButton = multiBtn.GetComponent<Button>();
            menuUI.quitButton = quitBtn.GetComponent<Button>();
            menuUI.menuNoticeText = notice;
            menuUI.mapDropdown = mapDD;
            menuUI.playerSlotDropdown = pDD;
            menuUI.enemySlotDropdown = eDD;
            menuUI.beginBattleButton = beginBtn.GetComponent<Button>();
            menuUI.setupBackButton = backBtn.GetComponent<Button>();
            menuUI.winnerText = winner;
            menuUI.finalScoreText = finalScore;
            menuUI.playerStatsText = pRowText;
            menuUI.enemyStatsText = eRowText;
            menuUI.rematchButton = rematchBtn.GetComponent<Button>();
            menuUI.backToMenuButton = menuBtn.GetComponent<Button>();
        }

        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform c in parent)
                if (c.name == name) return c;
            return null;
        }

        // ── UI primitives (RectTransform-first, no missing-component traps) ──

        static GameObject MkRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static RectTransform Mk(Transform t) => t as RectTransform;

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        static GameObject Panel(string name, Transform parent, Color color)
        {
            GameObject go = MkRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static Text Txt(Transform parent, string name, string content, int size, TextAnchor align, Color color)
        {
            GameObject go = MkRect(name, parent);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static GameObject Btn(Transform parent, string name, string label)
        {
            GameObject go = MkRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.27f, 0.27f, 0.27f, 1f);
            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = new Color(0.27f, 0.27f, 0.27f, 1f);
            cb.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            btn.colors = cb;
            var t = Txt(go.transform, "Label", label, 16, TextAnchor.MiddleCenter, Color.white);
            Stretch(Mk(t.rectTransform));
            return go;
        }

        static Dropdown Dropdown(Transform parent, string name)
        {
            GameObject go = MkRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            var dd = go.AddComponent<Dropdown>();

            GameObject labelGo = MkRect("Label", go.transform);
            var lRT = Mk(labelGo.transform);
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(10f, 0f); lRT.offsetMax = new Vector2(-25f, 0f);
            var lt = labelGo.AddComponent<Text>();
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.fontSize = 16; lt.alignment = TextAnchor.MiddleLeft; lt.color = Color.white;
            dd.captionText = lt;

            GameObject arrowGo = MkRect("Arrow", go.transform);
            var aRT = Mk(arrowGo.transform);
            aRT.anchorMin = new Vector2(1f, 0f); aRT.anchorMax = new Vector2(1f, 1f);
            aRT.offsetMin = new Vector2(-20f, 0f); aRT.offsetMax = new Vector2(-5f, 0f);
            var at = arrowGo.AddComponent<Text>();
            at.text = "▼"; at.fontSize = 12; at.alignment = TextAnchor.MiddleCenter; at.color = Color.white;
            at.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject tmplGo = MkRect("Template", go.transform);
            var trt = Mk(tmplGo.transform);
            trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = Vector2.zero; trt.sizeDelta = new Vector2(0f, 150f);
            var timg = tmplGo.AddComponent<Image>();
            timg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            var sr = tmplGo.AddComponent<ScrollRect>();
            tmplGo.SetActive(false);
            dd.template = trt;

            GameObject vpGo = MkRect("Viewport", tmplGo.transform);
            var vrt = Mk(vpGo.transform);
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.anchoredPosition = Vector2.zero; vrt.sizeDelta = Vector2.zero;
            var vimg = vpGo.AddComponent<Image>();
            vimg.color = new Color(0f, 0f, 0f, 0.3f);
            vpGo.AddComponent<RectMask2D>();
            sr.viewport = vrt;

            GameObject contentGo = MkRect("Content", vpGo.transform);
            var crt = Mk(contentGo.transform);
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = Vector2.one;
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero; crt.sizeDelta = Vector2.zero;
            sr.content = crt;

            GameObject itemGo = MkRect("Item", contentGo.transform);
            var irt = Mk(itemGo.transform);
            irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(1f, 0.5f);
            irt.sizeDelta = new Vector2(0f, 28f);
            var toggle = itemGo.AddComponent<Toggle>();
            var iimg = itemGo.AddComponent<Image>();
            iimg.color = new Color(0.3f, 0.3f, 0.3f);
            toggle.targetGraphic = iimg;
            toggle.image = iimg;

            GameObject ilGo = MkRect("ItemLabel", itemGo.transform);
            var ilrt = Mk(ilGo.transform);
            ilrt.anchorMin = Vector2.zero; ilrt.anchorMax = Vector2.one;
            ilrt.offsetMin = new Vector2(10f, 0f); ilrt.offsetMax = new Vector2(-10f, 0f);
            var it = ilGo.AddComponent<Text>();
            it.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            it.fontSize = 16; it.alignment = TextAnchor.MiddleLeft; it.color = Color.white;
            dd.itemText = it;

            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            return dd;
        }

        static void Anch(RectTransform rt, float ax0, float ay0, float ax1, float ay1, float ox, float oy)
        {
            rt.anchorMin = new Vector2(ax0, ay0);
            rt.anchorMax = new Vector2(ax1, ay1);
            rt.anchoredPosition = new Vector2(ox, oy);
            rt.sizeDelta = Vector2.zero;
        }
    }
}
