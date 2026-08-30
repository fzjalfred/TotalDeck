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
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
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
            camCtrl.cameraHeight = 35f;
            camCtrl.cameraAngle = 55f;
            camCtrl.cameraCenter = Vector3.zero;

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
            battleInit.playerStartPos = new Vector3(0f, 0f, 15f);
            battleInit.enemyStartPos = new Vector3(0f, 0f, -15f);
            battleInit.enemySpawnPos = new Vector3(0f, 0f, -20f);

            // ── BattlefieldZone ───────────────────────
            GameObject zoneObj = new GameObject("BattlefieldZone");
            zoneObj.AddComponent<BattlefieldZone>();

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
