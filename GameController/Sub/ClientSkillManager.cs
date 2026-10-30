using System.Collections.Generic;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

// 客户端技能状态的唯一持有者：服务器全量持有表、技能栏激活表（客户端决定）、每个技能的冷却与库存
public class ClientSkillManager : ClientSubManager
{
    private readonly List<int> heldSkills = new();
    private readonly List<int> activeSkills = new();   // 技能栏：按 skillId 升序，长度 ≤ 槽位数
    private readonly List<int> advancing = new();      // 参与冷却推进的技能 = 技能栏 + 空手三连
    private readonly SkillRecharge recharge = new();

    private int slotCapacity;
    private ushort lastOwnEntityId;

    // 空手三连绑 J：不属于技能栏，冷却照常推进
    private static readonly int[] UnarmedSkills =
    {
        Config.unarmed_punch_left, Config.unarmed_punch_right, Config.unarmed_attack_smash,
    };

    public IReadOnlyList<int> HeldSkills => heldSkills;

    public IReadOnlyList<int> ActiveSkills => activeSkills;

    public int SlotCapacity => slotCapacity;

    public SkillRecharge Recharge => recharge;

    #region 生命周期
    public override void Init(ClientLogicManager owner)
    {
        base.Init(owner);
        slotCapacity = ResolveSlotCapacity();
    }

    protected override void BindEvents()
    {
        EventManager.AddEvent<int>(ClientEvent.OnSkillUsed, OnSkillUsed);
    }

    protected override void UnbindEvents()
    {
        EventManager.RemoveEvent<int>(ClientEvent.OnSkillUsed, OnSkillUsed);
    }

    // 每局进战斗页时清空：持有表/技能栏/冷却都随对局重建
    public void ResetBattle()
    {
        heldSkills.Clear();
        activeSkills.Clear();
        advancing.Clear();
        recharge.Clear();
        lastOwnEntityId = 0;
        slotCapacity = ResolveSlotCapacity();
        foreach (var id in UnarmedSkills) recharge.AddSkill(id);
        RebuildAdvancing();
    }

    public override void Tick(float deltaTime)
    {
        var loading = ClientLoadingManager.Current;
        if (loading != null && loading.HasSession && loading.OwnEntityId != lastOwnEntityId)
        {
            lastOwnEntityId = loading.OwnEntityId;
            if (lastOwnEntityId != 0) recharge.ResetAll(); // 复活重建实体：库存按上限回满
        }
        recharge.Tick(advancing);
    }
    #endregion

    #region 持有与技能栏
    // 服务器下发的持有表（全量替换）：新技能栏内没有就自动选中，栏满后不再自动选中
    public void SetHeldSkills(List<int> ids)
    {
        if (slotCapacity <= 0) slotCapacity = ResolveSlotCapacity(); // 会话晚于初始化建立时补一次
        heldSkills.Clear();
        if (ids != null) heldSkills.AddRange(ids);

        for (int i = activeSkills.Count - 1; i >= 0; i--)
        {
            if (!heldSkills.Contains(activeSkills[i])) activeSkills.RemoveAt(i);
        }
        foreach (var skillId in heldSkills)
        {
            if (skillId < 0 || activeSkills.Contains(skillId)) continue;
            if (activeSkills.Count >= slotCapacity) break;
            activeSkills.Add(skillId);
        }
        activeSkills.Sort();

        // 冷却/库存记录跟着持有表走：栏外的技能也记录，只是不推进
        var recorded = recharge.GetSkillIds();
        foreach (var skillId in recorded)
        {
            if (!heldSkills.Contains(skillId) && !IsUnarmed(skillId)) recharge.RemoveSkill(skillId);
        }
        foreach (var skillId in heldSkills)
        {
            if (!recharge.HasSkill(skillId)) recharge.AddSkill(skillId);
        }
        RebuildAdvancing();
        EventManager.TrigEvent(ClientEvent.OnSkillBarRefresh);
    }

    // 技能选择面板关闭时应用：必须都持有且不超过栏位数
    public bool TrySetActiveSkills(List<int> ids)
    {
        if (ids == null) return false;
        foreach (var skillId in ids)
        {
            if (!heldSkills.Contains(skillId)) return false;
        }
        if (ids.Count > slotCapacity) return false;

        activeSkills.Clear();
        activeSkills.AddRange(ids);
        activeSkills.Sort();
        RebuildAdvancing();
        return true;
    }

    public bool IsActive(int skillId) => activeSkills.Contains(skillId);

    public int GetSlotSkillId(int slot)
    {
        return slot >= 0 && slot < activeSkills.Count ? activeSkills[slot] : -1;
    }
    #endregion

    #region 释放
    // 技能键：槽位 → 技能 id 由客户端决定，服务器只校验持有；空槽不发包
    public void CastSlot(int slot)
    {
        int skillId = GetSlotSkillId(slot);
        if (skillId < 0) return;
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendSkillCastInput(skillId);
    }

    // 空手攻击（J）：移动中随机左右拳，静止原地砸击
    public void CastUnarmed(bool moving)
    {
        var loading = ClientLoadingManager.Current;
        if (loading == null || !loading.HasSession || loading.OwnEntityId == 0) return; // 无战斗实体（观战/未开局）不发
        int skillId = moving
            ? (Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right)
            : Config.unarmed_attack_smash;
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendSkillCastInput(skillId);
    }

    // 服务器受理成功才扣一次：被沉默/强控/死亡/连点被抹掉的请求都不会有这条包
    private void OnSkillUsed(int skillId)
    {
        if (skillId < 0) return;
        recharge.Consume(skillId);
        EventManager.TrigEvent(ClientEvent.OnSkillBarRefresh);
    }
    #endregion

    #region 表现
    // 服务器只在攻击帧下发，客户端收到即播；不挂 anim.onAttack（那是赋值覆盖，会顶掉该动画本来的攻击帧事件）
    public void OnSkillCast(int skillId, SkillContext context)
    {
        if (context == null || context.ints.Count == 0) return;
        SkillManager.PlayVFX(skillId, context);
    }
    #endregion

    #region//Local
    private static bool IsUnarmed(int skillId)
    {
        foreach (var id in UnarmedSkills)
        {
            if (id == skillId) return true;
        }
        return false;
    }

    private void RebuildAdvancing()
    {
        advancing.Clear();
        advancing.AddRange(activeSkills);
        foreach (var id in UnarmedSkills) advancing.Add(id);
    }

    // 技能栏槽位数 = 本局角色属性（与服务器同一列 entity.csv 第 9 列）
    private static int ResolveSlotCapacity()
    {
        var loading = ClientLoadingManager.Current;
        if (loading == null || !loading.HasSession) return 0;
        int saveIndex = loading.Camp switch
        {
            EntityCamp.Attack => ClientSelection.selectedAttackIndex,
            EntityCamp.Defense => Config.attack_character_count + ClientSelection.selectedDefenseIndex,
            _ => -1,
        };
        if (saveIndex < 0 || Tool.InfoManager == null || Tool.SaveManager == null) return 0;
        return Mathf.Max(0, Tool.InfoManager.GetPlayerCharacterInfo(saveIndex)
            .GetAttribute(Tool.SaveManager.GetCharacterLevel(saveIndex)).weaponSlotCount);
    }
    #endregion
}
