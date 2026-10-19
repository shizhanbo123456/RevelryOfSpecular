using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public class EntitySkillController
{
    public EntityData owner;

    private readonly List<int> skillIds = new();


    public int CastingSlotIndex { get; private set; } = -1;

    private readonly Dictionary<int, int> weaponExp = new();

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
        weaponExp.Clear();
        CastingSlotIndex = -1;
    }

    #region 技能列表管理
    public void SetSkillList(List<int> ids)
    {
        skillIds.Clear();
        if (ids != null) skillIds.AddRange(ids);
        if (owner != null) owner.skillDirty = true;
    }

    public void AddSkill(int skillId)
    {
        if (skillId < 0 || skillIds.Contains(skillId)) return;
        skillIds.Add(skillId);
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

    #region 武器经验
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
    #endregion

    #region 释放
    public enum UseResult
    {
        Success,
        NoSkill,     // 该槽位没有技能（客户端与服务器技能表不一致）
        Silenced,
        Controlled,  // 强控（麻痹/冰冻/定身）
        Unregistered,
    }

    // 冷却与库存完全由客户端判定，服务器收到请求只做硬校验（技能存在、沉默/强控）
    public UseResult TryUseSkill(int skillId)
    {
        if (skillId < 0) return UseResult.NoSkill;
        if (!skillIds.Contains(skillId) && !IsUniversalSkill(skillId)) return UseResult.NoSkill;
        if (owner.effectController != null)
        {
            if (owner.effectController.IsSilenced()) return UseResult.Silenced;
            if (owner.effectController.IsActionBlocked()) return UseResult.Controlled;
        }
        if (!SkillManager.TryGet(skillId, out var skill))
        {
            Debug.LogError($"[技能] id={skillId} 未注册，无法释放");
            return UseResult.Unregistered;
        }

        CastingSlotIndex = skillIds.IndexOf(skillId);
        skill.BeginCast(owner, skillId);
        return UseResult.Success;
    }

    // 全角色通用技能：不在实体技能列表也可直接释放（空手攻击三连，人人天生可用）
    private static bool IsUniversalSkill(int skillId)
    {
        return skillId == Config.unarmed_punch_left
            || skillId == Config.unarmed_punch_right
            || skillId == Config.unarmed_attack_smash;
    }
    #endregion

    #region GM（仅 GM 指令调用，见 BattleManagerGm）
    // 强放：跳过沉默与强控校验，直接施放（技能仍需已注册）
    public bool GmForceUseSkill(int skillId)
    {
        if (skillId < 0) return false;
        if (!SkillManager.TryGet(skillId, out var skill)) return false;
        CastingSlotIndex = skillIds.IndexOf(skillId);
        skill.BeginCast(owner, skillId);
        return true;
    }
    #endregion
}
