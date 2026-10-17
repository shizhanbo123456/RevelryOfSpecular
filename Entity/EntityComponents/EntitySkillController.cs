using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public class EntitySkillController
{
    public EntityData owner;

    private readonly List<int> skillIds = new();


    public int CastingSlotIndex { get; private set; } = -1;

    private readonly Dictionary<int, float> rechargeEndTimes = new();

    private readonly List<int> rechargeKeys = new(); // TickRecharge 的遍历缓冲，避免每帧新列表

    private readonly Dictionary<int, int> stores = new();

    private readonly Dictionary<int, int> weaponExp = new();

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
        rechargeEndTimes.Clear();
        stores.Clear();
        weaponExp.Clear();
        CastingSlotIndex = -1;
    }

    #region 技能列表管理
    public void SetSkillList(List<int> ids)
    {
        skillIds.Clear();
        if (ids != null) skillIds.AddRange(ids);
        stores.Clear();
        foreach (var id in skillIds) stores[id] = SkillManager.GetSkillStore(id); // 库存上限唯一定义在技能池
        if (owner != null) owner.skillDirty = true;
    }

    public void AddSkill(int skillId)
    {
        if (skillId < 0 || skillIds.Contains(skillId)) return;
        skillIds.Add(skillId);
        stores[skillId] = SkillManager.GetSkillStore(skillId);
        if (owner != null) owner.skillDirty = true;
    }

    public void RemoveSkill(int skillId)
    {
        if (!skillIds.Remove(skillId)) return;
        if (owner != null) owner.skillDirty = true;
    }

    public List<int> GetSkillIds() => new(skillIds);

    public int SkillCount => skillIds.Count;

    public int GetSkillIdAt(int index) => index >= 0 && index < skillIds.Count ? skillIds[index] : -1;

    public void FillDisplayInfo(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        foreach (var skillId in skillIds)
        {
            info.skills.Add(new SCEntityDisplayInfo.SkillSlotRuntime()
            {
                skillId = skillId,
            });
        }
    }

    #endregion

    #region 复活继承
    public struct Loadout
    {
        public List<int> skillIds;
        public Dictionary<int, int> weaponExps;
    }

    public Loadout CaptureLoadout()
    {
        return new Loadout
        {
            skillIds = new List<int>(skillIds),
            weaponExps = new Dictionary<int, int>(weaponExp),
        };
    }

    // 复活重建实体后归还技能与经验；库存不继承，按上限回满（策划口径）
    public void ApplyLoadout(Loadout loadout)
    {
        SetSkillList(loadout.skillIds);
        weaponExp.Clear();
        if (loadout.weaponExps == null) return;
        foreach (var pair in loadout.weaponExps) weaponExp[pair.Key] = pair.Value;
    }
    #endregion

    #region CD 与库存
    // 充能剩余秒数：距下一点库存恢复的时间（无充能计时 = 0）
    public float GetCdRemain(int skillId)
    {
        if (!rechargeEndTimes.TryGetValue(skillId, out var end)) return 0f;
        return Mathf.Max(0f, end - Time.time);
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

    // 每帧推进充能：每 CD 秒恢复 1 点库存，回到上限即停；-1（空手/非玩家）不参与充能
    public void TickRecharge()
    {
        if (rechargeEndTimes.Count == 0) return;
        rechargeKeys.Clear();
        rechargeKeys.AddRange(rechargeEndTimes.Keys);
        foreach (var skillId in rechargeKeys)
        {
            if (Time.time < rechargeEndTimes[skillId]) continue;
            int max = SkillManager.GetSkillStore(skillId);
            int cur = GetStore(skillId);
            if (max < 0 || cur < 0)
            {
                rechargeEndTimes.Remove(skillId);
                continue;
            }
            if (cur < max) stores[skillId] = cur + 1;
            if (stores[skillId] < max) rechargeEndTimes[skillId] = Time.time + GetCdTotal(skillId);
            else rechargeEndTimes.Remove(skillId);
        }
    }

    // 充能一旦起跑就按自己的节奏走，连放不重置计时
    private void StartRecharge(int skillId, float advance)
    {
        if (rechargeEndTimes.ContainsKey(skillId)) return;
        int max = SkillManager.GetSkillStore(skillId);
        if (max < 0 || GetStore(skillId) >= max) return;
        rechargeEndTimes[skillId] = Time.time + Mathf.Max(0f, GetCdTotal(skillId) - advance);
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
    // 释放只看库存：CD 只决定库存回充的节奏，不额外拦释放（使用间隔由攻击动画限制）
    public bool TryUseSkill(int skillId, float cdAdvance = 0f)
    {
        if (skillId < 0) return false;
        if (GetStore(skillId) == 0) return false;
        if (owner.effectController != null && !owner.effectController.CanCastSkill()) return false;
        if (!SkillManager.TryGet(skillId, out var skill)) return false;

        CastingSlotIndex = skillIds.IndexOf(skillId);
        skill.BeginCast(owner, skillId);
        if (!GmIgnoreCd) ConsumeStore(skillId);
        StartRecharge(skillId, cdAdvance); // 延迟补偿：客户端按 CD 画充能进度，服务器提前这么多开始计时
        return true;
    }

    public string DescribeUseFailure(int skillId)
    {
        if (skillId < 0) return "槽位没有技能（id 无效）";
        if (GetStore(skillId) == 0) return $"库存为 0（{GetCdRemain(skillId):F2}s 后回 1 点）";
        if (owner != null && owner.effectController != null && !owner.effectController.CanCastSkill())
            return owner.effectController.IsSilenced() ? "被沉默" : "被强控（麻痹/冰冻/定身）";
        if (!SkillManager.TryGet(skillId, out _)) return "技能未注册（SkillManager 里找不到）";
        return "通过";
    }

    #endregion

    #region GM（仅 GM 指令调用，见 BattleManagerGm）
    // 无视库存：置位后普通施放路径（TryUseSkill）不再消耗库存；复活重建实体后重置
    public bool GmIgnoreCd { get; set; }

    // 强放：跳过库存 / 沉默与强控校验，直接施放（技能仍需已注册）
    public bool GmForceUseSkill(int skillId)
    {
        if (skillId < 0) return false;
        if (!SkillManager.TryGet(skillId, out var skill)) return false;
        CastingSlotIndex = skillIds.IndexOf(skillId);
        skill.BeginCast(owner, skillId);
        ConsumeStore(skillId);
        StartRecharge(skillId, 0f);
        return true;
    }

    // 立即回满库存并清充能计时
    public void GmResetAllCd()
    {
        rechargeEndTimes.Clear();
        foreach (var skillId in skillIds) stores[skillId] = SkillManager.GetSkillStore(skillId);
    }
    #endregion
}
