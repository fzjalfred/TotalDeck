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

            bootstrap.soldierPrefab = soldierPrefab;
            bootstrap.regimentPrefabs = new GameObject[] { infantryPrefab };

            // ── GameManager ───────────────────────────
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.soldierPrefab = soldierPrefab;
            gm.regimentPrefabs = new GameObject[] { infantryPrefab };

            // ── AIController ─────────────────────────
            GameObject aiObj = new GameObject("AIController");
            aiObj.AddComponent<AIController>();

            // ── DebugPanel (F1 toggle) ───────────────
            GameObject debugObj = new GameObject("DebugPanel");
            debugObj.AddComponent<DebugPanel>();

            // ── CardManager ──────────────────────────
            GameObject cmObj = new GameObject("CardManager");
            CardManager cm = cmObj.AddComponent<CardManager>();

            // Create card assets (definitions live in Assets/Cards/CardLibrary.cs)
            CardData footmanCard = CreateCardAsset("FootmanCard", 1, "Footman", "Deploy a 50-man footman regiment", 60, CardType.Unit, 0);
            CardData archerCard = CreateCardAsset("ArcherCard", 2, "Archer", "Ranged troop", 90, CardType.Unit, 0);
            CardData knightCard = CreateCardAsset("KnightCard", 3, "Knight", "Melee cavalry troop", 100, CardType.Unit, 0);
            CardData healCard = CreateCardAsset("HealCard", 4, "Heal", "Restore +15 soldiers to the most wounded friendly regiment", 40, CardType.Spell, 0, healAmount: 15);
            CardData infernoCard = CreateCardAsset("InfernoCard", 5, "Inferno", "Area damage spell", 80, CardType.Spell, 0);
            CardData frostCard = CreateCardAsset("FrostCard", 6, "Frost", "Slow / freeze debuff spell", 60, CardType.Spell, 0);

            cm.cardPool = new CardData[] { footmanCard, archerCard, knightCard, healCard, infernoCard, frostCard };
            cm.startingHand = new CardData[] { footmanCard, healCard };
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
            // UIBuilder lives in the Editor assembly — invoke via reflection
            // to avoid a cross-assembly compile dependency
            var uiBuilderType = System.Type.GetType("TotalDeck.EditorTools.UIBuilder, Assembly-CSharp-Editor");
            if (uiBuilderType != null)
            {
                var rebuild = uiBuilderType.GetMethod("Rebuild",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                rebuild?.Invoke(null, null);
            }

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
            string path = "Assets/Cards/" + fileName + ".asset";
            CardData existing = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Cards"))
                AssetDatabase.CreateFolder("Assets", "Cards");

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
            Text costText = CreateUITextInline(card.transform, "CostText", "$60", 12, TextAnchor.UpperLeft);
            SetAnchorsInline(costText.rectTransform, new Vector2(0f, 0.7f), new Vector2(0.5f, 1f), new Vector2(5f, 0f));
            costText.color = Color.white;

            // Title text
            Text titleText = CreateUITextInline(card.transform, "TitleText", "Card", 14, TextAnchor.UpperLeft);
            SetAnchorsInline(titleText.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 0.7f), new Vector2(5f, 0f));
            titleText.color = Color.white;

            // Description text
            Text descText = CreateUITextInline(card.transform, "DescText", "Description", 11, TextAnchor.UpperLeft);
            SetAnchorsInline(descText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.4f), new Vector2(5f, 0f));
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

        static Text CreateUITextInline(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Text t = go.AddComponent<Text>();
            t.text = content;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void SetAnchorsInline(RectTransform rt, Vector2 min, Vector2 max, Vector2 offset)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = offset;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
#endif
