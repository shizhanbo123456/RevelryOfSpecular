using Ros.Skill;
using Ros.Transport;

/// <summary>
/// 技能表现子管理器（客户端逻辑）：服务器广播的技能施放，按技能 id 取技能实例并用上下文重建轨迹播放。
/// 客户端技能表现的唯一入口（后续音效、本地预测等都从这里扩展）。
/// </summary>
public class ClientSkillManager : ClientSubManager
{
    /// <summary>接收服务器技能施放广播：把特效播放挂到施放者视图的 EntityAnim 攻击帧回调，
    /// 与服务器 WaitAttackFrame 对齐——攻击帧才创建表现，不立即播。用赋值（不清除）保证一个
    /// 攻击动画含多段攻击时每段都触发，下次施法自动覆盖本回调。</summary>
    public void OnSkillCast(int skillId, SkillContext context)
    {
        if (context == null || context.ints.Count == 0) return;
        ushort casterId = SkillBase.SkillContextConventions.GetCasterId(context);
        var view = Tool.ClientLogicManager != null && Tool.ClientLogicManager.EntityPlayers != null
            ? Tool.ClientLogicManager.EntityPlayers.GetView(casterId) : null;
        if (view != null && view.anim != null)
        {
            view.anim.onAttack = _ => SkillManager.PlayVFX(skillId, context);
            return;
        }
        SkillManager.PlayVFX(skillId, context); // 视图未就绪（极端时序）退化立即播，避免完全不显示
    }
}
