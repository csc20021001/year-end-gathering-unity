#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YearEnd;

public static class SafePlayerWalkSetupEditor
{
    private const string BasePath = "Assets/Art2D/PlayerWalkV2/";

    [MenuItem("歲末拾遺 2D/主角行走動畫/套用目前場景（不重建場景）")]
    public static void ApplyCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("請先停止遊戲", "請退出 Play 模式後再設定。", "確定");
            return;
        }

        Sprite[] idle = new Sprite[4];
        Sprite[][] walks = new Sprite[4][];
        string[] dirs = { "Down", "Up", "Left", "Right" };
        for (int d = 0; d < dirs.Length; d++)
        {
            idle[d] = ReadOne("Idle_" + dirs[d] + ".png");
            if (idle[d] == null) return;
            walks[d] = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                walks[d][i] = ReadOne("Walk_" + dirs[d] + "_" + i + ".png");
                if (walks[d][i] == null) return;
            }
        }

        Scene activeScene = SceneManager.GetActiveScene();
        int updated = 0;
        PlayerMovement2D[] players = Object.FindObjectsByType<PlayerMovement2D>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerMovement2D player in players)
        {
            if (player.gameObject.scene != activeScene) continue;
            Undo.RecordObject(player, "Setup player walking animation");
            player.idleDown = idle[0];
            player.idleUp = idle[1];
            player.idleLeft = idle[2];
            player.idleRight = idle[3];
            player.walkDown = walks[0];
            player.walkUp = walks[1];
            player.walkLeft = walks[2];
            player.walkRight = walks[3];
            player.walkFramesPerSecond = 7f;
            EditorUtility.SetDirty(player);
            updated++;
        }

        if (updated == 0)
        {
            EditorUtility.DisplayDialog("找不到玩家", "目前場景沒有 PlayerMovement2D。請先開啟 ResearchRoom 或 Outdoors 等正式遊戲場景。", "確定");
            return;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog("已設定動畫", "已將 4 張站立與 16 張行走影格指定到目前場景的 " + updated + " 位玩家。請按 Ctrl+S 儲存，再按 Play 測試。\n\n其他場景要另外套用一次。", "確定");
    }

    private static Sprite ReadOne(string filename)
    {
        string path = BasePath + filename;
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null)
        {
            EditorUtility.DisplayDialog("找不到 V2 影格", "找不到圖片：\n" + path,
                "確定");
            return null;
        }

        bool changed = imp.textureType != TextureImporterType.Sprite ||
            imp.spriteImportMode != SpriteImportMode.Single ||
            imp.filterMode != FilterMode.Point ||
            imp.mipmapEnabled ||
            imp.textureCompression != TextureImporterCompression.Uncompressed ||
            Mathf.Abs(imp.spritePixelsPerUnit - 100f) > 0.01f;
        if (changed)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        Sprite result = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (result == null)
            EditorUtility.DisplayDialog("無法載入 V2 影格", path, "確定");
        return result;
    }
}
#endif
