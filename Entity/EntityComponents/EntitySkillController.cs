using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 实体技能控制器 API。
/// 技能统一模型见策划案 11.1：武器与角色技能是同一实体（武器显示可选 / 释放动作可配置 / 释放效果必须）。
/// 外部只向 SkillManager 传入技能 id 和上下文即可（技能包实现待完善，本类先提供列表/选择/CD 框架）。
/// </summary>
public class EntitySkillController
{
    public EntityData owner;

    /// <summary>技能列表（顺序 = 滚轮循环顺序）：初始武器/局内武器/角色主动技能/大招。</summary>
    private readonly List<int> skillIds = new();

    /// <summary>滚轮当前选中下标（-1 无）。</summary>
    public int SelectedIndex { get; private set; } = -1;

    /// <summary>CD 剩余时间（技能 id → 剩余秒）。</summary>
    private readonly Dictionary<int, float> cdRemains = new();

    /// <summary>技能库存（技能 id → 剩余次数，-1 无限制）。</summary>
    private readonly Dictionary<int, int> stores = new();

    /// <summary>武器等级（技能 id → 武器升级等级）。</summary>
    private readonly Dictionary<int, int> weaponLevels = new();

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
        cdRemains.Clear();
        stores.Clear();
        weaponLevels.Clear();
        SelectedIndex = -1;
    }

    /// <summary>每帧推进 CD（具体技能效果判定 TODO）。</summary>
    public void OnUpdate()
    {
        if (cdRemains.Count == 0) return;
        var finished = new List<int>();
        foreach (var pair in cdRemains)
        {
            cdRemains[pair.Key] = Mathf.Max(0f, pair.Value - Time.deltaTime);
            if (cdRemains[pair.Key] <= 0f) finished.Add(pair.Key);
        }
        foreach (var id in finished)
        {
            cdRemains.Remove(id);
        }
    }

    #region 技能列表管理
    /// <summary>设置完整技能列表（服务器权威下发，顺序即滚轮循环顺序）。</summary>
    public void SetSkillList(List<int> ids)
    {
        skillIds.Clear();
        if (ids != null) skillIds.AddRange(ids);
        if (SelectedIndex >= skillIds.Count) SelectedIndex = -1;
    }

    /// <summary>追加技能到列表尾部（受武器槽位数量限制，由服务器校验）。</summary>
    public void AddSkill(int skillId)
    {
        if (skillId < 0 || skillIds.Contains(skillId)) return;
        skillIds.Add(skillId);
    }

    /// <summary>移除技能。</summary>
    public void RemoveSkill(int skillId)
    {
        skillIds.Remove(skillId);
    }

    /// <summary>技能列表拷贝。</summary>
    public List<int> GetSkillIds() => new(skillIds);

    /// <summary>当前选中技能 id（-1 无）。</summary>
    public int GetSelectedSkillId()
    {
        if (SelectedIndex < 0 || SelectedIndex >= skillIds.Count) return -1;
        return skillIds[SelectedIndex];
    }

    /// <summary>滚轮循环选择（delta &gt; 0 下一项，&lt; 0 上一项）。</summary>
    public void ScrollSelect(int delta)
    {
        if (skillIds.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }
        if (SelectedIndex < 0) SelectedIndex = 0;
        SelectedIndex = (SelectedIndex + delta) % skillIds.Count;
        if (SelectedIndex < 0) SelectedIndex += skillIds.Count;
    }

    /// <summary>直接选中某槽位（服务器同步用）。</summary>
    public void SelectIndex(int index)
    {
        SelectedIndex = index;
    }
    #endregion

    #region CD 与库存
    /// <summary>技能剩余 CD（秒）。</summary>
    public float GetCdRemain(int skillId)
    {
        return cdRemains.TryGetValue(skillId, out var cd) ? cd : 0f;
    }

    /// <summary>技能总 CD（来自 SkillManager 配置）。</summary>
    public float GetCdTotal(int skillId)
    {
        return SkillManager.GetSkillCD(skillId);
    }

    /// <summary>技能剩余库存（-1 无限制）。</summary>
    public int GetStore(int skillId)
    {
        return stores.TryGetValue(skillId, out var store) ? store : -1;
    }

    /// <summary>武器等级。</summary>
    public int GetWeaponLevel(int skillId)
    {
        return weaponLevels.TryGetValue(skillId, out var level) ? level : 0;
    }

    /// <summary>设置武器等级（重复获得自动升级）。</summary>
    public void SetWeaponLevel(int skillId, int level)
    {
        weaponLevels[skillId] = level;
    }

    /// <summary>开始 CD（技能释放后由 SkillManager 回写）。</summary>
    public void StartCd(int skillId)
    {
        cdRemains[skillId] = GetCdTotal(skillId);
    }

    /// <summary>减少库存。</summary>
    public void ConsumeStore(int skillId)
    {
        if (stores.TryGetValue(skillId, out var store) && store > 0)
        {
            stores[skillId] = store - 1;
        }
    }
    #endregion

    #region 释放
    /// <summary>
    /// 尝试释放技能（右键触发远程/施法类技能）。
    /// 服务器权威：实际伤害/效果由 SkillManager.DoDamageActs 在服务器执行（TODO 技能包实现后生效）。
    /// </summary>
    public bool TryUseSkill(int skillId, Vector3 dest)
    {
        if (skillId < 0) return false;
        if (GetCdRemain(skillId) > 0f) return false;
        if (GetStore(skillId) == 0) return false;

        SkillManager.DoDamageActs(skillId, owner, dest);
        SkillManager.PlayVFX(skillId, owner.transform.position, dest);
        StartCd(skillId);
        ConsumeStore(skillId);
        return true;
    }

    /// <summary>组装服务器下发用的技能运行时信息。</summary>
    public SCSkillRuntimeInfo GetRuntimeInfo()
    {
        var info = new SCSkillRuntimeInfo() { selectedIndex = SelectedIndex };
        foreach (var skillId in skillIds)
        {
            info.slots.Add(new SCSkillRuntimeInfo.SkillSlotRuntime()
            {
                skillId = skillId,
                level = GetWeaponLevel(skillId),
                cdRemain = GetCdRemain(skillId),
                cdTotal = GetCdTotal(skillId),
                store = GetStore(skillId),
                ranged = SkillManager.IsRanged(skillId),
                hasWeaponDisplay = SkillManager.HasWeaponDisplay(skillId),
            });
        }
        return info;
    }
    #endregion
}
