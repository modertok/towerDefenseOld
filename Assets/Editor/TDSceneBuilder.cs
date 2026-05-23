using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class TDSceneBuilder
{
    // ── Середньовічна палітра ─────────────────────────────────────────────
    static Color C_BG         = new Color(0.06f,0.10f,0.06f);
    static Color C_PARCHMENT  = new Color(0.18f,0.12f,0.07f);
    static Color C_WOOD       = new Color(0.12f,0.08f,0.04f);
    static Color C_STONE      = new Color(0.28f,0.24f,0.19f);
    static Color C_GOLD       = new Color(0.82f,0.65f,0.08f);
    static Color C_GOLD_DARK  = new Color(0.50f,0.38f,0.04f);
    static Color C_TEXT       = new Color(0.92f,0.85f,0.68f);
    static Color C_GREEN_BTN  = new Color(0.15f,0.38f,0.10f);
    static Color C_RED_BTN    = new Color(0.45f,0.10f,0.08f);
    static Color C_BLUE_BTN   = new Color(0.10f,0.22f,0.45f);
    static Color C_BORDER     = new Color(0.60f,0.48f,0.12f);

    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("TowerDefense/1 - Build Game Scene")]
    static void BuildGameScene()
    {
        // Не можна білдити під час гри!
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Зупини гру!",
                "Натисни ■ (Stop) перед тим як будувати сцену.", "OK");
            return;
        }
        EnsureFolder("Assets", "Scenes");
        // Завжди створюємо НОВУ порожню сцену (не чіпаємо SampleScene)
        var freshScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildGame();
        EditorSceneManager.SaveScene(freshScene, "Assets/Scenes/Game.unity");
        // Game.unity тепер відкрита → Press Play одразу!
        UpdateBuildSettings();
        EditorUtility.DisplayDialog("Tower Defense",
            "Game.unity побудована!\nПросто натисни ▶ Play!", "OK");
    }

    [MenuItem("TowerDefense/2 - Build Main Menu Scene")]
    static void BuildMainMenuScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Зупини гру!", "Натисни ■ перед білдом.", "OK");
            return;
        }
        EnsureFolder("Assets", "Scenes");
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildMainMenu();
        EditorSceneManager.SaveScene(s, "Assets/Scenes/MainMenu.unity");
        UpdateBuildSettings();
        if (System.IO.File.Exists("Assets/Scenes/Game.unity"))
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        EditorUtility.DisplayDialog("Tower Defense", "MainMenu.unity готова!", "OK");
    }

    // ══════════════════════════════════════════════════════════════════════
    // GAME SCENE
    // ══════════════════════════════════════════════════════════════════════

    static void BuildGame()
    {
        ClearScene();
        EnsureAssetFolders();

        var archer  = GetTower("Archer",  100, 1.0f, 3.0f, 20f, AttackType.Single, 0f,   0f,   0f);
        var mage    = GetTower("Mage",    150, 0.6f, 2.0f, 30f, AttackType.AoE,    0f,   1.5f, 0f);
        var freezer = GetTower("Freezer", 120, 1.0f, 3.0f, 10f, AttackType.Slow,   0.5f, 0f,   2f);
        var cannon  = GetTower("Cannon",  200, 0.3f, 4.5f, 65f, AttackType.Single, 0f,   0f,   0f);
        var goblin  = GetEnemy("Goblin", 10, 40,  3.0f, 1, 5,  false);
        var orc     = GetEnemy("Orc",    25, 150, 1.5f, 1, 12, false);
        var ghost   = GetEnemy("Ghost",  20, 80,  2.0f, 1, 8,  true);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();

        // ── Камера (зміщена вправо щоб sidebar не перекривав сітку) ───────
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        // ── Точний розрахунок (8 рядків × 1 = 8 одиниць, клітини: y=-4..4) ──
        // Canvas 1280×720, sidebar=130, HUD=58, рамка ~10px з кожного боку
        // Видима ігрова зона в canvas: x=140..1280, y=10..652
        // orthoSize=4.8 → клітини (-4..4) проектуються в y≈22..622 (з відступом)
        // cameraY=0.5 центрує сітку у видимій зоні
        // cameraX=0.4 → ліві клітини за рамкою sidebar з відступом
        cam.orthographicSize = 4.8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C_BG;
        camGo.transform.position = new Vector3(0.4f, 0.5f, -10f);
        camGo.AddComponent<AudioListener>();

        // ── Z-подібний шлях (база посередині-справа, не в кутку) ────────────
        // Кінець шляху на ряду 4 (y=0.5) → замок не перекривається з кнопкою HUD
        var wpPos = new Vector3[]
        {
            new Vector3(-5.5f,  1.5f, 0), // WP0 вхід (ряд 5)
            new Vector3(-1.5f,  1.5f, 0), // WP1 → (кол 4)
            new Vector3(-1.5f, -2.5f, 0), // WP2 ↓ (ряд 1)
            new Vector3( 3.5f, -2.5f, 0), // WP3 → (кол 9)
            new Vector3( 3.5f,  0.5f, 0), // WP4 ↑ (ряд 4, середина)
            new Vector3( 5.5f,  0.5f, 0), // WP5 → база
        };
        var wpParent = new GameObject("Waypoints");
        var wpTr = new Transform[wpPos.Length];
        for (int i = 0; i < wpPos.Length; i++)
        {
            var wp = new GameObject($"WP_{i}");
            wp.transform.SetParent(wpParent.transform);
            wp.transform.position = wpPos[i];
            wpTr[i] = wp.transform;
        }

        // ── Grid ──────────────────────────────────────────────────────────
        var gridGo = new GameObject("GridManager");
        var grid   = gridGo.AddComponent<GridManager>();
        grid.waypoints  = wpTr;
        grid.grassColor = new Color(0.16f, 0.30f, 0.12f);
        grid.pathColor  = new Color(0.60f, 0.48f, 0.30f);

        // ── База ──────────────────────────────────────────────────────────
        var baseGo = new GameObject("Base");
        baseGo.transform.position = new Vector3(6.5f, 0.5f, 0); // посередині-справа
        baseGo.AddComponent<Base>();
        var bsr = baseGo.AddComponent<SpriteRenderer>();
        bsr.sprite = MakeSquare(new Color(0.85f,0.68f,0.10f));
        bsr.sortingOrder = 3;
        baseGo.transform.localScale = Vector3.one * 0.85f;

        // Позначка БАЗА
        CreateWorldText(baseGo.transform, "БАЗА", new Vector3(0, 0.75f, 0), 0.24f, C_GOLD);

        // ── Вхід ворогів ──────────────────────────────────────────────────
        var entryGo = new GameObject("EnemyEntry");
        entryGo.transform.position = new Vector3(-6.3f, 1.5f, 0);
        var esr = entryGo.AddComponent<SpriteRenderer>();
        esr.sprite = MakeSquare(new Color(0.7f,0.12f,0.08f));
        esr.sortingOrder = 3;
        entryGo.transform.localScale = Vector3.one * 0.55f;

        // ── Range Display & TowerRangeDisplay ─────────────────────────────
        var rangeGo = new GameObject("TowerRangeDisplay");
        rangeGo.AddComponent<TowerRangeDisplay>();

        // ── Managers ──────────────────────────────────────────────────────
        var mgr = new GameObject("Managers");
        mgr.AddComponent<GameStateManager>();
        mgr.AddComponent<GameManager>();
        mgr.AddComponent<EconomyManager>();
        mgr.AddComponent<WaveManager>();
        mgr.AddComponent<ProjectilePool>();
        mgr.AddComponent<TowerPlacer>();

        var ai = mgr.AddComponent<AIAttacker>();
        ai.goblinData = goblin; ai.orcData = orc; ai.ghostData = ghost;

        var pool = mgr.AddComponent<EnemyPool>();
        pool.entries = new System.Collections.Generic.List<EnemyPool.PoolEntry>
        {
            new EnemyPool.PoolEntry { data=goblin, preWarm=15 },
            new EnemyPool.PoolEntry { data=orc,    preWarm=8  },
            new EnemyPool.PoolEntry { data=ghost,  preWarm=8  },
        };

        var boot = mgr.AddComponent<SceneBootstrapper>();
        boot.archerData=archer; boot.mageData=mage;
        boot.freezerData=freezer; boot.cannonData=cannon;
        boot.goblinData=goblin; boot.orcData=orc; boot.ghostData=ghost;

        // ── UI ────────────────────────────────────────────────────────────
        BuildGameUI(archer, mage, freezer, cannon);

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ══════════════════════════════════════════════════════════════════════
    // GAME UI  — середньовічний стиль
    // ══════════════════════════════════════════════════════════════════════

    static void BuildGameUI(TowerData archer, TowerData mage,
                            TowerData freezer, TowerData cannon)
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // (Рамка створюється в кінці методу, щоб рендеритись ПОВЕРХ HUD/sidebar.)

        // ── 1. HUD: верхня смуга покращена ───────────────────────────────
        var hud = StretchBar(canvasGo.transform, "HUD", 58, true);
        // Градієнтний темний фон HUD (використовуємо темний пергамент)
        var hudImg = AddImg(hud, new Color(0.12f, 0.08f, 0.04f, 0.97f));
        AddBorderBottom(hud, C_GOLD, 2);
        // Верхня золота лінія
        AddBorderTop(hud, new Color(0.60f, 0.45f, 0.05f), 2);

        var hlg = hud.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        // right padding=190 — резервує місце справа під StartWaveBtn (абсолютно позиційований)
        hlg.padding = new RectOffset(134, 190, 7, 7); hlg.spacing = 0;

        var hudUI = hud.AddComponent<HudUI>();
        hudUI.goldText   = HudTMP(hud.transform, "GoldText",   "Золото: 300", C_GOLD);
        hudUI.baseHPText = HudTMP(hud.transform, "BaseHPText", "База: 20 HP", new Color(1f,0.5f,0.3f));
        hudUI.roundText  = HudTMP(hud.transform, "RoundText",  "Раунд: 0/10", C_TEXT);
        hudUI.stateText  = HudTMP(hud.transform, "StateText",  "[ Пiдготовка ]", new Color(0.55f,1f,0.55f));

        // Кнопка ПОЧАТИ ХВИЛЮ — АБСОЛЮТНА позиція (поза HLG), top-right HUD
        var startWaveGo = new GameObject("StartWaveBtn");
        startWaveGo.transform.SetParent(hud.transform, false);
        var swRt = startWaveGo.AddComponent<RectTransform>();
        // якоримо до правого краю HUD по центру вертикалі
        swRt.anchorMin        = new Vector2(1, 0.5f);
        swRt.anchorMax        = new Vector2(1, 0.5f);
        swRt.pivot            = new Vector2(1, 0.5f);
        swRt.anchoredPosition = new Vector2(-14, 0); // 14px від правого краю (за рамкою)
        swRt.sizeDelta        = new Vector2(170, 42);
        // НЕ враховувати в HorizontalLayoutGroup
        var swIgn = startWaveGo.AddComponent<LayoutElement>();
        swIgn.ignoreLayout = true;

        var swImg = startWaveGo.AddComponent<Image>();
        swImg.color = C_GREEN_BTN;
        var swBtn = startWaveGo.AddComponent<Button>();
        SetBtnColors(swBtn, C_GREEN_BTN);

        var swLblGo = new GameObject("Lbl");
        swLblGo.transform.SetParent(startWaveGo.transform, false);
        var swLblRt = swLblGo.AddComponent<RectTransform>();
        swLblRt.anchorMin = Vector2.zero; swLblRt.anchorMax = Vector2.one;
        swLblRt.offsetMin = new Vector2(4,2); swLblRt.offsetMax = new Vector2(-4,-2);
        var swLblTmp = swLblGo.AddComponent<TextMeshProUGUI>();
        swLblTmp.text                = "ПОЧАТИ ХВИЛЮ";
        swLblTmp.fontSize            = 14;
        swLblTmp.color               = Color.white;
        swLblTmp.fontStyle           = FontStyles.Bold;
        swLblTmp.alignment           = TextAlignmentOptions.Center;
        swLblTmp.enableWordWrapping  = false;
        swLblTmp.overflowMode        = TextOverflowModes.Overflow;
        startWaveGo.AddComponent<StartWaveButton>();
        // HudUI більше не потрібно керувати кнопкою — StartWaveButton це робить сам
        hudUI.startWaveBtnGo = null;

        // ── 2. Бокова панель ─────────────────────────────────────────────
        // Ширина 130px. Усі елементи мінімальні щоб вміщатись навіть при scale=2
        // (екран 2560x1440 → scale=2 → кожен canvas px = 2 screen px)
        const float SW = 130f;
        const float BW = SW - 6f;
        var sidebar = SidePanel(canvasGo.transform, "Sidebar", SW);
        AddImg(sidebar, C_WOOD);
        AddBorderRight(sidebar, C_BORDER, 3);

        var sbl = sidebar.AddComponent<VerticalLayoutGroup>();
        sbl.childForceExpandWidth  = true;
        sbl.childForceExpandHeight = false;
        sbl.padding = new RectOffset(4, 4, 60, 4);
        sbl.spacing = 3;
        sbl.childAlignment = TextAnchor.UpperCenter;

        var sbTitle = PanelTMP(sidebar.transform, "SBTitle", "ВЕЖI", 12, C_GOLD);
        sbTitle.fontStyle = FontStyles.Bold;
        sbTitle.alignment = TextAlignmentOptions.Center;
        LE(sbTitle.gameObject, BW, 14);

        var sidebarUI = canvasGo.AddComponent<TowerSidebar>();
        sidebarUI.sidebarPanel = sidebar;
        sidebarUI.archerData   = archer;
        sidebarUI.mageData     = mage;
        sidebarUI.freezerData  = freezer;
        sidebarUI.cannonData   = cannon;

        // Кнопки 36px — компактні але читабельні
        sidebarUI.archerBtn  = SideBtnH(sidebar.transform,"Archer",
            "Лучник 100g\nОдна цiль",  new Color(0.25f,0.38f,0.15f), 36);
        sidebarUI.mageBtn    = SideBtnH(sidebar.transform,"Mage",
            "Маг 150g\nВибух AoE",     new Color(0.28f,0.10f,0.42f), 36);
        sidebarUI.freezerBtn = SideBtnH(sidebar.transform,"Freezer",
            "Льодяник 120g\nУповiл.",  new Color(0.08f,0.30f,0.48f), 36);
        sidebarUI.cannonBtn  = SideBtnH(sidebar.transform,"Cannon",
            "Гармата 200g\nВелика",    new Color(0.28f,0.22f,0.16f), 36);

        // (без декоративної лінії — просто текст)
        var goldLbl = PanelTMP(sidebar.transform,"GoldLbl","Золото: 300",13,C_GOLD);
        goldLbl.fontStyle = FontStyles.Bold;
        goldLbl.alignment = TextAlignmentOptions.Center;
        LE(goldLbl.gameObject, BW, 22);
        sidebarUI.goldLabel = goldLbl;

        var selLbl = PanelTMP(sidebar.transform,"SelLbl","Оберiть вежу\nзi списку",10,
            new Color(0.80f,0.80f,0.68f));
        selLbl.alignment = TextAlignmentOptions.Center;
        LE(selLbl.gameObject, BW, 28);
        sidebarUI.selectedLabel = selLbl;

        sidebar.SetActive(false);

        // ── 4. GameOver панель ────────────────────────────────────────────
        var goPanel = CenteredPanel(canvasGo.transform,"GameOverPanel",new Vector2(520,300));
        AddImg(goPanel, new Color(0.05f,0.04f,0.02f,0.97f));
        AddOutline(goPanel, C_GOLD, 3);

        // GameOverUI на CANVAS (не на прихованій goPanel)!
        var goUI = canvasGo.AddComponent<GameOverUI>();
        goUI.panel = goPanel;

        goUI.resultText = SimpleTMP(goPanel.transform,"ResultText",
            "ЗАХИСНИК ПЕРЕМІГ!", 34, C_GOLD);
        SetRT(goUI.resultText.GetComponent<RectTransform>(),
            new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0.5f,1f),
            new Vector2(0,-50),new Vector2(480,60));
        goUI.resultText.alignment = TextAlignmentOptions.Center;
        goUI.resultText.fontStyle = FontStyles.Bold;

        goUI.subtitleText = SimpleTMP(goPanel.transform,"SubtitleText",
            "База вистояла всi раунди!", 20, C_TEXT);
        SetRT(goUI.subtitleText.GetComponent<RectTransform>(),
            new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),
            new Vector2(0,20),new Vector2(480,38));
        goUI.subtitleText.alignment = TextAlignmentOptions.Center;

        goUI.restartButton = CenteredBtn(goPanel.transform,"RestartBtn",
            "ГРАТИ ЗНОВУ", C_GREEN_BTN, new Vector2(0,-80), new Vector2(220,52));

        goPanel.SetActive(false);

        // ── Декоративна рамка ОСТАННЬОЮ — щоб рендерилась поверх sidebar/HUD
        AddGameBorder(canvasGo.transform);
    }

    // ══════════════════════════════════════════════════════════════════════
    // MAIN MENU
    // ══════════════════════════════════════════════════════════════════════

    static void BuildMainMenu()
    {
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f,0.07f,0.04f);
        camGo.transform.position = new Vector3(0,0,-10);
        camGo.AddComponent<AudioListener>();

        var cGo = new GameObject("Canvas");
        var cv  = cGo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc  = cGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280,720);
        cGo.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Фоновий банер
        var banner = CenteredPanel(cGo.transform,"Banner",new Vector2(700,420));
        AddImg(banner, new Color(0.06f,0.05f,0.03f,0.96f));
        AddOutline(banner, C_BORDER, 4);

        // Заголовок
        var title = SimpleTMP(banner.transform,"Title","TOWER DEFENSE",62,C_GOLD);
        SetRT(title.GetComponent<RectTransform>(),
            new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0.5f,1f),
            new Vector2(0,-50),new Vector2(660,80));
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        var sub = SimpleTMP(banner.transform,"Sub","Захисти замок вiд орд ворогiв",19,C_TEXT);
        SetRT(sub.GetComponent<RectTransform>(),
            new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0.5f,1f),
            new Vector2(0,-140),new Vector2(600,32));
        sub.alignment = TextAlignmentOptions.Center;

        var ctrl = cGo.AddComponent<MainMenuController>();
        ctrl.startButton = CenteredBtn(banner.transform,"StartBtn",
            "ГРАТИ", C_GREEN_BTN, new Vector2(0,30), new Vector2(260,64));
        ctrl.startButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 28;

        ctrl.rulesButton = CenteredBtn(banner.transform,"RulesBtn",
            "ПРАВИЛА ГРИ", C_STONE, new Vector2(0,-50), new Vector2(260,50));

        // Панель правил
        var rules = CenteredPanel(cGo.transform,"RulesPanel",new Vector2(860,520));
        AddImg(rules, new Color(0.05f,0.04f,0.02f,0.97f));
        AddOutline(rules, C_BORDER, 4);
        ctrl.rulesPanel = rules;

        var rTitle = SimpleTMP(rules.transform,"RTitle","ПРАВИЛА ГРИ",26,C_GOLD);
        SetRT(rTitle.GetComponent<RectTransform>(),
            new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0.5f,1f),
            new Vector2(0,-30),new Vector2(820,42));
        rTitle.alignment=TextAlignmentOptions.Center; rTitle.fontStyle=FontStyles.Bold;

        const string R =
