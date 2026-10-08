using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public class EntitySkillController
{
    public EntityData owner;

    private readonly List<int> skillIds = new();

    public int SelectedIndex { get; private set; } = -1;

    public int CastingSlotIndex { get; private set; } = -1;

    private readonly Dictionary<int, float> cdEndTimes = new();

    private readonly Dictionary<int, int> stores = new();

    private readonly Dictionary<int, int> weaponExp = new();

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
        cdEndTimes.Clear();
        stores.Clear();
        weaponExp.Clear();
        SelectedIndex = -1;
        CastingSlotIndex = -1;
    }

    #region 技能列表管理
    public void SetSkillList(List<int> ids)
    {
        skillIds.Clear();
        if (ids != null) skillIds.AddRange(ids);
        if (SelectedIndex >= skillIds.Count) SelectedIndex = -1;
    }

    public void AddSkill(int skillId)
    {
        if (skillId < 0 || skillIds.Contains(skillId)) return;
        skillIds.Add(skillId);
    }

    public void RemoveSkill(int skillId)
    {
        skillIds.Remove(skillId);
    }

    public List<int> GetSkillIds() => new(skillIds);

    public int SkillCount => skillIds.Count;

    public int GetSkillIdAt(int index) => index >= 0 && index < skillIds.Count ? skillIds[index] : -1;

    public void SelectIndex(int index)
    {
        SelectedIndex = index;
    }
    #endregion

    #region CD 与库存
    public float GetCdRemain(int skillId)
    {
        if (cdEndTimes.TryGetValue(skillId, out var end))
        {
            if (Time.time >= end)
            {
                cdEndTimes.Remove(skillId);
                return 0f;
            }
            return end - Time.time;
        }
        return 0f;
    }

    public float GetCdTotal(int skillId)
    {
        return SkillManager.GetSkillCD(skillId);
    }

    public int GetStore(int skillId)
    {
        return stores.TryGetValue(skillId, out var store) ? store : -1;
    }

    public int GetWeaponExp(int skillId)
    {
        return weaponExp.TryGetValue(skillId, out var exp) ? exp : 0;
    }

    public void AddWeaponExp(int skillId, int amount = 1)
    {
        if (skillId < 0) return;
        weaponExp[skillId] = GetWeaponExp(skillId) + amount;
    }

    public void AddWeaponExpToRandom(int amount = 1)
    {
        if (skillIds.Count == 0) return;
        int index = UnityEngine.Random.Range(0, skillIds.Count);
        AddWeaponExp(skillIds[index], amount);
    }

    public void StartCd(int skillId)
    {
        cdEndTimes[skillId] = Time.time + GetCdTotal(skillId);
    }

    public void ConsumeStore(int skillId)
    {
        if (stores.TryGetValue(skillId, out var store) && store > 0)
        {
            stores[skillId] = store - 1;
        }
    }

    #endregion

    #region 释放
    public bool TryUseSkill(int skillId)
    {
        if (skillId < 0) return false;
        if (GetCdRemain(skillId) > 0f) return false;
        if (GetStore(skillId) == 0) return false;
        if (owner.effectController != null && !owner.effectController.CanCastSkill()) return false;
        if (!SkillManager.TryGet(skillId, out var skill)) return false;

        CastingSlotIndex = skillIds.IndexOf(skillId);
        skill.BeginCast(owner, skillId);
        StartCd(skillId);
        ConsumeStore(skillId);
        return true;
    }

    public string DescribeUseFailure(int skillId)
    {
        if (skillId < 0) return "槽位没有技能（id 无效）";
        float cd = GetCdRemain(skillId);
        if (cd > 0f) return $"CD 中（剩 {cd:F2}s）";
        if (GetStore(skillId) == 0) return "库存为 0";
        if (owner != null && owner.effectController != null && !owner.effectController.CanCastSkill())
            return owner.effectController.IsSilenced() ? "被沉默" : "被强控（麻痹/冰冻/定身）";
        if (!SkillManager.TryGet(skillId, out _)) return "技能未注册（SkillManager 里找不到）";
        return "通过";
    }

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
