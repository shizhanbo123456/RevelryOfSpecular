using Ros.Skill;
using Ros.Transport;

public class ClientSkillManager : ClientSubManager
{
    public void OnSkillCast(int skillId, SkillContext context)
    {
        if (context == null || context.ints.Count == 0) return;
        // 服务器只在攻击帧下发，客户端收到即播；不挂 anim.onAttack（那是赋值覆盖，会顶掉该动画本来的攻击帧事件）
        SkillManager.PlayVFX(skillId, context);
    }
}
