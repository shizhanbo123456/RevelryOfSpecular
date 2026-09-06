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

    /// <summary>武器经验（技能 id → 本局累计经验；无等级，经验直接加成武器伤害，见策划案 11.4）。</summary>
    private readonly Dictionary<int, int> weaponExp = new();

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
        cdRemains.Clear();
        stores.Clear();
        weaponExp.Clear();
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

    /// <summary>武器经验（无等级，经验直接加成该武器伤害，见策划案 11.4）。</summary>
    public int GetWeaponExp(int skillId)
    {
        return weaponExp.TryGetValue(skillId, out var exp) ? exp : 0;
    }

    /// <summary>给指定武器加经验（重复获得已持有武器 = 该武器 +1；槽满随机分配请用 AddWeaponExpToRandom）。</summary>
    public void AddWeaponExp(int skillId, int amount = 1)
    {
        if (skillId < 0) return;
        weaponExp[skillId] = GetWeaponExp(skillId) + amount;
    }

    /// <summary>给随机一件已持有武器加经验（武器槽满获得新武器时转经验随机分配，见策划案 5.2）。</summary>
    public void AddWeaponExpToRandom(int amount = 1)
    {
        if (skillIds.Count == 0) return;
        int index = UnityEngine.Random.Range(0, skillIds.Count);
        AddWeaponExp(skillIds[index], amount);
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
    /// 服务器权威：SkillManager.DoDamageActs 执行伤害逻辑，技能内部通过 BroadcastSkillCast
    /// 广播（技能 id + 轨迹上下文），客户端收到后用同一构建函数重建轨迹播放表现。
    /// 沉默/强控期间无法释放。
    /// </summary>
    public bool TryUseSkill(int skillId, Vector3 dest)
    {
        if (skillId < 0) return false;
        if (GetCdRemain(skillId) > 0f) return false;
        if (GetStore(skillId) == 0) return false;
        if (owner.effectController != null && !owner.effectController.CanCastSkill()) return false;

        SkillManager.DoDamageActs(skillId, owner, dest);
        StartCd(skillId);
        ConsumeStore(skillId);
        return true;
    }

    /// <summary>填充实体表现摘要的技能槽列表与滚轮选中下标。</summary>
    public void FillDisplayInfo(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        info.selectedIndex = SelectedIndex;
        foreach (var skillId in skillIds)
        {
            info.skills.Add(new SCEntityDisplayInfo.SkillSlotRuntime()
            {
                skillId = skillId,
                exp = GetWeaponExp(skillId),
                cdRemain = GetCdRemain(skillId),
                cdTotal = GetCdTotal(skillId),
                store = GetStore(skillId),
            });
        }
    }
    #endregion
}
