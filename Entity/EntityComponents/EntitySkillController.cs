using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public class EntitySkillController
{
    public EntityData owner;

    private readonly List<int> skillIds = new();

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
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
        foreach (var skillId in skillIds) info.skills.Add(skillId);
    }

    #endregion

    #region 复活继承
    public struct Loadout
    {
        public List<int> skillIds;
    }

    public Loadout CaptureLoadout()
    {
        return new Loadout
        {
            skillIds = new List<int>(skillIds),
        };
    }

    // 复活重建实体后归还持有技能（技能栏与库存都在客户端，不随实体走）
    public void ApplyLoadout(Loadout loadout)
    {
        SetSkillList(loadout.skillIds);
    }
    #endregion

    #region 释放
    public enum UseResult
    {
        Success,
        NoSkill,     // 该技能不在持有表里（客户端技能栏与服务器技能表不一致）
        Silenced,
        Controlled,  // 强控（麻痹/冰冻/定身）
        Unregistered,
    }

    // 服务器只记录持有、只做硬校验：通过即回一条成功回执，冷却与库存全在客户端推演
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

        skill.BeginCast(owner, skillId);
        if (Tool.BattleManager != null) Tool.BattleManager.NotifySkillUsed(owner, skillId);
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
        skill.BeginCast(owner, skillId);
        return true;
    }
    #endregion
}
