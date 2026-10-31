using UnityEditor;
using UnityEngine;

public static class SaveDataMenu
{
    private const string MenuPath = "Tools/存档/清除本地存档";

    [MenuItem(MenuPath, false, 200)]
    private static void ClearSave()
    {
        if (!EditorUtility.DisplayDialog("清除本地存档",
                $"将删除 PlayerPrefs 键：{SaveManager.SaveKey}\n此操作不可撤销，确定继续？", "清除", "取消"))
            return;

        bool existed = PlayerPrefs.HasKey(SaveManager.SaveKey);
        PlayerPrefs.DeleteKey(SaveManager.SaveKey);
        PlayerPrefs.Save();

        string result = existed ? "存档已清除。" : "本地原本没有存档，无需清除。";
        if (Application.isPlaying)
            result += "\n当前处于 Play 模式，运行中的存档数据不会重置，重进 Play 后生效。";

        Debug.Log($"[存档] {result.Replace("\n", " ")}");
        EditorUtility.DisplayDialog("清除本地存档", result, "确定");
    }
}
