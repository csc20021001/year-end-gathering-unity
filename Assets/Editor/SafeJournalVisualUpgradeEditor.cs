#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YearEnd;

public static class SafeJournalVisualUpgradeEditor
{
    const string ArtPath = "Assets/Art2D/JournalDecor/";
    const string ScenePath = "Assets/Scenes/";
    static readonly string[] Scenes = { "ResearchRoom", "Outdoors", "Exhibition", "BananaGarden", "Winery", "RiceCakeShop" };

    sealed class JournalArt
    {
        public Sprite overview, detail, unlocked, hover, locked, cornerLeft, cornerRight, bookmark;
    }

    [MenuItem("歲末拾遺 2D/探索筆記美術/美化目前場景（保留遊戲物件）")]
    public static void ApplyCurrent()
    {
        if (!CanRun()) return;
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path) || !File.Exists(scene.path))
        {
            EditorUtility.DisplayDialog("未儲存場景", "請先開啟並儲存既有場景。", "好"); return;
        }
        if (!HasJournal())
        {
            EditorUtility.DisplayDialog("缺少探索筆記", "請確認目前場景已安裝 ExplorationJournalUI2D。此工具不會重新產生場景或筆記功能。", "好"); return;
        }
        if (!EditorUtility.DisplayDialog("套用像素美術", "只修改本場景的探索筆記 UI，不會碰觸敵人、碰撞箱、傳送點。\n套用前將備份已儲存的 .unity 場景檔。", "備份並套用", "取消")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        JournalArt art = LoadArt(); if (art == null) return;
        try
        {
            string folder = BackupFolder();
            File.Copy(scene.path, Path.Combine(folder, Path.GetFileName(scene.path)));
            if (ApplyToOpenScene(art))
                EditorUtility.DisplayDialog("套用完成", "探索筆記已美化。請按 Ctrl+S 儲存。\n場景備份：\n" + folder, "好");
            else EditorUtility.DisplayDialog("未變更", "找不到完整的既有八格 UI；沒有修改場景。", "好");
        }
        catch (Exception e) { Debug.LogException(e); EditorUtility.DisplayDialog("操作失敗", e.Message + "\n請查看 Console。", "好"); }
    }

    [MenuItem("歲末拾遺 2D/探索筆記美術/一鍵美化六個場景（自動備份原場景）")]
    public static void ApplyAll()
    {
        if (!CanRun()) return;
        if (SceneManager.sceneCount > 1)
        {
            EditorUtility.DisplayDialog("多場景模式", "請先關閉其他同時開啟的場景，只留下單一場景。", "好"); return;
        }
        foreach (string name in Scenes)
            if (!File.Exists(ScenePath + name + ".unity"))
            {
                EditorUtility.DisplayDialog("缺少場景", ScenePath + name + ".unity\n未進行修改。", "好"); return;
            }
        if (!EditorUtility.DisplayDialog("一鍵美化六場景", "會先確認儲存目前編輯，備份六張既有 .unity 場景，再逐張修改探索筆記 UI。\n不會重建場景或修改敵人／碰撞箱／傳送點。\n確定繼續？", "備份並套用", "取消")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        JournalArt art = LoadArt(); if (art == null) return;
        string previous = SceneManager.GetActiveScene().path;
        string folder = ""; int count = 0; string skipped = "";
        try
        {
            folder = BackupFolder();
            foreach (string name in Scenes)
                File.Copy(ScenePath + name + ".unity", Path.Combine(folder, name + ".unity"));
            foreach (string name in Scenes)
            {
                EditorSceneManager.OpenScene(ScenePath + name + ".unity", OpenSceneMode.Single);
                if (ApplyToOpenScene(art))
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); count++;
                }
                else skipped += "\n" + name;
            }
            EditorUtility.DisplayDialog("完成", "成功套用：" + count + "/6 張場景" +
                (skipped.Length > 0 ? "\n跳過（缺少新版八格 UI）：" + skipped : "") +
                "\n\n原始場景備份：\n" + folder, "好");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("中途停止", "已完成 " + count + " 張。\n原場景備份：" + folder + "\n" + e.Message, "好");
        }
        finally
        {
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }

    static bool CanRun()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;
        EditorUtility.DisplayDialog("不能在 Play 模式套用", "請先停止遊戲。", "好"); return false;
    }
    static string BackupFolder()
    {
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "_JournalVisualBackups",
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(folder);
        return folder;
    }
    static GameHUD2D Hud()
    {
        foreach (GameHUD2D hud in UnityEngine.Object.FindObjectsByType<GameHUD2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (hud != null && hud.gameObject.scene == SceneManager.GetActiveScene()) return hud;
        return null;
    }
    static bool HasJournal()
    {
        GameHUD2D hud = Hud();
        return hud != null && hud.journalPanel != null && hud.GetComponent<ExplorationJournalUI2D>() != null;
    }
    static Sprite SpriteAt(string file)
    {
        string path = ArtPath + file;
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) throw new FileNotFoundException("缺少 PNG 素材", path);
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single ||
            imp.filterMode != FilterMode.Point || imp.mipmapEnabled ||
            imp.textureCompression != TextureImporterCompression.Uncompressed)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) throw new Exception("Sprite 匯入失敗：" + path);
        return s;
    }
    static JournalArt LoadArt()
    {
        try
        {
            return new JournalArt
            {
                overview = SpriteAt("JournalOverviewBackground.png"),
                detail = SpriteAt("JournalDetailBackground.png"),
                unlocked = SpriteAt("SlotUnlocked.png"),
                hover = SpriteAt("SlotUnlockedHighlight.png"),
                locked = SpriteAt("SlotLocked.png"),
                cornerLeft = SpriteAt("CornerFlowersLeft.png"),
                cornerRight = SpriteAt("CornerBerriesRight.png"),
                bookmark = SpriteAt("BlueBookmark.png")
            };
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            EditorUtility.DisplayDialog("未找到素材", "請完整匯入 Assets/Art2D/JournalDecor/。\n" + e.Message, "好");
            return null;
        }
    }
    static void SetBackground(Image img, Sprite sprite)
    {
        Undo.RecordObject(img, "Journal pixel art background");
        img.sprite = sprite; img.color = Color.white; img.type = Image.Type.Simple;
        img.preserveAspect = false; img.raycastTarget = false;
        EditorUtility.SetDirty(img);
    }
    static void MakeTransparent(Image img)
    {
        if (img == null) return;
        Undo.RecordObject(img, "Show art beneath journal legacy image");
        img.color = new Color(1,1,1,0); img.raycastTarget = false;
    }
    static void DisableOldTitle(Transform root, string name)
    {
        Transform t = root.Find(name);
        if (t != null && t.gameObject.activeSelf)
        {
            Undo.RecordObject(t.gameObject, "Art background already contains journal title");
            t.gameObject.SetActive(false);  
        }
    }
    static void PositionIfDefault(RectTransform t, Vector2 posFrom, Vector2 sizeFrom, Vector2 posTo, Vector2 sizeTo)
    {
        if (t == null) return;
        if (Vector2.Distance(t.anchoredPosition, posFrom) > .75f || Vector2.Distance(t.sizeDelta, sizeFrom) > 1.5f) return;
        Undo.RecordObject(t, "Align default detail page to artwork");
        t.anchoredPosition = posTo; t.sizeDelta = sizeTo;
    }
    static Image ImageChild(Transform parent, string child)
    {
        Transform t = parent != null ? parent.Find(child) : null;
        return t != null ? t.GetComponent<Image>() : null;
    }
    static Image Ornament(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 dimensions)
    {
        Transform tr = parent.Find(name);
        Image img;
        if (tr == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Add journal decoration");
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f,.5f);
            rt.anchoredPosition = position; rt.sizeDelta = dimensions;
            img = go.GetComponent<Image>();
        }
        else img = tr.GetComponent<Image>();
        if (img == null) return null;
        Undo.RecordObject(img, "Update journal decoration");
        img.sprite = sprite; img.type = Image.Type.Simple;
        img.color = Color.white; img.preserveAspect = true; img.raycastTarget = false;
        return img;
    }
    static bool ApplyToOpenScene(JournalArt art)
    {
        GameHUD2D hud = Hud();
        if (hud == null || hud.journalPanel == null) return false;
        ExplorationJournalUI2D ui = hud.GetComponent<ExplorationJournalUI2D>();
        if (ui == null || ui.overviewPanel == null || ui.detailPanel == null ||
            ui.slotButtons == null || ui.slotButtons.Length < 8 ||
            ui.unknownMarks == null || ui.unknownMarks.Length < 8) return false;
        for (int i=0;i<8;i++) if (ui.slotButtons[i] == null || ui.unknownMarks[i] == null || ui.slotButtons[i].GetComponent<Image>() == null) return false;
        Image overview = ui.overviewPanel.GetComponent<Image>();
        Image detail = ui.detailPanel.GetComponent<Image>();
        if (overview == null || detail == null) return false;

        Undo.RegisterFullObjectHierarchyUndo(hud.journalPanel, "Previous journal before decorating");
        MakeTransparent(hud.journalPanel.GetComponent<Image>());
        Image dim = Ornament(hud.journalPanel.transform, "JournalVisual_DimBehindBook", null,
            Vector2.zero, new Vector2(1600, 900));
        if (dim != null)
        {
            Undo.RecordObject(dim, "Darken scene behind journal");
            dim.color = new Color(0,0,0,.60f); dim.preserveAspect = false;
            dim.transform.SetAsFirstSibling();
        }
        SetBackground(overview, art.overview);
        SetBackground(detail, art.detail);
        DisableOldTitle(overview.transform, "Title");
        DisableOldTitle(detail.transform, "Detail heading");

        for (int i=0;i<8;i++)
        {
            Button btn = ui.slotButtons[i]; Image frame = btn.GetComponent<Image>();
            Undo.RecordObject(frame, "Decorated journal item slot");
            frame.sprite = art.unlocked; frame.color = Color.white;
            frame.type = Image.Type.Simple; frame.preserveAspect = true; frame.raycastTarget = true;
            Undo.RecordObject(btn, "Auto switch unlocked/locked slot artwork");
            btn.targetGraphic = frame;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = art.hover, pressedSprite = art.hover,
                selectedSprite = art.hover, disabledSprite = art.locked
            };
            MakeTransparent(ImageChild(btn.transform, "Card background"));
            Undo.RecordObject(ui.unknownMarks[i], "Avoid duplicate question mark");
            ui.unknownMarks[i].text = "";
        }

        MakeTransparent(ImageChild(detail.transform, "Image frame"));
        MakeTransparent(ImageChild(detail.transform, "Long text backdrop"));
        PositionIfDefault(ui.detailIcon != null ? ui.detailIcon.rectTransform : null,
            new Vector2(-338,150), new Vector2(260,238), new Vector2(-318,70), new Vector2(302,265));
        PositionIfDefault(ui.detailTitle != null ? ui.detailTitle.rectTransform : null,
            new Vector2(162,246), new Vector2(615,55), new Vector2(205,150), new Vector2(485,64));
        PositionIfDefault(ui.detailShort != null ? ui.detailShort.rectTransform : null,
            new Vector2(162,136), new Vector2(605,160), new Vector2(209,-4), new Vector2(475,136));
        if (ui.detailShort != null)
        {
            Undo.RecordObject(ui.detailShort, "Detail intro contrast");
            ui.detailShort.alignment = TextAnchor.UpperLeft;
            ui.detailShort.color = new Color(.27f,.20f,.14f,1f);
        }
        if (ui.detailScroll != null && ui.detailScroll.viewport != null)
            PositionIfDefault(ui.detailScroll.viewport,
                new Vector2(0,-148), new Vector2(987,310), new Vector2(0,-189), new Vector2(939,150));
        if (ui.detailBody != null)
        {
            Undo.RecordObject(ui.detailBody, "Detail text contrast");
            ui.detailBody.color = new Color(.27f,.20f,.14f,1f);
        }

        Ornament(overview.transform, "JournalVisual_LeftFlowers", art.cornerLeft,
            new Vector2(-530,247), new Vector2(72,95));
        Ornament(overview.transform, "JournalVisual_RightBerries", art.cornerRight,
            new Vector2(531,245), new Vector2(74,95));
        Ornament(detail.transform, "JournalVisual_BlueBookmark", art.bookmark,
            new Vector2(533,201), new Vector2(39,100));

        EditorUtility.SetDirty(ui); EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return true;
    }
}
#endif
