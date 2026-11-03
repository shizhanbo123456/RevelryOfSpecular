using System.Collections.Generic;
using UnityEngine;

// 人机技能栏：只做选栏与出招顺序；冷却与库存由实体的 EntitySkillController 记账
public class RobotSkillBar
{
    private readonly PlayerEntityData owner;
    private readonly DecisionMachine machine;
    private readonly List<int> slots = new();

    public RobotSkillBar(PlayerEntityData owner, DecisionMachine machine)
    {
        this.owner = owner;
        this.machine = machine;
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
    }

    private static int ComparePriority(int a, int b)
    {
        var ca = ConfigManager.SkillConfig.Get(a);
        var cb = ConfigManager.SkillConfig.Get(b);
        if (ca.qualityRank != cb.qualityRank) return cb.qualityRank.CompareTo(ca.qualityRank);
        return b.CompareTo(a);
    }

    // 栏内从高到低 → 空手三连；全不可用返回 false（调用方原地站立）
    // 空手按与目标的距离选：远了出直拳（够得到），贴身才出崩拳，避免崩拳够不着却原地空放
    public bool TryCastAny(EntityData target)
    {
        for (int i = 0; i < slots.Count; i++) if (TryCast(slots[i])) return true;

        bool close = target != null && FlatDistance(target) <= Config.unarmed_smash_range;
        int punch = Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right;
        int otherPunch = punch == Config.unarmed_punch_left ? Config.unarmed_punch_right : Config.unarmed_punch_left;

        // 远了只出直拳：崩拳够不到，宁可站着等冷却也不空放
        return close
            ? TryCast(Config.unarmed_attack_smash) || TryCast(punch) || TryCast(otherPunch)
            : TryCast(punch) || TryCast(otherPunch);
    }

    private float FlatDistance(EntityData target)
    {
        Vector3 d = target.transform.position - owner.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private bool TryCast(int skillId) => machine.TryUseSkill(skillId);
}
