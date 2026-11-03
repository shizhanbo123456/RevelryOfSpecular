using System.Collections.Generic;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

// 技能的唯一真源（服务器持有一份，客户端只在表现层读快照）：持有表、技能栏、冷却与库存
public class EntitySkillController
{
    public EntityData owner;

    private readonly List<int> skillIds = new();

    private readonly List<int> slotSkills = new(); // 技能栏：只有真人玩家启用，顺序 = 按键槽位

    private readonly List<int> pruneBuffer = new();

    private readonly List<int> advancingBuffer = new(); // 推进集合 = 槽位技能 + 空手三连

    // 服务器时间基准就是本机 Time.time；客户端的对应实例改用 ClientToServerTime
    private readonly SkillRecharge recharge = new(() => Time.time);

    private bool slotBarEnabled;
    private int slotCapacity;

    public void Init(EntityData data)
    {
        owner = data;
        skillIds.Clear();
        slotSkills.Clear();
        slotBarEnabled = false;
        slotCapacity = 0;
        recharge.Clear();
        // 空手三连人人天生可用且不消耗（store = -1），登记只为查询口径与其它技能一致
        recharge.AddSkill(Config.unarmed_punch_left);
        recharge.AddSkill(Config.unarmed_punch_right);
        recharge.AddSkill(Config.unarmed_attack_smash);
    }

    #region 技能列表管理
    public void SetSkillList(List<int> ids)
    {
        skillIds.Clear();
        if (ids != null) skillIds.AddRange(ids);

        // 冷却记录跟着持有表走：失去的清掉（空手三连保留），新得的按满库存入账
        recharge.FillSkillIds(pruneBuffer);
        foreach (var skillId in pruneBuffer)
        {
            if (skillIds.Contains(skillId) || IsUniversalSkill(skillId)) continue;
            recharge.RemoveSkill(skillId);
        }
        foreach (var skillId in skillIds) recharge.AddSkill(skillId);

        RebuildSlots();
        if (owner != null) owner.skillDirty = true;
    }

    public void AddSkill(int skillId)
    {
        if (skillId < 0 || skillIds.Contains(skillId)) return;
        skillIds.Add(skillId);
        recharge.AddSkill(skillId);
        AutoAddSlot(skillId);
        if (owner != null) owner.skillDirty = true;
    }

    public void RemoveSkill(int skillId)
    {
        if (!skillIds.Remove(skillId)) return;
        recharge.RemoveSkill(skillId);
        if (slotSkills.Remove(skillId)) RefreshAdvancing();
        if (owner != null) owner.skillDirty = true;
    }

    public List<int> GetSkillIds() => new(skillIds);

    public int SkillCount => skillIds.Count;

    public int GetSkillIdAt(int index) => index >= 0 && index < skillIds.Count ? skillIds[index] : -1;
    #endregion

    #region 技能栏（服务器持有）
    // 真人玩家的实体生成后启用：栏位按持有顺序补满，栏外技能恢复冻结
    public void EnableSlotBar(int capacity)
    {
        slotCapacity = Mathf.Max(0, capacity);
        slotBarEnabled = true;
        RebuildSlots();
    }

    // 选择面板提交：必须都持有、不重复、不超过栏位容量
    public bool TrySetSlotSkills(IReadOnlyList<int> ids)
    {
        if (!slotBarEnabled || ids == null) return false;
        if (ids.Count > slotCapacity) return false;
        for (int i = 0; i < ids.Count; i++)
        {
            if (!skillIds.Contains(ids[i])) return false;
            for (int j = 0; j < i; j++)
            {
                if (ids[j] == ids[i]) return false;
            }
        }
        slotSkills.Clear();
        for (int i = 0; i < ids.Count; i++) slotSkills.Add(ids[i]);
        slotSkills.Sort();
        RefreshAdvancing();
        return true;
    }

