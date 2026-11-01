using System.Collections.Generic;
using UnityEngine;

// 人机模拟技能栏：持有表按品质入栏，只有栏内技能与空手三连推进冷却
public class AiPlayerSkillBar
{
    private static readonly int[] UnarmedSkills = { Config.unarmed_punch_left, Config.unarmed_punch_right, Config.unarmed_attack_smash };

    private readonly PlayerEntityData owner;
    private readonly List<int> slots = new();
    private readonly Dictionary<int, float> remaining = new(); // 技能 id → 剩余冷却（秒）

    public AiPlayerSkillBar(PlayerEntityData owner)
    {
        this.owner = owner;
        for (int i = 0; i < UnarmedSkills.Length; i++) remaining[UnarmedSkills[i]] = 0f;
    }

    public int Capacity => owner.baseAttribute.weaponSlotCount;

    public IReadOnlyList<int> Slots => slots;

    // 持有表变化（珊瑚掉落 / GM / 复活继承）后重排：品质降序，同品质 id 降序
    public void Rebuild()
    {
        var held = owner.skillController.GetSkillIds();
        held.Sort(ComparePriority);

        slots.Clear();
        int capacity = Capacity;
        for (int i = 0; i < held.Count && slots.Count < capacity; i++) slots.Add(held[i]);

        Prune(held);
        for (int i = 0; i < held.Count; i++)
        {
            int id = held[i];
            if (remaining.ContainsKey(id)) continue;
            remaining[id] = ConfigManager.SkillConfig.Get(id).cd; // 新获得的技能从"未就绪"开始
        }
    }

    private static int ComparePriority(int a, int b)
    {
        var ca = ConfigManager.SkillConfig.Get(a);
        var cb = ConfigManager.SkillConfig.Get(b);
        if (ca.qualityRank != cb.qualityRank) return cb.qualityRank.CompareTo(ca.qualityRank);
        return b.CompareTo(a);
    }

    // 不再持有的技能删掉记录；空手三连常驻
    private void Prune(List<int> held)
    {
        var stale = new List<int>();
        foreach (var pair in remaining)
        {
            if (IsUnarmed(pair.Key)) continue;
            if (!held.Contains(pair.Key)) stale.Add(pair.Key);
        }
        for (int i = 0; i < stale.Count; i++) remaining.Remove(stale[i]);
    }

    private static bool IsUnarmed(int skillId)
    {
        for (int i = 0; i < UnarmedSkills.Length; i++) if (UnarmedSkills[i] == skillId) return true;
        return false;
    }

    // 栏外技能不推进 = 冷却冻结，剩余时间保留
    public void Tick(float deltaTime)
    {
        for (int i = 0; i < slots.Count; i++) Advance(slots[i], deltaTime);
        for (int i = 0; i < UnarmedSkills.Length; i++) Advance(UnarmedSkills[i], deltaTime);
    }

    private void Advance(int skillId, float deltaTime)
    {
        if (!remaining.TryGetValue(skillId, out var left)) return;
        if (left <= 0f) return;
        remaining[skillId] = Mathf.Max(0f, left - deltaTime);
    }

    // 栏内从高到低 → 空手三连；全不可用返回 false（调用方原地站立）
    // 空手与真人同规则：移动中出左右拳，静止出崩拳；首选冷却时才退到另一组
    public bool TryCastAny()
    {
        for (int i = 0; i < slots.Count; i++) if (TryCast(slots[i])) return true;

        int punch = Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right;
        return owner.IsMovingForward
            ? TryCast(punch) || TryCast(Config.unarmed_attack_smash)
            : TryCast(Config.unarmed_attack_smash) || TryCast(punch);
    }

    private bool TryCast(int skillId)
    {
        if (skillId < 0) return false;
        if (remaining.TryGetValue(skillId, out var left) && left > 0f) return false;
        if (owner.skillController.TryUseSkill(skillId) != EntitySkillController.UseResult.Success) return false;
        remaining[skillId] = ConfigManager.SkillConfig.Get(skillId).cd;
        return true;
    }
}
