
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using YearEnd;

public static class Editable2DSceneBuilder
{
    private const string Marker = "Assets/Scenes/.VisualPolishFollowCamera_v2.txt";
    private static Font font;
    private static int buildingIndex;
    private static readonly string[] SceneNames =
    {
        "ResearchRoom", "Outdoors", "Exhibition",
        "BananaGarden", "Winery", "RiceCakeShop"
    };
    private static Color Dark = Hex("#243A34"),
                         Cream = Hex("#F4EBDD"),
                         Accent = Hex("#406758");

    [MenuItem("歲末拾遺 2D/重新生成六個可編輯場景（會覆蓋場景）")]
    public static void Regenerate()
    {
        if (!EditorUtility.DisplayDialog(
            "會覆蓋六個場景",
            "這會覆蓋已經在 Hierarchy 中移動或修改的場景物件。請先備份！",
            "確認重建", "取消"))
            return;

        GenerateAll(true);
    }

    private static void GenerateAll(bool force)
    {
        if (!force && File.Exists(Marker)) return;

        try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { font = null; }

        if (font == null)
            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { font = null; }

        if (font == null)
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft JhengHei", "Noto Sans CJK TC", "Arial" }, 24);

        foreach (string png in Directory.GetFiles(
            "Assets/Art2D", "*.png", SearchOption.AllDirectories))
        {
            string path = png.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            if (imp.textureType != TextureImporterType.Sprite ||
                imp.spriteImportMode != SpriteImportMode.Single ||
                Mathf.Abs(imp.spritePixelsPerUnit - 100f) > .01f ||
                imp.filterMode != FilterMode.Point ||
                imp.mipmapEnabled ||
                imp.textureCompression != TextureImporterCompression.Uncompressed)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        for (int i = 0; i < SceneNames.Length; i++)
            BuildScene(i);

        var list = new EditorBuildSettingsScene[SceneNames.Length];
        for (int i = 0; i < SceneNames.Length; i++)
            list[i] = new EditorBuildSettingsScene(
                "Assets/Scenes/" + SceneNames[i] + ".unity", true);

        EditorBuildSettings.scenes = list;

        File.WriteAllText(Marker,
            "Six editable scenes were generated from sprites and Unity UI. Do not delete unless regeneration is intended.\n");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorSceneManager.OpenScene("Assets/Scenes/ResearchRoom.unity");

        Debug.Log("歲末拾遺：六張可編輯 Scene 建立完成。三個小遊戲各自有獨立 Canvas，請按 Play 測試。");
    }

    private static Sprite Sprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art2D/" + path + ".png");

    private static Sprite SceneArt(int index) => Sprite("Scenes/" + SceneNames[index]);
    private static Sprite Banana(string name) => Sprite("Minigames/Banana/" + name);
    private static Sprite Wine(string name) => Sprite("Minigames/Wine/" + name);
    private static Sprite Rice(string name) => Sprite("Minigames/RiceCake/" + name);
    private static Sprite White() => Sprite("Extras/WhiteFill");

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private static GameObject Obj(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static Vector3 Map(Vector2 p, float z = 0)
    {
        var v = SceneWorld2D.MapToWorld(p, buildingIndex);
        return new Vector3(v.x, v.y, z);
    }

    private static GameObject WorldSprite(
        string name, Transform root, Sprite sprite,
        Vector2 map, int order, float scale = 1)
    {
        var g = Obj(name, root);
        g.transform.position = Map(map);
        g.transform.localScale = Vector3.one * scale;
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = order;
        return g;
    }

    private static void Wall(
        string name, Transform parent,
        Vector2 position, Vector2 size)
    {
        GameObject go = Obj(name, parent);
        go.transform.position = position;
        var c = go.AddComponent<BoxCollider2D>();
        c.size = size;
    }

    private static void Interact(
        Transform root, string name, Vector2 map,
        Sprite image, WorldAction2D action,
        string label, float scale = .3f)
    {
        var go = WorldSprite(name, root, image, map, 20, scale);

        if (image == null)
        {
            var marker = Obj(
                "Door interaction hotspot (no oversized item sprite)", go.transform);
            var r = marker.AddComponent<SpriteRenderer>();
            r.sprite = White();
            r.color = new Color(.98f, .83f, .48f, .65f);
            r.sortingOrder = 18;
            marker.transform.localScale = new Vector3(11.0f, 4.0f, 1f);
        }

        var target = go.AddComponent<WorldInteractable2D>();
        target.action = action;
        target.displayName = label;
        target.range = 1.25f;

        var circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = .7f;
    }

    private static void BuildScene(int index)
    {
        buildingIndex = index;
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera cam = Obj("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = index == 1 ? 3.5f : 4.5f;
        cam.backgroundColor = Hex("#1B2326");
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -20);
        cam.gameObject.AddComponent<AudioListener>();

        Transform map = Obj("Map & Artwork [SpriteRenderer - draggable]").transform;
        GameObject background = WorldSprite(
            "Background 16x9 [replaceable Sprite]",
            map, SceneArt(index), new Vector2(50, 32), -30);

        if (index == 1)
            background.transform.localScale = Vector3.one * 2.8f;

        Transform obstacles =
            Obj("Walls & Obstacles [BoxCollider2D - editable]").transform;

        if (index == 1)
        {
            Wall("Village left edge", obstacles,
                new Vector2(-22.35f, 0), new Vector2(.26f, 25.2f));
            Wall("Village right edge", obstacles,
                new Vector2(22.35f, 0), new Vector2(.26f, 25.2f));
            Wall("Village top edge", obstacles,
                new Vector2(0, 12.48f), new Vector2(45, .26f));
            Wall("Village bottom edge", obstacles,
                new Vector2(0, -12.48f), new Vector2(45, .26f));
        }
        else
        {
            if (index == 0)
            {
                Wall("Hotel bed", obstacles,
                    new Vector2(-5.1f, .25f), new Vector2(2.0f, 3.0f));
            }

            Wall("Left edge", obstacles,
                new Vector2(-7.85f, 0), new Vector2(.26f, 9.3f));
            Wall("Right edge", obstacles,
                new Vector2(7.85f, 0), new Vector2(.26f, 9.3f));
            Wall("Top edge", obstacles,
                new Vector2(0, 4.45f), new Vector2(15.9f, .2f));
            Wall("Bottom edge", obstacles,
                new Vector2(0, -4.45f), new Vector2(15.9f, .2f));
        }

        Vector2 spawn =
            index == 0 ? new Vector2(50, 27) :
            index == 1 ? new Vector2(53, 40) :
            index == 2 ? new Vector2(50, 25) :
            new Vector2(50, 30);

        GameObject player = WorldSprite(
            "Player [new protagonist Sprite + Rigidbody2D + Collider2D]",
            null, Sprite("player_front"), spawn, 30, .82f);

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var cap = player.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.15f, 1.45f);
        cap.offset = new Vector2(0, -.12f);

        if (index == 1)
        {
            var follower = cam.gameObject.AddComponent<CameraFollow2D>();
            follower.target = player.transform;
            follower.worldMin = new Vector2(-22.2f, -12.25f);
            follower.worldMax = new Vector2(22.2f, 12.25f);
            follower.smoothing = .10f;

            cam.transform.position = new Vector3(
                player.transform.position.x,
                player.transform.position.y, -20);
        }

        var move = player.AddComponent<PlayerMovement2D>();
        move.front = Sprite("player_front");
        move.back = Sprite("player_back");
        move.left = Sprite("player_left");
        move.right = Sprite("player_right");
        move.moveSpeed = index == 1 ? 3.7f : 2.9f;

        Transform interact =
            Obj("Entrances & NPCs [move these in Scene]").transform;

        if (index == 0)
        {
            Interact(interact, "Wooden Board [auto pickup]",
                new Vector2(35, 37), Sprite("wooden_board"),
                WorldAction2D.Plank, "小木板", .45f);

            Interact(interact, "Hotel room door → Village",
                new Vector2(69, 42), null,
                WorldAction2D.ToOutdoors, "從旅館房門出去", .26f);
        }
        else if (index == 1)
        {
            Interact(interact, "Village entrance → BananaGarden",
                new Vector2(22, 45), null,
                WorldAction2D.EnterBananaGarden, "進入香蕉園", .30f);

            Interact(interact, "Village entrance → Winery",
                new Vector2(79, 48), null,
                WorldAction2D.EnterWinery, "進入釀酒坊", .30f);

            Interact(interact, "Village entrance → RiceCakeShop",
                new Vector2(83, 29), null,
                WorldAction2D.EnterRiceCakeShop, "進入年糕店", .30f);

            Interact(interact, "Village entrance → Hotel",
                new Vector2(53, 46), null,
                WorldAction2D.ToResearch, "返回旅館", .30f);

            Interact(interact, "Village entrance → Exhibition",
                new Vector2(30, 28), null,
                WorldAction2D.ToExhibition, "進入展示區", .30f);
        }
        else if (index == 2)
        {
            Interact(interact, "Final Puzzle Table [E]",
                new Vector2(50, 36), Sprite("wooden_board"),
                WorldAction2D.FinalPuzzle, "四項物件解謎", .24f);

            Interact(interact, "Door → Outdoors",
                new Vector2(50, 17), null,
                WorldAction2D.ToOutdoors, "返回聚落", .25f);
        }
        else
        {
            Sprite npc =
                index == 3 ? Sprite("Characters/Grandma") :
                index == 4 ? Sprite("Characters/Brewer") :
                Sprite("Characters/RiceMaster");

            WorldAction2D action =
                index == 3 ? WorldAction2D.BananaQuest :
                index == 4 ? WorldAction2D.WineQuest :
                WorldAction2D.RiceQuest;

            Interact(interact,
                index == 3 ? "Grandma [E]" :
                index == 4 ? "Brewer [E]" : "RiceCake Master [E]",
                index == 3 ? new Vector2(75, 44) :
                index == 4 ? new Vector2(51, 43) :
                new Vector2(64, 43),
                npc, action, "與委託 NPC 對話", .19f);

            Interact(interact, "Path / bridge → Outdoors",
                new Vector2(50, 21), null,
                WorldAction2D.ToOutdoors, "返回戶外", .25f);
        }

        SceneWorld2D controller =
            Obj("SceneWorld2D [Controller]").AddComponent<SceneWorld2D>();

        controller.sceneIndex = index;
        controller.Player = player.transform;
        controller.defaultSpawn = spawn;

        BuildCanvas(controller, index);

        EditorSceneManager.SaveScene(
            scene, "Assets/Scenes/" + SceneNames[index] + ".unity");
    }

    private static RectTransform Rect(
        string name, Transform parent, Vector2 pos,
        Vector2 size, Vector2? anchor = null)
    {
        var g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);

        var r = g.GetComponent<RectTransform>();
        var a = anchor ?? new Vector2(.5f, .5f);

        r.anchorMin = a;
        r.anchorMax = a;
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private static Image UIImage(
        string name, Transform parent,
        Vector2 pos, Vector2 size,
        Sprite sprite = null, Color? color = null,
        bool raycast = false)
    {
        var r = Rect(name, parent, pos, size);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite ?? White();
        img.color = color ?? Color.white;
        img.raycastTarget = raycast;
        img.preserveAspect = false;
        return img;
    }

    private static Text UIText(
        string name, Transform parent,
        Vector2 pos, Vector2 size, string value,
        int fontSize = 24, Color? color = null,
        TextAnchor align = TextAnchor.MiddleCenter)
    {
        var r = Rect(name, parent, pos, size);
        var t = r.gameObject.AddComponent<Text>();

        t.font = font;
        t.text = value;
        t.fontSize = fontSize;
        t.color = color ?? Dark;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        return t;
    }

    private static Button UIButton(
        string name, Transform parent,
        Vector2 pos, Vector2 size,
        string caption, Color? bg = null)
    {
        var img = UIImage(
            name, parent, pos, size,
            White(), bg ?? Accent, true);

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var color = btn.colors;
        color.highlightedColor = Hex("#D8E9B9");
        color.pressedColor = Hex("#92BA9C");
        btn.colors = color;

        UIText("Text", btn.transform,
            Vector2.zero, size - new Vector2(8, 8),
            caption, 20,
            bg.HasValue && (bg.Value.r + bg.Value.g + bg.Value.b > 1.6f)
                ? Dark : Color.white);

        return btn;
    }

    private static GameObject Panel(
        string name, Transform parent, Vector2 pos,
        Vector2 size, Color? bg = null)
    {
        return UIImage(
            name, parent, pos, size,
            White(), bg ?? Cream, false).gameObject;
    }

    private static void BuildCanvas(SceneWorld2D world, int sceneIndex)
    {
        var cg = Obj("Canvas [editable UI objects]");
        var canvas = cg.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        cg.AddComponent<GraphicRaycaster>();

        var scale = cg.AddComponent<CanvasScaler>();
        scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scale.referenceResolution = new Vector2(1600, 900);
        scale.matchWidthOrHeight = .5f;

        Obj("EventSystem")
            .AddComponent<EventSystem>()
            .gameObject.AddComponent<StandaloneInputModule>();

        var hud = cg.AddComponent<GameHUD2D>();
        world.hud = hud;

        UIImage("HUD bar", cg.transform,
            new Vector2(0, 425), new Vector2(1570, 60),
            null, new Color(.09f, .17f, .15f, .85f));

        hud.statusText = UIText("HUD status", cg.transform,
            new Vector2(0, 426), new Vector2(1530, 55),
            "歲末拾遺", 26, Color.white);

        hud.tipText = UIText("HUD tips", cg.transform,
            new Vector2(0, -415), new Vector2(1530, 55),
            "WASD 移動 ｜ E 互動", 20, Color.white);

        hud.interactionText = UIText("Interact hint", cg.transform,
            new Vector2(0, -357), new Vector2(1000, 54),
            "", 25, Hex("#F2C985"));

        hud.startPanel = Panel("Start panel", cg.transform,
            Vector2.zero, new Vector2(1200, 640));

        UIText("Title", hud.startPanel.transform,
            new Vector2(0, 165), new Vector2(980, 90),
            "歲末拾遺", 58);

        UIText("Story", hud.startPanel.transform,
            new Vector2(0, 24), new Vector2(980, 200),
            "研究學者在旅館醒來，取得小木板後探索聚落。\n三處工坊各有委託。WASD 移動｜E 互動｜Tab 筆記｜Esc 暫停。",
            25);

        hud.startButton = UIButton("Start", hud.startPanel.transform,
            new Vector2(0, -195), new Vector2(360, 70), "開始尋訪");

        hud.pausePanel = Panel("Pause panel", cg.transform,
            Vector2.zero, new Vector2(900, 580));

        UIText("Title", hud.pausePanel.transform,
            new Vector2(0, 110), new Vector2(500, 80),
            "暫停", 50);

        hud.resumeButton = UIButton("Resume", hud.pausePanel.transform,
            new Vector2(0, -35), new Vector2(300, 65), "繼續");

        hud.restartButton = UIButton("Restart", hud.pausePanel.transform,
            new Vector2(0, -130), new Vector2(300, 65), "重新開始");

        hud.endPanel = Panel("Results panel", cg.transform,
            Vector2.zero, new Vector2(950, 610));

        hud.resultText = UIText("Results", hud.endPanel.transform,
            new Vector2(0, 70), new Vector2(780, 320), "結算", 38);

        hud.endRestartButton = UIButton("Restart",
            hud.endPanel.transform,
            new Vector2(0, -184), new Vector2(300, 70), "再玩一次");

        hud.journalPanel = Panel("Journal panel",
            cg.transform, Vector2.zero, new Vector2(1150, 750));

        hud.journalText = UIText("Journal", hud.journalPanel.transform,
            new Vector2(0, 55), new Vector2(1050, 570),
            "研究筆記", 22);

        hud.journalCloseButton = UIButton("Close",
            hud.journalPanel.transform,
            new Vector2(0, -308), new Vector2(330, 60), "關閉筆記");

        hud.startPanel.SetActive(false);
        hud.pausePanel.SetActive(false);
        hud.endPanel.SetActive(false);
        hud.journalPanel.SetActive(false);

        if (sceneIndex >= 2)
            BuildMiniUI(cg.transform, world, sceneIndex);
    }

    private static Image ArtImage(
        string name, Transform parent,
        Vector2 pos, Vector2 size, Sprite art)
    {
        var img = UIImage(
            name, parent, pos, size,
            art, Color.white, false);

        img.preserveAspect = true;
        return img;
    }

    private static MinigameSwipeTarget Drag(
        string name, Transform parent,
        Vector2 pos, Vector2 size,
        int index, MiniGameUI2D game)
    {
        var img = UIImage(
            name, parent, pos, size, White(),
            new Color(.89f, .77f, .48f, .08f), true);

        var drag = img.gameObject.AddComponent<MinigameSwipeTarget>();
        drag.owner = game;
        drag.index = index;
        drag.image = img;
        return drag;
    }

    private static void Hide(params GameObject[] objects)
    {
        foreach (var g in objects)
            if (g != null) g.SetActive(false);
    }

    private static void BuildMiniUI(
        Transform parent, SceneWorld2D world, int index)
    {
        var root = Panel(
            "Minigame Canvas Overlay [all child UI editable]",
            parent, Vector2.zero, new Vector2(1600, 900),
            Hex("#F0ECD9"));

        var game = parent.gameObject.AddComponent<MiniGameUI2D>();
        game.root = root;
        world.minigames = game;

        var card = root.transform;

        game.heading = UIText("Game title", card,
            new Vector2(0, 372), new Vector2(1250, 58),
            "小遊戲", 36);

        game.instruction = UIText("Guide - NPC explanation", card,
            new Vector2(0, 307), new Vector2(1220, 75),
            "依照 NPC 的指示操作", 23);

        game.feedback = UIText("Feedback", card,
            new Vector2(0, -383), new Vector2(1250, 46),
            "", 23, Hex("#984C32"));

        game.closeButton = UIButton("Close or ESC", card,
            new Vector2(657, 381), new Vector2(185, 52),
            "離開 / Esc");

        if (index == 3) BuildBanana(card, game);
        else if (index == 4) BuildWine(card, game);
        else if (index == 5) BuildRice(card, game);
        else if (index == 2) BuildFinal(card, game);

        root.SetActive(false);
    }

    private static void GrandmaHelp(Transform panel, string message)
    {
        var back = UIImage(
            "Grandma explanation box [PDF left column]",
            panel, new Vector2(-495, 0),
            new Vector2(260, 475), White(), Hex("#C9D5DC"));

        UIText("Grandma directions", back.transform,
            Vector2.zero, new Vector2(225, 445),
            message, 20, Dark);
    }

    private static void BuildBanana(Transform card, MiniGameUI2D game)
    {
        var plant = Panel(
            "Banana 01 - Choose five plants [PDF p2]",
            card, new Vector2(0, -12), new Vector2(1400, 555));
        game.bananaPlant = plant;

        UIImage("Fence behind plants", plant.transform,
            new Vector2(0, -65), new Vector2(1300, 360),
            null, Hex("#745E47"));

        game.plantButtons = new Button[5];
        game.plantImages = new Image[5];

        Sprite[] plants =
        {
            Banana("PlantYoung"), Banana("PlantReady"),
            Banana("PlantOld"), Banana("PlantYoung"),
            Banana("PlantOld")
        };

        string[] names =
        {
            "植株 A", "植株 B", "植株 C", "植株 D", "植株 E"
        };

        for (int i = 0; i < 5; i++)
        {
            float x = -500 + i * 250;

            var btn = UIButton(
                "Plant_" + i + " [click to select]",
                plant.transform, new Vector2(x, -10),
                new Vector2(215, 390), "",
                new Color(.76f, .80f, .68f, .3f));

            game.plantButtons[i] = btn;
            game.plantImages[i] = ArtImage(
                "Independent plant sprite", btn.transform,
                new Vector2(0, 28), new Vector2(200, 300),
                plants[i]);

            UIText("Plant status", btn.transform,
                new Vector2(0, -160), new Vector2(190, 42),
                names[i], 19);
        }

        var cut = Panel(
            "Banana 02 - Cut twice [PDF p3]",
            card, new Vector2(0, -18), new Vector2(1300, 560));
        game.bananaCut = cut;

        GrandmaHelp(cut.transform,
            "阿嬤：選對香蕉植株後，請點擊兩處砍取位置，各取得一段假莖。後續材料處理只做一組。");

        game.cutImage = ArtImage(
            "Selected pseudostem - changes after each cut",
            cut.transform, new Vector2(90, 5),
            new Vector2(415, 500), Banana("StemFull"));

        game.cutOriginalSprite = Banana("StemFull");
        game.cutOneSprite = Banana("Cut_1");
        game.cutTwoSprite = Banana("Cut_2");

        game.cutButton = UIButton(
            "First pseudostem cutting point",
            cut.transform, new Vector2(98, -85),
            new Vector2(165, 64), "砍取①",
            new Color(.25f, .58f, .39f, .72f));

        game.cutSecondButton = UIButton(
            "Second pseudostem cutting point",
            cut.transform, new Vector2(146, 62),
            new Vector2(165, 64), "砍取②",
            new Color(.25f, .58f, .39f, .72f));

        game.bananaStatus = UIText(
            "Banana progress", card,
            new Vector2(0, -296), new Vector2(1200, 54),
            "", 24);

        var peel = Panel(
            "Banana 03 - Peel three layers [PDF p4]",
            card, new Vector2(0, -18), new Vector2(1300, 555));
        game.bananaPeel = peel;

        GrandmaHelp(peel.transform,
            "阿嬤：按住葉鞘，逐層剝開假莖。每剝掉一層，才會露出下面的纖維。請由外至內處理。");

        game.peelCore = ArtImage(
            "Exposed inner sheath core", peel.transform,
            new Vector2(115, -10), new Vector2(415, 510),
            Banana("SheathCore"));

        game.peelLayers = new Image[3];
        game.peelSwipe = new MinigameSwipeTarget[3];

        Sprite[] sheaths =
        {
            Banana("SheathOuter"),
            Banana("SheathMiddle"),
            Banana("SheathCore")
        };

        for (int i = 2; i >= 0; i--)
        {
            game.peelLayers[i] = ArtImage(
                "Layer_" + i + " [independent]",
                peel.transform, new Vector2(125, -10),
                new Vector2(415 - i * 25, 490 - i * 20),
                sheaths[i]);

            game.peelSwipe[i] = Drag(
                "PeelDrag_" + i + " [hold LMB and drag right]",
                peel.transform, new Vector2(125, -10),
                new Vector2(410, 460), i, game);
        }

        UIText("Peel hint", peel.transform,
            new Vector2(475, 20), new Vector2(210, 180),
            "沿右方的材料長條拖曳\n外層 → 中層 → 內層", 24);

        var scrape = Panel(
            "Banana 04 - Scrape black spots [PDF p5]",
            card, new Vector2(0, -18), new Vector2(1300, 555));
        game.bananaScrape = scrape;

        GrandmaHelp(scrape.transform,
            "阿嬤：沿纖維方向按住滑鼠刮除黑色雜質。請注意纖維完整度，不能重複刮太多次。");

        game.cleanFiber = ArtImage(
            "Clean fiber - persistent base", scrape.transform,
            new Vector2(125, -5), new Vector2(390, 490),
            Banana("FiberClean"));

        game.stains = new Image[6];
        game.scrapeSwipe = new MinigameSwipeTarget[6];

        for (int i = 0; i < 6; i++)
        {
            float y = 180 - i * 72;

            game.stains[i] = ArtImage(
                "Stain_" + (i + 1) + " [hide independently]",
                scrape.transform, new Vector2(125, -5),
                new Vector2(390, 490),
                Banana("Stain_" + (i + 1)));

            game.scrapeSwipe[i] = Drag(
                "Scrape drag area " + (i + 1),
                scrape.transform, new Vector2(125, y),
                new Vector2(380, 70), i, game);
        }

        UIText("Scrape hint", scrape.transform,
            new Vector2(485, 30), new Vector2(195, 200),
            "按住滑鼠向右拖曳\n逐個清除黑色雜質", 24);

        var sun = Panel(
            "Banana 05 - Sun-dry and collect [PDF p6]",
            card, new Vector2(0, -18), new Vector2(1300, 555));
        game.bananaSun = sun;

        GrandmaHelp(sun.transform,
            "阿嬤：將纖維放在日光下晾曬，接近適當程度就按 E 收取。太早仍濕，太晚會影響纖維品質。");

        ArtImage("Independent drying rack",
            sun.transform, new Vector2(120, 72),
            new Vector2(570, 340), Banana("DryingRack"));

        game.rackFiber = ArtImage(
            "Independent banana fibers",
            sun.transform, new Vector2(120, 73),
            new Vector2(340, 250), Banana("DryingFiber"));

        UIImage("Dry meter background",
            sun.transform, new Vector2(130, -186),
            new Vector2(590, 36),
            null, Hex("#B2C2BB"));

        UIImage("Successful pickup band 70-80 percent",
            sun.transform, new Vector2(130 + 590f * .25f, -186),
            new Vector2(590 * .10f, 36),
            null, Hex("#E3B668"));

        game.dryingFill = UIImage(
            "Dry meter FILL [0..1]",
            sun.transform, new Vector2(130, -186),
            new Vector2(590, 36),
            White(), Hex("#71B58B"));

        game.dryingFill.transform.SetAsLastSibling();
        game.dryingFill.type = Image.Type.Filled;
        game.dryingFill.fillMethod = Image.FillMethod.Horizontal;
        game.dryingFill.rectTransform.pivot = new Vector2(.5f, .5f);
        game.dryingFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        game.dryingFill.fillAmount = 0;

        UIText("Target 70%-80%", sun.transform,
            new Vector2(130, -248), new Vector2(600, 50),
            "70～80% 按 E 收取", 25);

        game.retryDryButton = UIButton(
            "Retry drying", sun.transform,
            new Vector2(470, -214),
            new Vector2(230, 58), "重新日曬");

        var done = Panel(
            "Banana 06 - Return to Grandma [PDF p7]",
            card, new Vector2(0, -18),
            new Vector2(1300, 555));
        game.bananaDone = done;

        ArtImage("Banana leaf quest reward preview",
            done.transform, new Vector2(-200, 15),
            new Vector2(320, 360),
            Sprite("Collectibles/BananaLeaf"));

        game.finishBananaButton = UIButton(
            "Back to Grandma", done.transform,
            new Vector2(260, -78),
            new Vector2(300, 75), "返回香蕉園");

        Hide(plant, cut, peel, scrape, sun, done);
    }

    private static void BuildWine(Transform card, MiniGameUI2D game)
    {
        var panel = Panel(
            "Winery | PDF p9 | left jar / right 5x2 materials",
            card, new Vector2(0, -18),
            new Vector2(1460, 570));

        game.winePanel = panel;

        UIImage("Recipe card background",
            panel.transform, new Vector2(0, 213),
            new Vector2(1360, 64),
            null, Hex("#E0D4AE"));

        game.recipeText = UIText(
            "Recipe | disappears for jars 4 & 5",
            panel.transform, new Vector2(0, 213),
            new Vector2(1260, 60), "", 24);

        game.wineStatus = UIText(
            "Jar 1-5 progress", panel.transform,
            new Vector2(-480, 137),
            new Vector2(415, 50), "", 23);

        game.jarImage = ArtImage(
            "Independent empty pottery jar",
            panel.transform, new Vector2(-430, -24),
            new Vector2(320, 330), Wine("EmptyWineJar"));

        game.jarContents = new Image[3];

        string[] content =
        {
            "JarContents_Rice",
            "JarContents_Starter",
            "JarContents_Water"
        };

        for (int i = 0; i < 3; i++)
        {
            game.jarContents[i] = ArtImage(
                "Visible jar material layer - " + content[i],
                panel.transform, new Vector2(-435, 27),
                new Vector2(125, 86), Wine(content[i]));

            game.jarContents[i].gameObject.SetActive(false);
        }

        game.jarUndoButton = UIButton(
            "Tap jar - remove most recently added ingredient",
            panel.transform, new Vector2(-430, -26),
            new Vector2(315, 315), "",
            new Color(.4f, .3f, .2f, .01f));

        game.jarText = UIText(
            "Current materials and undo prompt",
            panel.transform, new Vector2(-435, -234),
            new Vector2(450, 76), "", 20);

        game.ingredientButtons = new Button[10];
        game.ingredientIcons = new Image[10];
        game.ingredientLabels = new Text[10];

        game.ingredientSprites = new Sprite[]
        {
            Wine("RiceIngredient"),
            Wine("StarterIngredient"),
            Wine("Herbs")
        };

        for (int i = 0; i < 10; i++)
        {
            int row = i / 5;
            int col = i % 5;
            float x = 55 + col * 135f;
            float y = 59 - row * 144f;

            var btn = UIButton(
                "Ingredient slot " + (i + 1),
                panel.transform,
                new Vector2(x, y),
                new Vector2(119, 124), "",
                Hex("#D6C7A6"));

            game.ingredientButtons[i] = btn;

            int type =
                (i == 0 || i == 3 || i == 6) ? 0 :
                (i == 1 || i == 4 || i == 7) ? 1 : 2;

            game.ingredientIcons[i] = ArtImage(
                "Ingredient icon", btn.transform,
                new Vector2(0, 13),
                new Vector2(91, 80),
                game.ingredientSprites[type]);

            game.ingredientLabels[i] = UIText(
                "Ingredient name", btn.transform,
                new Vector2(0, -48),
                new Vector2(106, 30),
                type == 0 ? "糯米" :
                type == 1 ? "酒麴" : "其他",
                17, Dark);
        }

        game.waterButton = UIButton(
            "Click water trough to add water",
            panel.transform, new Vector2(350, -227),
            new Vector2(445, 84), "",
            Hex("#ADC5C7"));

        ArtImage("Water trough pixel art",
            game.waterButton.transform,
            new Vector2(-120, 0),
            new Vector2(114, 75),
            Wine("WaterBasin"));

        UIText("Water control label",
            game.waterButton.transform,
            new Vector2(68, 0),
            new Vector2(260, 60),
            "點擊水槽加水", 23, Dark);

        game.recipeAgainButton = UIButton(
            "Review memory recipe 5 seconds",
            panel.transform, new Vector2(568, 161),
            new Vector2(210, 52),
            "重看配方（5 秒）");

        UIText("Confirm recipe with SPACE",
            panel.transform, new Vector2(342, -277),
            new Vector2(620, 44),
            "SPACE：確認目前這罈的材料", 21);

        Hide(panel);
    }

    private static void BuildRice(Transform card, MiniGameUI2D game)
    {
        var panel = Panel(
            "RiceCake - mortar, mallet, rhythm [PDF p12]",
            card, new Vector2(0, -12),
            new Vector2(1400, 575));

        game.ricePanel = panel;

        game.mortarImage = ArtImage(
            "Separate stone mortar", panel.transform,
            new Vector2(-280, -8),
            new Vector2(530, 440),
            Rice("EmptyStoneMortar"));

        game.doughImage = ArtImage(
            "Independent dough image [changes on hit]",
            panel.transform, new Vector2(-280, 43),
            new Vector2(280, 200),
            Rice("Dough_01_Initial"));

        game.malletImage = ArtImage(
            "Independent mallet - movable on SPACE",
            panel.transform, new Vector2(235, 125),
            new Vector2(330, 330),
            Rice("WoodenMallet"));

        game.malletRect = game.malletImage.rectTransform;

        game.doughStates = new[]
        {
            Rice("Dough_01_Initial"),
            Rice("Dough_02_Pounded"),
            Rice("Dough_03_Pounded"),
            Rice("Dough_04_Flipped"),
            Rice("Dough_05_Side"),
            Rice("Dough_06_Finished")
        };

        game.riceStatus = UIText(
            "Flip prompts (hidden progress)",
            panel.transform, new Vector2(352, -95),
            new Vector2(480, 105),
            "請敲擊", 24);

        game.riceJudgement = UIText(
            "Perfect / Good / Miss",
            panel.transform, new Vector2(353, 8),
            new Vector2(450, 80), "", 27);

        game.rhythmTrack = Panel(
            "Visible rhythm timing track",
            panel.transform, new Vector2(0, -237),
            new Vector2(750, 40),
            Hex("#B9C3C9"));

        UIImage("GOOD orange timing zone",
            game.rhythmTrack.transform,
            new Vector2(222, 0),
            new Vector2(78, 40),
            null, Hex("#F0A344"));

        UIImage("PERFECT red timing zone",
            game.rhythmTrack.transform,
            new Vector2(240, 0),
            new Vector2(32, 40),
            null, Hex("#D45145"));

        game.rhythmMarker = UIImage(
            "Moving beat indicator",
            game.rhythmTrack.transform,
            new Vector2(-248, 0),
            new Vector2(11, 62),
            null, Hex("#345A7A")).rectTransform;

        UIText("Controls", panel.transform,
            new Vector2(0, -299),
            new Vector2(950, 48),
            "SPACE：敲擊　｜　看到翻動提示時按 E", 22);

        Hide(panel);
    }

    private static void BuildFinal(Transform card, MiniGameUI2D game)
    {
        var panel = Panel(
            "Final puzzle (original fictional placement)",
            card, new Vector2(0, -12),
            new Vector2(1400, 575));

        game.finalPanel = panel;

        game.finalStatus = UIText(
            "Four items", panel.transform,
            new Vector2(0, 192),
            new Vector2(1000, 65),
            "已放置 0/4", 24);

        game.finalButtons = new Button[4];

        string[] labels =
        {
            "小木板", "香蕉葉", "糯米糕", "酒與酒杯"
        };

        string[] icons =
        {
            "wooden_board", "banana_leaf",
            "rice_cake", "wine_set"
        };

        for (int i = 0; i < 4; i++)
        {
            var btn = UIButton(
                "Place " + labels[i],
                panel.transform,
                new Vector2(-480 + i * 320, -30),
                new Vector2(288, 265), "",
                Hex("#CFC8AA"));

            ArtImage("Relic", btn.transform,
                new Vector2(0, 30),
                new Vector2(200, 190),
                Sprite(icons[i]));

            UIText("Name", btn.transform,
                new Vector2(0, -106),
                new Vector2(230, 45),
                labels[i], 23);

            game.finalButtons[i] = btn;
        }

        Hide(panel);
    }
}
#endif
