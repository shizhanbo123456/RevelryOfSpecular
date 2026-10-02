using Ros.Skill;
using Ros.Transport;

public class ClientSkillManager : ClientSubManager
{
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
