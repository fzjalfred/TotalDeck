#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TotalDeck.EditorTools
{
    /// <summary>
    /// Editor menu tool that sets up the complete TotalDeck scene with all
    /// required GameObjects, components, and ScriptableObject assets.
    /// Access via menu: Tools > TotalDeck > Setup Scene
    /// Also runs automatically on first project load if the scene doesn't exist yet.
    /// </summary>
    [InitializeOnLoad]
    public static class TotalDeckSceneSetup
    {
        const string SCENE_PATH = "Assets/Scenes/TotalDeck.unity";
        const string SETUP_FLAG = "TotalDeck_SceneSetupDone";

        static TotalDeckSceneSetup()
        {
            // Auto-run scene setup on first load if not done yet
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(SETUP_FLAG, false) && !AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH))
                {
                    SetupScene();
                    SessionState.SetBool(SETUP_FLAG, true);
                }
            };
        }

        [MenuItem("Tools/TotalDeck/Setup Scene", false, 0)]
        public static void SetupScene()
        {
            // Create or open the scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Ground Plane ──────────────────────────
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 units
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                groundRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                groundRenderer.sharedMaterial.color = new Color(0.18f, 0.29f, 0.13f);
            }
            ground.layer = LayerMask.NameToLayer("Default");

            // ── Camera ────────────────────────────────
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            CameraController camCtrl = camObj.AddComponent<CameraController>();
            camCtrl.cameraHeight = 70f;
            camCtrl.cameraAngle = 55f;
            camCtrl.cameraCenter = Vector3.zero;
            camCtrl.panLimitX = new Vector2(-40f, 40f);
            camCtrl.panLimitZ = new Vector2(-40f, 40f);

            // ── Lighting ─────────────────────────────
            GameObject sun = new GameObject("Directional Light");
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.84f);
            sunLight.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ── Event System ─────────────────────────
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // ── GameBootstrap ────────────────────────
            GameObject bootstrapObj = new GameObject("GameBootstrap");
            GameBootstrap bootstrap = bootstrapObj.AddComponent<GameBootstrap>();

            // Create prefabs via factory at runtime, but also create visible soldier prefab
            GameObject soldierPrefab = CreateSoldierPrefabAsset();
            GameObject infantryPrefab = CreateRegimentPrefabAsset("InfantryPrefab");
            GameObject heavyPrefab = CreateRegimentPrefabAsset("HeavyElitePrefab");

            bootstrap.soldierPrefab = soldierPrefab;
            bootstrap.regimentPrefabs = new GameObject[] { infantryPrefab, heavyPrefab };

            // ── GameManager ───────────────────────────
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.soldierPrefab = soldierPrefab;
            gm.regimentPrefabs = new GameObject[] { infantryPrefab, heavyPrefab };

            // ── AIController ─────────────────────────
            GameObject aiObj = new GameObject("AIController");
            aiObj.AddComponent<AIController>();

            // ── CardManager ──────────────────────────
            GameObject cmObj = new GameObject("CardManager");
            CardManager cm = cmObj.AddComponent<CardManager>();

            // Create card assets
            CardData infantryCard = CreateCardAsset("InfantryCard", 1, "Infantry", "Deploy a 50-man infantry regiment", 60, CardType.Unit, 0);
            CardData healCard = CreateCardAsset("HealCard", 2, "Field Medic", "Heal a friendly regiment (+15 soldiers)", 40, CardType.Spell, 0, healAmount: 15);
            CardData heavyCard = CreateCardAsset("HeavyEliteCard", 3, "Heavy Elite", "Deploy a powerful heavy elite regiment", 120, CardType.Unit, 1);

            cm.cardPool = new CardData[] { infantryCard, healCard, heavyCard };
            cm.startingHand = new CardData[] { infantryCard, healCard };
            gm.cardPool = cm.cardPool;

            // ── RTSInputController ───────────────────
            GameObject rtsObj = new GameObject("RTSInputController");
            rtsObj.AddComponent<RTSInputController>();
            rtsObj.AddComponent<DragSelectionVisual>();

            // ── BattleInitializer ────────────────────
            GameObject battleInitObj = new GameObject("BattleInitializer");
            BattleInitializer battleInit = battleInitObj.AddComponent<BattleInitializer>();
            battleInit.playerStartPos = new Vector3(0f, 0f, 30f);
            battleInit.enemyStartPos = new Vector3(0f, 0f, -30f);
            battleInit.enemySpawnPos = new Vector3(0f, 0f, -40f);

            // ── BattlefieldZone ───────────────────────
            GameObject zoneObj = new GameObject("BattlefieldZone");
            var zone = zoneObj.AddComponent<BattlefieldZone>();
            zone.zoneLineLength = 80f;

            // ── HillZone (King of the Hill) ───────────
            GameObject hillObj = new GameObject("HillZone");
            HillZone hill = hillObj.AddComponent<HillZone>();
            hill.CreateVisual();

            // ── UI Canvas ─────────────────────────────
            CreateUICanvas(gm, cm);

            // Save scene
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log("[TotalDeck] Scene setup complete! Saved to " + SCENE_PATH);
            Debug.Log("[TotalDeck] Press Play to start the game.");
        }

        static GameObject CreateSoldierPrefabAsset()
        {
            // Check if already exists
            string path = "Assets/Prefabs/Soldier.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Create a capsule-based soldier
            GameObject soldier = new GameObject("Soldier");
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(soldier.transform);
            capsule.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            capsule.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            capsule.name = "Model";

            // Ensure collider
            CapsuleCollider col = capsule.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.radius = 0.35f;
                col.height = 1f;
            }

            soldier.AddComponent<Soldier>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(soldier, path);
            Object.DestroyImmediate(soldier);
            return prefab;
        }

        static GameObject CreateRegimentPrefabAsset(string name)
        {
            string path = "Assets/Prefabs/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            GameObject regiment = new GameObject(name);
            regiment.AddComponent<Regiment>();
            regiment.AddComponent<RegimentVisual>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(regiment, path);
            Object.DestroyImmediate(regiment);
            return prefab;
        }

        static CardData CreateCardAsset(string fileName, int id, string name, string desc, int cost, CardType type, int prefabIdx, int healAmount = 0)
        {
            string path = "Assets/ScriptableObjects/" + fileName + ".asset";
            CardData existing = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.cardID = id;
            card.cardName = name;
            card.description = desc;
            card.playCost = cost;
            card.cardType = type;
            card.prefabIndex = prefabIdx;
            card.healAmount = healAmount;

            AssetDatabase.CreateAsset(card, path);
            return card;
        }

        static void CreateUICanvas(GameManager gm, CardManager cm)
        {
            // Root Canvas
            GameObject canvasObj = new GameObject("UICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // ── Top Bar ────────────────────────────
            GameObject topBar = CreateUIPanel(canvasObj.transform, "TopBar", new Color(0.17f, 0.17f, 0.17f));
            RectTransform topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.anchorMin = new Vector2(0f, 1f);
            topBarRT.anchorMax = new Vector2(1f, 1f);
            topBarRT.pivot = new Vector2(0.5f, 1f);
            topBarRT.sizeDelta = new Vector2(0f, 55f);
            topBarRT.anchoredPosition = Vector2.zero;

            // Phase text (left)
            Text phaseText = CreateUIText(topBar.transform, "PhaseText", "Planning Phase", 16, TextAnchor.MiddleLeft);
            SetAnchors(phaseText.rectTransform, new Vector2(0f, 0f), new Vector2(0.3f, 1f), new Vector2(20f, 0f));

            // Timer text (left, after phase)
            Text timerText = CreateUIText(topBar.transform, "TimerText", "20s", 16, TextAnchor.MiddleLeft);
            SetAnchors(timerText.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.45f, 1f), new Vector2(0f, 0f));

            // Treasury text (center)
            Text treasuryText = CreateUIText(topBar.transform, "TreasuryText", "$250", 22, TextAnchor.MiddleCenter);
            treasuryText.color = new Color(0f, 1f, 0.67f);
            SetAnchors(treasuryText.rectTransform, new Vector2(0.4f, 0f), new Vector2(0.6f, 1f), new Vector2(0f, 10f));

            // Income text (center, below treasury)
            Text incomeText = CreateUIText(topBar.transform, "IncomeText", "$100", 12, TextAnchor.MiddleCenter);
            incomeText.color = new Color(0.87f, 0.87f, 0.87f);
            SetAnchors(incomeText.rectTransform, new Vector2(0.4f, 0f), new Vector2(0.6f, 0.4f), new Vector2(0f, -5f));

            // Upkeep text
            Text upkeepText = CreateUIText(topBar.transform, "UpkeepText", "0", 12, TextAnchor.MiddleCenter);
            upkeepText.color = new Color(1f, 0.42f, 0.42f);
            SetAnchors(upkeepText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.6f, 0.4f), new Vector2(0f, -5f));

            // Balance text
            Text balanceText = CreateUIText(topBar.transform, "BalanceText", "Balance: +$100", 13, TextAnchor.MiddleCenter);
            balanceText.color = new Color(0f, 1f, 0.67f);
            SetAnchors(balanceText.rectTransform, new Vector2(0.4f, 0f), new Vector2(0.7f, 0.4f), new Vector2(-30f, -5f));

            // Skip button (right)
            GameObject skipBtn = CreateUIButton(topBar.transform, "SkipButton", "Engage!");
            SetAnchors(skipBtn.GetComponent<RectTransform>(), new Vector2(0.9f, 0.2f), new Vector2(1f, 0.8f), new Vector2(-20f, 0f));

            // ── Hill Scoreboard (top center) ────────
            // Format: 【01|023】【03|023】 player bracket left, enemy bracket right
            GameObject scorePanel = new GameObject("HillScoreboard");
            scorePanel.transform.SetParent(canvasObj.transform, false);
            RectTransform scoreRT = scorePanel.AddComponent<RectTransform>();
            scoreRT.anchorMin = new Vector2(0.5f, 1f);
            scoreRT.anchorMax = new Vector2(0.5f, 1f);
            scoreRT.pivot = new Vector2(0.5f, 1f);
            scoreRT.anchoredPosition = new Vector2(0f, -62f);
            scoreRT.sizeDelta = new Vector2(420f, 52f);
            Image scoreBG = scorePanel.AddComponent<Image>();
            scoreBG.color = new Color(0f, 0f, 0f, 0.55f);

            HillScoreUI hillUI = scorePanel.AddComponent<HillScoreUI>();

            // Left bracket: player score | player count
            Text pScore = CreateUIText(scorePanel.transform, "PlayerScoreText", "00", 30, TextAnchor.MiddleRight);
            pScore.color = new Color(0.3f, 0.67f, 0.97f);
            SetAnchors(pScore.rectTransform, new Vector2(0.05f, 0.35f), new Vector2(0.30f, 1f), Vector2.zero);
            Text pCount = CreateUIText(scorePanel.transform, "PlayerCountText", "000", 15, TextAnchor.MiddleRight);
            pCount.color = new Color(0.3f, 0.67f, 0.97f, 0.75f);
            SetAnchors(pCount.rectTransform, new Vector2(0.30f, 0.1f), new Vector2(0.46f, 0.55f), Vector2.zero);

            Text sepL = CreateUIText(scorePanel.transform, "SepL", "|", 22, TextAnchor.MiddleCenter);
            sepL.color = new Color(1f, 1f, 1f, 0.35f);
            SetAnchors(sepL.rectTransform, new Vector2(0.46f, 0f), new Vector2(0.52f, 1f), Vector2.zero);

            // Right bracket: enemy score | enemy count
            Text eScore = CreateUIText(scorePanel.transform, "EnemyScoreText", "00", 30, TextAnchor.MiddleLeft);
            eScore.color = new Color(1f, 0.42f, 0.42f);
            SetAnchors(eScore.rectTransform, new Vector2(0.55f, 0.35f), new Vector2(0.80f, 1f), Vector2.zero);
            Text eCount = CreateUIText(scorePanel.transform, "EnemyCountText", "000", 15, TextAnchor.MiddleLeft);
            eCount.color = new Color(1f, 0.42f, 0.42f, 0.75f);
            SetAnchors(eCount.rectTransform, new Vector2(0.80f, 0.1f), new Vector2(0.96f, 0.55f), Vector2.zero);

            Text sepR = CreateUIText(scorePanel.transform, "SepR", "|", 22, TextAnchor.MiddleCenter);
            sepR.color = new Color(1f, 1f, 1f, 0.35f);
            SetAnchors(sepR.rectTransform, new Vector2(0.50f, 0f), new Vector2(0.56f, 1f), Vector2.zero);

            hillUI.playerScoreText = pScore;
            hillUI.playerCountText = pCount;
            hillUI.enemyScoreText = eScore;
            hillUI.enemyCountText = eCount;

            // ── Scoring window progress bar (below scoreboard) ──
            // Unelapsed = gray-white track; elapsed = faction color sweeping
            // left -> right; hint text sits below the bar.
            GameObject barBG = new GameObject("ScoreProgressBG");
            barBG.transform.SetParent(scorePanel.transform, false);
            Image bgImg = barBG.AddComponent<Image>();
            bgImg.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            RectTransform barBGRT = barBG.GetComponent<RectTransform>();
            barBGRT.anchorMin = new Vector2(0.05f, 0f);
            barBGRT.anchorMax = new Vector2(0.95f, 0.38f);
            barBGRT.anchoredPosition = Vector2.zero;
            barBGRT.sizeDelta = Vector2.zero;

            GameObject barFill = new GameObject("ScoreProgressFill");
            barFill.transform.SetParent(barBG.transform, false);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.67f, 0.97f);
            fillImg.type = Image.Type.Simple;
            // Bar grows by stretching anchors (HillScoreUI sets anchorMax.x =
            // progress). Filled-type rendering is unreliable on sprite-less
            // images in Tuanjie — it renders full-width.
            RectTransform barFillRT = barFill.GetComponent<RectTransform>();
            barFillRT.anchorMin = Vector2.zero;
            barFillRT.anchorMax = new Vector2(0f, 1f);
            barFillRT.anchoredPosition = Vector2.zero;
            barFillRT.sizeDelta = Vector2.zero;

            hillUI.progressFill = fillImg;

            // ── Bottom Panel ──────────────────────
            GameObject bottomPanel = CreateUIPanel(canvasObj.transform, "BottomPanel", new Color(0.13f, 0.13f, 0.13f));
            RectTransform bottomRT = bottomPanel.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0f);
            bottomRT.pivot = new Vector2(0.5f, 0f);
            bottomRT.sizeDelta = new Vector2(0f, 150f);
            bottomRT.anchoredPosition = Vector2.zero;

            // Draw card button (left)
            GameObject drawBtn = CreateUIButton(bottomPanel.transform, "DrawButton", "Draw Card");
            SetAnchors(drawBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.1f), new Vector2(0.15f, 0.9f), new Vector2(20f, 0f));
            Text drawCostText = CreateUIText(drawBtn.transform, "DrawCostText", "$50", 16, TextAnchor.MiddleCenter);
            drawCostText.color = new Color(0f, 1f, 0.67f);
            SetAnchors(drawCostText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.3f), new Vector2(0f, 0f));

            // Hand container (right of draw button)
            GameObject handContainer = new GameObject("HandContainer");
            handContainer.transform.SetParent(bottomPanel.transform, false);
            handContainer.AddComponent<RectTransform>();
            HorizontalLayoutGroup handLayout = handContainer.AddComponent<HorizontalLayoutGroup>();
            handLayout.childAlignment = TextAnchor.MiddleLeft;
            handLayout.spacing = 15f;
            handLayout.padding = new RectOffset(20, 20, 0, 0);
            ContentSizeFitter fitter = handContainer.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            SetAnchors(handContainer.GetComponent<RectTransform>(), new Vector2(0.15f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));

            // Card prefab for hand
            GameObject cardPrefab = CreateCardUIPrefab();

            // ── GameUI component ───────────────────
            GameUI gameUI = canvasObj.AddComponent<GameUI>();
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
            gameUI.cardPrefab = cardPrefab;
            gameUI.bottomPanel = bottomPanel;

            // ── Hints panel ────────────────────────
            GameObject hintsObj = new GameObject("Hints");
            hintsObj.transform.SetParent(canvasObj.transform, false);
            // Match the hint text's anchored region; a default rect would
            // leave a 100x100 square at canvas center
            RectTransform hintsRT = hintsObj.GetComponent<RectTransform>();
            hintsRT.anchorMin = new Vector2(0f, 1f);
            hintsRT.anchorMax = new Vector2(0.35f, 0.7f);
            hintsRT.anchoredPosition = new Vector2(20f, -20f);
            hintsRT.sizeDelta = Vector2.zero;
            Text hintsText = CreateUIText(hintsObj.transform, "HintText",
                "[Economy & Draw System]\n" +
                "• Draw cost increases each draw (50→100→150), resets next turn\n" +
                "• Kill enemies for bounty, paid at turn end\n" +
                "• Each regiment costs $15 upkeep per turn\n" +
                "• Negative treasury = mass desertion!\n\n" +
                "[Controls]\n" +
                "• Left-click/drag to select your regiments\n" +
                "• Right-click to move or charge enemy",
                13, TextAnchor.UpperLeft);
            hintsText.color = new Color(1f, 1f, 1f, 0.8f);
            SetAnchors(hintsText.rectTransform, new Vector2(0f, 1f), new Vector2(0.35f, 0.7f), new Vector2(20f, -20f));

            // Background for hints
            Image hintsBG = hintsObj.AddComponent<Image>();
            if (hintsBG == null) hintsBG = hintsObj.AddComponent<Image>();
            hintsBG.color = new Color(0f, 0f, 0f, 0.6f);
            hintsText.transform.SetAsLastSibling();

            // ── Game Menu Framework (main menu + results screen) ──
            CreateMenuFramework(canvasObj);
        }

        static void CreateMenuFramework(GameObject canvasObj)
        {
            GameObject menuUIObj = new GameObject("GameMenuUI");
            menuUIObj.transform.SetParent(canvasObj.transform, false);
            GameMenuUI menuUI = menuUIObj.AddComponent<GameMenuUI>();

            // ── Main Menu Panel ───────────────────
            GameObject mainPanel = CreateUIPanel(canvasObj.transform, "MainMenuPanel", new Color(0f, 0f, 0f, 0.75f));
            RectTransform mainRT = mainPanel.GetComponent<RectTransform>();
            mainRT.anchorMin = Vector2.zero;
            mainRT.anchorMax = Vector2.one;
            mainRT.anchoredPosition = Vector2.zero;
            mainRT.sizeDelta = Vector2.zero;

            Text title = CreateUIText(mainPanel.transform, "TitleText", "TOTAL DECK", 64, TextAnchor.MiddleCenter);
            SetAnchors(title.rectTransform, new Vector2(0f, 0.75f), new Vector2(1f, 0.9f), Vector2.zero);
            title.color = new Color(0f, 1f, 0.67f);
            title.fontStyle = FontStyle.Bold;

            Text subtitle = CreateUIText(mainPanel.transform, "SubtitleText", "占山为王 · 卡牌军团 RTS", 20, TextAnchor.MiddleCenter);
            SetAnchors(subtitle.rectTransform, new Vector2(0f, 0.68f), new Vector2(1f, 0.75f), Vector2.zero);
            subtitle.color = new Color(0.85f, 0.85f, 0.85f);

            Button startBtn = CreateUIButton(mainPanel.transform, "StartButton", "开始游戏").GetComponent<Button>();
            SetAnchors(startBtn.GetComponent<RectTransform>(), new Vector2(0.35f, 0.52f), new Vector2(0.65f, 0.60f), Vector2.zero);

            Button settingsBtn = CreateUIButton(mainPanel.transform, "SettingsButton", "设  置").GetComponent<Button>();
            SetAnchors(settingsBtn.GetComponent<RectTransform>(), new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.50f), Vector2.zero);

            Button multiBtn = CreateUIButton(mainPanel.transform, "MultiplayerButton", "多人游戏").GetComponent<Button>();
            SetAnchors(multiBtn.GetComponent<RectTransform>(), new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.40f), Vector2.zero);

            Button quitBtn = CreateUIButton(mainPanel.transform, "QuitButton", "退  出").GetComponent<Button>();
            SetAnchors(quitBtn.GetComponent<RectTransform>(), new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.30f), Vector2.zero);

            Text notice = CreateUIText(mainPanel.transform, "MenuNoticeText", "", 14, TextAnchor.MiddleCenter);
            SetAnchors(notice.rectTransform, new Vector2(0f, 0.14f), new Vector2(1f, 0.20f), Vector2.zero);
            notice.color = new Color(1f, 0.85f, 0.2f);

            menuUI.mainMenuPanel = mainPanel;
            menuUI.startButton = startBtn;
            menuUI.settingsButton = settingsBtn;
            menuUI.multiplayerButton = multiBtn;
            menuUI.quitButton = quitBtn;
            menuUI.menuNoticeText = notice;

            // ── Game Over Panel (results screen) ──
            GameObject overPanel = CreateUIPanel(canvasObj.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.85f));
            RectTransform overRT = overPanel.GetComponent<RectTransform>();
            overRT.anchorMin = Vector2.zero;
            overRT.anchorMax = Vector2.one;
            overRT.anchoredPosition = Vector2.zero;
            overRT.sizeDelta = Vector2.zero;
            overPanel.SetActive(false);

            Text winner = CreateUIText(overPanel.transform, "WinnerText", "VICTORY!", 56, TextAnchor.MiddleCenter);
            SetAnchors(winner.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 0.88f), Vector2.zero);
            winner.fontStyle = FontStyle.Bold;

            Text finalScore = CreateUIText(overPanel.transform, "FinalScoreText", "占领积分  0 : 0", 26, TextAnchor.MiddleCenter);
            SetAnchors(finalScore.rectTransform, new Vector2(0f, 0.60f), new Vector2(1f, 0.70f), Vector2.zero);
            finalScore.color = Color.white;

            // Stats table header
            Text header = CreateUIText(overPanel.transform, "StatsHeader", "        击杀    阵亡", 18, TextAnchor.MiddleCenter);
            SetAnchors(header.rectTransform, new Vector2(0.15f, 0.50f), new Vector2(0.85f, 0.58f), Vector2.zero);
            header.color = new Color(0.8f, 0.8f, 0.8f);

            // Player stats row
            GameObject pRow = CreateUIPanel(overPanel.transform, "PlayerStatsRow", new Color(0.3f, 0.67f, 0.97f, 0.15f));
            RectTransform pRowRT = pRow.GetComponent<RectTransform>();
            pRowRT.anchorMin = new Vector2(0.15f, 0.40f);
            pRowRT.anchorMax = new Vector2(0.85f, 0.50f);
            pRowRT.anchoredPosition = Vector2.zero;
            pRowRT.sizeDelta = Vector2.zero;
            Text pRowText = CreateUIText(pRow.transform, "PlayerStatsText", "玩家  0    0", 18, TextAnchor.MiddleCenter);
            SetAnchors(pRowText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero);
            pRowText.color = new Color(0.3f, 0.67f, 0.97f);

            // Enemy stats row
            GameObject eRow = CreateUIPanel(overPanel.transform, "EnemyStatsRow", new Color(1f, 0.42f, 0.42f, 0.15f));
            RectTransform eRowRT = eRow.GetComponent<RectTransform>();
            eRowRT.anchorMin = new Vector2(0.15f, 0.30f);
            eRowRT.anchorMax = new Vector2(0.85f, 0.40f);
            eRowRT.anchoredPosition = Vector2.zero;
            eRowRT.sizeDelta = Vector2.zero;
            Text eRowText = CreateUIText(eRow.transform, "EnemyStatsText", "敌方  0    0", 18, TextAnchor.MiddleCenter);
            SetAnchors(eRowText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero);
            eRowText.color = new Color(1f, 0.42f, 0.42f);

            Button rematchBtn = CreateUIButton(overPanel.transform, "RematchButton", "再来一局").GetComponent<Button>();
            SetAnchors(rematchBtn.GetComponent<RectTransform>(), new Vector2(0.28f, 0.14f), new Vector2(0.48f, 0.22f), Vector2.zero);

            Button menuBtn = CreateUIButton(overPanel.transform, "BackToMenuButton", "返回主菜单").GetComponent<Button>();
            SetAnchors(menuBtn.GetComponent<RectTransform>(), new Vector2(0.52f, 0.14f), new Vector2(0.72f, 0.22f), Vector2.zero);

            menuUI.gameOverPanel = overPanel;
            menuUI.winnerText = winner;
            menuUI.finalScoreText = finalScore;
            menuUI.playerStatsText = pRowText;
            menuUI.enemyStatsText = eRowText;
            menuUI.rematchButton = rematchBtn;
            menuUI.backToMenuButton = menuBtn;

            // ── HUD root grouping ─────────────────
            // Group existing play HUD panels under a single hudRoot for state toggling
            GameObject hudRoot = new GameObject("HUDRoot");
            hudRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform hudRT = hudRoot.GetComponent<RectTransform>();
            hudRT.anchorMin = Vector2.zero;
            hudRT.anchorMax = Vector2.one;
            hudRT.anchoredPosition = Vector2.zero;
            hudRT.sizeDelta = Vector2.zero;

            foreach (string hudName in new[] { "TopBar", "BottomPanel", "HillScoreboard", "Hints" })
            {
                Transform hud = canvasObj.transform.Find(hudName);
                if (hud != null)
                    hud.SetParent(hudRoot.transform, false);
            }

            menuUI.hudRoot = hudRoot;
        }

        static GameObject CreateCardUIPrefab()
        {
            GameObject card = new GameObject("CardPrefab");
            card.SetActive(false);

            RectTransform cardRT = card.AddComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(130f, 115f);

            Image cardBG = card.AddComponent<Image>();
            cardBG.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button cardBtn = card.AddComponent<Button>();

            // Cost text
            Text costText = CreateUIText(card.transform, "CostText", "$60", 12, TextAnchor.UpperLeft);
            SetAnchors(costText.rectTransform, new Vector2(0f, 0.7f), new Vector2(0.5f, 1f), new Vector2(5f, 0f));
            costText.color = Color.white;

            // Title text
            Text titleText = CreateUIText(card.transform, "TitleText", "Card", 14, TextAnchor.UpperLeft);
            SetAnchors(titleText.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 0.7f), new Vector2(5f, 0f));
            titleText.color = Color.white;

            // Description text
            Text descText = CreateUIText(card.transform, "DescText", "Description", 11, TextAnchor.UpperLeft);
            SetAnchors(descText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.4f), new Vector2(5f, 0f));
            descText.color = new Color(0.73f, 0.73f, 0.73f);

            // Selection highlight
            GameObject selObj = new GameObject("SelectionHighlight");
            selObj.transform.SetParent(card.transform, false);
            RectTransform selRT = selObj.AddComponent<RectTransform>();
            selRT.anchorMin = Vector2.zero;
            selRT.anchorMax = Vector2.one;
            selRT.sizeDelta = Vector2.zero;
            Image selImg = selObj.AddComponent<Image>();
            selImg.color = new Color(0f, 1f, 0.67f, 0.15f);
            selObj.SetActive(false);

            // Disabled overlay
            GameObject disObj = new GameObject("DisabledOverlay");
            disObj.transform.SetParent(card.transform, false);
            RectTransform disRT = disObj.AddComponent<RectTransform>();
            disRT.anchorMin = Vector2.zero;
            disRT.anchorMax = Vector2.one;
            disRT.sizeDelta = Vector2.zero;
            Image disImg = disObj.AddComponent<Image>();
            disImg.color = new Color(0f, 0f, 0f, 0.5f);
            disObj.SetActive(false);

            // CardUIElement
            CardUIElement cardUI = card.AddComponent<CardUIElement>();
            cardUI.costText = costText;
            cardUI.titleText = titleText;
            cardUI.descText = descText;
            cardUI.cardImage = cardBG;
            cardUI.selectionHighlight = selImg;
            cardUI.disabledOverlay = disObj;

            // Save as prefab
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(card, "Assets/Prefabs/CardUI.prefab");
            Object.DestroyImmediate(card);
            return prefab;
        }

        // ── UI Helper Methods ──────────────────────

        static GameObject CreateUIPanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        static GameObject CreateUIButton(Transform parent, string name, string label)
        {
            GameObject btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            btn.AddComponent<RectTransform>();
            Image img = btn.AddComponent<Image>();
            img.color = new Color(0.27f, 0.27f, 0.27f, 1f);
            Button button = btn.AddComponent<Button>();
            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.27f, 0.27f, 0.27f, 1f);
            cb.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            cb.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = cb;

            Text btnText = CreateUIText(btn.transform, "Label", label, 14, TextAnchor.MiddleCenter);
            SetAnchors(btnText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero);
            btnText.color = Color.white;

            return btn;
        }

        static Text CreateUIText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            textObj.AddComponent<RectTransform>();
            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 offset)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = offset;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
#endif
