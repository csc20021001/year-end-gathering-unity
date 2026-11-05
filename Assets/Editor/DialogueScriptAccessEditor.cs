#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using YearEnd;

public static class DialogueScriptAccessEditor
{
    private const string ScriptPath = "Assets/Resources/DialogueScriptV1.asset";

    [MenuItem("歲末拾遺 2D/劇情對話/開啟可編輯劇本（Inspector）")]
    public static void OpenDialogueScript()
    {
        var asset = AssetDatabase.LoadAssetAtPath<DialogueScript2D>(ScriptPath);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("找不到對話劇本",
                "請確認已匯入 Assets/Resources/DialogueScriptV1.asset。", "確定");
            return;
        }
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log("對話台詞可在 Inspector → Conversations → Lines 修改；Id 請保持不變。");
    }
}
#endif
