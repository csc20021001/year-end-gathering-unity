#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YearEnd;

public static class SafeJournalSetupEditor
{
    private static Font font;
    private static readonly string[] IconPaths =
    {
        "FinePlankNormal.png", "BananaLeafNormal.png", "RiceCakeNormal.png", "WineNormal.png",
        "FinePlankHidden.png", "BananaLeafHidden.png", "RiceCakeHidden.png", "WineHidden.png"
    };
    private static readonly string[] Names =
    {
        "小木板", "香蕉葉", "年糕", "酒與酒杯",
        "精緻的小木板", "脈絡分明的香蕉葉", "Q彈的年糕", "月光的酒與酒杯"
    };
    private static readonly string[] Short =
    {
        "旅館中發現的小木板。", "從婆婆手中取得的香蕉葉。",
        "親手搗製的糯米食品。", "釀酒師交付的已發酵酒與酒杯。",
        "在旅館床尾發現、雕工細緻的小木板。", "葉脈清晰的特殊香蕉葉。",
        "完成全程完美敲擊後得到的年糕。", "在酒甕附近找到的特殊酒與酒杯。"
    };
    private static readonly string[] Body =
    {
        "遊戲中的小木板是原創收集物。可在此補充物件來源與劇情描述。",
        "香蕉葉與香蕉假莖葉鞘所取得的纖維不同。請在此填寫已查證的文化背景與遊戲設計差異。",
        "本作的敲擊玩法是遊戲化設計；糯米糕、麻糬及年糕的名稱和做法請依可靠來源核對。",
        "遊戲中的五罈材料數量是虛構配方。釀酒與祭儀相關背景須區分不同家族的實際做法。",
        "隱藏物品：旅館床尾發現。這個雕刻是遊戲虛構設計，不宣稱為真實噶瑪蘭文物。",
        "隱藏物品：取得香蕉葉後，再與婆婆對話五次。詳細故事待補充。",
        "隱藏物品：整場年糕小遊戲所有敲擊都達成 Perfect。詳細故事待補充。",
        "隱藏物品：領取普通酒後，在釀酒坊的酒甕旁按 E 取得。詳細故事待補充。"
    };
    private static Color ColorOf(string code)
    {
        ColorUtility.TryParseHtmlString(code, out Color c); return c;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create journal element");
        go.transform.SetParent(parent, false);
        var t = (RectTransform)go.transform;
        t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);
        t.anchoredPosition=center;
        t.sizeDelta=size;
        return t;
    }
    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size,
        Color color, Sprite sprite=null, bool preserve=false)
    {
        var rt = Rect(name,parent,position,size);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite=sprite;
        im.color=color;
        im.preserveAspect=preserve;
        im.raycastTarget=false;
        return im;
    }
    private static Text Text(string name, Transform parent, Vector2 position, Vector2 size, string value,
        int pt=26, TextAnchor align=TextAnchor.MiddleCenter, Color? color=null)
    {
        var rt=Rect(name,parent,position,size);
        var t=rt.gameObject.AddComponent<Text>();
        t.font=font; t.text=value; t.fontSize=pt;
        t.color=color??ColorOf("#342C26");
        t.alignment=align;
        t.horizontalOverflow=HorizontalWrapMode.Wrap;
        t.verticalOverflow=VerticalWrapMode.Truncate;
        t.raycastTarget=false;
        return t;
    }
    private static Sprite LoadIcon(int i)
    {
        string path="Assets/Art2D/Journal/"+IconPaths[i];
        var imp=AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.filterMode != FilterMode.Point))
        {
            imp.textureType=TextureImporterType.Sprite;
            imp.spriteImportMode=SpriteImportMode.Single;
            imp.alphaIsTransparency=true;
            imp.filterMode=FilterMode.Point;
            imp.mipmapEnabled=false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("歲末拾遺 2D/安全新增探索筆記UI（只修改目前開啟的場景）")]
    public static void CreateInCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("無法建立", "請先退出 Play 模式。", "好"); return;
        }
        var hud=Object.FindFirstObjectByType<GameHUD2D>(FindObjectsInactive.Include);
        if (hud == null || hud.journalPanel == null)
        {
            EditorUtility.DisplayDialog("找不到原筆記", "請先開啟既有六個場景之一，確認 Canvas 上有 GameHUD2D 和 Journal panel。", "好"); return;
        }
        if (hud.GetComponent<ExplorationJournalUI2D>() != null)
        {
            EditorUtility.DisplayDialog("已存在", "這個場景已加上新探索筆記，不會再重複建立，也不會覆寫你調整過的位置。", "好"); return;
        }
        if (!EditorUtility.DisplayDialog("安全新增探索筆記", "只會在目前場景原有 Journal panel 下新增八格總覽與詳細頁；保留玩家、NPC、碰撞箱、傳送點與其他所有物件。請完成後 Ctrl+S 儲存場景。", "新增", "取消")) return;

        Undo.RegisterFullObjectHierarchyUndo(hud.journalPanel, "Backup previous journal hierarchy");
        font = hud.journalText != null && hud.journalText.font != null
            ? hud.journalText.font : hud.statusText != null ? hud.statusText.font : null;
        if (font == null) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var comp=Undo.AddComponent<ExplorationJournalUI2D>(hud.gameObject);
        if (hud.journalText != null) hud.journalText.gameObject.SetActive(false);
        if (hud.journalCloseButton != null) hud.journalCloseButton.gameObject.SetActive(false);

        Transform parent=hud.journalPanel.transform;
        var overview=Image("Overview [4x2]",parent,Vector2.zero,new Vector2(1120,720),ColorOf("#F1E4C8"));
        var detail=Image("Detail [replaces overview]",parent,Vector2.zero,new Vector2(1120,720),ColorOf("#F1E4C8"));
        comp.overviewPanel=overview.gameObject; comp.detailPanel=detail.gameObject;
        Text("Title",overview.transform,new Vector2(0,304),new Vector2(760,65),"探索筆記",38);
        Text("Tip",overview.transform,new Vector2(0,-323),new Vector2(960,35),"點擊已取得物件可閱讀記錄 · ESC 返回遊戲",17);

        comp.items=new JournalItemInfo2D[8];
        for(int i=0;i<8;i++)
        {
            int r=i/4, c=i%4;
            float x=-375+c*250f, y=138-r*259f;
            var slot=Image("Slot "+(i+1).ToString("00")+" - "+Names[i],overview.transform,
                new Vector2(x,y),new Vector2(232,240),ColorOf("#AD8764"));
            var btn=slot.gameObject.AddComponent<Button>();
            btn.targetGraphic=slot;
            slot.raycastTarget=true;
            var cb=btn.colors;
            cb.highlightedColor=ColorOf("#D7B587");
            cb.pressedColor=ColorOf("#927253");
            cb.disabledColor=ColorOf("#96765C");
            btn.colors=cb;
            comp.slotButtons[i]=btn;
            var inner=Image("Card background",slot.transform,Vector2.zero,new Vector2(220,228),ColorOf("#F5EBD7"));
            var icon=Image("Item picture",inner.transform,new Vector2(0,31),new Vector2(178,157),Color.white,null,true);
            var label=Text("Small item name",inner.transform,new Vector2(0,-88),new Vector2(207,47),Names[i],19);
            var unknown=Text("Unknown (?)",inner.transform,Vector2.zero,new Vector2(120,145),"?",72);
            icon.gameObject.SetActive(false); label.gameObject.SetActive(false);
            comp.slotIcons[i]=icon; comp.slotNames[i]=label; comp.unknownMarks[i]=unknown;
            comp.items[i]=new JournalItemInfo2D { itemName=Names[i], icon=LoadIcon(i), shortDescription=Short[i], detailedDescription=Body[i] };
        }

        Text("Detail heading",detail.transform,new Vector2(0,313),new Vector2(840,55),"物品記錄",36);
        Image("Image frame",detail.transform,new Vector2(-338,150),new Vector2(286,260),ColorOf("#AD8764"));
        comp.detailIcon=Image("Large item picture",detail.transform,new Vector2(-338,150),new Vector2(260,238),Color.white,null,true);
        comp.detailTitle=Text("Item name",detail.transform,new Vector2(162,246),new Vector2(615,55),"",34);
        comp.detailShort=Text("Short introduction",detail.transform,new Vector2(162,136),new Vector2(605,160),"",24,TextAnchor.MiddleLeft);
        Image("Long text backdrop",detail.transform,new Vector2(0,-148),new Vector2(1020,342),ColorOf("#E0CFAC"));
        var viewport=Image("Description Viewport",detail.transform,new Vector2(0,-148),new Vector2(987,310),new Color(1,1,1,.004f));
        viewport.raycastTarget=true;
        var mask=viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic=false;
        var content=Text("Editable long description (text loaded from Inspector)",viewport.transform,
            Vector2.zero,new Vector2(930,300),"",23,TextAnchor.UpperLeft);
        var contentRt=content.rectTransform;
        contentRt.anchorMin=new Vector2(.5f,1f);contentRt.anchorMax=new Vector2(.5f,1f);
        contentRt.pivot=new Vector2(.5f,1f);contentRt.anchoredPosition=new Vector2(0,-8);
        content.verticalOverflow=VerticalWrapMode.Overflow;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=viewport.rectTransform; scroll.content=contentRt;
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        comp.detailBody=content;comp.detailScroll=scroll;
        Text("ESC reminder",detail.transform,new Vector2(0,-334),new Vector2(960,26),"ESC：返回探索筆記",18);
        detail.gameObject.SetActive(false);
        EditorUtility.SetDirty(hud); EditorUtility.SetDirty(comp);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Selection.activeGameObject=overview.gameObject;
        EditorUtility.DisplayDialog("新增完成", "已加入可編輯的八格總覽和詳細頁面。請按 Ctrl+S 儲存場景。其他五個場景也要各執行一次。", "好");
    }
}
#endif