    // 下发给客户端的快照：槽位技能 + 各自 cd 根戳
    public SCSkillRecharge BuildRecharge()
    {
        var info = new SCSkillRecharge();
        if (!slotBarEnabled) return info;
        info.skillIds.AddRange(slotSkills);
        recharge.ExportRoots(slotSkills, info.roots);
        return info;
    }
    #endregion

    #region 复活继承
    public struct Loadout
    {
        public List<int> skillIds;
        public List<int> slotSkills;
    }

    public Loadout CaptureLoadout()
    {
        return new Loadout
        {
            skillIds = new List<int>(skillIds),
            slotSkills = new List<int>(slotSkills),
        };
    }

    // 复活重建实体后归还持有技能与槽位选择；冷却随新实体从满库存起算
    public void ApplyLoadout(Loadout loadout)
    {
        SetSkillList(loadout.skillIds);
        RestoreSlots(loadout.slotSkills);
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
        Cooling,     // 冷却未回满 / 库存为 0
    }

    // 服务器唯一收口：持有 → 沉默 → 强控 → 已注册 → 冷却，通过才扣一次库存
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
        if (recharge.GetStore(skillId) == 0) return UseResult.Cooling;

        skill.BeginCast(owner, skillId);
        recharge.Consume(skillId);
        if (Tool.BattleManager != null) Tool.BattleManager.NotifySkillUsed(owner, skillId);
        return UseResult.Success;
    }

    // 库存次数 != 0 即可用（-1 = 无限库存）；输入阻断由调用方判断
    public bool IsSkillReady(int skillId)
    {
        if (skillId < 0) return false;
        return recharge.GetStore(skillId) != 0;
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
    // 强放：跳过沉默、强控与冷却校验，直接施放且不扣库存（技能仍需已注册）
    public bool GmForceUseSkill(int skillId)
    {
        if (skillId < 0) return false;
        if (!SkillManager.TryGet(skillId, out var skill)) return false;
        skill.BeginCast(owner, skillId);
        return true;
    }
    #endregion

    #region//Local
    // 持有表变化后重排：栏内保留原有顺序，缺的位置按持有顺序补
    private void RebuildSlots()
    {
        if (!slotBarEnabled) return;
        for (int i = slotSkills.Count - 1; i >= 0; i--)
        {
            if (!skillIds.Contains(slotSkills[i])) slotSkills.RemoveAt(i);
        }
        for (int i = 0; i < skillIds.Count && slotSkills.Count < slotCapacity; i++)
        {
            if (!slotSkills.Contains(skillIds[i])) slotSkills.Add(skillIds[i]);
        }
        slotSkills.Sort();
        RefreshAdvancing();
    }

    // 新获得的技能：栏位有空就补进去；无论是否入栏都重算一次冻结集合
    private void AutoAddSlot(int skillId)
    {
        if (!slotBarEnabled) return;
        if (slotSkills.Count < slotCapacity && !slotSkills.Contains(skillId))
        {
            slotSkills.Add(skillId);
            slotSkills.Sort();
        }
        RefreshAdvancing();
    }

    // 复活沿用死亡前的槽位选择（与 CaptureLoadout 同源，只需过滤掉已失去的技能）
    private void RestoreSlots(List<int> ids)
    {
        if (!slotBarEnabled || ids == null) return;
        slotSkills.Clear();
        foreach (var skillId in ids)
        {
            if (skillIds.Contains(skillId) && !slotSkills.Contains(skillId)) slotSkills.Add(skillId);
        }
        slotSkills.Sort();
        RefreshAdvancing();
    }

    // 只有栏内技能吃"卸下冻结"，空手三连始终推进
    private void RefreshAdvancing()
    {
        advancingBuffer.Clear();
        advancingBuffer.AddRange(slotSkills);
        advancingBuffer.Add(Config.unarmed_punch_left);
        advancingBuffer.Add(Config.unarmed_punch_right);
        advancingBuffer.Add(Config.unarmed_attack_smash);
        recharge.SetAdvancing(advancingBuffer);
    }
    #endregion
}
