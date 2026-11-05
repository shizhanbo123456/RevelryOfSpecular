using System.Collections.Generic;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

// 客户端技能表现数据：服务器下发的持有表（选择面板用）与槽位冷却快照（技能栏用），本地不算库存
public class ClientSkillManager : ClientSubManager
{
    private readonly List<int> heldSkills = new();
    private readonly List<int> activeSkills = new();   // 槽位技能，顺序即按键顺序，来自服务器快照

    // 根戳由服务器下发，读数只是 (服务器时间 − 根戳) / CD，与服务器时钟对齐
    private readonly SkillRecharge recharge = new(() => NetworkManager.ClientToServerTime(Time.time));

    private int slotCapacity;

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

    // 每局进战斗页时清空：持有表与槽位都随对局重建，等服务器重发
    public void ResetBattle()
    {
        heldSkills.Clear();
        activeSkills.Clear();
        recharge.Clear();
        slotCapacity = ResolveSlotCapacity();
    }
    #endregion

    #region 持有与技能栏
    // 服务器下发的持有表（全量替换）：只影响选择面板，槽位与冷却以服务器快照为准
    public void SetHeldSkills(List<int> ids)
    {
        if (slotCapacity <= 0) slotCapacity = ResolveSlotCapacity(); // 会话晚于初始化建立时补一次
        heldSkills.Clear();
        if (ids != null) heldSkills.AddRange(ids);

        for (int i = activeSkills.Count - 1; i >= 0; i--)
        {
            if (!heldSkills.Contains(activeSkills[i])) activeSkills.RemoveAt(i);
        }
        EventManager.TrigEvent(ClientEvent.OnSkillBarRefresh);
    }

    // 增量新增（掉落 / GM 下发）：已持有则忽略
    public void AddHeldSkill(int skillId)
    {
        if (skillId < 0 || heldSkills.Contains(skillId)) return;
        heldSkills.Add(skillId);
        EventManager.TrigEvent(ClientEvent.OnSkillBarRefresh);
    }

    // 服务器快照：槽位技能 + cd 根戳，一次覆盖本地显示状态（不每帧同步）
    public void ApplyRecharge(IReadOnlyList<int> skillIds, IReadOnlyList<float> roots)
    {
        activeSkills.Clear();
        if (skillIds != null) activeSkills.AddRange(skillIds);
        recharge.SetRoots(skillIds, roots);
        EventManager.TrigEvent(ClientEvent.OnSkillBarRefresh);
    }

    // 选择面板关闭时上行：服务器校验后回推快照，本地不预演（避免闪现错误库存）
    public bool TrySetActiveSkills(List<int> ids)
    {
        if (ids == null) return false;
        foreach (var skillId in ids)
        {
            if (!heldSkills.Contains(skillId)) return false;
        }
        if (ids.Count > slotCapacity) return false;
        if (Tool.NetworkManager != null) Tool.NetworkManager.SendSkillSelection(ids);
        return true;
    }

    public bool IsActive(int skillId) => activeSkills.Contains(skillId);

    public int GetSlotSkillId(int slot)
    {
        return slot >= 0 && slot < activeSkills.Count ? activeSkills[slot] : -1;
    }
    #endregion

    #region 释放
    // 技能键：槽位 → 技能 id 由服务器下发的槽位表决定，空槽不发包
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
    #endregion

    #region 表现
    // 服务器只在攻击帧下发，客户端收到即播；不挂 anim.onAttack（那是赋值覆盖，会顶掉该动画本来的攻击帧事件）
    public void OnSkillCast(SCSkillDisplayContext context)
    {
        if (context == null) return;
        SkillManager.PlayWave(context);
    }
    #endregion

    #region//Local
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
