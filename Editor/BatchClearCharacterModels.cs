using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 清除 Assets/Files/Info/Entity 下所有预制体的子物体（只保留根物体及组件）；直接改资产、不可 Undo，先提交 Git
// 菜单位置：Tools/清除角色模型/，流程 = 预览勾选 → 二次确认 → 执行
public static class BatchClearCharacterModelsMenu
{
    public const string RootFolder = "Assets/Files/Info/Entity";
    private const string MenuPath = "Tools/清除角色模型/清除 Entity 预制体所有子物体...";

    [MenuItem(MenuPath, false, 0)]
    private static void OpenPreview()
    {
        BatchClearPreviewWindow.Open();
    }
}

public class BatchClearPreviewWindow : EditorWindow
{
    private class Item
    {
        public string path;
        public string name;
        public int directChildren;
        public int totalDescendants;
        public bool doClear = true;
    }

    private List<Item> items;
    private Vector2 scroll;

    public static void Open()
    {
        var window = GetWindow<BatchClearPreviewWindow>("清除角色模型 - 预览");
        window.minSize = new Vector2(560f, 420f);
        window.Scan();
    }

    private void Scan()
    {
        items = new List<Item>();

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BatchClearCharacterModelsMenu.RootFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int direct = prefab.transform.childCount;
            if (direct == 0) continue; // 没有子物体的预制体无需处理

            int total = 0;
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                if (t != prefab.transform) total++;

            items.Add(new Item { path = path, name = prefab.name, directChildren = direct, totalDescendants = total });
        }

        items.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
    }

    private void OnGUI()
    {
        if (items == null) Scan();

        int selectedCount = 0, willDeleteDirect = 0, willDeleteTotal = 0;
        foreach (var it in items)
        {
            if (!it.doClear) continue;
            selectedCount++;
            willDeleteDirect += it.directChildren;
            willDeleteTotal += it.totalDescendants;
        }

        EditorGUILayout.HelpBox(
            $"扫描 {BatchClearCharacterModelsMenu.RootFolder}：共 {items.Count} 个带子物体的预制体。\n" +
            $"已勾选 {selectedCount} 个，将删除 {willDeleteDirect} 个直接子物体（含后代共 {willDeleteTotal} 个节点），每个预制体只保留根物体。\n" +
            "此操作直接修改资产文件且不可撤销！", MessageType.Warning);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(60f)))
            items.ForEach(i => i.doClear = true);
        if (GUILayout.Button("全不选", GUILayout.Width(60f)))
            items.ForEach(i => i.doClear = false);
        if (GUILayout.Button("重新扫描", GUILayout.Width(80f)))
            Scan();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var it in items)
        {
            EditorGUILayout.BeginHorizontal();

            var oldColor = GUI.color;
            GUI.color = it.doClear ? oldColor : new Color(1f, 1f, 1f, 0.45f);

            it.doClear = EditorGUILayout.Toggle(it.doClear, GUILayout.Width(18f));
            EditorGUILayout.LabelField(it.name, EditorStyles.boldLabel, GUILayout.Width(150f));
            EditorGUILayout.LabelField($"{it.directChildren}/{it.totalDescendants}", GUILayout.Width(58f));
            EditorGUILayout.LabelField(it.path, EditorStyles.miniLabel);

            GUI.color = oldColor;

            if (GUILayout.Button("定位", GUILayout.Width(44f)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(it.path));

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);

        EditorGUI.BeginDisabledGroup(selectedCount == 0);
        if (GUILayout.Button($"执行清除（{selectedCount} 个预制体）", GUILayout.Height(32f)))
            ExecuteClear(selectedCount);
        EditorGUI.EndDisabledGroup();
    }

    private void ExecuteClear(int selectedCount)
    {
        if (!EditorUtility.DisplayDialog(
                "确认清除？",
                $"将直接修改 {selectedCount} 个预制体资产文件，删除其全部子物体（只保留根物体）。\n\n" +
                "此操作不可撤销（Undo 无效），建议先确认 Git 已提交当前状态。\n\n确定继续吗？",
                "确定清除", "取消"))
            return;

        int done = 0, deletedNodes = 0;
        var log = new StringBuilder();

        try
        {
            foreach (var it in items)
            {
                if (!it.doClear) continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "清除角色模型", $"{it.name} ({done + 1}/{selectedCount})",
                        (float)done / selectedCount))
                {
                    log.AppendLine($"== 用户取消，已完成 {done}/{selectedCount} ==");
                    break;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(it.path);
                try
                {
                    int before = root.GetComponentsInChildren<Transform>(true).Length - 1;
                    foreach (Transform child in root.transform)
                        Object.DestroyImmediate(child.gameObject);

                    PrefabUtility.SaveAsPrefabAsset(root, it.path);
                    deletedNodes += before;
                    log.AppendLine($"[OK] {it.path}：删除 {before} 个节点");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                done++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Scan(); // 清除后重新扫描，剩余条目归零或显示未处理的
        }

        log.Insert(0, $"<b>清除角色模型</b> 完成 {done} 个预制体，共删除 {deletedNodes} 个节点\n");
        Debug.Log(log.ToString());
    }
}
