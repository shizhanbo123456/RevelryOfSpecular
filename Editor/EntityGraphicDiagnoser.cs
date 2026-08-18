using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 临时诊断工具：运行时（Play 模式）点击菜单，打印当前所有实体图形
/// 的"逻辑类型 vs 实际渲染 Mesh"，定位植物/矿石双向串换的具体实体。
/// 用法：Play 模式下 菜单 Tools → 诊断实体图形串换，看 Console 输出。
/// </summary>
public static class EntityGraphicDiagnoser
{
    [MenuItem("Tools/诊断实体图形串换")]
    public static void Diagnose()
    {
        var logic = Object.FindObjectOfType<ClientLogicManager>();
        if (logic == null)
        {
            Debug.LogError("[诊断] 找不到 ClientLogicManager（未进入世界？）");
            return;
        }

        var epmField = typeof(ClientLogicManager).GetField("entityPlayerManager", BindingFlags.Instance | BindingFlags.NonPublic);
        var epm = epmField?.GetValue(logic);
        if (epm == null)
        {
            Debug.LogError("[诊断] 找不到 entityPlayerManager");
            return;
        }

        var infoListField = epm.GetType().GetField("infoList", BindingFlags.Instance | BindingFlags.NonPublic);
        var graphicsField = epm.GetType().GetField("graphics", BindingFlags.Instance | BindingFlags.NonPublic);
        var infoList = infoListField?.GetValue(epm) as IDictionary;
        var graphics = graphicsField?.GetValue(epm) as IDictionary;
        if (infoList == null || graphics == null)
        {
            Debug.LogError("[诊断] 反射字段失败");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== 实体图形诊断 ===");
        int total = 0, mismatch = 0;

        foreach (DictionaryEntry e in infoList)
        {
            total++;
            int id = (int)e.Key;
            var info = e.Value;
            var typeField = info.GetType().GetField("type");
            var entityType = (EntityType)typeField.GetValue(info);
            var x = (float)info.GetType().GetField("x").GetValue(info);
            var y = (float)info.GetType().GetField("y").GetValue(info);
            var z = (float)info.GetType().GetField("z").GetValue(info);

            string meshName = "无graphic";
            string meshPath = "";
            bool hasGraphic = graphics.Contains(id);
            Vector3 actualPos = Vector3.zero;
            if (hasGraphic)
            {
                var t = (Transform)graphics[id];
                actualPos = t.position;
                // 递归收集所有子物体的 MeshFilter（模型可能挂在子物体上）
                var mfs = t.GetComponentsInChildren<MeshFilter>(true);
                if (mfs.Length > 0)
                {
                    var names = new System.Collections.Generic.List<string>();
                    foreach (var m in mfs)
                        if (m.sharedMesh != null) names.Add(m.sharedMesh.name);
                    meshName = names.Count > 0 ? string.Join("+", names) : "有MeshFilter但mesh为空";
                }
                var sr = t.GetComponentInChildren<Renderer>();
                if (sr != null && sr.sharedMaterial != null)
                    meshPath = sr.sharedMaterial.name;
            }

            bool expectPlant = entityType.IsPlant() || entityType.IsInfectedPlant();
            bool expectOre = entityType.IsOre() || entityType.IsInfectedOre();
            bool isCrystalMesh = meshName.Contains("Crystal") || meshName.Contains("Hex") || meshName.Contains("Cluster");
            bool isPlantMesh = meshName.Contains("Fantasy") || meshName.Contains("Plant") || meshName.Contains("Bush") || meshName.Contains("Tree");

            string verdict = "";
            if (expectPlant && isCrystalMesh) { verdict = "  <<< 串换! 植物实体却渲染水晶mesh"; mismatch++; }
            if (expectOre && isPlantMesh) { verdict = "  <<< 串换! 矿石实体却渲染植物mesh"; mismatch++; }

            sb.AppendLine(string.Format("id={0,-5} type={1,-14} mesh={2,-40} mat={3,-24} infoPos=({4:F1},{5:F1},{6:F1}) actPos=({7:F1},{8:F1},{9:F1}){10}",
                id, entityType, meshName, meshPath, x, y, z, actualPos.x, actualPos.y, actualPos.z, verdict));
        }

        sb.AppendLine(string.Format("共 {0} 个实体，疑似串换 {1} 个", total, mismatch));
        Debug.Log(sb.ToString());
    }
}