"МЕТА\n" +
"  Захисник: встояти всi 10 раундiв з HP > 0\n" +
"  AI-Атакуючий: знизити HP бази до 0\n\n" +
"ЯК ГРАТИ\n" +
"  1. У фазi Пiдготовки — вибери вежу з лiвої панелi\n" +
"  2. Клiкни на зелену клiтинку, щоб поставити вежу\n" +
"  3. Натисни >> ПОЧАТИ ХВИЛЮ\n" +
"  4. Вежi стрiляють автоматично\n\n" +
"ВЕЖI\n" +
"  Лучник (100g)    Одна цiль | сер. дальнiсть\n" +
"  Маг (150g)       Вибух AoE | коротка дальнiсть\n" +
"  Льодяник (120g)  Уповiльнення -50%\n" +
"  Гармата (200g)   Велика шкода | довга дальнiсть\n\n" +
"ВОРОГИ\n" +
"  Гоблiн (10 очкiв)   швидкий, слабкий\n" +
"  Орк (25 очкiв)      повiльний, мiцний\n" +
"  Привид (20 очкiв)   iгнорує Льодяник\n\n" +
"ПIДКАЗКА: клiкни на вежу щоб побачити коло дальностi";

        var rText = SimpleTMP(rules.transform,"RText",R,14,new Color(0.88f,0.82f,0.68f));
        SetRT(rText.GetComponent<RectTransform>(),
            Vector2.zero,Vector2.one,new Vector2(0.5f,0.5f),
            new Vector2(0,-20),new Vector2(-40,-90));
        rText.alignment=TextAlignmentOptions.TopLeft; rText.lineSpacing=4;

        ctrl.closeRulesButton = CenteredBtn(rules.transform,"CloseBtn",
            "ЗАКРИТИ", C_RED_BTN, new Vector2(0,-220), new Vector2(180,44));
        rules.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SCRIPTABLE OBJECT HELPERS
    // ══════════════════════════════════════════════════════════════════════

    static TowerData GetTower(string n,int cost,float fr,float range,float dmg,
        AttackType t,float slow,float aoe,float dur)
    {
        string path=$"Assets/ScriptableObjects/TowerData/{n}Data.asset";
        var e=AssetDatabase.LoadAssetAtPath<TowerData>(path);
        if(e!=null){e.cost=cost;e.fireRate=fr;e.range=range;e.damage=dmg;
            e.attackType=t;e.slowFraction=slow;e.aoeRadius=aoe;e.slowDuration=dur;return e;}
        var d=ScriptableObject.CreateInstance<TowerData>();
        d.towerName=n;d.cost=cost;d.fireRate=fr;d.range=range;d.damage=dmg;
        d.attackType=t;d.slowFraction=slow;d.aoeRadius=aoe;d.slowDuration=dur;
        AssetDatabase.CreateAsset(d,path);return d;
    }

    static EnemyData GetEnemy(string n,int cost,int hp,float spd,int dmg,int gold,bool immune)
    {
        string path=$"Assets/ScriptableObjects/EnemyData/{n}Data.asset";
        var e=AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if(e!=null){e.maxHP=hp;e.moveSpeed=spd;e.cost=cost;
            e.goldReward=gold;e.damageToBase=dmg;e.immuneToSlow=immune;return e;}
        var d=ScriptableObject.CreateInstance<EnemyData>();
        d.enemyName=n;d.cost=cost;d.maxHP=hp;d.moveSpeed=spd;
        d.damageToBase=dmg;d.goldReward=gold;d.immuneToSlow=immune;
        AssetDatabase.CreateAsset(d,path);return d;
    }

    static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity",     true),
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // UI FACTORIES
    // ══════════════════════════════════════════════════════════════════════

    static GameObject StretchBar(Transform p,string n,float h,bool top)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        float ay=top?1f:0f;
        rt.anchorMin=new Vector2(0,ay);rt.anchorMax=new Vector2(1,ay);
        rt.pivot=new Vector2(0.5f,ay);rt.anchoredPosition=Vector2.zero;
        rt.sizeDelta=new Vector2(0,h);return go;
    }

    /// Ліва бокова панель повної висоти
    static GameObject SidePanel(Transform p,string n,float w)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=Vector2.zero;rt.anchorMax=new Vector2(0,1);
        rt.pivot=new Vector2(0,0.5f);rt.anchoredPosition=Vector2.zero;
        rt.sizeDelta=new Vector2(w,0);return go;
    }

    static GameObject CenteredPanel(Transform p,string n,Vector2 sz)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0.5f,0.5f);
        rt.anchoredPosition=Vector2.zero;rt.sizeDelta=sz;return go;
    }

    static void SetRT(RectTransform rt,Vector2 mn,Vector2 mx,Vector2 piv,Vector2 pos,Vector2 sz)
    {rt.anchorMin=mn;rt.anchorMax=mx;rt.pivot=piv;rt.anchoredPosition=pos;rt.sizeDelta=sz;}

    static TextMeshProUGUI HudTMP(Transform p,string n,string txt,Color c)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        go.AddComponent<RectTransform>();
        var t=go.AddComponent<TextMeshProUGUI>();
        t.text=txt;t.fontSize=18;t.color=c;t.fontStyle=FontStyles.Bold;
        t.alignment=TextAlignmentOptions.Center;return t;
    }

    static TextMeshProUGUI PanelTMP(Transform p,string n,string txt,float sz,Color c)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        go.AddComponent<RectTransform>();
        var t=go.AddComponent<TextMeshProUGUI>();
        t.text=txt;t.fontSize=sz;t.color=c;return t;
    }

    static TextMeshProUGUI SimpleTMP(Transform p,string n,string txt,float sz,Color c)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        go.AddComponent<RectTransform>();
        var t=go.AddComponent<TextMeshProUGUI>();
        t.text=txt;t.fontSize=sz;t.color=c;return t;
    }

    // SideBtn зі стандартною висотою 60
    static Button SideBtn(Transform p,string n,string lbl,Color bg)
        => SideBtnH(p,n,lbl,bg,60);

    // SideBtn з кастомною висотою. Ширина = sidebar - padding щоб не вилазило.
    static Button SideBtnH(Transform p,string n,string lbl,Color bg,float h)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        go.AddComponent<RectTransform>();
        var img=go.AddComponent<Image>();img.color=bg;
        var btn=go.AddComponent<Button>();
        SetBtnColors(btn,bg);
        // 122 = sidebar(130) - padding(4*2) - запас(0). Уміщається на 130px панелі.
        LE(go,122,h);

        var lGo=new GameObject("Lbl");lGo.transform.SetParent(go.transform,false);
        var lRt=lGo.AddComponent<RectTransform>();
        lRt.anchorMin=Vector2.zero;lRt.anchorMax=Vector2.one;
        lRt.offsetMin=new Vector2(2,2);lRt.offsetMax=new Vector2(-2,-2);
        var t=lGo.AddComponent<TextMeshProUGUI>();
        t.text=lbl;t.fontSize=10;t.color=C_TEXT;
        t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;
        t.enableAutoSizing=true; t.fontSizeMin=8; t.fontSizeMax=11;
        return btn;
    }

    static void SepLine(GameObject parent,float w)
    {
        var s=new GameObject("Sep");s.transform.SetParent(parent.transform,false);
        s.AddComponent<RectTransform>();
        AddImg(s,C_BORDER);LE(s,w,2);
    }

    static Button CenteredBtn(Transform p,string n,string lbl,Color bg,Vector2 pos,Vector2 sz)
    {
        var go=new GameObject(n);go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0.5f,0.5f);
        rt.anchoredPosition=pos;rt.sizeDelta=sz;
        var img=go.AddComponent<Image>();img.color=bg;
        var btn=go.AddComponent<Button>();SetBtnColors(btn,bg);

        var lGo=new GameObject("Lbl");lGo.transform.SetParent(go.transform,false);
        var lRt=lGo.AddComponent<RectTransform>();
        lRt.anchorMin=Vector2.zero;lRt.anchorMax=Vector2.one;
        lRt.offsetMin=lRt.offsetMax=Vector2.zero;
        var t=lGo.AddComponent<TextMeshProUGUI>();
        t.text=lbl;t.fontSize=20;t.color=C_TEXT;
        t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;
        return btn;
    }

    static void SetBtnColors(Button b,Color base_)
    {
        var cb=ColorBlock.defaultColorBlock;
        cb.normalColor=base_;cb.highlightedColor=base_*1.4f;
        cb.pressedColor=base_*0.6f;cb.selectedColor=base_;
        cb.colorMultiplier=1f;b.colors=cb;
    }

    static Image AddImg(GameObject go,Color c)
    {var i=go.AddComponent<Image>();i.color=c;return i;}

    static void AddOutline(GameObject go,Color c,float w=2)
    {var o=go.AddComponent<Outline>();o.effectColor=c;o.effectDistance=new Vector2(w,w);}

    static void AddBorderBottom(GameObject go,Color c,float h)
    { MakeBorderChild(go,"BorderB",Vector2.zero,new Vector2(1,0),new Vector2(0.5f,0),new Vector2(0,h),c); }

    static void AddBorderTop(GameObject go,Color c,float h)
    { MakeBorderChild(go,"BorderT",new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,h),c); }

    static void AddBorderRight(GameObject go,Color c,float w)
    { MakeBorderChild(go,"BorderR",new Vector2(1,0),Vector2.one,new Vector2(1,0.5f),new Vector2(w,0),c); }

    static void MakeBorderChild(GameObject go,string name,
        Vector2 mn,Vector2 mx,Vector2 piv,Vector2 sz,Color c)
    {
        var ch=new GameObject(name); ch.transform.SetParent(go.transform,false);
        var rt=ch.AddComponent<RectTransform>();
        rt.anchorMin=mn; rt.anchorMax=mx; rt.pivot=piv;
        rt.anchoredPosition=Vector2.zero; rt.sizeDelta=sz;
        var img = ch.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
    }

    /// Старовинна декоративна рамка — товста, помітна, середньовічна.
    /// Рамка обмежує ІГРОВУ ЗОНУ (справа від sidebar, нижче HUD), а не весь канвас.
    /// Створює контейнер з відступами, кладе шари рамки + 4 кути.
    static void AddGameBorder(Transform canvasParent)
    {
        const float HUD_H   = 58f;   // висота HUD
        const float SIDE_W  = 130f;  // ширина sidebar

        Color cOuter = new Color(0.20f, 0.14f, 0.04f, 1f);   // темне золото
        Color cMid   = new Color(0.78f, 0.60f, 0.10f, 1f);   // яскраве золото
        Color cInner = new Color(0.45f, 0.32f, 0.05f, 1f);   // середнє золото
        Color cLight = new Color(0.96f, 0.85f, 0.35f, 1f);   // акцентна світла лінія

        // ── Контейнер: ігрова зона ─────────────────────────────────────────
        var frame = new GameObject("GameAreaFrame");
        frame.transform.SetParent(canvasParent, false);
        var fRt = frame.AddComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(SIDE_W, 0);     // обріж зліва на sidebar
        fRt.offsetMax = new Vector2(0, -HUD_H);     // обріж зверху на HUD
        var fp = frame.transform;

        // ── ЛІВА рамка (4 шари) — межа sidebar↔grid ────────────────────────
        BorderGO(fp,"FL0",Vector2.zero,new Vector2(0,1),new Vector2(0,.5f),new Vector2(10,0),cOuter);
        BorderGO(fp,"FL1",Vector2.zero,new Vector2(0,1),new Vector2(0,.5f),new Vector2( 7,0),cMid);
        BorderGO(fp,"FL2",Vector2.zero,new Vector2(0,1),new Vector2(0,.5f),new Vector2( 4,0),cInner);
        BorderGO(fp,"FL3",Vector2.zero,new Vector2(0,1),new Vector2(0,.5f),new Vector2( 2,0),cLight);

        // ── ПРАВА рамка ────────────────────────────────────────────────────
        BorderGO(fp,"FR0",new Vector2(1,0),Vector2.one,new Vector2(1,.5f),new Vector2(10,0),cOuter);
        BorderGO(fp,"FR1",new Vector2(1,0),Vector2.one,new Vector2(1,.5f),new Vector2( 7,0),cMid);
        BorderGO(fp,"FR2",new Vector2(1,0),Vector2.one,new Vector2(1,.5f),new Vector2( 4,0),cInner);
        BorderGO(fp,"FR3",new Vector2(1,0),Vector2.one,new Vector2(1,.5f),new Vector2( 2,0),cLight);

        // ── ВЕРХНЯ рамка — межа HUD↔grid ───────────────────────────────────
        BorderGO(fp,"FT0",new Vector2(0,1),Vector2.one,new Vector2(.5f,1),new Vector2(0,10),cOuter);
        BorderGO(fp,"FT1",new Vector2(0,1),Vector2.one,new Vector2(.5f,1),new Vector2(0, 7),cMid);
        BorderGO(fp,"FT2",new Vector2(0,1),Vector2.one,new Vector2(.5f,1),new Vector2(0, 4),cInner);
        BorderGO(fp,"FT3",new Vector2(0,1),Vector2.one,new Vector2(.5f,1),new Vector2(0, 2),cLight);

        // ── НИЖНЯ рамка ────────────────────────────────────────────────────
        BorderGO(fp,"FB0",Vector2.zero,new Vector2(1,0),new Vector2(.5f,0),new Vector2(0,10),cOuter);
        BorderGO(fp,"FB1",Vector2.zero,new Vector2(1,0),new Vector2(.5f,0),new Vector2(0, 7),cMid);
        BorderGO(fp,"FB2",Vector2.zero,new Vector2(1,0),new Vector2(.5f,0),new Vector2(0, 4),cInner);
        BorderGO(fp,"FB3",Vector2.zero,new Vector2(1,0),new Vector2(.5f,0),new Vector2(0, 2),cLight);

        // ── 4 кутові декоративні квадрати (великі + малі акценти) ──────────
        float cs1 = 18f, cs2 = 9f;
        // ↙ нижній-лівий
        Corner(fp, Vector2.zero,        Vector2.zero,        Vector2.zero,        new Vector2(cs1,cs1), cMid);
        Corner(fp, Vector2.zero,        Vector2.zero,        Vector2.zero,        new Vector2(cs2,cs2), cLight);
        // ↘ нижній-правий
        Corner(fp, new Vector2(1,0),    new Vector2(1,0),    new Vector2(1,0),    new Vector2(cs1,cs1), cMid);
        Corner(fp, new Vector2(1,0),    new Vector2(1,0),    new Vector2(1,0),    new Vector2(cs2,cs2), cLight);
        // ↖ верхній-лівий
        Corner(fp, new Vector2(0,1),    new Vector2(0,1),    new Vector2(0,1),    new Vector2(cs1,cs1), cMid);
        Corner(fp, new Vector2(0,1),    new Vector2(0,1),    new Vector2(0,1),    new Vector2(cs2,cs2), cLight);
        // ↗ верхній-правий
        Corner(fp, Vector2.one,         Vector2.one,         Vector2.one,         new Vector2(cs1,cs1), cMid);
        Corner(fp, Vector2.one,         Vector2.one,         Vector2.one,         new Vector2(cs2,cs2), cLight);
    }

    static void Corner(Transform p, Vector2 mn, Vector2 mx, Vector2 piv, Vector2 sz, Color c)
    {
        var go = new GameObject("Corner"); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin=mn; rt.anchorMax=mx; rt.pivot=piv;
        rt.anchoredPosition=Vector2.zero; rt.sizeDelta=sz;
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;   // НЕ блокувати кліки по полю!
    }

    // Extension method helper щоб ланцюжок не ламався
    static void BorderGO(Transform parent, string name,
        Vector2 mn, Vector2 mx, Vector2 piv, Vector2 sz, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin=mn; rt.anchorMax=mx; rt.pivot=piv;
        rt.anchoredPosition=Vector2.zero; rt.sizeDelta=sz;
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;   // НЕ блокувати кліки!
    }

    // (дублікат MakeBorderChild видалено)

    static void LE(GameObject go,float pw,float ph)
    {var l=go.AddComponent<LayoutElement>();l.preferredWidth=pw;l.preferredHeight=ph;
     l.flexibleWidth=0; l.flexibleHeight=0;}  // ОБА на 0, інакше VLG розтягує елементи (золотий блок!)

    static Sprite MakeSquare(Color c)
    {
        var t=new Texture2D(4,4){filterMode=FilterMode.Point};
        var p=new Color[16];for(int i=0;i<16;i++)p[i]=c;
        t.SetPixels(p);t.Apply();
        return Sprite.Create(t,new Rect(0,0,4,4),new Vector2(0.5f,0.5f),4);
    }

    static void CreateWorldText(Transform parent,string text,Vector3 offset,float scale,Color c)
    {
        // Без TMP у world space (skip — не критично)
    }

    static void EnsureAssetFolders()
    {
        EnsureFolder("Assets","ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects","TowerData");
        EnsureFolder("Assets/ScriptableObjects","EnemyData");
    }

    static void EnsureFolder(string p,string n)
    {if(!AssetDatabase.IsValidFolder($"{p}/{n}"))AssetDatabase.CreateFolder(p,n);}

    static void ClearScene()
    {
        foreach(var go in UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().GetRootGameObjects())
            Object.DestroyImmediate(go);
    }
}
