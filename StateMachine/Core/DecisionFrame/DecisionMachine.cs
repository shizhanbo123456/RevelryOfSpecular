using System.Collections.Generic;
using Ros.Skill.Utils;
using UnityEngine;

// 决策状态机骨架：只负责节流与状态切换，行为由子类写入请求字段后交给行为层
public abstract class DecisionMachine
{
    // <= 0 表示每帧思考；子类按自身决策频率设置
    protected float ThinkInterval { get; set; }

    private DecisionState current;
    private float nextThinkTime;

    protected DecisionState Current => current;

    // 子类返回自己的实体：冷却与行为层共用
    protected abstract EntityData MachineOwner { get; }

    private readonly SkillRecharge recharge = new LocalTimeRecharge();
    private readonly List<int> heldSkills = new();

    public virtual void Tick()
    {
        if (Time.time < nextThinkTime) return;
        nextThinkTime = Time.time + ThinkInterval;

        if (current == null) throw new System.Exception($"{GetType().Name} 未经 Initialize 就参与 Tick");

        OnThink(); // 跨状态共享的决策数据（如刷新目标）必须先于状态更新

        DecisionState next = current.Decide();
        if (next == null || next == current) return;

        SwitchTo(next);
        // 切换后立刻让新状态产出一份请求，否则本拍行为层拿不到新的期望行为
        current.Decide();
    }

    // 每次思考的钩子：子类在此准备所有状态都要用的数据
    protected virtual void OnThink() { }

    // 队友请求一起追击敌人（末日呼叫 / 集结召唤都走这里）：由当前状态决定是否接受
    public void RequestChase(EntityData target)
    {
        if (current == null || target == null) return;
        DecisionState next = current.OnAllyChaseRequest(target);
        if (next == null || next == current) return;
        SwitchTo(next);
    }

    protected void SwitchTo(DecisionState next)
    {
        if (next == null || next == current) return;
        if (current != null) current.Exit();
        current = next;
        current.Enter();
    }

    // staggerPhase ∈ [0,1)：按实体 id 错峰首帧，避免同类单位同帧集中决策
    protected void Initialize(DecisionState initialState, float staggerPhase)
    {
        current = initialState;
        nextThinkTime = Time.time + ThinkInterval * staggerPhase;
        current.Enter();
    }

    #region//技能冷却与库存
    // 每帧推进：NPC 没有技能栏概念，持有即恢复；状态机在 Tick 里与行为层一起调用
    protected void TickSkillRecharge()
    {
        SyncSkills();
        recharge.TickAll();
    }

    // 存储次数 > 0 即可用；store = -1 表示无限库存（不消耗，等价于只看动作节奏）
    public bool IsSkillReady(int skillId)
    {
        if (skillId < 0) return false;
        if (MachineOwner.InputBlocked(InputBlockOp.Attack)) return false;
        return recharge.GetStore(skillId) != 0;
    }

    public bool TryUseSkill(int skillId)
    {
        if (!IsSkillReady(skillId)) return false;
        if (MachineOwner.skillController.TryUseSkill(skillId) != EntitySkillController.UseResult.Success) return false;
        recharge.Consume(skillId);
        return true;
    }

    public int SkillIdAt(int slot) => MachineOwner.skillController.GetSkillIdAt(slot);

    private readonly List<int> extraSkills = new();

    // 登记不进入持有表、但需要单独记冷却的技能（空手三连走这条）
    public void RegisterExtraSkill(int skillId)
    {
        if (skillId < 0 || extraSkills.Contains(skillId)) return;
        extraSkills.Add(skillId);
        if (!recharge.HasSkill(skillId)) recharge.AddSkill(skillId);
    }

    // 持有即恢复；技能表变化时按满库存登记新技能、清掉已失去的
    private void SyncSkills()
    {
        var sc = MachineOwner.skillController;
        int count = sc.SkillCount;

        bool same = count == heldSkills.Count;
        if (same)
        {
            for (int i = 0; i < count; i++)
            {
                if (heldSkills[i] == sc.GetSkillIdAt(i)) continue;
                same = false;
                break;
            }
        }
        if (same) return;

        heldSkills.Clear();
        for (int i = 0; i < count; i++) heldSkills.Add(sc.GetSkillIdAt(i));

        List<int> recorded = recharge.GetSkillIds();
        for (int i = 0; i < recorded.Count; i++)
        {
            if (heldSkills.Contains(recorded[i])) continue;
            if (extraSkills.Contains(recorded[i])) continue;
            recharge.RemoveSkill(recorded[i]);
        }
        for (int i = 0; i < heldSkills.Count; i++)
        {
            if (!recharge.HasSkill(heldSkills[i])) recharge.AddSkill(heldSkills[i]);
        }
    }

    // NPC 的时间基准就是本地 Time.time，与各状态机里的 Time.time 判断同源
    private sealed class LocalTimeRecharge : SkillRecharge
    {
        public override float GetServerTime() => Time.time;
    }
    #endregion
}
