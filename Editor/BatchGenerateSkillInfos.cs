using System.Collections.Generic;
using System.IO;
using System.Text;
using Ros.Skill;
using UnityEditor;
using UnityEngine;

// 按技能类型分组生成/清理 SkillInfo 资产：配置名 = 技能 id（skill_{id}.asset），可重复执行
// 菜单：Tools/配置/更新技能配置
public static class BatchGenerateSkillInfosMenu
{
    private const string SkillRoot = "Assets/Files/Info/Skill";
    private const string Prefix = "skill_";

    // 段划分与 SkillType 枚举顺序一致（段长不可调整，见《03-技能与Buff总表》）
    private static readonly (SkillType start, SkillType end, string folder)[] Segments =
    {
        (SkillType.SkillFanIceCone, SkillType.SkillSkyfallWeapon, "Sword"),
        (SkillType.SkillBoomerangWeapon, SkillType.SkillIceFanTiny, "Spear"),
        (SkillType.SkillBackVolley, SkillType.SkillShieldFour, "Gun"),
        (SkillType.SkillSplitShot, SkillType.SkillAllyShieldTwentyFive, "MagicOrb"),
        (SkillType.SkillRockShield, SkillType.SkillFogCast, "Defense"),
        (SkillType.SkillZombieClawR, SkillType.SkillNeutralStatueSpore, "Npc"),
        (SkillType.SkillPunchLeft, SkillType.SkillPunchSmash, "Unarmed"),
    };

    [MenuItem("Tools/配置/更新技能配置", false, 1)]
    private static void UpdateSkills()
    {
        EnsureRoot();
        foreach (var seg in Segments) EnsureFolder(seg.folder);

        var targets = new List<(int id, string folder)>(); // 期望的 id → 分组
        foreach (var seg in Segments)
            for (int id = (int)seg.start; id <= (int)seg.end; id++) targets.Add((id, seg.folder));

        // 现有资产归类：id 命中则待归位/复用，其余 SkillInfo 视为用不上
        var byId = new Dictionary<int, string>();
        var orphan = new List<string>();   // 用不上的技能配置（待删）
        var foreign = new List<string>();  // Skill 目录下的非技能配置（不动，仅提示）

        foreach (string guid in AssetDatabase.FindAssets("", new[] { SkillRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".asset")) continue;
            if (!(AssetDatabase.LoadMainAssetAtPath(path) is SkillInfo)) { foreign.Add(path); continue; }

            int id;
            if (!TryParseId(path, out id) || !targets.Exists(t => t.id == id)) { orphan.Add(path); continue; }
            if (byId.ContainsKey(id)) { orphan.Add(path); continue; } // 同 id 多份，只留一份
            byId[id] = path;
        }

        var toCreate = new List<(int id, string path)>();
        var toMove = new List<(string from, string to)>();
        var blocked = new List<string>();

        foreach (var target in targets)
        {
            string path = $"{SkillRoot}/{target.folder}/{Prefix}{target.id}.asset";
            if (byId.TryGetValue(target.id, out string current))
            {
                if (current == path) continue;
                // 目标已占用：占用者本身是待删资产时仍可归位（删除动作先执行）
                if (AssetDatabase.LoadMainAssetAtPath(path) != null && !orphan.Contains(path))
                {
                    blocked.Add($"{current} 无法归位到 {path}（目标已存在其它资产）");
                    continue;
                }
                toMove.Add((current, path));
            }
            else toCreate.Add((target.id, path));
        }

        var summary = new StringBuilder();
        summary.AppendLine($"技能 id 共 {targets.Count} 个：新建 {toCreate.Count} / 归位 {toMove.Count} / 删除 {orphan.Count} / 受阻 {blocked.Count} / 跳过非技能资产 {foreign.Count}");
        AppendBrief(summary, "删除", orphan);
        AppendBrief(summary, "受阻", blocked);
        AppendBrief(summary, "跳过", foreign);

        if (!EditorUtility.DisplayDialog("更新技能配置", summary.ToString(), "执行", "取消")) return;

        int deleted = 0;
        foreach (string path in orphan)
        {
            // 删除前逐个复核类型，避免误删同目录下的其它资产
            if (!(AssetDatabase.LoadMainAssetAtPath(path) is SkillInfo))
            {
                Debug.LogError($"[技能配置] {path} 不是 SkillInfo，已跳过删除");
                continue;
            }
            AssetDatabase.DeleteAsset(path);
            deleted++;
        }

        int moved = 0;
        foreach (var move in toMove)
        {
            if (AssetDatabase.LoadMainAssetAtPath(move.to) != null)
            {
                Debug.LogError($"[技能配置] 归位失败，目标已存在：{move.to}");
                continue;
            }
            string error = AssetDatabase.MoveAsset(move.from, move.to);
            if (string.IsNullOrEmpty(error)) { moved++; continue; }
            Debug.LogError($"[技能配置] 移动失败 {move.from} → {move.to}：{error}");
        }

        int created = 0;
        foreach (var item in toCreate)
        {
            var info = ScriptableObject.CreateInstance<SkillInfo>();
            info.id = item.id;
            AssetDatabase.CreateAsset(info, item.path);
            created++;
        }

        // 已存在的资产补写 id（文件名即 id，字段未填时按文件名修正）
        foreach (var target in targets)
        {
            var info = AssetDatabase.LoadAssetAtPath<SkillInfo>($"{SkillRoot}/{target.folder}/{Prefix}{target.id}.asset");
            if (info == null || info.id == target.id) continue;
            info.id = target.id;
            EditorUtility.SetDirty(info);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<b>更新技能配置</b>（{SkillRoot}）新建 {created}，归位 {moved}，删除 {deleted}，" +
                  $"跳过非技能资产 {foreign.Count}，受阻 {blocked.Count}。");
    }

    // 确认面板只列关键条目（删除/受阻/跳过），新建与归位数量见汇总行
    private static void AppendBrief(StringBuilder sb, string title, List<string> lines)
    {
        if (lines.Count == 0) return;
        sb.AppendLine();
        sb.AppendLine($"[{title}] {lines.Count} 条");
        int shown = Mathf.Min(lines.Count, 10);
        for (int i = 0; i < shown; i++) sb.AppendLine($"  {lines[i]}");
        if (lines.Count > shown) sb.AppendLine($"  …其余 {lines.Count - shown} 条未列出");
    }

    private static bool TryParseId(string path, out int id)
    {
        id = -1;
        string name = Path.GetFileNameWithoutExtension(path);
        if (!name.StartsWith(Prefix)) return false;
        return int.TryParse(name.Substring(Prefix.Length), out id);
    }

    private static void EnsureRoot()
    {
        if (AssetDatabase.IsValidFolder(SkillRoot)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Files")) AssetDatabase.CreateFolder("Assets", "Files");
        if (!AssetDatabase.IsValidFolder("Assets/Files/Info")) AssetDatabase.CreateFolder("Assets/Files", "Info");
        AssetDatabase.CreateFolder("Assets/Files/Info", "Skill");
    }

    private static void EnsureFolder(string folderName)
    {
        if (AssetDatabase.IsValidFolder($"{SkillRoot}/{folderName}")) return;
        AssetDatabase.CreateFolder(SkillRoot, folderName);
    }
}
